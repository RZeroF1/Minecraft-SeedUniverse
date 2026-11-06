# Cubiomes Source and Build Information

- Upstream project: [Cubitect/cubiomes](https://github.com/Cubitect/cubiomes).
- Pinned revision: `e61f90580cbdd883214a8054670dacae655e59c0` (2024-11-10).
- License: MIT, Copyright (c) 2020 Cubitect. See `LICENSE` for the complete text.
- `native/cubiomes/upstream/` contains the unmodified generation, finder, random number, naming, palette, and table files from this revision. This project does not use viewer or GPL code.
- `manifest.json` records SHA-256 hashes of the upstream Git objects with LF line endings. Verify the manifest against the raw file bytes.
- `native/cubiomes/seedmap.c` and `seedmap.h` are the Native C ABI adapter written by RZeroF1 and licensed under the root MPL 2.0 project license. ABI 1 defines block coordinates, dimensions, capability queries, buffer limits, handle ownership, and error codes in the header.
- `native/cubiomes/build.cmd x64 <output>` uses the installed Visual Studio C++ tools with `/O2 /MT /LD /Brepro` and the Windows C calling convention. The native build does not access the network. A regular .NET build invokes this script and places `rzero_seedmap.dll` in the output directory.
- Java integer overflow semantics are required for correct Cubiomes behavior. Tests using fixed seeds, coordinates, and slime chunks verify the MSVC build. Ports to GCC or Clang must use `-fwrapv` and rerun the tests.
- The public capability table explicitly exposes the supported Java versions between `1.16.5` and `1.21.3`. The unnumbered Winter Drop enum is not exposed.
- Cubiomes predicts biomes and structure locations; it does not generate a complete block world. Biome and approximate terrain checks do not establish that every structure will generate in the game. Custom world generation, Bedrock, and upgraded worlds containing regions from different versions are outside the current capability table.
- The managed ABI uses only `DllImportSearchPath.AssemblyDirectory` to locate the Native DLL and does not register an assembly-wide global resolver.
