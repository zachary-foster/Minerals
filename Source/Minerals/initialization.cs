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
            if (MineralsMain.Settings.removeStartingChunksSetting)
            {
                removeStartingChunks(map);
            }
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

        public static void removeStartingChunks(Map map)
        {
            string[] toRemove = {"ChunkSandstone", "ChunkGranite", "ChunkLimestone", "ChunkSlate", "ChunkMarble", "ChunkLava", "Filth_RubbleRock"};
            List<Thing> thingsToCheck = map.listerThings.AllThings;
            for (int i = thingsToCheck.Count - 1; i >= 0; i--)
            {
                if (toRemove.Any(thingsToCheck[i].def.defName.Equals))
                {
                    thingsToCheck[i].Destroy(DestroyMode.Vanish);
                }
            }
        }

    }
}
