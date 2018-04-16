
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
        // ======= Growth rate factors ======= //

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

    }       





    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_DynamicMineral : ThingDef_StaticMineral
    {
        // Temperature effects on growth rate
        public float aboveMaxEvaporateRate = 1f;  // How quickly it melts when above maxStableTemperature
        public int maxStableTemperature = 200; // Will evaporate above this temperature
        public int maxGrowTemperature = 100; // Will not grow above this temperature
        public int idealGrowTemperature = 50; // Grows fastest at this temperature
        public int minGrowTemperature = 0; // Will not grow below this temperature
        public int minStableTemperature = -1000; // Will evaporate below this temperature
        public float belowMinEvaporateRate = 1f;  // How quickly it melts when below minStableTemperature
         
        // Weather effects on growth rate
        public float rainDissolveRate = 0f; // How quickly it dissolves in rain
        public float lightBoostFactor = 0f; // How effective light is for speeding formation
        public float minGrowLight = 0f; // Must have this much light to grow

        // Fertility effects of growth rate
        public float minFertility = 0.0f;
        public float maxFertility = 999f;

    }
}