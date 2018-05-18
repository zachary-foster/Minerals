
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
    public class DynamicMineral : StaticMineral
    {

        public new ThingDef_DynamicMineral attributes
        {
            get
            {
                return base.attributes as ThingDef_DynamicMineral;
            }
        }


        // ======= Growth rate factors ======= //


        public float getModValue(growthRateModifier mod) 
        {
            // If growth rate factor is not needed, do not calculate
            if (mod == null | !mod.active)
            {
                return 0f;
            }
            else
            {
                return mod.value(this);
            }
        }

        public static float getModValueAtPos(growthRateModifier mod, ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap) 
        {
            // If growth rate factor is not needed, do not calculate
            if (mod == null | !mod.active)
            {
                return 0f;
            }
            else
            {
                return mod.value(myDef, aPosition, aMap);
            }
        }


        public static float growthRateFactor(growthRateModifier mod, float myValue)
        {
            // Growth rate factor not defined
            if (mod == null)
            {
                return 1f;
            }

            // Check that the growth rate modifier is in use
            if (! mod.active)
            {
                return 1f;
            }
                
            // decays if too high or low
            float stableRangeSize = mod.maxStable - mod.minStable;
            if (myValue > mod.maxStable) 
            {
                return - mod.aboveMaxDecayRate * (myValue - mod.maxStable) / stableRangeSize;
            } 
            if (myValue < mod.minStable) 
            {
                return - mod.belowMinDecayRate * (mod.minStable - myValue) / stableRangeSize;
            }
            
            // does not grow if too high or low
            if (myValue < mod.minGrow || myValue > mod.maxGrow) 
            {
                return 0f;
            } 
            
            // slowed growth if too high or low
            if (myValue < mod.idealGrow)
            {
                return 1f - ((mod.idealGrow - myValue) / (mod.idealGrow - mod.minGrow));
            }
            else 
            {
                return 1f - ((myValue - mod.idealGrow) / (mod.maxGrow - mod.idealGrow));
            }
        }

        public List<float> allGrowthRateFactors 
        {
            get
            {
                return this.attributes.allRateModifiers.Select(mod => growthRateFactor(mod, getModValue(mod))).ToList();
            }
        }

        public static List<float> allGrowthRateFactorsAtPos(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap) 
        {

            return myDef.allRateModifiers.Select(mod => growthRateFactor(mod, getModValueAtPos(mod, myDef, aPosition, aMap))).ToList();

        }



        public float GrowthRate
        {
            get
            {
                // Get growth rate factors
                List<float> rateFactors = this.allGrowthRateFactors;
                List<float> positiveFactors = rateFactors.FindAll(fac => fac >= 0);
                List<float> negativeFactors = rateFactors.FindAll(fac => fac < 0);

                // if any factors are negative, add them together and ignore positive factors
                if (negativeFactors.Count > 0)
                {
                    return negativeFactors.Sum();
                }

                // if all positive, multiply them
                if (positiveFactors.Count > 0)
                {
                    return positiveFactors.Aggregate(1f, (acc, val) => acc * val);
                }

                // If there are no growth rate factors, grow at full speed
                return 1f;
            }
        }

        public static float GrowthRateAtPos(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap) 
        {
            // Get growth rate factors
            List<float> rateFactors = allGrowthRateFactorsAtPos(myDef, aPosition, aMap);
            List<float> positiveFactors = rateFactors.FindAll(fac => fac >= 0);
            List<float> negativeFactors = rateFactors.FindAll(fac => fac < 0);

            // if any factors are negative, add them together and ignore positive factors
            if (negativeFactors.Count > 0)
            {
                return negativeFactors.Sum();
            }

            // if all positive, multiply them
            if (positiveFactors.Count > 0)
            {
                return positiveFactors.Aggregate(1f, (acc, val) => acc * val);
            }

            // If there are no growth rate factors, grow at full speed
            return 1f;

        }


        public float GrowthPerTick
        {
            get
            {
                float growthPerTick = (1f / (GenDate.TicksPerDay * this.attributes.growDays));
                return growthPerTick * this.GrowthRate;
            }
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Size: " + this.size.ToStringPercent());
            stringBuilder.AppendLine("Growth rate: " + this.GrowthRate.ToStringPercent());
            foreach (growthRateModifier mod in this.attributes.allRateModifiers)
            {
                stringBuilder.AppendLine(mod.GetType().Name + ": " + growthRateFactor(mod, getModValue(mod)));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }


        public override void TickLong()
        {
            // Try to grow
            this.size += this.GrowthPerTick * 2000; // dont know why 2000, just imitating what plants do

            // Try to reproduce
            if (this.size > this.attributes.minReproductionSize)
            {
                if (Rand.Range(0f, 1f) < this.attributes.reproduceProp)
                {
                    this.TryReproduce();
                }
            }

            // Try to die
            if (this.size <= 0 && Rand.Range(0f, 1f) < this.attributes.deathProb)
            {
                this.Destroy(DestroyMode.Vanish);
                //base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 100000, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
            }
        }

    }       





    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_DynamicMineral : ThingDef_StaticMineral
    {
        // The number of days it takes to grow at max growth speed
        public float growDays = 100f;


        public float minReproductionSize = 0.8f;
        public float reproduceProp = 0.001f;
        public float deathProb = 0.001f;
        public float spawnProb = 0.0001f; // chance of spawning de novo each tick
        public tempGrowthRateModifier tempGrowthRateModifer;  // Temperature effects on growth rate
        public rainGrowthRateModifier rainGrowthRateModifer;  // Rain effects on growth rate
        public lightGrowthRateModifier lightGrowthRateModifer; // Light effects on growth rate
        public fertGrowthRateModifier fertGrowthRateModifer;  // Fertility effects on growth rate
        public distGrowthRateModifier distGrowthRateModifer;  // Distance to needed terrain effects on growth rate
        public sizeGrowthRateModifier sizeGrowthRateModifer;  // Current size effects on growth rate


        public List<growthRateModifier> allRateModifiers 
        {
            get 
            {
                List<growthRateModifier> output = new List<growthRateModifier>{
                    tempGrowthRateModifer,
                    rainGrowthRateModifer,
                    lightGrowthRateModifer,
                    fertGrowthRateModifer,
                    distGrowthRateModifer,
                    sizeGrowthRateModifer
                };
                output.RemoveAll(item => item == null);
                return output;
            }
        }
    }


    public abstract class growthRateModifier
    {
        public bool active; // This modifier only has an effect when true
        public float aboveMaxDecayRate;  // How quickly it decays when above maxStableFert
        public float maxStable; // Will decay above this fertility level
        public float maxGrow; // Will not grow above this fertility level
        public float idealGrow; // Grows fastest at this fertility level
        public float minGrow; // Will not grow below this fertility level
        public float minStable; // Will decay below this fertility level
        public float belowMinDecayRate;  // How quickly it decays when below minStableFert

        public abstract float value(DynamicMineral aMineral);
        public abstract float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap);
    }

    public class tempGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Position.GetTemperature(aMineral.Map);
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return aPosition.GetTemperature(aMap);
        }
    }

    public class rainGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.weatherManager.curWeather.rainRate;
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return aMap.weatherManager.curWeather.rainRate;
        }
    }

    public class lightGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.glowGrid.GameGlowAt(aMineral.Position);
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return aMap.glowGrid.GameGlowAt(aPosition);
        }
    }


    public class fertGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.fertilityGrid.FertilityAt(aMineral.Position);
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return aMap.fertilityGrid.FertilityAt(aPosition);
        }
    }

    public class distGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.distFromNeededTerrain;
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return StaticMineral.posDistFromNeededTerrain(myDef, aMap, aPosition);
        }
    }


    public class sizeGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.size;
        }

        public override float value(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            return 0.01f;
        }
    }



    public class DynamicMineralWatcher : MapComponent
    {

        public static int ticksPerLook = 1000; // 100 is about once a second on 1x speed
        public int tick_counter = 1;

        public DynamicMineralWatcher(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Run each class' watcher
            tick_counter += 1;
            if (tick_counter > ticksPerLook)
            {
                tick_counter = 1;
                Look();
            }
        }

        // The main function controlling what is done each time the map is looked at
        public void Look()
        {
            SpawnDynamicMinerals();
        }


        public void SpawnDynamicMinerals() 
        {
            foreach (ThingDef_DynamicMineral mineralType in Verse.DefDatabase<ThingDef_DynamicMineral>.AllDefs)
            {
                // Check that the map type is ok
                if (! StaticMineral.CanSpawnInBiome(mineralType, map))
                {
                    continue;
                }

               

                // Get number of positions to check
                float numToCheck = map.Area * mineralType.spawnProb;

                if (numToCheck < 1 & Rand.Range(0f, 1f) > numToCheck)
                {
                    continue;
                }
                else
                {
                    numToCheck = 1;
                }

                //Log.Message("Trying to spawn " + mineralType.defName + " with prob of " + mineralType.spawnProb + " and " + numToCheck + " blocks");


                // Try to spawn in a subset of positions
                for (int i = 0; i < numToCheck; i++)
                {
                    // Pick a random location
                    IntVec3 aPos = map.AllCells.RandomElement();

                    //Log.Message("GrowthRateAtPos: " + DynamicMineral.GrowthRateAtPos(mineralType, aPos, map));
                    // Dont try to place on invlaid terrain
                    if (! StaticMineral.IsTerrainOkAt(mineralType, map, aPos))
                    {
                        continue;
                    }

                    // Dont always spawn if growth rate is not good
                    if (Rand.Range(0f, 1f) > DynamicMineral.GrowthRateAtPos(mineralType, aPos, map))
                    {
                        continue;
                    }

                    // Try to spawn at that location
                    StaticMineral.TrySpawnAt(aPos, mineralType, map);
                    Log.Message(mineralType.defName + " spawned at " + aPos);

                }
                
            }
        }
    }
}



