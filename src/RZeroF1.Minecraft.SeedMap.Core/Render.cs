// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace RZeroF1.Minecraft.SeedMap;

/// <summary>以 Minecraft 方块为单位的二维世界坐标点。</summary>
public readonly record struct Point(double X, double Z);

/// <summary>供渲染宿主使用的屏幕坐标点，不依赖具体 UI 框架。</summary>
public readonly record struct ScreenPoint(double X, double Y);

/// <summary>线段，坐标空间由调用方决定。</summary>
public readonly record struct Line(Point Start, Point End);

/// <summary>
/// 供地图宿主复用的相机数学，不包含 XAML 或平台类型。
/// </summary>
public sealed class Camera
{
    public const double MinPixelsPerBlock = 1.0 / 256;
    public const double MaxPixelsPerBlock = 8;

    public double CenterX { get; private set; }
    public double CenterZ { get; private set; }
    public double PixelsPerBlock { get; private set; }

    public Camera(double centerX = 0, double centerZ = 0, double pixelsPerBlock = .25)
    {
        CenterX = Api.ClampCoordinate(centerX);
        CenterZ = Api.ClampCoordinate(centerZ);
        PixelsPerBlock = Math.Clamp(pixelsPerBlock, MinPixelsPerBlock, MaxPixelsPerBlock);
    }

    public Point ScreenToWorld(double screenX, double screenY, double viewportWidth, double viewportHeight) =>
        new(
            CenterX + (screenX - viewportWidth / 2) / PixelsPerBlock,
            CenterZ + (screenY - viewportHeight / 2) / PixelsPerBlock);

    public ScreenPoint WorldToScreen(double x, double z, double viewportWidth, double viewportHeight) =>
        new(
            (x - CenterX) * PixelsPerBlock + viewportWidth / 2,
            (z - CenterZ) * PixelsPerBlock + viewportHeight / 2);

    public Bounds VisibleBounds(double viewportWidth, double viewportHeight)
    {
        var topLeft = ScreenToWorld(0, 0, viewportWidth, viewportHeight);
        var bottomRight = ScreenToWorld(viewportWidth, viewportHeight, viewportWidth, viewportHeight);
        return new(
            (int)Math.Max(-Api.WorldLimit, Math.Floor(topLeft.X)),
            (int)Math.Max(-Api.WorldLimit, Math.Floor(topLeft.Z)),
            (int)Math.Min(Api.WorldLimit, Math.Ceiling(bottomRight.X)),
            (int)Math.Min(Api.WorldLimit, Math.Ceiling(bottomRight.Z)));
    }

    public void MoveTo(double x, double z)
    {
        CenterX = Api.ClampCoordinate(x);
        CenterZ = Api.ClampCoordinate(z);
    }

    /// <summary>围绕屏幕锚点缩放；相机移动受世界边界限制。</summary>
    public void ZoomAt(double factor, double anchorX, double anchorY, double viewportWidth, double viewportHeight)
    {
        if (!double.IsFinite(factor) || factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor));

        var before = ScreenToWorld(anchorX, anchorY, viewportWidth, viewportHeight);
        PixelsPerBlock = Math.Clamp(PixelsPerBlock * factor, MinPixelsPerBlock, MaxPixelsPerBlock);
        var after = ScreenToWorld(anchorX, anchorY, viewportWidth, viewportHeight);
        MoveTo(CenterX + before.X - after.X, CenterZ + before.Z - after.Z);
    }

    public int SelectSamplingStep() => SelectSamplingStep(PixelsPerBlock);

    public static int SelectSamplingStep(double pixelsPerBlock)
    {
        double blocksPerPixel = 1 / Math.Max(pixelsPerBlock, double.Epsilon);
        return blocksPerPixel <= 1 ? 1
            : blocksPerPixel <= 4 ? 4
            : blocksPerPixel <= 16 ? 16
            : blocksPerPixel <= 64 ? 64
            : 256;
    }
}

/// <summary>为 <see cref="Engine"/> 规划可见瓦片、预取范围与加载顺序。</summary>
public static class TilePlanner
{
    public static IReadOnlySet<Engine.TileKey> Visible(Bounds bounds, int step)
    {
        ValidateStep(step);
        int span = checked(Engine.TileSize * step);
        var result = new HashSet<Engine.TileKey>();
        for (int z = Api.FloorDiv(bounds.MinZ, span); z <= Api.FloorDiv(bounds.MaxZ - 1, span); z++)
            for (int x = Api.FloorDiv(bounds.MinX, span); x <= Api.FloorDiv(bounds.MaxX - 1, span); x++)
                result.Add(new(step, x, z));
        return result;
    }

    public static IReadOnlySet<Engine.TileKey> WithPrefetch(
        Bounds bounds, int step, int rings = 1)
    {
        ValidateStep(step);
        if (rings < 0) throw new ArgumentOutOfRangeException(nameof(rings));
        int span = checked(Engine.TileSize * step);
        int minX = Api.FloorDiv(Math.Max(-Api.WorldLimit, bounds.MinX - rings * span), span);
        int maxX = Api.FloorDiv(Math.Min(Api.WorldLimit - 1, bounds.MaxX - 1 + rings * span), span);
        int minZ = Api.FloorDiv(Math.Max(-Api.WorldLimit, bounds.MinZ - rings * span), span);
        int maxZ = Api.FloorDiv(Math.Min(Api.WorldLimit - 1, bounds.MaxZ - 1 + rings * span), span);
        var result = new HashSet<Engine.TileKey>();
        for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
                result.Add(new(step, x, z));
        return result;
    }

