/* This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * Copyright (c) 2026 RZeroF1.
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
#pragma once
#include <stdint.h>

#ifdef _WIN32
#define CM_API __declspec(dllexport)
#else
#define CM_API __attribute__((visibility("default")))
#endif

/*
 * RZeroF1 seed map ABI 1. All functions are cdecl. Handles own a Generator;
 * never use a handle concurrently, and destroy it after all calls finish.
 * No caller buffer is retained. UTF-8 names are static, process-lifetime strings.
 * Coordinates are blocks, X=east, Z=south, bounds are [min,max).
 * Tiles use aligned block origins, step={1,4,16,64,256}, row-major int32 IDs.
 * Y is always blocks (the wrapper performs Cubiomes' vertical scale conversion).
 * World bounds are [-30,000,000,30,000,000); calls outside fail with CM_RANGE.
 * Ordered cm_biome_sample additionally takes caller-owned search state. Calls
 * are synchronous and bounded; cancellation belongs between calls/tiles.
 */
enum { CM_OK=0, CM_INVALID=-1, CM_UNSUPPORTED=-2, CM_RANGE=-3,
       CM_MEMORY=-4, CM_GENERATION=-5, CM_CAPACITY=-6 };
typedef struct { int32_t id, dimensions; const char *key; } cm_version;
typedef struct { int32_t id, dimension, region_blocks, flags; const char *key; } cm_structure;
typedef struct { int32_t type, x, z, flags; } cm_hit;

CM_API int cm_abi_version(void);
CM_API const char *cm_source_revision(void);
CM_API int cm_versions(cm_version *out, int capacity);
CM_API int cm_structures(int version, int dimension, cm_structure *out, int capacity);
CM_API const char *cm_biome_name(int version, int dimension, int id);
CM_API uint32_t cm_biome_color(int id);
CM_API int cm_create(int version, uint64_t seed, int dimension, void **out);
CM_API void cm_destroy(void *handle);
CM_API int cm_biomes(void *handle, int64_t x, int64_t z, int width, int height,
                     int step, int y, int32_t *out, int capacity);
/* Ordered vanilla biome-source sampling. Initialize search_state to zero and
 * retain it only for this handle's sequence. A failed call leaves it unchanged.
 * cm_biomes remains independent of search_state and of previous queries. */
CM_API int cm_biome_sample(void *handle, int64_t x, int64_t z, int step, int y,
                          uint64_t *search_state, int32_t *out);
/* One placement region per call; mine shafts use 16x16 block regions.
 * Strongholds use cm_strongholds. Result flags: 1=biome predicate checked,
 * 2=approximate terrain checked, 4=exact End City terrain checked. A
 * missing flag means that stage was not performed. No flag implies
 * block-world certainty. */
CM_API int cm_structure_region(void *handle, int type, int region_x, int region_z, cm_hit *out);
CM_API int cm_strongholds(void *handle, cm_hit *out, int capacity);
CM_API int cm_slime(uint64_t seed, int chunk_x, int chunk_z);
