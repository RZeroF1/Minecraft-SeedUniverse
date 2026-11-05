# NuGet 包

## 包信息

- Package ID：`RZeroF1.Minecraft.SeedMap`
- 版本：`0.2.0`
- Target Framework：`net10.0`
- 原生支持：`win-x64`
- License：MPL-2.0；Cubiomes 上游保留 MIT

包只有一个托管程序集和一个原生运行时目录：

```text
lib/net10.0/
    RZeroF1.Minecraft.SeedMap.dll
    RZeroF1.Minecraft.SeedMap.xml
runtimes/win-x64/native/
    rzero_seedmap.dll
README.md
docs/
LICENSE
THIRD-PARTY-NOTICES.md
licenses/cubiomes/
```

## 本地打包

```powershell
dotnet build src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
dotnet pack src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj -c Release -r win-x64
```

包输出到 `artifacts/packages`。打包项目在收到 `win-x86` 或 `win-arm64` 时会显式
失败；这是因为当前包没有这些平台的 Native 资产。

## 包消费验证

`tests/PackageConsumer` 的 `NuGet.Config` 先读取 `../../artifacts/packages`，再读取
nuget.org。项目只使用 `PackageReference`，没有 `ProjectReference`：

```powershell
dotnet run --project tests/PackageConsumer/PackageConsumer.csproj -c Release -r win-x64
```

该程序会从真实 `.nupkg` 加载托管程序集和 `rzero_seedmap.dll`，验证瓦片、结构、相机和 BGRA API。

## 发布说明

公开发布前应把 `.nupkg` 上传到协作者和 CI 都能访问的稳定源，并在下游项目中固定
预览版本。当前本地构建验证已完成，公开 NuGet 源发布仍需要单独的凭据和发布操作。
