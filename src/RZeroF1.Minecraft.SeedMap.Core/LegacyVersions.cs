// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
namespace RZeroF1.Minecraft.SeedMap;

public static partial class Api
{
    // Every release has its own stable Native ID and exact-JAR verification.
    private static readonly string[] LegacyReleases =
    [
        "1.2.5", "1.3.1", "1.3.2", "1.4.2", "1.4.4", "1.4.5",
        "1.4.6", "1.4.7", "1.5.1", "1.5.2", "1.6.1", "1.6.2",
        "1.6.4", "1.7.2", "1.7.3", "1.7.4", "1.7.5", "1.7.6",
        "1.7.7", "1.7.8", "1.7.9", "1.7.10", "1.8", "1.8.1",
        "1.8.2", "1.8.3", "1.8.4", "1.8.5", "1.8.6", "1.8.7",
        "1.8.8", "1.8.9", "1.9", "1.9.1", "1.9.2", "1.9.3",
        "1.9.4", "1.10", "1.10.1", "1.10.2", "1.11", "1.11.1",
        "1.11.2", "1.12", "1.12.1", "1.12.2",
    ];

    private static VersionCapability LegacyCapability(VersionInfo version)
    {
        string release = version.Key;
        int minor = int.Parse(release.Split('.')[1], System.Globalization.CultureInfo.InvariantCulture);
        bool historicalCache = minor <= 3;
        string biomeProfile = minor switch
        {
            2 => "cubiomes-legacy-1.2.5",
            <= 6 => "cubiomes-legacy-1.3-1.6",
            9 or 10 => "cubiomes-legacy-1.9-1.10",
            _ => "cubiomes-legacy-1.7-1.8-1.11-1.12"
        };
        string structureProfile = release == "1.4.2" ? "cubiomes-legacy-1.4.2" : minor switch
        {
            2 => "cubiomes-legacy-1.2.5",
            3 => "cubiomes-legacy-1.3",
            <= 6 => "cubiomes-legacy-1.4.4-1.6.4",
            7 => "cubiomes-legacy-1.7",
            8 => "cubiomes-legacy-1.8",
            9 => "cubiomes-legacy-1.9",
            10 => "cubiomes-legacy-1.10",
            _ => "cubiomes-legacy-1.11-1.12"
        };
        var structures = new[] { -1, 0, 1 }
            .SelectMany(dimension => Structures(version.Id, dimension))
            .Where(structure => structure.Id != 100)
            .Select(structure => new StructureCapability(structure.Id, structure.Key,
                VersionSupportStatus.Supported,
                structure.Id == 20 && minor >= 11 ? VersionSupportStatus.Supported : VersionSupportStatus.Unsupported,
                VersionSupportStatus.Unsupported)
            {
                RawPlacement = VersionSupportStatus.Supported,
                BiomePredicate = structure.Id == 15 ? VersionSupportStatus.Unsupported : VersionSupportStatus.Supported,
                GenerationPoint = structure.Id == 15 ? VersionSupportStatus.Unsupported : VersionSupportStatus.Supported
            })
            .ToArray();
        return new(release, biomeProfile, structureProfile, version.Id,
            historicalCache ? VersionSupportStatus.Experimental : VersionSupportStatus.Supported,
            VersionSupportStatus.Supported, VersionSupportStatus.Supported, VersionSupportStatus.Supported,
            VersionVerificationStatus.GameJarAlgorithms,
            historicalCache
                ? "DEFAULT 原版群系、维度和结构候选已逐版对照；强要塞仅验证独立种子基准，旧 IntCache 历史状态仍属实验性；未验证完整结构方块生成。"
                : "DEFAULT 原版群系、维度、强要塞和各版本可用结构的位置谓词已逐版对照；结构查询返回候选，未验证完整地形与结构方块生成。")
        {
            Strongholds = historicalCache ? VersionSupportStatus.Experimental : VersionSupportStatus.Supported,
            StructureCapabilities = Array.AsReadOnly(structures)
        };
    }
}
