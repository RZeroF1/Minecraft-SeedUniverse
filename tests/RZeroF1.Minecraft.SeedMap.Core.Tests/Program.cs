// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
using System.Diagnostics;
using System.Security.Cryptography;
using RZeroF1.Minecraft.SeedMap;

static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
var timer = Stopwatch.StartNew();
Assert(SeedMap.ParseSeed("-1") == -1, "negative seed");
Assert(SeedMap.ParseSeed(long.MinValue.ToString()) == long.MinValue, "minimum seed");
Assert(SeedMap.ParseSeed(long.MaxValue.ToString()) == long.MaxValue, "maximum seed");
Assert(SeedMap.ParseSeed("hello") == 99162322, "text hash");
Assert(SeedMap.ParseSeed("polygenelubricants") == int.MinValue, "signed text hash");
Assert(SeedMap.ParseSeed("😀") == 1772899, "UTF16 surrogate hash");
Assert(SeedMap.FloorDiv(-1, 128) == -1 && SeedMap.FloorDiv(-129, 128) == -2, "negative floor");
int version = SeedMap.Versions.First(v => v.Key == "1.20.6").Id;
using (var edge = SeedMap.Create(new(123, version, 0, 64)))
{
    Assert(SeedMap.Generate(edge, 29_999_872, 29_999_872, 1, 1, 256, 64)[0] >= 0, "last valid coarse sample");
    try { SeedMap.Generate(edge, 30_000_000, 0, 1, 1, 1, 64); throw new Exception("world boundary"); }
    catch (InvalidOperationException) { }
}
int cases = 0;
foreach (long seed in new long[] { 123, -1, long.MinValue, 99162322 })
    foreach (int dimension in new[] { 0, -1, 1 })
    {
        using var g = SeedMap.Create(new(seed, version, dimension, 64));
        foreach (var (x, z) in new[] { (0, 0), (-16000, 20000), (-4, -4), (29_998_720, -29_998_720) })
        {
            var all = SeedMap.Generate(g, x, z, 32, 32, 4, 64);
            for (int tz = 0; tz < 2; tz++)
                for (int tx = 0; tx < 2; tx++)
                {
                    var tile = SeedMap.Generate(g, x + tx * 64, z + tz * 64, 16, 16, 4, 64);
                    for (int row = 0; row < 16; row++)
                        Assert(all.AsSpan((tz * 16 + row) * 32 + tx * 16, 16).SequenceEqual(tile.AsSpan(row * 16, 16)), "tile seam");
                }
            cases++;
        }
    }
Console.WriteLine($"PASS seed/coordinate/biome tiling: {cases} cases");
foreach (var v in SeedMap.Versions)
    foreach (int dim in new[] { 0, -1, 1 })
    {
        using var g = SeedMap.Create(new(-3525218295543650087, v.Id, dim, 64));
        foreach (int step in new[] { 1, 4, 16, 64, 256 })
            Assert(SeedMap.Generate(g, -step * 8, step * 4, 8, 8, step, 64).All(id => id >= 0), "version/dimension/step");
    }
Console.WriteLine("PASS all capability versions/dimensions/scales");
foreach (var v in SeedMap.Versions)
    foreach (int dim in new[] { -1, 0, 1 })
    {
        var biomes = SeedMap.Biomes(v.Id, dim);
        Assert(biomes.All(b => !string.Equals(b.Name, b.Key.Replace('_', ' '), StringComparison.Ordinal)),
            $"localized biome names {v.Key}/{dim}");
    }
Console.WriteLine("PASS localized biome names across all versions/dimensions");
// Independent Java Random and signed int overflow implementation for slime chunks.
static bool SlimeJava(long seed, int x, int z)
{
    long initial = unchecked(seed + (long)unchecked(x*x*4987142) + unchecked(x*5947611)
        + (long)unchecked(z*z)*4392871 + unchecked(z*389711));
    ulong state = unchecked((ulong)(initial ^ 987234911L) ^ 0x5deece66dUL) & ((1UL << 48) - 1);
    int bits, value;
    do
    {
        state = (state * 0x5deece66dUL + 11) & ((1UL << 48) - 1);
        bits = (int)(state >> 17); value = bits % 10;
    } while (unchecked(bits - value + 9) < 0);
    return value == 0;
}
int slimeCases = 0;
foreach (long seed in new long[] { 0, 123, -1, long.MinValue, long.MaxValue })
    foreach (int x in new[] { -1874999, -46341, -100, -1, 0, 1, 100, 46341, 1874999 })
        foreach (int z in new[] { -1874999, -46341, -1, 0, 1, 46341, 1874999 })
        { Assert(SeedMap.IsSlimeChunk(seed, x, z) == SlimeJava(seed, x, z), $"slime {seed}/{x}/{z}"); slimeCases++; }
