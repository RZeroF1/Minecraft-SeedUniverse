// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
namespace RZeroF1.Minecraft.SeedMap;

/// <summary>Per-viewport bounded data cache; no XAML objects or persistent native handles.</summary>
public sealed class SeedMapEngine : IDisposable
{
    public const int TileSize = 128;
    public const long CacheBudget = 48 * 1024 * 1024;
    public readonly record struct TileKey(int Step, int X, int Z);
    private sealed record CacheEntry(int[] Ids, LinkedListNode<TileKey> Node);
    private readonly SeedMap.World _world;
    private readonly SemaphoreSlim _workers = new(2);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<TileKey, CacheEntry> _cache = new();
    private readonly LinkedList<TileKey> _lru = new();
    private readonly object _gate = new();
    private readonly Dictionary<(int Type, int X, int Z), SeedMap.StructureHit?> _regions = new();
    private readonly Queue<(int Type, int X, int Z)> _regionOrder = new();
    private SeedMap.StructureHit[]? _strongholds;
    private long _bytes;
    private bool _disposed;
    public long CacheBytes { get { lock (_gate) return _bytes; } }
    public int CacheCount { get { lock (_gate) return _cache.Count; } }
    public SeedMap.World World => _world;
    public SeedMapEngine(SeedMap.World world) => _world = world;

    public async Task<int[]> TileAsync(TileKey key, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cache.TryGetValue(key, out var entry))
            {
                _lru.Remove(entry.Node); _lru.AddFirst(entry.Node);
                return entry.Ids;
            }
        }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token);
        var ct = linked.Token;
        await _workers.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var ids = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var generator = SeedMap.Create(_world);
                var data = new int[TileSize * TileSize];
                Array.Fill(data, -1);
                long x = (long)key.X * TileSize * key.Step, z = (long)key.Z * TileSize * key.Step;
                for (int row = 0; row < TileSize; row += 16)
                {
                    ct.ThrowIfCancellationRequested();
                    long wz = z + (long)row * key.Step;
                    int left = Math.Max(0, (int)Math.Ceiling((-SeedMap.WorldLimit - (double)x) / key.Step));
                    int top = Math.Max(0, (int)Math.Ceiling((-SeedMap.WorldLimit - (double)wz) / key.Step));
                    int right = Math.Min(TileSize, (int)Math.Ceiling((SeedMap.WorldLimit - (double)x) / key.Step));
                    int bottom = Math.Min(16, (int)Math.Ceiling((SeedMap.WorldLimit - (double)wz) / key.Step));
                    if (right <= left || bottom <= top) continue;
                    int[] strip = SeedMap.Generate(generator, x + (long)left * key.Step, wz + (long)top * key.Step,
                        right - left, bottom - top, key.Step, _world.Y);
                    for (int r = top; r < bottom; r++)
                        Array.Copy(strip, (r - top) * (right - left), data, (row + r) * TileSize + left, right - left);
                }
                return data;
            }, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            lock (_gate)
            {
                if (_disposed) throw new OperationCanceledException(ct);
                if (!_cache.ContainsKey(key))
                {
                    _cache.Add(key, new(ids, _lru.AddFirst(key)));
                    _bytes += ids.LongLength * sizeof(int);
                }
                while (_bytes > CacheBudget && _lru.Last is { } last)
                {
                    _bytes -= _cache[last.Value].Ids.LongLength * sizeof(int);
                    _cache.Remove(last.Value); _lru.RemoveLast();
                }
            }
            return ids;
        }
        finally { _workers.Release(); }
    }

    public async Task<(List<SeedMap.StructureHit> Hits, bool Limited)> QueryStructuresAsync(
        SeedMap.Bounds bounds, IReadOnlyList<SeedMap.StructureInfo> types, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token);
        var ct = linked.Token;
        await _workers.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                using var generator = SeedMap.Create(_world);
                var hits = new List<SeedMap.StructureHit>();
                var seen = new HashSet<(int, int, int)>();
                bool limited = false;
                int work = 0;
                foreach (var type in types)
                {
                    ct.ThrowIfCancellationRequested();
                    if (type.Id == 100)
                    {
                        SeedMap.StructureHit[]? strongholds;
                        lock (_gate) strongholds = _strongholds;
                        strongholds ??= SeedMap.Strongholds(generator);
                        lock (_gate) { if (!_disposed) _strongholds = strongholds; }
                        ct.ThrowIfCancellationRequested();
                        foreach (var hit in strongholds)
                            if (bounds.Contains(hit.X, hit.Z)) hits.Add(hit);
                        continue;
                    }
                    int size = type.RegionBlocks;
                    int x0 = SeedMap.FloorDiv(bounds.MinX, size), x1 = SeedMap.FloorDiv(bounds.MaxX - 1, size);
                    int z0 = SeedMap.FloorDiv(bounds.MinZ, size), z1 = SeedMap.FloorDiv(bounds.MaxZ - 1, size);
                    long count = (long)(x1 - x0 + 1) * (z1 - z0 + 1);
                    if (count > 6000 || count + work > 12000) { limited = true; continue; }
                    work += (int)count;
                    for (int z = z0; z <= z1; z++)
                        for (int x = x0; x <= x1; x++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var key = (type.Id, x, z);
                            SeedMap.StructureHit? hit;
                            bool cached;
                            lock (_gate) cached = _regions.TryGetValue(key, out hit);
                            if (!cached)
                            {
                                hit = SeedMap.QueryRegion(generator, type.Id, x, z);
                                lock (_gate)
                                {
                                    if (!_disposed && !_regions.ContainsKey(key))
                                    {
                                        _regions.Add(key, hit); _regionOrder.Enqueue(key);
                                        while (_regions.Count > 8192) _regions.Remove(_regionOrder.Dequeue());
                                    }
                                }
                            }
                            if (hit is not null && bounds.Contains(hit.X, hit.Z) && seen.Add((hit.Type, hit.X, hit.Z)))
                                hits.Add(hit);
                        }
                }
                return (hits, limited);
            }, ct).ConfigureAwait(false);
        }
        finally { _workers.Release(); }
    }

    public async Task<(int X, int Z)?> FindBiomeAsync(int biome, int centerX, int centerZ, int radius, CancellationToken ct)
    {
        const int step = 32;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        ct = linked.Token;
        await _workers.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                using var generator = SeedMap.Create(_world);
                int cx = SeedMap.FloorDiv(centerX, step), cz = SeedMap.FloorDiv(centerZ, step);
                for (int ring = 0; ring <= radius / step; ring++)
                    for (int dz = -ring; dz <= ring; dz++)
                        for (int dx = -ring; dx <= ring; dx++)
                        {
                            if (ring > 0 && Math.Abs(dx) != ring && Math.Abs(dz) != ring) continue;
                            ct.ThrowIfCancellationRequested();
                            int x = (cx + dx) * step, z = (cz + dz) * step;
                            if (Math.Abs((long)x) >= SeedMap.WorldLimit || Math.Abs((long)z) >= SeedMap.WorldLimit) continue;
                            if (SeedMap.Generate(generator, x, z, 1, 1, 1, _world.Y)[0] == biome) return ((int X, int Z)?)(x, z);
                        }
                return null;
            }, ct).ConfigureAwait(false);
        }
        finally { _workers.Release(); }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            _cache.Clear(); _lru.Clear(); _bytes = 0;
            _regions.Clear(); _regionOrder.Clear(); _strongholds = null;
        }
        // In-flight work owns callbacks until it finishes; no unmanaged handles here.
    }
}
