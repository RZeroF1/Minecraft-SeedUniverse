# API 说明

## 世界与 Native 能力

| API | 用途 |
|---|---|
| `SeedMap.Versions` | 查询 Native 实际支持的 Java 版本。 |
| `SeedMap.Biomes(version, dimension)` | 查询群系 ID、key、中文名和颜色。 |
| `SeedMap.Structures(version, dimension)` | 查询结构 ID、key、名称和区域大小。 |
| `SeedMap.ParseSeed(text)` | 解析十进制 `long`；其他文本按 Java `String.hashCode()` 处理。 |
| `SeedMap.Create(world)` / `SeedMap.Generate(...)` | 使用 SafeHandle 管理的 generator 直接生成群系 ID。 |
| `SeedMap.QueryRegion(...)` / `SeedMap.Strongholds(...)` | 查询结构区域和要塞位置。 |
| `SeedMap.IsSlimeChunk(...)` | 查询史莱姆区块。 |

坐标使用 Minecraft block 坐标，边界为 `[-30_000_000, 30_000_000)`。`World`
使用版本 ID、维度和固定 Y 高度；版本 ID 应来自 `SeedMap.Versions`。

## 异步瓦片与搜索

```csharp
using var engine = new SeedMapEngine(world);
int[] tile = await engine.TileAsync(
    new SeedMapEngine.TileKey(Step: 4, X: 0, Z: 0),
    cancellationToken);

var result = await engine.QueryStructuresAsync(
    new SeedMap.Bounds(-2000, -2000, 2000, 2000),
    SeedMap.Structures(world.Version, world.Dimension),
    cancellationToken);
```

`SeedMapEngine` 提供固定大小瓦片、两个并发工作槽、取消、48 MiB LRU、结构区域
缓存和 `FindBiomeAsync`。结构区域过多时返回 `Limited=true`，消费端应提示用户
缩小范围或提高采样倍率。

## 相机和瓦片计划

```csharp
var camera = new SeedMapCamera(centerX: 0, centerZ: 0, pixelsPerBlock: .25);
var bounds = camera.VisibleBounds(viewportWidth: 1200, viewportHeight: 760);
camera.ZoomAt(1.25, anchorX: 600, anchorY: 380, 1200, 760);

int step = camera.SelectSamplingStep();
var visible = SeedMapTilePlanner.Visible(bounds, step);
var wanted = SeedMapTilePlanner.WithPrefetch(bounds, step, rings: 1);
var batch = SeedMapTilePlanner.OrderForLoading(
    wanted, visible, camera.CenterX, camera.CenterZ, maximum: 2);
```

`ZoomAt` 保持鼠标锚点下的世界坐标不变。`SelectSamplingStep` 返回
`1/4/16/64/256`。消费端负责把计划接到自己的任务生命周期和图像对象。

## 颜色、几何和 BGRA

```csharp
var biomes = SeedMap.Biomes(world.Version, world.Dimension)
    .ToDictionary(b => b.Id);
var selected = new HashSet<int> { 1, 4, 5 };
byte[] pixels = SeedMapPalette.ToBgra(
    tile, biomes, selected, hideUnselected: false, darkTheme: true);

double distance = SeedMapGeometry.Distance(0, 0, 300, 400);
SeedMap.Bounds chunk = SeedMapGeometry.ChunkBounds(10, -4);
```

BGRA 缓冲区按行优先排列，每个像素四个字节，顺序为 B、G、R、A。颜色值采用
`0xAARRGGBB`。`SeedMapGeometry.GridLines`、`Circle` 和 `ChunkBounds` 可用于
生成 Canvas、SVG、Skia 或其他渲染器使用的几何数据。

## 离屏导出

```csharp
var image = await SeedMapExport.RenderAsync(
    world,
    new SeedMap.Bounds(-1024, -1024, 1024, 1024),
    step: 4,
    colors: palette,
    profile: new SeedMapProfile { Seed = world.Seed },
    cancellationToken);
```

`Image.Pixels` 是 BGRA8；单次导出最多 16,777,216 像素。`StructuresLimited` 表示
结构密集区域有部分查询因工作量上限被省略，像素本身仍是有效的群系结果。

## Session

`SeedMapSession` 只定义通用 profile、历史、路径点和存储接口，不决定应用路径。
宿主可以使用 `FileSeedMapSessionStore`，也可以实现数据库、云同步或内存存储。
