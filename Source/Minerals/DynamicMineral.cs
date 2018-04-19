
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

        public float growthRateFactor(growthRateModifier mod)
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

            // Get value the growth rate depends on
            float myValue = mod.value(this);
            
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
                return this.attributes.allRateModifiers.Select(mod => growthRateFactor(mod)).ToList();
            }
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
            stringBuilder.AppendLine("Size: " + this.size);
            stringBuilder.AppendLine("Growth rate: " + this.GrowthRate);
            stringBuilder.AppendLine("Temp factor: " + this.growthRateFactor(this.attributes.tempGrowthRateModifer));
            stringBuilder.AppendLine("Rain factor: " + this.growthRateFactor(this.attributes.rainGrowthRateModifer));
            stringBuilder.AppendLine("Light factor: " + this.growthRateFactor(this.attributes.lightGrowthRateModifer));
            stringBuilder.AppendLine("Size factor: " + this.growthRateFactor(this.attributes.sizeGrowthRateModifer));
            stringBuilder.AppendLine("Fertility factor: " + this.growthRateFactor(this.attributes.fertGrowthRateModifer));
            stringBuilder.AppendLine("Distance factor: " + this.growthRateFactor(this.attributes.distGrowthRateModifer));
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
                base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 100000, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
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
        public float growDays = 100f;
        public float minReproductionSize = 0.8f;
        public float reproduceProp = 0.001f;
        public float deathProb = 0.001f;
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
    }

    public class tempGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Position.GetTemperature(aMineral.Map);
        }
    }

    public class rainGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.weatherManager.curWeather.rainRate;
        }
    }

    public class lightGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.glowGrid.GameGlowAt(aMineral.Position);
        }
    }


    public class fertGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.Map.fertilityGrid.FertilityAt(aMineral.Position);
        }
    }

    public class distGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.distFromNeededTerrain;
        }
    }


    public class sizeGrowthRateModifier : growthRateModifier
    {
        public override float value(DynamicMineral aMineral)
        {
            return aMineral.size;
        }
    }
}