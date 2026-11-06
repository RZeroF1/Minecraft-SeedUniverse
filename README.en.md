# RZeroF1.Minecraft.SeedMap

[简体中文](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.md) | [English](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.en.md)

`RZeroF1.Minecraft.SeedMap` is a .NET 10 SDK for Minecraft Java Edition seed maps. Built on a pinned Cubiomes revision, it provides biome sampling, asynchronous map tiles, structure queries, nearby biome searches, camera and coordinate math, color conversion, geometry helpers, and offscreen BGRA export.

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
- Source version: `0.2.1`
- Target framework: `.NET 10`
- Native runtime: Windows `win-x64`
- Minecraft Java release catalog: 43 release entries covering `1.13.2`–`1.21.11`, backed by 14 unique algorithm profiles
- Version status: existing `1.16.5`–`1.21.3` Native profiles remain supported; `1.13.2`–`1.16.1` and their patch aliases are Native-smoke/experimental profiles; `1.21.5` has a verified biome JAR comparison but experimental structures; `1.21.11` still lacks a game oracle
- Dimensions: Overworld `0`, Nether `-1`, End `1`
- Sampling steps: `1`, `4`, `16`, `64`, `256` blocks
- Cubiomes revision: base `e61f90580cbdd883214a8054670dacae655e59c0` with PR #162 changes at `ca9fcb4e2b4f29a88b15a19fe1f00a73bf503ec2`

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
  <PackageReference Include="RZeroF1.Minecraft.SeedMap" Version="0.2.1" />
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

## Supported scope

The package includes Windows x64 native assets and targets vanilla Minecraft Java world computation. Structure results use Cubiomes placement predictions and viability checks. Custom world generation, Minecraft Bedrock, worlds containing regions from different game versions, and full block-world generation are outside the supported scope.

## Build and test

Building from source requires Windows, the .NET 10 SDK, and Visual Studio C++ Build Tools. Run these commands from the repository root:

```powershell
dotnet build src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/RZeroF1.Minecraft.SeedMap.Core.Tests/RZeroF1.Minecraft.SeedMap.Core.Tests.csproj -c Release -r win-x64
dotnet pack src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/PackageConsumer/PackageConsumer.csproj -c Release -r win-x64
```

`tests/PackageConsumer` uses `PackageReference` to verify the actual `.nupkg` and native DLL loading. Its configured sources include local packages and nuget.org. For verification with an isolated cache, use the [restore command in the NuGet guide](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md#独立包消费验证).

## License

Original RZeroF1 code is licensed under MPL 2.0; see [LICENSE](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/LICENSE). Cubiomes retains its upstream MIT license and Cubitect copyright notice; see the [third-party notices](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/THIRD-PARTY-NOTICES.md) and [source information](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/licenses/cubiomes/SOURCE.md). License texts and third-party source documents are maintained in English.
