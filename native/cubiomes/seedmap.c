/* This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * Copyright (c) 2026 RZeroF1.
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
#include "seedmap.h"
#include "upstream/finders.h"
#include "upstream/util.h"
#include <stdlib.h>
#include <string.h>

static const cm_version versions[] = {
    {100,7,"1.2.5"},
    {101,7,"1.3.1"},
    {102,7,"1.3.2"},
    {103,7,"1.4.2"},
    {104,7,"1.4.4"},
    {105,7,"1.4.5"},
    {106,7,"1.4.6"},
    {107,7,"1.4.7"},
    {108,7,"1.5.1"},
    {109,7,"1.5.2"},
    {110,7,"1.6.1"},
    {111,7,"1.6.2"},
    {112,7,"1.6.4"},
    {113,7,"1.7.2"},
    {114,7,"1.7.3"},
    {115,7,"1.7.4"},
    {116,7,"1.7.5"},
    {117,7,"1.7.6"},
    {118,7,"1.7.7"},
    {119,7,"1.7.8"},
    {120,7,"1.7.9"},
    {121,7,"1.7.10"},
    {122,7,"1.8"},
    {123,7,"1.8.1"},
    {124,7,"1.8.2"},
    {125,7,"1.8.3"},
    {126,7,"1.8.4"},
    {127,7,"1.8.5"},
    {128,7,"1.8.6"},
    {129,7,"1.8.7"},
    {130,7,"1.8.8"},
    {131,7,"1.8.9"},
    {132,7,"1.9"},
    {133,7,"1.9.1"},
    {134,7,"1.9.2"},
    {135,7,"1.9.3"},
    {136,7,"1.9.4"},
    {137,7,"1.10"},
    {138,7,"1.10.1"},
    {139,7,"1.10.2"},
    {140,7,"1.11"},
    {141,7,"1.11.1"},
    {142,7,"1.11.2"},
    {143,7,"1.12"},
    {144,7,"1.12.1"},
    {145,7,"1.12.2"},
    {MC_1_21_5,7,"1.21.5"}, {MC_1_21_11,7,"1.21.11"}, {MC_1_21_WD,7,"1.21.4"},
    {MC_1_21_3,7,"1.21.3"}, {MC_1_21_1,7,"1.21.1"},
    {MC_1_20_6,7,"1.20.6"}, {MC_1_19_4,7,"1.19.4"},
    {MC_1_19_2,7,"1.19.2"}, {MC_1_18_2,7,"1.18.2"},
    {MC_1_17_1,7,"1.17.1"}, {MC_1_16_5,7,"1.16.5"},
    {MC_1_16_1,7,"1.16.1"}, {MC_1_15_2,7,"1.15.2"},
    {MC_1_14_4,7,"1.14.4"}, {MC_1_13_2,7,"1.13.2"}
};
/* Stable ABI release IDs are independent of Cubiomes enum values. */
static const int legacy_profiles[] = { MC_1_2, MC_1_3, MC_1_3, MC_1_4, MC_1_4, MC_1_4, MC_1_4, MC_1_4, MC_1_5, MC_1_5, MC_1_6, MC_1_6, MC_1_6, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_7, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_8, MC_1_9, MC_1_9, MC_1_9, MC_1_9, MC_1_9, MC_1_10, MC_1_10, MC_1_10, MC_1_11, MC_1_11, MC_1_11, MC_1_12, MC_1_12, MC_1_12 };
static int profile_version(int id) {
    return id >= 100 && id < 146 ? legacy_profiles[id-100] : id;
}
enum { RELEASE_1_4_2 = 103 };
static int valid_version(int mc) {
    for (int i=0;i<(int)(sizeof(versions)/sizeof(*versions));i++)
        if (versions[i].id==mc) return 1;
    return 0;
}
static int valid_type(int type) {
    switch (type) {
    case Desert_Pyramid: case Jungle_Temple: case Swamp_Hut: case Igloo:
    case Village: case Ocean_Ruin: case Shipwreck: case Monument: case Mansion:
    case Outpost: case Ruined_Portal: case Ruined_Portal_N: case Ancient_City:
    case Treasure: case Mineshaft: case Fortress: case Bastion: case End_City:
    case Trail_Ruins: case Trial_Chambers: return 1;
    default: return 0;
    }
}
static int64_t floor_div(int64_t a, int b) {
    int64_t q=a/b, r=a%b; return q-(r<0);
}
int cm_abi_version(void) { return 1; }
const char *cm_source_revision(void) { return "ca9fcb4e2b4f29a88b15a19fe1f00a73bf503ec2+legacy46.1"; }
int cm_versions(cm_version *out, int capacity) {
    int n=(int)(sizeof(versions)/sizeof(*versions));
    if (!out) return n;
    if (capacity<n) return CM_CAPACITY;
    memcpy(out,versions,sizeof(versions)); return n;
}
int cm_structures(int mc, int dim, cm_structure *out, int capacity) {
    if (!valid_version(mc) || dim < -1 || dim > 1) return CM_UNSUPPORTED;
    mc=profile_version(mc);
    int n=0;
    for(int type=0;type<FEATURE_NUM;type++) {
        StructureConfig c;
        if (!valid_type(type)) continue;
        if (type==Mineshaft) {
            if(dim!=DIM_OVERWORLD) continue;
            c.dim=DIM_OVERWORLD; c.regionSize=1;
        } else if(!getStructureConfig(type,mc,&c) || c.dim!=dim) continue;
        if (out) {
            if(n>=capacity) return CM_CAPACITY;
            out[n]=(cm_structure){type,dim,16*c.regionSize,1,struct2str(type)};
        }
        n++;
    }
    if(dim==DIM_OVERWORLD) {
        if(out) {
            if(n>=capacity) return CM_CAPACITY;
            out[n]=(cm_structure){100,dim,0,1,"stronghold"};
        }
        n++;
    }
    return n;
}
const char *cm_biome_name(int mc,int dim,int id) {
    if(!valid_version(mc)) return NULL;
    mc=profile_version(mc);
    if(mc<=MC_1_12 && dim==DIM_END && id!=the_end) return NULL;
    if(id<0 || id>255 || !biomeExists(mc,id) ||
        getDimension(id)!=dim || (dim==0 && !isOverworld(mc,id))) return NULL;
    return biome2str(mc,id);
}
uint32_t cm_biome_color(int id) {
    unsigned char colors[256][3];
    initBiomeColors(colors);
    if(id<0 || id>255) return 0xff555555;
    return 0xff000000u | (colors[id][0]<<16) | (colors[id][1]<<8) | colors[id][2];
}
int cm_create(int mc,uint64_t seed,int dim,void **out) {
    if(!out) return CM_INVALID;
    *out=NULL;
    if(!valid_version(mc) || dim < -1 || dim > 1) return CM_UNSUPPORTED;
    Generator *g=(Generator*)calloc(1,sizeof(Generator));
    if(!g) return CM_MEMORY;
    uint32_t flags = mc==RELEASE_1_4_2 ? LEGACY_TEMPLE_142 : 0;
    setupGenerator(g,profile_version(mc),flags); applySeed(g,dim,seed); *out=g; return CM_OK;
}
void cm_destroy(void *handle) { free(handle); }
int cm_biomes(void *handle,int64_t x,int64_t z,int w,int h,int step,int y,int32_t *out,int capacity) {
    Generator *g=(Generator*)handle;
    if(!g || !out || w<=0 || h<=0 || w>512 || h>512 || capacity<w*h) return CM_INVALID;
    if(step!=1 && step!=4 && step!=16 && step!=64 && step!=256) return CM_UNSUPPORTED;
    if(x%step || z%step || x < -30000000 || z < -30000000 ||
       x+(int64_t)(w-1)*step>=30000000 || z+(int64_t)(h-1)*step>=30000000 || y < -64 || y>320)
        return CM_RANGE;
    Range r={step,(int)(x/step),(int)(z/step),w,h,
             step==1?y:(int)floor_div(y,4),1};
    int *cache=allocCache(g,r);
    if(!cache) return CM_MEMORY;
    int rc=genBiomes(g,cache,r);
    if(!rc) {
        // Pre-1.13 End terrain categories are internal; vanilla exposes ID 9.
        if(g->mc<=MC_1_12 && g->dim==DIM_END)
            for(int i=0;i<w*h;i++) out[i]=the_end;
        else memcpy(out,cache,(size_t)w*h*sizeof(int32_t));
    }
    free(cache);
    return rc?CM_GENERATION:CM_OK;
}
int cm_structure_region(void *handle,int type,int rx,int rz,cm_hit *out) {
    Generator *g=(Generator*)handle;
    if(!g || !out) return CM_INVALID;
    if(!valid_type(type)) return CM_UNSUPPORTED;
    Pos pos;
    int flags=1;
    if(type==Mineshaft) {
        flags=0; /* Placement only: mine shafts have no biome viability test. */
        if(g->dim!=0) return CM_UNSUPPORTED;
        if(rx < -1875000 || rz < -1875000 || rx>=1875000 || rz>=1875000) return CM_RANGE;
        /* The implementation uses inclusive end chunk coordinates. */
        if(!getMineshafts(g->mc,g->seed,rx,rz,rx,rz,&pos,1)) return 0;
        /* getMineshafts outputs block coordinates. */
    } else {
        StructureConfig c;
        if(!getStructureConfig(type,g->mc,&c) || c.dim!=g->dim) return CM_UNSUPPORTED;
        int region=16*c.regionSize;
        if((int64_t)rx*region < -30000000-region || (int64_t)rx*region>=30000000 ||
           (int64_t)rz*region < -30000000-region || (int64_t)rz*region>=30000000) return CM_RANGE;
        if(!getStructurePos(type,g->mc,g->seed,rx,rz,&pos)) return 0;
        if(!isViableStructurePos(type,g,pos.x,pos.z,0)) return 0;
        if(!isViableStructureTerrain(type,g,pos.x,pos.z)) return 0;
        if(g->mc>=MC_1_18 && (type==Desert_Pyramid || type==Jungle_Temple || type==Mansion)) flags|=2;
        // The vanilla End City height predicate was added in 1.11.
        if(type==End_City && g->mc>=MC_1_11) {
            SurfaceNoise sn;
            initSurfaceNoise(&sn,DIM_END,g->seed);
            if(!isViableEndCityTerrain(g,&sn,pos.x,pos.z)) return 0;
            flags|=2;
        }
    }
    if(pos.x < -30000000 || pos.z < -30000000 || pos.x>=30000000 || pos.z>=30000000) return 0;
    *out=(cm_hit){type,pos.x,pos.z,flags};
    return 1;
}
int cm_strongholds(void *handle,cm_hit *out,int capacity) {
    Generator *g=(Generator*)handle;
    if(!g || !out) return CM_INVALID;
    if(g->dim!=DIM_OVERWORLD) return CM_UNSUPPORTED;
    if(capacity<128) return CM_CAPACITY;
    StrongholdIter sh;
    initFirstStronghold(&sh,g->mc,g->seed);
    int n=0, more;
    int max_count = g->mc < MC_1_9 ? 3 : 128;
    do {
        more=nextStronghold(&sh,g);
        out[n++]=(cm_hit){100,sh.pos.x,sh.pos.z,1};
    } while(more>0 && n<max_count);
    return n;
}
int cm_slime(uint64_t seed,int cx,int cz) {
    if(cx < -1875000 || cz < -1875000 || cx>=1875000 || cz>=1875000) return CM_RANGE;
    return isSlimeChunk(seed,cx,cz);
}
