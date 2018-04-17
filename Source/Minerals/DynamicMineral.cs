
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

        public ThingDef_DynamicMineral attributes
        {
            get
            {
                return this.def as ThingDef_DynamicMineral;
            }
        }


        // ======= Growth rate factors ======= //

        public float growthRateFactor(growthRateModifier mod)
        {
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
            else if (myValue < mod.minGrow || myValue > mod.maxGrow) 
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
            
            // just right!
            return 1f;
        }

        public List<float> allGrowthRateFactors 
        {
            get
            {
                this.attributes.allRateModifiers.Select(mod => growthRateFactor(mod));
            }
        }


        public override float GrowthRate
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

        public new float GrowthPerTick
        {
            get
            {
                float growthPerTick = (1f / (GenDate.TicksPerDay * this.attributes.growDays));
                return growthPerTick * this.GrowthRate;
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


    public class growthRateModifier
    {
        public bool active; // This modifier only has an effect when true
        public float aboveMaxDecayRate;  // How quickly it decays when above maxStableFert
        public float maxStable; // Will decay above this fertility level
        public float maxGrow; // Will not grow above this fertility level
        public float idealGrow; // Grows fastest at this fertility level
        public float minGrow; // Will not grow below this fertility level
        public float minStable; // Will decay below this fertility level
        public float belowMinDecayRate;  // How quickly it decays when below minStableFert

        public virtual float value(DynamicMineral aMineral);
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