Console.WriteLine($"PASS independent Java RNG/slime: {slimeCases} cases");
using (var g = SeedMap.Create(new(123, version, 0, 64)))
{
    var villages = new List<SeedMap.StructureHit>();
    for (int z=-3;z<=3;z++)
        for(int x=-3;x<=3;x++)
            if(SeedMap.QueryRegion(g,5,x,z) is { } hit) villages.Add(hit);
    Assert(villages.Count > 0, "village samples");
    var golden = new (int X,int Z)[] {
        (-192,-1504),(640,-800),(-1616,-352),(-288,272),(-912,560),(-224,544),
        (400,784),(736,576),(1728,896),(-1472,1344),(-976,1104),(1696,1248),
        (-1328,1728),(-144,1680),(336,1792),(896,1648),(1248,1920),(1696,1744)
    };
    Assert(villages.Select(h=>(h.X,h.Z)).SequenceEqual(golden),"pinned Cubiomes village golden");
    Assert(villages.Select(h=>(h.Type,h.X,h.Z)).Distinct().Count()==villages.Count,"structure uniqueness");
    int villageRegion = SeedMap.Structures(version,0).First(s=>s.Id==5).RegionBlocks;
    foreach(var h in villages)
        Assert(SeedMap.QueryRegion(g,5,SeedMap.FloorDiv(h.X,villageRegion),SeedMap.FloorDiv(h.Z,villageRegion))==h,"structure region");
    Assert(SeedMap.Strongholds(g).Length==128,"stronghold count");
    Console.WriteLine("Village fixed seed 123 / Java 1.20.6: "+string.Join("; ",villages.Select(h=>$"{h.X},{h.Z}")));
}
using (var engine = new SeedMapEngine(new(123, version, 0, 64)))
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    try { await engine.TileAsync(new(4, 9, 9), cancellation.Token); throw new Exception("cancellation"); }
    catch(OperationCanceledException) { }
    var keys = new[] { new SeedMapEngine.TileKey(4, -1, -1), new(4, 0, 0), new(4, 1, -1), new(4, 2, 2) };
    var parallel = await Task.WhenAll(keys.Select(k=>engine.TileAsync(k,CancellationToken.None)));
    using var g=SeedMap.Create(engine.World);
    int originBiome = SeedMap.Generate(g, 0, 0, 1, 1, 1, 64)[0];
    Assert(await engine.FindBiomeAsync(originBiome, 0, 0, 64, CancellationToken.None) == (0, 0), "nearby biome location is block-verified");
    for(int i=0;i<keys.Length;i++)
        Assert(parallel[i].SequenceEqual(SeedMap.Generate(g,keys[i].X*512,keys[i].Z*512,128,128,4,64)),"parallel/strip consistency");
    var cached = await engine.TileAsync(keys[0],CancellationToken.None);
    Assert(ReferenceEquals(cached,parallel[0]),"cache reuse");
    await engine.TileAsync(new(256,-916,-916),CancellationToken.None);
    var types = SeedMap.Structures(version, 0).Where(t => t.Id is 5 or 100).ToArray();
    var area = new SeedMap.Bounds(-2000, -2000, 2000, 2000);
    var first = await engine.QueryStructuresAsync(area, types, CancellationToken.None);
    var repeated = await engine.QueryStructuresAsync(area, types, CancellationToken.None);
    Assert(!first.Limited && first.Hits.SequenceEqual(repeated.Hits), "cached regions and strongholds match");
    Assert(first.Hits.All(h => area.Contains(h.X, h.Z)) &&
        first.Hits.DistinctBy(h => (h.Type, h.X, h.Z)).Count() == first.Hits.Count, "visible structure bounds and deduplication");
    var mine = SeedMap.Structures(version, 0).Single(t => t.Key == "mineshaft");
    var limited = await engine.QueryStructuresAsync(new(-10000,-10000,10000,10000), new[] { mine }, CancellationToken.None);
    Assert(limited.Limited && limited.Hits.Count == 0, "dense structure query is bounded");
    Console.WriteLine($"PASS async/cancel/cache/edge: {engine.CacheBytes} bytes");
}
using (var engine = new SeedMapEngine(new(123, version, 1, 64)))
{
    var firstKey = new SeedMapEngine.TileKey(256, 500, 500);
    var initial = await engine.TileAsync(firstKey, CancellationToken.None);
    for (int i = 1; i <= 800; i++)
        await engine.TileAsync(new(256, i % 30 - 15, i / 30 - 15), CancellationToken.None);
    Assert(engine.CacheBytes <= SeedMapEngine.CacheBudget && engine.CacheCount <= 768, "tile LRU byte budget");
    var regenerated = await engine.TileAsync(firstKey, CancellationToken.None);
    Assert(!ReferenceEquals(initial, regenerated) && initial.SequenceEqual(regenerated), "LRU eviction preserves deterministic data");
    Console.WriteLine($"PASS LRU stress: {engine.CacheCount} tiles / {engine.CacheBytes} bytes");
}
var profile = new SeedMapProfile { Seed=123 };
uint[] colors = Enumerable.Repeat(0xff507090u,256).ToArray();
var export = await SeedMapExport.RenderAsync(new(123,version,0,64),new(-128,-128,128,128),4,colors,profile,CancellationToken.None);
Assert(export.Width==64 && export.Height==64 && export.Pixels.Length==16384,"export dimensions");
profile.Structures = new() { 5 };
var structureExport = await SeedMapExport.RenderAsync(new(123,version,0,64),new(-1024,-1024,1024,1024),4,colors,profile,CancellationToken.None);
Assert(!structureExport.StructuresLimited &&
    structureExport.Pixels.Where((_, index) => index % 4 == 0).Any(b => b != 0x90), "export contains selected structures");
