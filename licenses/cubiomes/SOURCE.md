# Cubiomes 来源与构建说明

- 上游项目：[Cubitect/cubiomes](https://github.com/Cubitect/cubiomes)。
- 固定修订版：`e61f90580cbdd883214a8054670dacae655e59c0`（2024-11-10）。
- 许可证：MIT，Copyright (c) 2020 Cubitect；完整文本见 `LICENSE`。
- `native/cubiomes/upstream/` 保存此修订版中未修改的生成、查找器、随机数、命名、
  调色板和表文件；本项目没有使用 viewer 或 GPL 代码。
- `manifest.json` 按固定上游 Git 对象的 LF 字节记录文件 SHA-256。清单校验应在
  读取文件原始字节后进行。
- `native/cubiomes/seedmap.c/.h` 是 RZeroF1 编写的 Native C ABI 适配层，采用根目录
  MPL 2.0 项目许可证。ABI 1 在头文件中定义 block 坐标、维度、能力查询、缓冲区
  上限、句柄所有权和错误码。
- `native/cubiomes/build.cmd x64 <output>` 使用已安装的 Visual Studio C++ 工具，
  参数为 `/O2 /MT /LD /Brepro`，使用 Windows C calling convention；构建过程不
  访问网络。普通 .NET 构建会调用它，并将 `rzero_seedmap.dll` 放到输出目录。
- Java 整数溢出语义是 Cubiomes 正确工作的前提。固定 seed、坐标和史莱姆区块测试
  会校验 MSVC 构建结果；移植到 GCC 或 Clang 时需要使用 `-fwrapv` 并重新运行测试。
- 对外能力表显式暴露 Java `1.16.5` 到 `1.21.3` 中当前支持的版本；未编号的
  Winter Drop enum 不对外暴露。
- Cubiomes 预测群系和结构位置，不生成完整方块世界。群系和近似地形检查不能证明
  每个结构一定会在游戏中生成；自定义世界生成、Bedrock 和升级后的混合版本存档
  不在当前能力表内。
- Managed ABI 只使用 `DllImportSearchPath.AssemblyDirectory` 查找 Native DLL，
  不注册程序集级别的全局 resolver。
