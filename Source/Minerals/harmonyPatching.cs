using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions 
using Verse;         // RimWorld universal objects 
using RimWorld.Planet;

namespace Minerals
{



    /* Replace slate with basalt in the Alpha biomes Pyroclastic Conflagration biome*/
    [HarmonyPatch(typeof(World))]
    [HarmonyPatch("NaturalRockTypesIn")]
    public static class Minerals_World_NaturalRockTypesIn_Patch
    {

        [HarmonyPostfix]
        public static void MakeRocksAccordingToBiome(int tile, ref World __instance, ref IEnumerable<ThingDef> __result)
        {
            if (__instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_PyroclasticConflagration"))
            {
                List<ThingDef> replacedList = new List<ThingDef>();
                ThingDef item = DefDatabase<ThingDef>.GetNamed("AB_Obsidianstone");
                replacedList.Add(item);
                replacedList.Add(DefDatabase<ThingDef>.GetNamed("ZF_BasaltBase"));

                __result = replacedList;
            }
            else if (__instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_OcularForest") || __instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_GallatrossGraveyard") || __instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_GelatinousSuperorganism") || __instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_MechanoidIntrusion") || __instance.grid.Tiles.ToList()[tile].Biomes.Any(b => b.defName == "AB_RockyCrags")) {
                return;
            }
            else
            {
                // Pick a set of random rocks
                Rand.PushState();
                Rand.Seed = tile;
                List<ThingDef> list = (from d in DefDatabase<ThingDef>.AllDefs
                                       where d.category == ThingCategory.Building && d.building.isNaturalRock && !d.building.isResourceRock &&
                                       !d.IsSmoothed && d.defName != "GU_RoseQuartz" && d.defName != "AB_SlimeStone" &&
                                       d.defName != "GU_AncientMetals" && d.defName != "AB_Cragstone" && d.defName != "AB_Obsidianstone" &&
                                       d.defName != "BiomesIslands_CoralRock" && d.defName != "LavaRock" && d.defName != "AB_Mudstone"
                                       select d).ToList<ThingDef>();
                int num = Rand.RangeInclusive(MineralsMain.Settings.terrainCountRangeSetting.min, MineralsMain.Settings.terrainCountRangeSetting.max);
                if (num > list.Count)
                {
                    num = list.Count;
                }
                List<ThingDef> list2 = new List<ThingDef>();
                for (int i = 0; i < num; i++)
                {
                    ThingDef item = list.RandomElement<ThingDef>();
                    list.Remove(item);
                    list2.Add(item);
                }
                Rand.PopState();
                __result = list2;
            }
        }
    }



    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        // this static constructor runs to create a HarmonyInstance and install a patch.
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("com.zacharyfoster.minerals");

            // Spawn rocks on map generation
            MethodInfo targetmethod = AccessTools.Method(typeof(GenStep_RockChunks), "Generate");
            HarmonyMethod postfixmethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("initNewMapRocks"));
            harmony.Patch(targetmethod, null, postfixmethod);

            // Spawn ice after plants
            MethodInfo icetargetmethod = AccessTools.Method(typeof(GenStep_Plants), "Generate");
            HarmonyMethod icepostfixmethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("initNewMapIce"));
            harmony.Patch(icetargetmethod, null, icepostfixmethod);


            harmony.PatchAll();


        }

        public static void initNewMapRocks(GenStep_RockChunks __instance, Map map)
        {
            mapBuilder.initRocks(map);
        }

        public static void initNewMapIce(GenStep_RockChunks __instance, Map map)
        {
            mapBuilder.initIce(map);
        }
    }
}
