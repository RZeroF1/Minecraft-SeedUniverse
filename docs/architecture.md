# 架构

SeedMap 的职责是提供可复用的数据、计算、几何和图像缓冲区能力。UI 控件、窗口、
主题、动画、输入事件、剪贴板和文件选择器由消费端维护。

```text
消费端应用
  ├─ UI 状态、输入、WriteableBitmap、Canvas、动画和窗口生命周期
  └─ RZeroF1.Minecraft.SeedMap
       ├─ SeedMap：Native ABI、SafeHandle、能力表和同步计算
       ├─ SeedMapEngine：异步瓦片、取消、结构区域查询、附近群系搜索、LRU
       ├─ SeedMapCamera：屏幕/世界坐标、视野、锚点缩放和采样步长
       ├─ SeedMapTilePlanner：可见瓦片、预取范围和加载优先级
       ├─ SeedMapPalette：群系颜色和 BGRA8 转换
       ├─ SeedMapGeometry：距离、网格、区块边界和测量几何
       ├─ SeedMapExport：无 XAML 的离屏 BGRA 导出
       └─ SeedMapSession：通用工作区、profile、路径点和存储接口
            │
            ▼
       rzero_seedmap.dll
            │
            ▼
       固定修订版 Cubiomes C source
```

Native 层在 `native/cubiomes/seedmap.c` 和 `seedmap.h` 中提供小而明确的 C ABI。
托管层在首次访问能力表时检查 ABI 版本和 Cubiomes source revision，版本不匹配时
立即失败。每个 Native generator 由 `SafeHandle` 管理，不能在多个工作线程之间
共享。

`SeedMapEngine.TileAsync` 返回固定 `128 × 128` 的群系 ID 数组；瓦片坐标表示
`TileKey.X * 128 * Step` 和 `TileKey.Z * 128 * Step` 的世界坐标起点。请求最多
使用两个工作槽，取消会同时连接调用方 token 和 engine 生命周期 token。

`SeedMapCamera` 和 `SeedMapTilePlanner` 把原先视口中的坐标与请求计划数学提取为
平台无关 API。它们不接受 `Point`、`Canvas`、`Image` 或 `WriteableBitmap`，因此
下游可以将结果接到任意 UI 框架。

`SeedMapPalette` 只处理群系颜色选择和 BGRA8 写入；它不创建图像对象。消费端负责
把返回的字节写入具体图像类型。`SeedMapExport` 复用同一坐标、颜色、网格、史莱姆
区块、结构和路径点模型生成离屏像素。

## 平台边界

当前 NuGet 包只发布 `runtimes/win-x64/native/rzero_seedmap.dll`。项目文件会在
收到其他 Runtime Identifier 时显式报错，避免 x86 或 ARM64 静默生成没有原生后端
的产物。消费端的多平台工程应把 SeedMap 功能限制到 Windows x64 配置。
