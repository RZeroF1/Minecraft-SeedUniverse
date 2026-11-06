// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
namespace RZeroF1.Minecraft.SeedMap;

/// <summary>使用共享坐标与颜色模型生成有尺寸上限的离屏像素缓冲区。</summary>
public static class Export
{
    public sealed record Image(int Width, int Height, byte[] Pixels, bool StructuresLimited = false);

    public static async Task<Image> RenderAsync(World world, Bounds bounds, int step,
        uint[] colors, Profile profile, CancellationToken ct)
    {
        if (step is not (1 or 4 or 16 or 64 or 256)) throw new ArgumentOutOfRangeException(nameof(step));
        int x0 = Api.FloorDiv(bounds.MinX, step) * step, z0 = Api.FloorDiv(bounds.MinZ, step) * step;
        int width = (int)Math.Ceiling((bounds.MaxX - (double)x0) / step);
        int height = (int)Math.Ceiling((bounds.MaxZ - (double)z0) / step);
        if (width <= 0 || height <= 0 || (long)width * height > 16_777_216)
            throw new ArgumentException("导出区域超过 1600 万像素。");
        bool grid = profile.Grid, slime = profile.Slime && world.Dimension == 0;
        var markers = profile.Waypoints.Where(w => w.Dimension == world.Dimension).Select(w => (w.X, w.Z)).ToArray();
        var selected = profile.Structures.ToHashSet();
        var types = Api.Structures(world.Version, world.Dimension).Where(t => selected.Contains(t.Id)).ToArray();
        using var structureEngine = new Engine(world);
        var structures = await structureEngine.QueryStructuresAsync(bounds, types, ct).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            using var generator = Api.Create(world);
            var pixels = new byte[checked(width * height * 4)];
            for (int tz = 0; tz < height; tz += 64)
                for (int tx = 0; tx < width; tx += 128)
                {
                    ct.ThrowIfCancellationRequested();
                    int w = Math.Min(128, width - tx), h = Math.Min(64, height - tz);
                    int wx = x0 + tx * step, wz = z0 + tz * step;
                    // The Java world edge need not be aligned to a coarse sample grid.
                    int left = Math.Max(0, (int)Math.Ceiling((-Api.WorldLimit - (double)wx) / step));
                    int top = Math.Max(0, (int)Math.Ceiling((-Api.WorldLimit - (double)wz) / step));
                    int right = Math.Min(w, (int)Math.Ceiling((Api.WorldLimit - (double)wx) / step));
                    int bottom = Math.Min(h, (int)Math.Ceiling((Api.WorldLimit - (double)wz) / step));
                    int[]? ids = right > left && bottom > top
                        ? Api.Generate(generator, wx + left * step, wz + top * step, right - left, bottom - top, step, world.Y) : null;
                    for (int z = 0; z < h; z++)
                        for (int x = 0; x < w; x++)
                        {
                            int id = ids is not null && x >= left && x < right && z >= top && z < bottom
                                ? ids[(z - top) * (right - left) + x - left] : -1;
                            uint c = id >= 0 && id < colors.Length ? colors[id] : 0xff555555;
                            int bx = wx + x * step, bz = wz + z * step;
                            if (slime && step <= 4 && Math.Abs((long)bx) < Api.WorldLimit && Math.Abs((long)bz) < Api.WorldLimit &&
                                Api.IsSlimeChunk(world.Seed, Api.FloorDiv(bx, 16), Api.FloorDiv(bz, 16)))
                                c = Blend(c, 0xff50d050);
                            if (grid && step <= 4 && (bx % 16 == 0 || bz % 16 == 0)) c = Blend(c, 0xff303030);
                            int offset = ((tz + z) * width + tx + x) * 4;
                            pixels[offset] = (byte)c; pixels[offset + 1] = (byte)(c >> 8);
                            pixels[offset + 2] = (byte)(c >> 16); pixels[offset + 3] = 255;
                        }
                }
            foreach (var marker in structures.Hits.Select(h => (h.X, h.Z, Color: 0xff00a8b5u))
                .Concat(markers.Select(m => (m.X, m.Z, Color: 0xffffbe18u))))
            {
                int x = (marker.X - x0) / step, z = (marker.Z - z0) / step;
                for (int dz = -4; dz <= 4; dz++)
                    for (int dx = -4; dx <= 4; dx++)
                        if (x + dx >= 0 && x + dx < width && z + dz >= 0 && z + dz < height && dx * dx + dz * dz <= 16)
                        {
                            int i = ((z + dz) * width + x + dx) * 4;
                            pixels[i] = (byte)marker.Color; pixels[i + 1] = (byte)(marker.Color >> 8);
                            pixels[i + 2] = (byte)(marker.Color >> 16); pixels[i + 3] = 255;
                        }
            }
            return new Image(width, height, pixels, structures.Limited);
        }, ct).ConfigureAwait(false);
    }

    private static uint Blend(uint a, uint b) => 0xff000000 |
        ((((a >> 16) & 255) + ((b >> 16) & 255)) / 2 << 16) |
        ((((a >> 8) & 255) + ((b >> 8) & 255)) / 2 << 8) | (((a & 255) + (b & 255)) / 2);
}
