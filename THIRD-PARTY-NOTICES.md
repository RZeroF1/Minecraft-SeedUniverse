# 第三方声明

## Cubiomes

本项目使用 Cubitect 的 Cubiomes，固定修订版为 `e61f90580cbdd883214a8054670dacae655e59c0`。Cubiomes 提供 Minecraft Java 的群系、结构和随机生成计算能力，继续采用上游 MIT 许可证并保留 Cubitect 的版权声明。

固定的上游源码位于 `native/cubiomes/upstream/`；许可证、来源说明和文件哈希清单位于 `licenses/cubiomes/`。

`native/cubiomes/seedmap.c` 和 `seedmap.h` 是 RZeroF1 为本项目编写的 Native C ABI 适配层。适配层只负责定义边界、参数和资源生命周期。
