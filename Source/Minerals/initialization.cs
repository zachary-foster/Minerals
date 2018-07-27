using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions 
using Verse;         // RimWorld universal objects 

namespace Minerals
{
    public static class mapBuilder
    {

        public static void initAll(Map map)
        {
            initStaticMinerals(map);
            Log.Message("Minerals loaded");
        }

        public static void initStaticMinerals(Map map)
        {
            foreach (ThingDef_StaticMineral mineralType in DefDatabase<ThingDef_StaticMineral>.AllDefs)
            {
                mineralType.InitNewMap(map);
            }
        }

    }


}
