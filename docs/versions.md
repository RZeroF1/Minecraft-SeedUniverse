# Minecraft Java 发行版本与验证状态

当前源码目录包含 89 个发行版本条目。1.13.2–1.21.11 的 43 个发行版和 1.2.5–1.12.2 的 46 个发行版都使用各自官方 SHA-1 校验客户端完成了独立原版 Java Oracle 对照；消费端运行 SDK 无需 Java 或游戏 JAR。

`Supported` 针对对应能力与 DEFAULT 世界参数；结构查询返回已验证的原版候选和位置谓词阶段。`Experimental` 表示已执行精确 JAR 对照，但仍有版本差异、阶段限制或未完成的生产等价实现。`Api.Biomes` 是 DEFAULT 可生成群系目录，省略原版注册表中未用于该世界类型的条目。原版完整地形、结构方块落地、升级世界和自定义生成参数不由候选等级覆盖。1.2.5、1.3.1、1.3.2 的强要塞在独立种子基准通过，任意 IntCache 历史状态仍标为 `Experimental`。1.18 及以后群系保留 Java `ParameterList` 等距离选择差异的实验性状态。

## 1.13.2–1.21.11

| 发行版本 | 稳定 Native ID | 群系 | 结构候选 | 维度 | 强要塞 | 原版验证 |
| --- | ---: | --- | --- | --- | --- | --- |
| 1.13.2 | 16 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.14 | 147 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.14.1 | 17 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.14.2 | 17 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.14.3 | 17 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.14.4 | 17 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.15 | 18 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.15.1 | 18 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.15.2 | 18 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16 | 19 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16.1 | 19 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16.2 | 20 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16.3 | 20 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16.4 | 20 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.16.5 | 20 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.17.1 | 21 | Supported | Supported | Supported | Supported | GameJarAlgorithms |
| 1.18 | 148 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.18.1 | 148 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.18.2 | 22 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.19 | 23 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.19.1 | 23 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.19.2 | 23 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.19.3 | 24 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.19.4 | 24 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.1 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.2 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.3 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.4 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.5 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.20.6 | 25 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21 | 26 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.1 | 26 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.2 | 27 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.3 | 27 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.4 | 28 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.5 | 146 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.6 | 146 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.7 | 146 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.8 | 146 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.9 | 29 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.10 | 29 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |
| 1.21.11 | 29 | Experimental | Experimental | Supported | Supported | GameJarAlgorithms |

1.18 及以后仍保留 Experimental 的原因是原版 `Climate.ParameterList` 在等距离参数上的列表顺序与当前固定 Native 二叉树的选择顺序存在 225 个固定矩阵差异；原版气候值本身已逐点核对，结构 generation-point 阶段另有独立记录。结构候选、地形和完整方块生成分别记录，不因群系或 placement 相同而自动合并。