    public static IReadOnlyList<Engine.TileKey> OrderForLoading(
        IEnumerable<Engine.TileKey> candidates,
        IReadOnlySet<Engine.TileKey> visible,
        double centerX,
        double centerZ,
        int maximum)
    {
        if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        return candidates
            .OrderBy(key => visible.Contains(key) ? 0 : 1)
            .ThenBy(key =>
            {
                double x = (key.X + .5) * Engine.TileSize * key.Step;
                double z = (key.Z + .5) * Engine.TileSize * key.Step;
                return Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2);
            })
            .Take(maximum)
            .ToArray();
    }

    private static void ValidateStep(int step)
    {
        if (step is not (1 or 4 or 16 or 64 or 256))
            throw new ArgumentOutOfRangeException(nameof(step));
    }
}

/// <summary>为地图渲染提供颜色选择与 BGRA 转换。</summary>
public static class Palette
{
    public static uint ResolveBiomeColor(
        int biomeId,
        IReadOnlyDictionary<int, BiomeInfo> biomes,
        IReadOnlySet<int>? selected = null,
        bool hideUnselected = false,
        bool darkTheme = false)
    {
        uint color = biomes.TryGetValue(biomeId, out var biome) ? biome.Color : 0xff555555;
        if (selected is null || selected.Count == 0 || selected.Contains(biomeId))
            return color;
        if (hideUnselected)
            return darkTheme ? 0xff202020u : 0xffe5e5e5u;

        uint red = (color >> 16) & 255, green = (color >> 8) & 255, blue = color & 255;
        uint gray = (red * 3 + green * 6 + blue) / 10;
        uint target = darkTheme ? gray / 3 : (gray + 510) / 3;
        return 0xff000000u | target * 0x010101u;
    }

    public static byte[] ToBgra(
        ReadOnlySpan<int> biomeIds,
        IReadOnlyDictionary<int, BiomeInfo> biomes,
        IReadOnlySet<int>? selected = null,
        bool hideUnselected = false,
        bool darkTheme = false)
    {
        var pixels = new byte[checked(biomeIds.Length * 4)];
        WriteBgra(biomeIds, pixels, biomes, selected, hideUnselected, darkTheme);
        return pixels;
    }

    public static void WriteBgra(
        ReadOnlySpan<int> biomeIds,
        Span<byte> destination,
        IReadOnlyDictionary<int, BiomeInfo> biomes,
        IReadOnlySet<int>? selected = null,
        bool hideUnselected = false,
        bool darkTheme = false)
    {
        if (destination.Length < biomeIds.Length * 4)
            throw new ArgumentException("目标缓冲区不足。", nameof(destination));
        for (int i = 0; i < biomeIds.Length; i++)
        {
            uint color = ResolveBiomeColor(biomeIds[i], biomes, selected, hideUnselected, darkTheme);
            int offset = i * 4;
            destination[offset] = (byte)color;
            destination[offset + 1] = (byte)(color >> 8);
            destination[offset + 2] = (byte)(color >> 16);
            destination[offset + 3] = 255;
        }
    }

    public static uint Blend(uint first, uint second) => 0xff000000u |
        ((((first >> 16) & 255) + ((second >> 16) & 255)) / 2 << 16) |
        ((((first >> 8) & 255) + ((second >> 8) & 255)) / 2 << 8) |
        (((first & 255) + (second & 255)) / 2);
}

/// <summary>为地图叠加层和测量提供不依赖 UI 框架的几何计算。</summary>
public static class Geometry
{
    public static double Distance(double x1, double z1, double x2, double z2) =>
        Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));

    public static Bounds ChunkBounds(int chunkX, int chunkZ) =>
        new(checked(chunkX * 16), checked(chunkZ * 16), checked(chunkX * 16 + 16), checked(chunkZ * 16 + 16));

    public static IEnumerable<Line> GridLines(Bounds bounds, int spacing = 16)
    {
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        int firstX = Api.FloorDiv(bounds.MinX, spacing) * spacing;
        int firstZ = Api.FloorDiv(bounds.MinZ, spacing) * spacing;
        for (int x = firstX; x < bounds.MaxX; x += spacing)
            yield return new(new(x, bounds.MinZ), new(x, bounds.MaxZ));
        for (int z = firstZ; z < bounds.MaxZ; z += spacing)
            yield return new(new(bounds.MinX, z), new(bounds.MaxX, z));
    }

    public static IReadOnlyList<Point> Circle(double centerX, double centerZ, double radius, int segments = 32)
    {
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var points = new Point[segments];
        for (int i = 0; i < segments; i++)
        {
            double angle = Math.PI * 2 * i / segments;
            points[i] = new(centerX + Math.Cos(angle) * radius, centerZ + Math.Sin(angle) * radius);
        }
        return points;
    }
}
