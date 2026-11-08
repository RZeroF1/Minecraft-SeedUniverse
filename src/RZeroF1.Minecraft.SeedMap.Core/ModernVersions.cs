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
        bool establishedModernQuery = minor <= 17;
        var structures = new[] { -1, 0, 1 }
            .SelectMany(dimension => Structures(version.Id, dimension))
            .Where(structure => structure.Id != 100)
            .Select(structure =>
            {
                bool exactCandidate = establishedModernQuery ||
                    structure.Id is not (5 or 9 or 11 or 23);
                bool exactBiomePredicate = structure.Id != 15 &&
                    (establishedModernQuery || structure.Id is not (5 or 9 or 11 or 23));
                bool exactGenerationPoint = structure.Id != 15 &&
                    (establishedModernQuery || structure.Id is not (5 or 9 or 11 or 23));
                VersionSupportStatus terrain = structure.Id is 1 or 2 or 9
                    ? VersionSupportStatus.Experimental
                    : (!establishedModernQuery && (structure.Id is 5 or 11 or 23))
                        ? VersionSupportStatus.Experimental
                        : structure.Id == 20
                            ? VersionSupportStatus.Supported
                            : VersionSupportStatus.Unsupported;
                var capability = new StructureCapability(structure.Id, structure.Key,
                    exactCandidate ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
                    terrain,
                    VersionSupportStatus.Unsupported)
                {
                    RawPlacement = VersionSupportStatus.Supported,
                    BiomePredicate = exactBiomePredicate ? VersionSupportStatus.Supported :
                        structure.Id == 15 ? VersionSupportStatus.Unsupported : VersionSupportStatus.Experimental,
                    GenerationPoint = exactGenerationPoint ? VersionSupportStatus.Supported :
                        structure.Id == 15 ? VersionSupportStatus.Unsupported : VersionSupportStatus.Experimental
                };
                return capability;
            })
            .ToArray();
        bool structuresSupported = structures.All(s => s.Candidates == VersionSupportStatus.Supported);
        bool biomesSupported = true;
        string notes = establishedModernQuery
            ? "精确 JAR 的五尺度群系、三维度、128 强要塞、矿井 RNG 及已登记结构的位置谓词已独立对照；完整结构方块生成仍未声明。"
            : "27 个精确 JAR 的群系、三维度、128 强要塞、原始 placement 和逐结构阶段已独立对照；Desert Pyramid、Jungle Temple、Village、Mansion、Ruined Portal 和 Trail Ruins 的候选或高度阶段仍属实验性。";
        return new(release, biomeProfile, structureProfile, version.Id,
            structuresSupported ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            biomesSupported ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            structuresSupported ? VersionSupportStatus.Supported : VersionSupportStatus.Experimental,
            VersionSupportStatus.Supported,
            VersionVerificationStatus.GameJarAlgorithms, notes)
        {
            Strongholds = VersionSupportStatus.Supported,
            StructureCapabilities = Array.AsReadOnly(structures)
        };
    }
}