| 发行版本 | 稳定 Native ID | 群系／维度 | 结构候选 | 强要塞 | 完整方块生成 |
| --- | --- | --- | --- | --- | --- |
| 1.2.5 | 100 | Supported | Supported | Experimental | Unsupported |
| 1.3.1 | 101 | Supported | Supported | Experimental | Unsupported |
| 1.3.2 | 102 | Supported | Supported | Experimental | Unsupported |
| 1.4.2 | 103 | Supported | Supported | Supported | Unsupported |
| 1.4.4 | 104 | Supported | Supported | Supported | Unsupported |
| 1.4.5 | 105 | Supported | Supported | Supported | Unsupported |
| 1.4.6 | 106 | Supported | Supported | Supported | Unsupported |
| 1.4.7 | 107 | Supported | Supported | Supported | Unsupported |
| 1.5.1 | 108 | Supported | Supported | Supported | Unsupported |
| 1.5.2 | 109 | Supported | Supported | Supported | Unsupported |
| 1.6.1 | 110 | Supported | Supported | Supported | Unsupported |
| 1.6.2 | 111 | Supported | Supported | Supported | Unsupported |
| 1.6.4 | 112 | Supported | Supported | Supported | Unsupported |
| 1.7.2 | 113 | Supported | Supported | Supported | Unsupported |
| 1.7.3 | 114 | Supported | Supported | Supported | Unsupported |
| 1.7.4 | 115 | Supported | Supported | Supported | Unsupported |
| 1.7.5 | 116 | Supported | Supported | Supported | Unsupported |
| 1.7.6 | 117 | Supported | Supported | Supported | Unsupported |
| 1.7.7 | 118 | Supported | Supported | Supported | Unsupported |
| 1.7.8 | 119 | Supported | Supported | Supported | Unsupported |
| 1.7.9 | 120 | Supported | Supported | Supported | Unsupported |
| 1.7.10 | 121 | Supported | Supported | Supported | Unsupported |
| 1.8 | 122 | Supported | Supported | Supported | Unsupported |
| 1.8.1 | 123 | Supported | Supported | Supported | Unsupported |
| 1.8.2 | 124 | Supported | Supported | Supported | Unsupported |
| 1.8.3 | 125 | Supported | Supported | Supported | Unsupported |
| 1.8.4 | 126 | Supported | Supported | Supported | Unsupported |
| 1.8.5 | 127 | Supported | Supported | Supported | Unsupported |
| 1.8.6 | 128 | Supported | Supported | Supported | Unsupported |
| 1.8.7 | 129 | Supported | Supported | Supported | Unsupported |
| 1.8.8 | 130 | Supported | Supported | Supported | Unsupported |
| 1.8.9 | 131 | Supported | Supported | Supported | Unsupported |
| 1.9 | 132 | Supported | Supported | Supported | Unsupported |
| 1.9.1 | 133 | Supported | Supported | Supported | Unsupported |
| 1.9.2 | 134 | Supported | Supported | Supported | Unsupported |
| 1.9.3 | 135 | Supported | Supported | Supported | Unsupported |
| 1.9.4 | 136 | Supported | Supported | Supported | Unsupported |
| 1.10 | 137 | Supported | Supported | Supported | Unsupported |
| 1.10.1 | 138 | Supported | Supported | Supported | Unsupported |
| 1.10.2 | 139 | Supported | Supported | Supported | Unsupported |
| 1.11 | 140 | Supported | Supported | Supported | Unsupported |
| 1.11.1 | 141 | Supported | Supported | Supported | Unsupported |
| 1.11.2 | 142 | Supported | Supported | Supported | Unsupported |
| 1.12 | 143 | Supported | Supported | Supported | Unsupported |
| 1.12.1 | 144 | Supported | Supported | Supported | Unsupported |
| 1.12.2 | 145 | Supported | Supported | Supported | Unsupported |

## 实际结构能力

每版只列出原版存在的结构：全部版本有矿井、村庄、Nether Fortress 和强要塞；神殿从本轮的 1.3.1 起验证，女巫小屋从 1.4.2 起验证，Monument 从 1.8 起验证，Igloo／End City 从 1.9 起验证，Mansion 从 1.11 起验证。查询 `Api.GetVersionCapability(release).StructureCapabilities` 获取精确结构清单和各阶段等级。

End City 的 1.9–1.10.2 位置谓词没有高度过滤；1.11–1.12.2 复现原版 ChunkPrimer 索引和旋转 seed 的行为。仅这一位置谓词所需的高度检查被验证；完整末地地形及末地城方块放置仍未声明支持。Mansion 的支持同样只到 canSpawn 群系及位置谓词。

## 算法 Profile

群系实现分为四组：1.2.5；1.3.1–1.6.4；1.7.2–1.8.9 与 1.11–1.12.2；1.9–1.10.2。1.9／1.10 的 birch mutation 中间层与前后版本不同，不能合并。结构另选 Profile：1.4.2 的 quart 判定与组件分类、1.8 的 Monument、1.11 开始的末地城高度规则均有独立边界。

Profile 共享不合并发行身份。46 个发行版本分别保留稳定 Native ID、验证等级和独立世界缓存；旧有公开 ID 不变。`BiomeProfileKey`、`StructureProfileKey` 是算法标识，不用作发行版本名称或跨世界缓存键。

## 使用能力目录

```csharp
var capability = RZeroF1.Minecraft.SeedMap.Api.GetVersionCapability("1.12.2");
Console.WriteLine(capability.Verification); // GameJarAlgorithms
Console.WriteLine(capability.Strongholds);
foreach (var structure in capability.StructureCapabilities)
    Console.WriteLine($"{structure.Key}: candidate={structure.Candidates}, full={structure.FullGeneration}");
```

范围外的正式 1.0.0／1.1.0、26.x、Bedrock、自定义世界参数和混合版本存档没有在本轮新增。原版资源、映射和研究工具保留为内部验证材料，不随源码包或 NuGet 分发。
