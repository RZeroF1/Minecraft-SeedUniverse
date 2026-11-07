# Cubiomes Source and Build Information

- Upstream project: [Cubitect/cubiomes](https://github.com/Cubitect/cubiomes).
- Base revision: `e61f90580cbdd883214a8054670dacae655e59c0` (2024-11-10); applied the version-support changes from PR #162 at `ca9fcb4e2b4f29a88b15a19fe1f00a73bf503ec2`.
- License: MIT, Copyright (c) 2020 Cubitect. See `LICENSE` for the complete text.
- `native/cubiomes/upstream/` contains the upstream generation, finder, random number, naming, palette, and table files from the base revision plus the recorded PR #162 version-support changes. This project does not use viewer or GPL code.
- Local patch set `legacy46.1+modern43.1` corrects legacy mineshaft RNG, scattered-feature checks, village candidates, 1.8 monuments, 1.4.2 temple semantics, pre-1.13 End City predicates and distant-coordinate float remainder, 1.15–1.17 three-dimensional Voronoi access, 1.16.1 biome boundaries, 1.14 outpost rules, 1.18 depth boundaries, and modern stronghold preferences. Original Java resources and research data are not distributed.
- Runtime source identity: `ca9fcb4e2b4f29a88b15a19fe1f00a73bf503ec2+legacy46.1+modern43.1`. The suffix identifies this project's local adaptations, not an upstream Git revision.
- `manifest.json` records SHA-256 hashes of the adapted source files. Verify the manifest against the raw file bytes.
- `native/cubiomes/seedmap.c` and `seedmap.h` are the Native C ABI adapter written by RZeroF1 and licensed under the root MPL 2.0 project license. ABI 1 defines block coordinates, dimensions, capability queries, buffer limits, handle ownership, and error codes in the header.
- `native/cubiomes/build.cmd x64 <output>` uses the installed Visual Studio C++ tools with `/O2 /MT /LD /Brepro` and the Windows C calling convention. The native build does not access the network. A regular .NET build invokes this script and places `rzero_seedmap.dll` in the output directory.
- Java integer overflow semantics are required for correct Cubiomes behavior. Tests using fixed seeds, coordinates, and slime chunks verify the MSVC build. Ports to GCC or Clang must use `-fwrapv` and rerun the tests.
- The public catalog contains all 46 official Java releases from `1.2.5` through `1.12.2` and all 43 registered releases from `1.13.2` through `1.21.11`. Exact-release and feature verification status is maintained by the managed API. Formal `1.0.0`/`1.1.0` and 26.x are not exposed.
- Cubiomes predicts biomes and structure locations; it does not generate a complete block world. Biome and approximate terrain checks do not establish that every structure will generate in the game. Custom world generation, Bedrock, and upgraded worlds containing regions from different versions are outside the current capability table.
- The managed ABI uses only `DllImportSearchPath.AssemblyDirectory` to locate the Native DLL and does not register an assembly-wide global resolver.
