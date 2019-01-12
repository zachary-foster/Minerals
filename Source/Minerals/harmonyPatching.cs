using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions 
using Verse;         // RimWorld universal objects 

namespace Minerals
{

    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        // this static constructor runs to create a HarmonyInstance and install a patch.
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.Minerals");

            // Spawn rocks on map generation
            MethodInfo targetmethod = AccessTools.Method(typeof(GenStep_RockChunks), "Generate");
            HarmonyMethod postfixmethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("initNewMap"));
            harmony.Patch(targetmethod, null, postfixmethod);


            harmony.PatchAll();


        }

        public static void initNewMap(GenStep_RockChunks __instance, Map map) {
            mapBuilder.initAll(map);
        }


        [HarmonyPatch(typeof(SK.SkyfallerUtil))]
        [HarmonyPatch("SpawnSkyfaller")]
        [HarmonyPatch(new Type[] {typeof(ThingDef), typeof(IEnumerable<Thing>), typeof(IntVec3), typeof(Map) })]
        static class ImpactPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref IEnumerable<Thing> things)
            {
                List<Thing> replacementList = new List<Thing>();
                foreach (Thing item in things)
                {
                    Thing toReturn = item;
                    if (item.def.mineable && (! StaticMineral.isMineral(item)))
                    {
                        // check if any of the minerals replace this one 
                        foreach (ThingDef_StaticMineral mineralType in DefDatabase<ThingDef_StaticMineral>.AllDefs)
                        {
                            if (mineralType.ThingsToReplace == null || mineralType.ThingsToReplace.Count == 0 || mineralType.replaceAll == false)
                            {
                                continue;
                            }

                            if (mineralType.ThingsToReplace.Any(item.def.defName.Equals))
                            {
                                toReturn = (StaticMineral) ThingMaker.MakeThing(mineralType);
                                break;
                            }
                        }

                    }
                    replacementList.Add(toReturn);
                }
                things = replacementList;
            }
        }

    }
}