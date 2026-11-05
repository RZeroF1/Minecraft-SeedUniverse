# RZeroF1.Minecraft.SeedMap

`RZeroF1.Minecraft.SeedMap` 是一个面向 Minecraft Java Edition 的纯能力 SDK。它把 Cubiomes 的世界计算封装为稳定的 Native C ABI 和 .NET API，提供种子解析、版本和维度能力、群系采样、异步地图瓦片、结构查询、附近群系搜索、坐标与相机数学、颜色转换、几何辅助和离屏 BGRA 导出。

消费端可以使用 WinUI、WPF、Avalonia、WebView、控制台或服务端程序自行决定界面和图像呈现方式。

```text
消费端 UI / 图像框架
        │
        ▼
RZeroF1.Minecraft.SeedMap
  世界计算 · 瓦片 · 结构 · 坐标 · BGRA · 导出
        │
        ▼
rzero_seedmap.dll
        │
        ▼
固定修订版 Cubiomes
```

## 当前能力

- 包 ID：`RZeroF1.Minecraft.SeedMap`
- 当前版本：`0.1.1`
- 目标框架：`.NET 10`
- 原生运行时：Windows `win-x64`
- Minecraft Java：`1.16.5`、`1.17.1`、`1.18.2`、`1.19.2`、`1.19.4`、`1.20.6`、`1.21.1`、`1.21.3`
- 维度：主世界 `0`、下界 `-1`、末地 `1`
- 采样步长：`1`、`4`、`16`、`64`、`256`
- Cubiomes 修订版：`e61f90580cbdd883214a8054670dacae655e59c0`

## 快速开始

```csharp
using RZeroF1.Minecraft.SeedMap;

var version = SeedMap.Versions.Single(v => v.Key == "1.20.6");
var world = new SeedMap.World(123, version.Id, 0, 64);

using var engine = new SeedMapEngine(world);
int[] ids = await engine.TileAsync(
    new SeedMapEngine.TileKey(Step: 4, X: 0, Z: 0),
    cancellationToken);

var biomes = SeedMap.Biomes(version.Id, world.Dimension)
    .ToDictionary(b => b.Id);
byte[] bgra = SeedMapPalette.ToBgra(ids, biomes);
```

每个异步任务使用独立的 Native generator；`SeedMapEngine` 提供两个并发工作槽、可取消请求和 48 MiB LRU 缓存。`SeedMapExport.RenderAsync` 可以在后台任务中生成有限尺寸的 BGRA8 图像缓冲区，消费端可以用自己的 PNG 编码器写出文件。

## 安装

```xml
<PackageReference Include="RZeroF1.Minecraft.SeedMap"
                  Version="0.1.1" />
```

当前包只提供 `win-x64` 原生资产。应用应明确使用 Windows x64；x86、ARM64、其他操作系统、Minecraft Bedrock 和完整方块世界生成不在当前支持范围内。

## 测试

```powershell
dotnet build src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/RZeroF1.Minecraft.SeedMap.Core.Tests/RZeroF1.Minecraft.SeedMap.Core.Tests.csproj -c Release -r win-x64
dotnet pack src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/PackageConsumer/PackageConsumer.csproj -c Release -r win-x64
```

`tests/PackageConsumer` 只引用本地生成的 `.nupkg`，不引用 SDK 源码项目。

## 许可证

RZeroF1 原创代码采用 MPL 2.0，见 [`LICENSE`](LICENSE)。Cubiomes 上游代码保留 MIT 许可证和 Cubitect 的版权声明，见 [`licenses/cubiomes`](licenses/cubiomes) 以及 [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。
