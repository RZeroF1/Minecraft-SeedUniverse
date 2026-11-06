# RZeroF1.Minecraft.SeedMap

[简体中文](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.md) | [English](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/README.en.md)

`RZeroF1.Minecraft.SeedMap` 是面向 Minecraft Java Edition 的 .NET 10 种子地图 SDK，基于固定修订版 Cubiomes，提供群系采样、异步地图瓦片、结构查询、附近群系搜索、坐标与相机数学、颜色转换、几何辅助和离屏 BGRA 导出。

SDK 提供数据、计算和图像缓冲区。消费端可以使用 WinUI、WPF、Avalonia、WebView、控制台或服务端程序，自行实现交互和图像呈现。

## 文档

| 文档 | 内容 |
|---|---|
| [API 说明](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/api.md) | 世界模型、群系与结构查询、瓦片、相机、颜色和导出示例。 |
| [架构说明](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/architecture.md) | 模块职责、Native 资源、异步任务和缓存生命周期。 |
| [NuGet 与发布](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md) | 安装、本地打包、独立包消费验证和发布流程。 |

## 当前能力

- 包 ID：`RZeroF1.Minecraft.SeedMap`
- 当前源码版本：`0.2.1`
- 目标框架：`.NET 10`
- 原生运行时：Windows `win-x64`
- Minecraft Java：`1.16.5`、`1.17.1`、`1.18.2`、`1.19.2`、`1.19.4`、`1.20.6`、`1.21.1`、`1.21.3`
- 维度：主世界 `0`、下界 `-1`、末地 `1`
- 采样步长：`1`、`4`、`16`、`64`、`256` 个方块
- Cubiomes 修订版：`e61f90580cbdd883214a8054670dacae655e59c0`

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

## 安装

在 .NET 10 项目中添加包引用，并明确使用 Windows x64：

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="RZeroF1.Minecraft.SeedMap" Version="0.2.1" />
</ItemGroup>
```

公开版本见 [NuGet](https://www.nuget.org/packages/RZeroF1.Minecraft.SeedMap)，源码和版本记录见 [GitHub](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/releases)。尚未发布的源码版本可按[本地打包流程](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md#本地构建与打包)验证。

## 快速开始

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

示例可用于启用隐式 `using` 的 .NET 10 控制台项目。瓦片大小为 `128 × 128`，`Step: 4` 表示每个样本间隔四个方块；瓦片 `(0, 0)` 对应世界坐标 `[0, 512) × [0, 512)`。返回的 BGRA8 缓冲区按行排列，每个像素四个字节。

`Engine` 提供两个并发工作槽、可取消请求和 48 MiB LRU 瓦片缓存，每个后台计算任务独立创建并释放 Native generator。`Export.RenderAsync` 可以生成离屏像素，消费端可使用自己的 PNG 编码器保存图像。

## 支持范围

当前包提供 Windows x64 原生资产，面向原版 Minecraft Java 世界计算。结构结果采用 Cubiomes 的位置预测和适用性检查；自定义世界生成、Minecraft Bedrock、混合版本存档和完整方块世界生成不在当前支持范围内。

## 构建与测试

源码构建需要 Windows、.NET 10 SDK 和 Visual Studio C++ Build Tools。在仓库根目录执行：

```powershell
dotnet build src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/RZeroF1.Minecraft.SeedMap.Core.Tests/RZeroF1.Minecraft.SeedMap.Core.Tests.csproj -c Release -r win-x64
dotnet pack src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet run --project tests/PackageConsumer/PackageConsumer.csproj -c Release -r win-x64
```

`tests/PackageConsumer` 通过 `PackageReference` 验证实际 `.nupkg` 及 Native DLL 加载，其包源包含本地产物和 nuget.org。需要隔离缓存验证时，使用 [NuGet 文档中的还原命令](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/docs/nuget.md#独立包消费验证)。

## 许可证

RZeroF1 原创代码采用 MPL 2.0，见 [LICENSE](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/LICENSE)。Cubiomes 上游代码保留 MIT 许可证和 Cubitect 的版权声明，见[第三方声明](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/THIRD-PARTY-NOTICES.md)和[来源说明](https://github.com/RZeroF1/WinUI-Minecraft-SeedMap/blob/main/licenses/cubiomes/SOURCE.md)。许可证和第三方来源文档使用英文。
