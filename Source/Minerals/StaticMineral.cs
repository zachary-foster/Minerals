
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions 
using Verse;         // RimWorld universal objects 

namespace Minerals
{
    /// <summary>
    /// Mineral class
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class StaticMineral : Mineable
    {

        // ======= Private Variables ======= //
        private float yieldPct = 0;

        // The current size of the mineral
        private float mySize = 0.05f;
        public float size
        {
            get
            {
                return mySize;
            }

            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 1)
                {
                    value = 1;
                }
                mySize = value;
            }
        }


        private float? myDistFromNeededTerrain = null;
        public float distFromNeededTerrain
        {
            get
            {
                if (myDistFromNeededTerrain == null) // not yet set
                {
                    myDistFromNeededTerrain = posDistFromNeededTerrain(this.attributes, this.Map, this.Position);
                }

                return (float)myDistFromNeededTerrain;
            }

            set
            {
                myDistFromNeededTerrain = value;
            }
        }


        public ThingDef_StaticMineral attributes
        {
            get
            {
                return this.def as ThingDef_StaticMineral;
            }
        }

        // ======= Spawning conditions ======= //


        public static bool CanSpawnAt(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            // Check that the terrain is ok
            if (! IsTerrainOkAt(myDef, map, position))
            {
                return false;
            }

            // Check that it is near any needed terrains
            if (! StaticMineral.isNearNeededTerrain(myDef, map, position))
            {
                return false;
            }

            // Check that it is under a roof if it needs to be
            if (! StaticMineral.isRoofConditionOk(myDef, map, position))
            {
                return false;
            }

            // Look for stuff in the way
            List<Thing> list = map.thingGrid.ThingsListAt(position);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.BlockPlanting)
                {
                    return false;
                }
                if (
                    myDef.passability == Traversability.Impassable &&
                    (
                        thing.def.category == ThingCategory.Pawn ||
                        thing.def.category == ThingCategory.Item ||
                        thing.def.category == ThingCategory.Building ||
                        thing.def.category == ThingCategory.Plant
                    ))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CanSpawnInBiome(ThingDef_StaticMineral myDef, Map map) 
        {
            return myDef.allowedBiomes.Any(map.Biome.defName.Contains);
        }

        public static bool IsTerrainOkAt(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            return myDef.allowedTerrains.Any(terrain.defName.Contains);
        }
                   
        public static bool isNearNeededTerrain(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            if (myDef.neededNearbyTerrains.Count == 0)
            {
                return true;
            }
            for (int xOffset = -(int)Math.Ceiling(myDef.neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(myDef.neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(myDef.neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(myDef.neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (myDef.neededNearbyTerrains.Any(terrain.defName.Contains))
                        {
                            if (position.DistanceTo(checkedPosition) <= myDef.neededNearbyTerrainRadius) 
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool isRoofConditionOk(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            if (myDef.mustBeUnderoof && (!position.Roofed(map)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // The distance a position is from a needed terrain type
        // A little slower than `isNearNeededTerrain` because all squares are checked
        public static float posDistFromNeededTerrain(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            float output = -1;

            for (int xOffset = -(int)Math.Ceiling(myDef.neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(myDef.neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(myDef.neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(myDef.neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (myDef.neededNearbyTerrains.Any(terrain.defName.Contains))
                        {
                            float distanceToPos = position.DistanceTo(checkedPosition);
                            if (output < 0 || output > distanceToPos) 
                            {
                                output = distanceToPos;
                            }
                        }
                    }
                }
            }

            return output;
        }

        // ======= Spawning individuals ======= //


        public static StaticMineral TrySpawnAt(IntVec3 dest, ThingDef_StaticMineral myDef, Map map)
        {
            if (StaticMineral.CanSpawnAt(myDef, map, dest))
            {
                return StaticMineral.SpawnAt(dest, myDef, map);
            }
            else
            {
                return null;
            }
        }

        public static StaticMineral SpawnAt(IntVec3 dest, ThingDef_StaticMineral myDef, Map map)
        {
            StaticMineral output = (StaticMineral)GenSpawn.Spawn(myDef, dest, map);
            map.mapDrawer.MapMeshDirty(dest, MapMeshFlag.Things);
            return output;
        }


        // ======= Reproduction ======= //



        public bool TryFindReproductionDestination(int radius, Map map, out IntVec3 foundCell)
        {
            Predicate<IntVec3> validator = (IntVec3 c) => this.Position.InHorDistOf(c, radius) && GenSight.LineOfSight(this.Position, c, map, true, null, 0, 0) && StaticMineral.IsTerrainOkAt(this.attributes, map, c);
            return CellFinder.TryFindRandomCellNear(this.Position, map, Mathf.CeilToInt(radius), validator, out foundCell);
        }

        public StaticMineral TryReproduce()
        {
            IntVec3 dest;
            if (! this.TryFindReproductionDestination(this.attributes.spawnRadius, this.Map, out dest))
            {
                return null;
            }
            return StaticMineral.TrySpawnAt(dest, this.attributes, this.Map);
        }


        // ======= Spawning clusters ======= //


        public static void SpawnCluster(Map map, IntVec3 position, ThingDef_StaticMineral myDef)
        {
            // Make a cluster center
            StaticMineral mineral = StaticMineral.TrySpawnAt(position, myDef, map);
            mineral.size = Rand.Range(0.3f,0.8f);

            // Pick cluster size
            int clusterSize = (int)Rand.Range(myDef.minClusterSize, myDef.maxClusterSize);

            // Grow cluster 
            GrowCluster(map, mineral, clusterSize);
        }


        public static void GrowCluster(Map map, StaticMineral sourceMineral, int times)
        {
            if (times > 0)
            {
                StaticMineral newGrowth = sourceMineral.TryReproduce();
                if (newGrowth != null)
                {
                    newGrowth.size = Rand.Range(0.95f, 1.05f) * sourceMineral.size;
                    GrowCluster(map, newGrowth, times - 1);

                }

            }
        }


        // ======= Map initialization ======= //


        public static void InitNewMap(Map map, ThingDef_StaticMineral myDef)
        {
            // Print to log

            // Check that it is a valid biome
            if (! StaticMineral.CanSpawnInBiome(myDef, map))
            {
                Log.Message("Minerals: " + myDef.defName + " cannot be added to this biome");
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(myDef.minClusterProbability, myDef.maxClusterProbability);
            Log.Message("Minerals: " + myDef.defName + " will be spawned at a probability of " + spawnProbability);

            // Find spots to spawn it
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (current.InBounds(map) && StaticMineral.CanSpawnAt(myDef, map, current) && Rand.Range(0f, 1f) < spawnProbability)
                {
                    StaticMineral.SpawnCluster(map, current, myDef);
                }
            }


        }

        // ======= Yeilding resources ======= //

        public void incPctYeild(int amount, Pawn miner)
        {
            this.yieldPct += (float)Mathf.Min(amount, this.HitPoints) / (float)base.MaxHitPoints * miner.GetStatValue(StatDefOf.MiningYield, true);
        }

        // hackish solution since I cant override Mineable.DestroyMined
        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            if (this.def.building.mineableThing != null && this.def.building.mineableYieldWasteable && dinfo.Def == DamageDefOf.Mining && dinfo.Instigator != null && dinfo.Instigator is Pawn)
            {
                this.incPctYeild(dinfo.Amount, (Pawn)dinfo.Instigator);
            }
            if (this.size < this.yieldPct)
            {
                dinfo.SetAmount(0);
            }
            base.PreApplyDamage(dinfo, out absorbed);
        }
            
        // ======= Appearance ======= //

        public override void Print(SectionLayer layer)
        {

            Rand.PushState();
            Rand.Seed = this.Position.GetHashCode();
            int numToPrint = Mathf.CeilToInt(this.size * (float)this.attributes.maxMeshCount);
            if (numToPrint < 1)
            {
                numToPrint = 1;
            }
            float sizeToPrint = this.attributes.visualSizeRange.LerpThroughRange(this.size);
            Vector3 trueCenter = this.TrueCenter();
            for (int i = 0; i < numToPrint; i++)
            {

                Vector3 center = trueCenter;
                center.y = this.attributes.Altitude;
                center.x += Rand.Range(- this.attributes.visualSpread, this.attributes.visualSpread);
                center.z += Rand.Range(- this.attributes.visualSpread, this.attributes.visualSpread);
                Material matSingle = this.Graphic.MatSingle;
                Vector2 thisSize = new Vector2(sizeToPrint, sizeToPrint);
                Material mat = matSingle;
                Printer_Plane.PrintPlane(layer, center, thisSize, mat, 0, Rand.Bool);
            }
//            if (this.attributes.graphicData.shadowData != null)
//            {
//                Vector3 center2 = a + this.attributes.graphicData.shadowData.offset * num2;
//                if (flag)
//                {
//                    center2.z = this.Position.ToVector3Shifted().z + this.attributes.graphicData.shadowData.offset.z;
//                }
//                center2.y -= 0.046875f;
//                Vector3 volume = this.attributes.graphicData.shadowData.volume * num2;
//                Printer_Shadow.PrintShadow(layer, center2, volume, Rot4.North);
//            }
            Rand.PopState();
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("PercentGrowth: " + this.size);
            return stringBuilder.ToString().TrimEndNewlines();
        }


    }       







    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_StaticMineral : ThingDef
    {
        // How far away it can spawn from an existing location
        // Even though it is a static mineral, the map initialization uses "reproduction" to make clusters 
        public int spawnRadius = 1; 

        // For a given map, the minimum/maximum probablility a cluster will spawn for every possible location
        public float minClusterProbability = 0.001f; 
        public float maxClusterProbability = 0.01f;

        // How  many squares each cluster will be
        public int minClusterSize = 1;
        public int maxClusterSize = 10;

        // The biomes this can appear in
        public List<string> allowedBiomes = new List<string> {};

        // The terrains this can appear on
        public List<string> allowedTerrains = new List<string> {};

        // The terrains this must be near to, but not necessarily on, and how far away it can be
        public List<string> neededNearbyTerrains = new List<string> {};
        public float neededNearbyTerrainRadius = 3f;

        // If true, growth rate and initial size depends on distance from needed terrains
        public bool neededNearbyTerrainSizeEffect = true;

        // If true, only grows under roofs
        public bool mustBeUnderoof = true;

        // The maximum number of images that will be printed per square
        public int maxMeshCount = 1;

        // The size range of images printed
        public FloatRange visualSizeRange  = new FloatRange(0.3f, 1.0f);
        public float visualSpread = 0.5f;

        // The amount of resource returned if the mineral is its maximum size
        public int maxMinedYeild = 10;
    }

}