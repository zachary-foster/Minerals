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
    public class SaltCrystal : Plant
    {
        public float myDistToWater = -1f;


        public ThingDef_SaltCrystal attributes
        {
            get
            {
                return this.def as ThingDef_SaltCrystal;
            }
        }


        public static float distFromWater(ThingDef_SaltCrystal myDef, Map map, IntVec3 position)
        {
            float output = -1;

            for (int xOffset = -myDef.waterOffsetRadius; xOffset <= myDef.waterOffsetRadius; xOffset++)
            {
                for (int zOffset = -myDef.waterOffsetRadius; zOffset <= myDef.waterOffsetRadius; zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (myDef.needsToBeNearTerrains.Any(terrain.defName.Contains))
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


        public static bool isNearWater(ThingDef_SaltCrystal myDef, Map map, IntVec3 position)
        {
            for (int xOffset = -myDef.waterOffsetRadius; xOffset <= myDef.waterOffsetRadius; xOffset++)
            {
                for (int zOffset = -myDef.waterOffsetRadius; zOffset <= myDef.waterOffsetRadius; zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (myDef.needsToBeNearTerrains.Any(terrain.defName.Contains))
                        {
                            if (position.DistanceTo(checkedPosition) <= myDef.waterOffsetRadius) 
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        public float temperatureGrowthRateFactor
        {
            get
            {
                float temperature = this.Position.GetTemperature(this.Map);
                if (temperature > this.attributes.maxStableTemperature) // melts if too hot
                {
                    return - this.attributes.meltRate * (temperature - this.attributes.maxStableTemperature);
                } 
                else if (temperature < this.attributes.minGrowTemperature || temperature > this.attributes.maxGrowTemperature)
                {
                    return 0f;
                } 
                else if (temperature < this.attributes.idealGrowTemperature) // grows slower in extreme cold
                {
                    return 1f - ((this.attributes.idealGrowTemperature - temperature) / (this.attributes.idealGrowTemperature - this.attributes.minGrowTemperature));
                }
                else // grows slower when too warm
                {
                    return 1f - ((temperature - this.attributes.idealGrowTemperature) / (this.attributes.maxGrowTemperature - this.attributes.idealGrowTemperature));
                }

            }
        }


        public float weatherGrowthRateFactor
        {
            get
            {
                if (this.Position.Roofed(this.Map) || this.Map.weatherManager.curWeather.rainRate < 0.05)
                {
                    return 1;
                }
                else 
                {
                    return -1 * this.Map.weatherManager.curWeather.rainRate * this.attributes.dissolveRate;
                }
            }
        }


        public float sizeGrowthRateFactor
        {
            get
            {
                if (this.growthInt >= 1)
                {
                    return 0.01f; // Needs to be above 0 to dissolve
                }
                else
                {
                    return (1.1f - this.growthInt) / 1.1f;
                }
            }
        }


        public float lightGrowthRateFactor
        {
            get
            {
                float light = this.Map.glowGrid.GameGlowAt(this.Position);
                if (light >= this.attributes.minGrowLight)
                {
                    return 1 + light * this.attributes.lightBoostFactor;
                }
                else
                {
                    return 0f;
                }
            }
        }

        public float waterOffsetRateFactor
        {
            get
            {
                if (this.attributes.waterOffsetGrowthEffect)
                {
                    if (this.myDistToWater >= 0 && this.myDistToWater <= this.attributes.waterOffsetRadius)
                    {
                        return (float)Math.Pow((this.attributes.waterOffsetRadius + 1 - this.myDistToWater) / this.attributes.waterOffsetRadius, 3d);
                    }
                    else
                    {
                        return 0f;
                    }

                }
                else
                {
                    return 1f;
                }
            }
        }

        public override float GrowthRate
        {
            get
            {
                if (this.temperatureGrowthRateFactor < 0) // If melting from heat
                {
                    return this.temperatureGrowthRateFactor;
                }
                else if (this.weatherGrowthRateFactor < 0) // If melting from rain
                {
                    return this.weatherGrowthRateFactor * this.sizeGrowthRateFactor * this.temperatureGrowthRateFactor;
                } else // growing normally
                {
                    return this.temperatureGrowthRateFactor * this.sizeGrowthRateFactor * this.lightGrowthRateFactor * this.weatherGrowthRateFactor * this.waterOffsetRateFactor;
                }
            }
        }

        public new float GrowthPerTick
        {
            get
            {
                float growthPerTick = (1f / (GenDate.TicksPerDay * this.def.plant.growDays));
                return growthPerTick * this.GrowthRate;
            }
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("PercentGrowth".Translate(new object[]
                {
                    this.GrowthPercentString
                }));
            stringBuilder.AppendLine("GrowthRate".Translate() + ": " + this.GrowthRate.ToStringPercent());    
            //stringBuilder.AppendLine("myDistToWater".Translate() + ": " + this.myDistToWater);         
            //stringBuilder.AppendLine("waterOffsetRateFactor".Translate() + ": " + this.waterOffsetRateFactor);         
            //stringBuilder.AppendLine("lightGrowthRateFactor".Translate() + ": " + this.lightGrowthRateFactor);             
            //stringBuilder.AppendLine("weatherGrowthRateFactor".Translate() + ": " + this.weatherGrowthRateFactor);
            //stringBuilder.AppendLine("sizeGrowthRateFactor".Translate() + ": " + this.sizeGrowthRateFactor);

            if (this.growthInt < 0)
            {
                stringBuilder.AppendLine("Dissolving".Translate());
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        //        
        //        
        //        // ===================== Setup Work =====================
        //
        //        /// <summary>
        //        /// Save and load internal state variables (stored in savegame data).
        //        /// </summary>
        //        public override void ExposeData()
        //        {
        //            base.ExposeData();
        //            //Scribe_References.Look<Cluster>(ref this.cluster, "cluster");
        //        }
        //
        //
        // ===================== Main Work Function =====================
        public override void TickLong()
        {

            bool plantWasAlreadyMature = (this.LifeStage == PlantLifeStage.Mature);
            this.growthInt += this.GrowthPerTick * GenTicks.TickLongInterval;

            if (this.growthInt > 1)
            {
                this.growthInt = 1;
            }

            if (this.growthInt < 0)
            {
                this.growthInt = 0;
            }


            if (!plantWasAlreadyMature
                && (this.LifeStage == PlantLifeStage.Mature))
            {
                // Plant just became mature.
                this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things);
            }

            if (this.growthInt > this.attributes.minReproductionSize)
            {
                this.ageInt += GenTicks.TickLongInterval;
                if (Rand.Range(0f, 1f) < this.attributes.reproduceProp * this.waterOffsetRateFactor)
                {
                    SaltCrystalReproduction.TryReproduceFrom(this, base.Position, this.attributes, this.attributes.spawnRadius, base.Map);
                }
            }

            if (this.growthInt <= 0 && Rand.Range(0f, 1f) < this.attributes.deathProp)
            {
                base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 100000, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
            }
        }


        public static bool IsFertilityConditionOkAt(ThingDef_SaltCrystal plantDef, Map map, IntVec3 position)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            return !plantDef.disallowedTerrains.Any(terrain.defName.Contains);
        }

        public static bool CanTerrainSupportPlantAt(ThingDef_SaltCrystal myDef, Map map, IntVec3 position)
        {
            if (IsFertilityConditionOkAt(myDef, map, position) == false)
            {
                return false;
            }
            if (myDef.mustBeNextToWater && SaltCrystal.isNearWater(myDef, map, position) == false)
            {
                return false;
            }
            List<Thing> list = map.thingGrid.ThingsListAt(position);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.BlockPlanting)
                {
                    return false;
                }
                if (myDef.passability == Traversability.Impassable && (thing.def.category == ThingCategory.Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CanBiomeSuppoprtPlantAt(ThingDef_SaltCrystal myDef, Map map) 
        {
            return myDef.allowedBiomes.Any(map.Biome.defName.Contains);
        }


        public static void spawnSaltCluster(Map map, IntVec3 position)
        {
            // Make a cluster center
            ThingDef_SaltCrystal thingDef = (ThingDef_SaltCrystal)ThingDef.Named("SaltCrystal");
            SaltCrystal crystal = (SaltCrystal)SaltCrystalReproduction.TryReproduceInto(position, thingDef, map);
            crystal.Growth = Rand.Range(0.4f, 0.8f) * crystal.waterOffsetRateFactor;

            // Pick cluster size
            int clusterSize = (int)Rand.Range(thingDef.minClusterSize, thingDef.maxClusterSize);

            // Grow cluster 
            growCluster(map, crystal, position, clusterSize);
        }


        public static void growCluster(Map map, SaltCrystal crystal, IntVec3 position, int times)
        {
            if (times > 0)
            {
                SaltCrystal newGrowth = (SaltCrystal)SaltCrystalReproduction.TryReproduceFrom(crystal, position, crystal.attributes, crystal.attributes.spawnRadius, map);
                if (newGrowth != null)
                {
                    growCluster(map, newGrowth, newGrowth.Position, times - 1);
                    newGrowth.Growth = Rand.Range(0.6f, 1.1f) * crystal.growthInt * crystal.waterOffsetRateFactor;
                }

            }
        }

    }       







    /// <summary>
    /// ThingDef_SaltCrystal class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_SaltCrystal : ThingDef
    {
        public int maxStableTemperature = 1000; // Will evaporate above this temperature
        public int maxGrowTemperature = 500; // Will not grow above this temperature
        public int minGrowTemperature = -10; // Will not grow below this temperature
        public int idealGrowTemperature = 20; // Grows fastest at this temperature
        public float meltRate = 1.0f; // How quickly it melts when above maxStableTemperature
        public float dissolveRate = 1.0f; // How quickly it dissolves in rain
        public float lightBoostFactor = 1.0f; // How effective light is for speeding formation
        public float minGrowLight = 0.9f; // Must have this much light to grow

        public float minFertility = 0.0f;
        public float maxFertility = 999f;

        public int spawnRadius = 1; // How far away it can spawn
        public float minReproductionSize = 0.5f; // Smallest it can be and reproduce
        public float noWaterSpawnFactor = 0.0f; // Multiplied by likelyhood of spawning near water
        public float reproduceProp = 0.001f; // How likly in a reproduction will be tried each long tick
        public float deathProp = 0.001f; //How likly it goes away for ever each long tick (only when 0% grown))

        public float minClusterPorbability = 0.001f; 
        public float maxClusterPorbability = 0.01f; 
        public int minClusterSize = 10;
        public int maxClusterSize = 30;

        public List<string> allowedBiomes = new List<string> { "AridShrubland", "TemperateForest", "ExtremeDesert", "Desert", "TropicalRainforest", "BorealArchipelago", "BorealForest", "DesertArchipelago", "TemperateArchipelago", "TropicalArchipelago", "TKKN_VolcanicFlow", "TKKN_Desert", "TKKN_Oasis", "TKKN_RedwoodForest" };
 
        public bool mustBeNextToWater = true; // Must be within `waterOffsetRadius` to spawn
        public int waterOffsetRadius = 2; // How close it has to be to water
        public bool waterOffsetGrowthEffect = true; // If true, growth rate depends on distance from water
        public List<string> needsToBeNearTerrains = new List<string> { "Ice", "ice", "Water", "water" };
        public List<string> disallowedTerrains = new List<string> { "Ice", "ice", "Ocean", "ocean", "Water", "water" };
    }





    public static class SaltCrystalReproduction
    {
        public static bool TryFindReproductionDestination(SaltCrystal crystal, IntVec3 source, ThingDef_SaltCrystal plantDef, int radius, Map map, out IntVec3 foundCell)
        {
            Predicate<IntVec3> validator = (IntVec3 c) => source.InHorDistOf(c, radius) && GenSight.LineOfSight(source, c, map, true, null, 0, 0) && SaltCrystal.CanTerrainSupportPlantAt(plantDef, map, c);
            return CellFinder.TryFindRandomCellNear(source, map, Mathf.CeilToInt(radius), validator, out foundCell);
        }

        public static Plant TryReproduceFrom(SaltCrystal crystal, IntVec3 source, ThingDef_SaltCrystal plantDef, int radius, Map map)
        {
            IntVec3 dest;
            if (!SaltCrystalReproduction.TryFindReproductionDestination(crystal, source, plantDef, radius, map, out dest))
            {
                return null;
            }
            return SaltCrystalReproduction.TryReproduceInto(dest, plantDef, map);
        }

        public static Plant TryReproduceInto(IntVec3 dest, ThingDef_SaltCrystal myDef, Map map)
        {
            if (SaltCrystal.CanTerrainSupportPlantAt(myDef, map, dest))
            {
                SaltCrystal output = (SaltCrystal)GenSpawn.Spawn(myDef, dest, map);
                output.myDistToWater = SaltCrystal.distFromWater(myDef, map, dest);
                return output;
            }
            else
            {
                return null;
            }
        }

    }


}



