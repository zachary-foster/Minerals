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

            MethodInfo targetmethod = AccessTools.Method(typeof(GenStep_RockChunks),"Generate");

            // find the static method to call before (i.e. Prefix) the targetmethod
            HarmonyMethod postfixmethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("initNewMap"));

            harmony.Patch(targetmethod, null, postfixmethod) ;
        }

        public static void initNewMap(GenStep_RockChunks __instance, Map map) {
            mapBuilder.initAll(map);
        }
    }
}