// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RZeroF1.Minecraft.SeedMap;

/// <summary>Typed, versioned Cubiomes ABI. Each worker owns its native generator.</summary>
public static class SeedMap
{
    private const string Library = "rzero_seedmap";
    public const int WorldLimit = 30_000_000;
    public const int AbiVersion = 1;
    public const string Revision = "e61f90580cbdd883214a8054670dacae655e59c0";
    private static readonly Lazy<VersionInfo[]> Capabilities = new(Initialize);

    public sealed record VersionInfo(int Id, string Key, int Dimensions);
    public sealed record BiomeInfo(int Id, string Key, string Name, uint Color);
    public sealed record StructureInfo(int Id, string Key, string Name, int RegionBlocks);
    public readonly record struct World(long Seed, int Version, int Dimension, int Y);
    public readonly record struct Bounds(int MinX, int MinZ, int MaxX, int MaxZ)
    {
        public bool Contains(int x, int z) => x >= MinX && x < MaxX && z >= MinZ && z < MaxZ;
    }
    public sealed record StructureHit(int Type, int X, int Z, int Flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeVersion { public int Id, Dimensions; public IntPtr Key; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStructure { public int Id, Dimension, Region, Flags; public IntPtr Key; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeHit { public int Type, X, Z, Flags; }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_abi_version();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern IntPtr cm_source_revision();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_versions([Out] NativeVersion[]? output, int capacity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_structures(int version, int dimension, [Out] NativeStructure[]? output, int capacity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern IntPtr cm_biome_name(int version, int dimension, int id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern uint cm_biome_color(int id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_create(int version, ulong seed, int dimension, out IntPtr handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern void cm_destroy(IntPtr handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_biomes(Generator handle, long x, long z, int w, int h, int step, int y, [Out] int[] output, int capacity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_structure_region(Generator handle, int type, int rx, int rz, out NativeHit hit);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_strongholds(Generator handle, [Out] NativeHit[] output, int capacity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl), DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)] private static extern int cm_slime(ulong seed, int chunkX, int chunkZ);

    public sealed class Generator : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal Generator(IntPtr value) : base(true) => SetHandle(value);
        protected override bool ReleaseHandle() { cm_destroy(handle); return true; }
    }

    public static IReadOnlyList<VersionInfo> Versions => Capabilities.Value;

    private static VersionInfo[] Initialize()
    {
        if (cm_abi_version() != AbiVersion || Marshal.PtrToStringUTF8(cm_source_revision()) != Revision)
            throw new InvalidOperationException("种子地图组件版本不匹配，请重新构建或安装。");
        var items = new NativeVersion[cm_versions(null, 0)];
        Check(cm_versions(items, items.Length));
        return items.Select(v => new VersionInfo(v.Id, Marshal.PtrToStringUTF8(v.Key)!, v.Dimensions)).ToArray();
    }

    public static long ParseSeed(string text)
    {
        text = text.Trim();
        if (text.Length == 0) throw new ArgumentException("请输入种子。");
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value)) return value;
        // Java String.hashCode: UTF-16 code units, signed int, sign extended to long.
        int hash = 0;
        foreach (char ch in text) hash = unchecked(hash * 31 + ch);
        return hash;
    }

    public static int FloorDiv(int value, int divisor) => (int)Math.Floor(value / (double)divisor);
    public static double ClampCoordinate(double value) => Math.Clamp(double.IsFinite(value) ? value : 0, -WorldLimit + 1, WorldLimit - 1);

    public static Generator Create(World world)
    {
        _ = Versions;
        Check(cm_create(world.Version, unchecked((ulong)world.Seed), world.Dimension, out var handle));
        return new Generator(handle);
    }

    public static int[] Generate(Generator generator, long x, long z, int width, int height, int step, int y)
    {
        var ids = new int[checked(width * height)];
        Check(cm_biomes(generator, x, z, width, height, step, y, ids, ids.Length));
        return ids;
    }

    public static BiomeInfo[] Biomes(int version, int dimension)
    {
        _ = Versions;
        var list = new List<BiomeInfo>();
        for (int id = 0; id < 256; id++)
        {
            string? key = Marshal.PtrToStringUTF8(cm_biome_name(version, dimension, id));
            if (key is not null) list.Add(new(id, key, BiomeName(key), cm_biome_color(id)));
        }
        return list.ToArray();
    }

    public static StructureInfo[] Structures(int version, int dimension)
    {
        _ = Versions;
        var items = new NativeStructure[32];
        int count = cm_structures(version, dimension, items, items.Length);
        Check(count);
        return items.Take(count).Select(s =>
        {
            var key = Marshal.PtrToStringUTF8(s.Key)!;
            return new StructureInfo(s.Id, key, StructureName(key), s.Region);
        }).ToArray();
    }

    public static StructureHit? QueryRegion(Generator generator, int type, int rx, int rz)
    {
        int rc = cm_structure_region(generator, type, rx, rz, out var hit);
        Check(rc);
        return rc == 0 ? null : new(hit.Type, hit.X, hit.Z, hit.Flags);
    }

    public static StructureHit[] Strongholds(Generator generator)
    {
        var hits = new NativeHit[128];
        int count = cm_strongholds(generator, hits, hits.Length);
        Check(count);
        return hits.Take(count).Select(h => new StructureHit(h.Type, h.X, h.Z, h.Flags)).ToArray();
    }

    public static bool IsSlimeChunk(long seed, int x, int z)
    {
        _ = Versions;
        int result = cm_slime(unchecked((ulong)seed), x, z);
        Check(result);
        return result != 0;
    }

    private static void Check(int code)
    {
        if (code >= 0) return;
        throw new InvalidOperationException(code switch
        {
            -1 => "地图参数无效。",
            -2 => "此版本或维度不支持该功能。",
            -3 => "坐标超出 Java 世界边界。",
            -4 => "地图组件内存分配失败。",
            -6 => "地图组件缓冲区不足。",
            _ => "地图生成失败。"
        });
    }

    private static string StructureName(string key) => key switch
    {
        "desert_pyramid" => "沙漠神殿",
        "jungle_pyramid" or "jungle_temple" => "丛林神庙",
        "swamp_hut" => "沼泽小屋",
        "igloo" => "雪屋",
        "village" => "村庄",
        "ocean_ruin" => "海底废墟",
        "shipwreck" => "沉船",
        "monument" => "海底神殿",
        "mansion" => "林地府邸",
        "pillager_outpost" or "outpost" => "掠夺者前哨站",
        "ruined_portal" or "ruined_portal_n" or "ruined_portal_nether" => "废弃传送门",
        "ancient_city" => "远古城市",
        "buried_treasure" or "treasure" => "埋藏的宝藏",
        "mineshaft" => "废弃矿井",
        "fortress" => "下界要塞",
        "bastion_remnant" or "bastion" => "堡垒遗迹",
        "end_city" => "末地城",
        "trail_ruins" => "古迹废墟",
        "trial_chambers" => "试炼密室",
        "stronghold" => "要塞",
        _ => key.Replace('_', ' ')
    };

    private static string BiomeName(string key) => key switch
    {
        "plains" => "平原",
        "sunflower_plains" => "向日葵平原",
        "desert" => "沙漠",
        "forest" => "森林",
        "flower_forest" => "繁花森林",
        "birch_forest" => "桦木森林",
        "dark_forest" => "黑森林",
        "taiga" => "针叶林",
        "swamp" => "沼泽",
        "mangrove_swamp" => "红树林沼泽",
        "river" => "河流",
        "frozen_river" => "冻河",
        "ocean" => "海洋",
        "deep_ocean" => "深海",
        "warm_ocean" => "暖水海洋",
        "lukewarm_ocean" => "温水海洋",
        "cold_ocean" => "冷水海洋",
        "frozen_ocean" => "冻洋",
        "deep_lukewarm_ocean" => "温水深海",
        "deep_cold_ocean" => "冷水深海",
        "deep_frozen_ocean" => "冰冻深海",
        "beach" => "沙滩",
        "snowy_beach" => "积雪沙滩",
        "stony_shore" => "石岸",
        "jungle" => "丛林",
        "sparse_jungle" => "稀疏丛林",
        "bamboo_jungle" => "竹林",
        "savanna" => "热带草原",
        "savanna_plateau" => "热带高原",
        "badlands" => "恶地",
        "eroded_badlands" => "风蚀恶地",
        "wooded_badlands" => "疏林恶地",
        "mushroom_fields" => "蘑菇岛",
        "snowy_plains" => "雪原",
        "snowy_taiga" => "积雪针叶林",
        "ice_spikes" => "冰刺平原",
        "meadow" => "草甸",
        "grove" => "雪林",
        "snowy_slopes" => "积雪山坡",
        "jagged_peaks" => "尖峭山峰",
        "frozen_peaks" => "冰封山峰",
        "stony_peaks" => "裸岩山峰",
        "cherry_grove" => "樱花树林",
        "deep_dark" => "深暗之域",
        "lush_caves" => "繁茂洞穴",
        "dripstone_caves" => "溶洞",
        "old_growth_pine_taiga" => "原始松木针叶林",
        "old_growth_spruce_taiga" => "原始云杉针叶林",
        "old_growth_birch_forest" => "原始桦木森林",
        "windswept_hills" => "风袭丘陵",
        "windswept_forest" => "风袭森林",
        "windswept_gravelly_hills" => "风袭沙砾丘陵",
        "windswept_savanna" => "风袭草原",
        "nether_wastes" => "下界荒地",
        "soul_sand_valley" => "灵魂沙峡谷",
        "crimson_forest" => "绯红森林",
        "warped_forest" => "诡异森林",
        "basalt_deltas" => "玄武岩三角洲",
        "the_end" => "末地",
        "small_end_islands" => "末地小型岛屿",
        "end_midlands" => "末地内陆",
        "end_highlands" => "末地高地",
        "end_barrens" => "末地荒地",
        _ => key.Replace('_', ' ')
    };
}
