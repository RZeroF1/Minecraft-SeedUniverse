// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// Copyright (c) 2026 RZeroF1.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
using RZeroF1.Minecraft.SeedMap;

var version = SeedMap.Versions.Single(v => v.Key == "1.20.6");
var world = new SeedMap.World(123, version.Id, 0, 64);
using var engine = new SeedMapEngine(world);
var tile = await engine.TileAsync(new SeedMapEngine.TileKey(4, 0, 0), CancellationToken.None);
if (tile.Length != 128 * 128 || tile.Any(id => id < 0))
    throw new InvalidOperationException("Package tile result is invalid.");
var hits = await engine.QueryStructuresAsync(
    new SeedMap.Bounds(-2000, -2000, 2000, 2000),
    SeedMap.Structures(version.Id, 0).Where(s => s.Id == 5).ToArray(),
    CancellationToken.None);
var camera = new SeedMapCamera(0, 0, .25);
var bounds = camera.VisibleBounds(1024, 768);
if (camera.SelectSamplingStep() != 4 || !bounds.Contains(0, 0))
    throw new InvalidOperationException("Package camera API is invalid.");
var biomes = SeedMap.Biomes(version.Id, 0).ToDictionary(b => b.Id);
var pixels = SeedMapPalette.ToBgra(tile, biomes);
if (pixels.Length != tile.Length * 4)
    throw new InvalidOperationException("Package BGRA API is invalid.");
Console.WriteLine($"PASS package consumer tile={tile.Length} villages={hits.Hits.Count} native={SeedMap.Revision} bgra={pixels.Length}");