profile.Waypoints.Add(new() { X = -288, Z = 272, Dimension = 0 });
var waypointExport = await SeedMapExport.RenderAsync(new(123,version,0,64),new(-512,0,0,512),4,colors,profile,CancellationToken.None);
int waypointPixel = (68 * waypointExport.Width + 56) * 4;
Assert(waypointExport.Pixels.AsSpan(waypointPixel, 4).SequenceEqual(new byte[] { 24, 190, 255, 255 }), "waypoint renders above coincident structure");
try
{
    await SeedMapExport.RenderAsync(new(123,version,0,64),new(-10000,-10000,10000,10000),1,colors,profile,CancellationToken.None);
    throw new Exception("export bound was not enforced");
}
catch (ArgumentException) { }
Console.WriteLine($"PASS bounded export: {Convert.ToHexString(SHA256.HashData(export.Pixels))}");
var camera = new SeedMapCamera(0, 0, .25);
var anchorBefore = camera.ScreenToWorld(640, 360, 1280, 720);
camera.ZoomAt(2, 640, 360, 1280, 720);
Assert(SeedMapGeometry.Distance(0, 0, 3, 4) == 5, "geometry distance");
Assert(camera.ScreenToWorld(640, 360, 1280, 720) == anchorBefore, "zoom anchor");
Assert(camera.SelectSamplingStep() == 4, "camera sampling step");
var palette = SeedMap.Biomes(version, 0).ToDictionary(b => b.Id);
var bgra = SeedMapPalette.ToBgra(new[] { 0, 999 }, palette);
Assert(bgra.Length == 8 && bgra[3] == 255, "BGRA conversion");
Assert(SeedMapTilePlanner.Visible(new SeedMap.Bounds(0, 0, 128 * 4, 128 * 4), 4).Count == 1, "tile planner");
Console.WriteLine($"PASS all checks in {timer.Elapsed.TotalSeconds:F2}s; dynamic code={System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");

