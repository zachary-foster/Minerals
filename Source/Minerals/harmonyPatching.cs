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
            harmony.Patch(targetmethod, null, postfixmethod) ;

//            // modify NPS frost
//            MethodInfo frostMethod = AccessTools.Method(Type.GetType("NPS.FrostGrid"), "CheckVisualOrPathCostChange");
//            HarmonyMethod newFrostMethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("myCheckVisualOrPathCostChange"));
//            harmony.Patch(frostMethod, null, null, newFrostMethod) ;

        }

        public static void initNewMap(GenStep_RockChunks __instance, Map map) {
            mapBuilder.initAll(map);
        }

//        public static void myCheckVisualOrPathCostChange(MapComponent __instance, IntVec3 c, float oldDepth, float newDepth) {
//        }
    }
}