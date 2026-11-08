# RZeroF1.Minecraft.SeedMap

[简体中文](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.md) | [English](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.en.md)

`RZeroF1.Minecraft.SeedMap` is a .NET 10 SDK for Minecraft Java Edition seed maps. Built on a pinned Cubiomes revision, it provides biome sampling, asynchronous map tiles, structure queries, exact base terrain heights, nearby biome searches, camera and coordinate math, color conversion, geometry helpers, and offscreen BGRA export.

The SDK supplies data, computation, and pixel buffers. Host applications can use WinUI, WPF, Avalonia, WebView, a console, or a server process to implement their own interaction and rendering.

## Documentation

The detailed guides are maintained in Simplified Chinese.

| Guide | Contents |
|---|---|
| [API guide](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/api.md) | World models, biome and structure queries, tiles, cameras, colors, and export examples. |
| [Architecture](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/architecture.md) | Module responsibilities, native resources, asynchronous work, and cache lifetimes. |
| [NuGet and publishing](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md) | Installation, local packaging, independent package verification, and the publishing process. |

## Capabilities

- Package ID: `RZeroF1.Minecraft.SeedMap`
- Source version: `0.2.2`
- Target framework: `.NET 10`
- Native runtime: Windows `win-x64`
- Minecraft Java public release catalog: 86 entries: 43 historical releases from `1.4.2` through `1.12.2`, plus the existing 43 entries from `1.13.2` through `1.21.11`; exact Native IDs for `1.2.5`, `1.3.1`, and `1.3.2` remain available to internal regression tooling; see the [version matrix](docs/versions.md)
- Exact-JAR verification: the 86 public releases and the three internally retained releases from 1.2.5 through 1.3.2 have independent game-JAR Oracle records. Biomes, dimensions, strongholds, mineshafts, and registered structure candidate stages for 1.13.2–1.17.1 are Supported; full structure block generation remains outside the support boundary.
- Modern version status: Biomes for 1.18–1.21.11 are Supported after 234,864 ordered exact-JAR samples per release, with zero mismatches across all 27 releases. Village, Woodland Mansion, Ruined Portal, and Trail Ruins now use their actual projected height and three-dimensional biome predicate; their candidate, generation-point, and related base-terrain stages are Supported. Desert Pyramid and Jungle Temple retain an approximate additional terrain check.
- Terrain heights: Exact `WORLD_SURFACE_WG` and `OCEAN_FLOOR_WG` base-height queries for the default, unblended Overworld in all 27 registered releases from 1.18 through 1.21.11. Results use vanilla `getBaseHeight` first-free-Y semantics; post-carving and decorated chunk heightmaps are outside this API.
- Dimensions: Overworld `0`, Nether `-1`, End `1`
- Sampling steps: `1`, `4`, `16`, `64`, `256` blocks
- Cubiomes revision: base `e61f90580cbdd883214a8054670dacae655e59c0` with PR #162 changes at `ca9fcb4e2b4f29a88b15a19fe1f00a73bf503ec2` and local `legacy46.1+modern43.3+terrain27.1` adaptations

```text
Host UI / image framework
        │
        ▼
RZeroF1.Minecraft.SeedMap
  World computation · Tiles · Structures · Coordinates · BGRA · Export
        │
        ▼
rzero_seedmap.dll
        │
        ▼
Pinned Cubiomes revision
```

## Installation

Add the package to a .NET 10 project and explicitly target Windows x64:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="RZeroF1.Minecraft.SeedMap" Version="0.2.2" />
</ItemGroup>
```

Published packages are available on [NuGet](https://www.nuget.org/packages/RZeroF1.Minecraft.SeedMap); source releases are on [GitHub](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/releases). To verify an unpublished source version, follow the [local packaging instructions](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md#本地构建与打包).

## Quick start

```csharp
using SeedMap = RZeroF1.Minecraft.SeedMap;

var version = SeedMap.Api.Versions.Single(v => v.Key == "1.20.6");
var world = new SeedMap.World(123, version.Id, 0, 64);
var cancellationToken = CancellationToken.None;

using var engine = new SeedMap.Engine(world);
int[] ids = await engine.TileAsync(
    new SeedMap.Engine.TileKey(Step: 4, X: 0, Z: 0),
    cancellationToken);

var biomes = SeedMap.Api.Biomes(version.Id, world.Dimension)
    .ToDictionary(b => b.Id);
byte[] bgra = SeedMap.Palette.ToBgra(ids, biomes);
Console.WriteLine($"{SeedMap.Engine.TileSize} × {SeedMap.Engine.TileSize}, {bgra.Length} bytes");
```

This example runs in a .NET 10 console project with implicit usings enabled. Each tile contains `128 × 128` samples. `Step: 4` samples at four-block intervals, so tile `(0, 0)` covers world coordinates `[0, 512) × [0, 512)`. The BGRA8 buffer uses row-major order with four bytes per pixel.

`Engine` provides two concurrent work slots, cancellable requests, and a 48 MiB LRU tile cache. Each background computation creates and disposes its own native generator. `Export.RenderAsync` produces offscreen pixel buffers that the host can save using its own PNG encoder.

## Base terrain heights

```csharp
using var generator = SeedMap.Api.Create(world);
int surfaceY = SeedMap.Api.SampleTerrainHeight(generator, x: 0, z: 0);
int oceanFloorY = SeedMap.Api.SampleTerrainHeight(
    generator, x: 0, z: 0, SeedMap.TerrainHeightmap.OceanFloorWorldGen);
int[] heights = SeedMap.Api.GenerateTerrainHeights(
    generator, x: -16, z: -16, width: 32, height: 32, step: 1,
    cancellation: cancellationToken);
```

Results are row-major. Origins need not align to the positive block step. Each generator owns reusable noise state and bounded caches. Height computation is more expensive than biome sampling; use background batches and result caching for larger maps. `Api.GetTerrainCapability` reports accuracy separately for each release, dimension, and heightmap type.

## Supported scope

The package includes Windows x64 native assets and targets vanilla Minecraft Java world computation. Structure results use Cubiomes placement predictions and viability checks. Custom world generation, Minecraft Bedrock, worlds containing regions from different game versions, and full block-world generation are outside the supported scope.

## Build and package

Building from source requires Windows, the .NET 10 SDK, and Visual Studio C++ Build Tools. Run these commands from the repository root:

```powershell
dotnet build src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet pack src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
```

Use `scripts/Test-ArtifactIsolation.ps1` to inspect package contents, license files, and build outputs; see the [NuGet guide](docs/nuget.md).

## License

Original RZeroF1 code is licensed under MPL 2.0; see [LICENSE](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/LICENSE). Cubiomes retains its upstream MIT license and Cubitect copyright notice; see the [third-party notices](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/THIRD-PARTY-NOTICES.md) and [source information](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/licenses/cubiomes/SOURCE.md). License texts and third-party source documents are maintained in English.
