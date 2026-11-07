// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
using RZeroF1.Minecraft.SeedMap;
using SeedMap = RZeroF1.Minecraft.SeedMap;
using System.Diagnostics;
using System.Security.Cryptography;

var version = SeedMap.Api.Versions.Single(v => v.Key == "1.20.6");
var world = new SeedMap.World(123, version.Id, 0, 64);
using var engine = new Engine(world);
var tile = await engine.TileAsync(new Engine.TileKey(4, 0, 0), CancellationToken.None);
if (tile.Length != 128 * 128 || tile.Any(id => id < 0))
    throw new InvalidOperationException("Package tile result is invalid.");
var hits = await engine.QueryStructuresAsync(
    new SeedMap.Bounds(-2000, -2000, 2000, 2000),
    SeedMap.Api.Structures(version.Id, 0).Where(s => s.Id == 5).ToArray(),
    CancellationToken.None);
var camera = new Camera(0, 0, .25);
var bounds = camera.VisibleBounds(1024, 768);
if (camera.SelectSamplingStep() != 4 || !bounds.Contains(0, 0))
    throw new InvalidOperationException("Package camera API is invalid.");
var biomes = SeedMap.Api.Biomes(version.Id, 0).ToDictionary(b => b.Id);
var pixels = Palette.ToBgra(tile, biomes);
if (pixels.Length != tile.Length * 4)
    throw new InvalidOperationException("Package BGRA API is invalid.");
Console.WriteLine($"PASS package consumer tile={tile.Length} villages={hits.Hits.Count} native={SeedMap.Api.Revision} bgra={pixels.Length}");
var legacy = SeedMap.Api.Versions.Where(v => v.Id is >= 100 and < 146).ToArray();
if (SeedMap.Api.Versions.Count != 89 || legacy.Length != 46)
    throw new InvalidOperationException("Package release catalog is incomplete.");
foreach (var release in legacy)
{
    var capability = SeedMap.Api.GetVersionCapability(release.Key);
    if (capability.Verification != VersionVerificationStatus.GameJarAlgorithms ||
        capability.StructureCapabilities.Any(s => s.FullGeneration != VersionSupportStatus.Unsupported))
        throw new InvalidOperationException($"Invalid package capability: {release.Key}");
    using var generator = SeedMap.Api.Create(new(123, release.Id, 0, 64));
    if (SeedMap.Api.Strongholds(generator).Length != (int.Parse(release.Key.Split('.')[1]) <= 8 ? 3 : 128))
        throw new InvalidOperationException($"Invalid package strongholds: {release.Key}");
    using var end = SeedMap.Api.Create(new(123, release.Id, 1, 64));
    if (SeedMap.Api.Generate(end, -16000, 24000, 1, 1, 1, 64)[0] != 9 ||
        !SeedMap.Api.Biomes(release.Id, 1).Select(b => b.Id).SequenceEqual(new[] { 9 }))
        throw new InvalidOperationException($"Invalid package End semantics: {release.Key}");
}
var nativeModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
    .Single(m => string.Equals(m.ModuleName, "rzero_seedmap.dll", StringComparison.OrdinalIgnoreCase));
Console.WriteLine($"PASS package legacy={legacy.Length} assembly={typeof(SeedMap.Api).Assembly.Location}");
Console.WriteLine($"LOADED_NATIVE {nativeModule.FileName} SHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(nativeModule.FileName)))}");
if (Environment.GetEnvironmentVariable("SEEDMAP_CAPABILITY_OUTPUT") is { Length: > 0 } capabilityOutput)
{
    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    File.WriteAllText(capabilityOutput, System.Text.Json.JsonSerializer.Serialize(SeedMap.Api.VersionCapabilities, options));
}
