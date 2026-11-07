// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
namespace RZeroF1.Minecraft.SeedMap;

public static partial class Api
{
    private static VersionCapability ModernCapability(VersionInfo version)
    {
        string release = version.Key;
        int minor = int.Parse(release.Split('.')[1], System.Globalization.CultureInfo.InvariantCulture);
        string nativeProfile = ReleaseToNative[release];
        string biomeProfile = release switch
        {
            "1.14" => "cubiomes-1.14.4",
            "1.21" or "1.21.1" => "cubiomes-1.21.3",
            "1.21.4" => "cubiomes-1.21-winter-drop",
            _ when minor == 21 && release is not ("1.21.2" or "1.21.3") => "cubiomes-1.21.5-pr162",
            _ => "cubiomes-" + nativeProfile
        };
        string structureProfile = release switch
        {
            "1.14" => "cubiomes-1.14",
            "1.21" or "1.21.1" => "cubiomes-1.21.1",
            "1.21.5" or "1.21.6" or "1.21.7" or "1.21.8" => "cubiomes-1.21.5-1.21.8-strongholds",
            _ => biomeProfile
        };
        bool exactCandidateStage = minor <= 17;
        var structures = new[] { -1, 0, 1 }
            .SelectMany(dimension => Structures(version.Id, dimension))
            .Where(structure => structure.Id != 100)
            .Select(structure =>
            {
                // Raw registry placements alone do not verify QueryRegion's
                // additional biome and terrain predicates.
                return new StructureCapability(structure.Id, structure.Key,
                    exactCandidateStage ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
                    exactCandidateStage ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
                    VersionSupportStatus.Unsupported);
            })
            .ToArray();
        string notes = exactCandidateStage
            ? "精确 JAR 的五尺度群系、三维度、128 强要塞、矿井 RNG 及已登记结构的位置谓词已独立对照；完整结构方块生成仍未声明。"
            : "精确 JAR 的三维度、128 强要塞、矿井 RNG、结构 placement 和原版 generation-point 阶段已独立记录；群系存在 Java ParameterList 等距离选择差异，结构候选与地形能力保持实验性。";
        return new(release, biomeProfile, structureProfile, version.Id,
            exactCandidateStage ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            exactCandidateStage ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            exactCandidateStage ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            VersionSupportStatus.Supported,
            VersionVerificationStatus.GameJarAlgorithms, notes)
        {
            Strongholds = VersionSupportStatus.Supported,
            StructureCapabilities = Array.AsReadOnly(structures)
        };
    }
}
