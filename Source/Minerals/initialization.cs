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
            initLiveMinerals(map);
            Log.Message("Minerals loaded");
        }

        public static void initLiveMinerals(Map map)
        {
            spawnColdstone(map);
            spawnGlowstone(map);
            spawnSalt(map);
            foreach (ThingDef_StaticMineral mineralType in Verse.DefDatabase<ThingDef_StaticMineral>.AllDefs)
            {
                object[] args = { map, mineralType };
                mineralType.thingClass.GetMethod("InitNewMap").Invoke(null, args);
            }
        }

        public static void spawnColdstone(Map map)
        {
            ThingDef_ColdstoneCrystal thingDef = (ThingDef_ColdstoneCrystal)ThingDef.Named("ColdstoneCrystal");

            // Check that it is a valid biome
            if (ColdstoneCrystal.CanBiomeSuppoprtPlantAt(thingDef, map) == false)
            {
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(thingDef.minClusterPorbability, thingDef.maxClusterPorbability);

            // Find spots to spawn it
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (current.InBounds(map) && ColdstoneCrystal.CanTerrainSupportPlantAt(thingDef, map, current) && Rand.Range(0f, 1f) < spawnProbability)
                {
                    ColdstoneCrystal.spawnColdstoneCluster(map, current);
                 }
            }
           
        }

        public static void spawnGlowstone(Map map)
        {
            ThingDef_GlowstoneCrystal thingDef = (ThingDef_GlowstoneCrystal)ThingDef.Named("GlowstoneCrystal");

            // Check that it is a valid biome
            if (GlowstoneCrystal.CanBiomeSuppoprtPlantAt(thingDef, map) == false)
            {
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(thingDef.minClusterPorbability, thingDef.maxClusterPorbability);

            // Find spots to spawn it
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (current.InBounds(map) && GlowstoneCrystal.CanTerrainSupportPlantAt(thingDef, map, current) && Rand.Range(0f, 1f) < spawnProbability)
                {
                    GlowstoneCrystal.spawnGlowstoneCluster(map, current);
                }
            }

        }

    
        public static void spawnSalt(Map map)
        {
            ThingDef_SaltCrystal thingDef = (ThingDef_SaltCrystal)ThingDef.Named("SaltCrystal");

            // Check that it is a valid biome
            if (SaltCrystal.CanBiomeSuppoprtPlantAt(thingDef, map) == false)
            {
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(thingDef.minClusterPorbability, thingDef.maxClusterPorbability);

            // Find spots to spawn it
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (current.InBounds(map) && SaltCrystal.CanTerrainSupportPlantAt(thingDef, map, current) && Rand.Range(0f, 1f) < spawnProbability)
                {
                    SaltCrystal.spawnSaltCluster(map, current);
                }
            }

        }
    }


}
