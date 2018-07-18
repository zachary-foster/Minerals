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
    /// SaltCrystal class
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class SnowDrift : DynamicMineral
    {
        public static float grothRateFactor(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            // If melting, dont change
            float rate = 0f;
            float baseRate = DynamicMineral.GrowthRateAtPos(myDef, aPosition, aMap);
            if (baseRate < 0) {
                // Melt faster in water
                if (aMap.terrainGrid.TerrainAt(aPosition).defName.Contains("Water"))
                {
                    if (aMap.terrainGrid.TerrainAt(aPosition).defName.Contains("Moving"))
                    {

                        return Math.Abs(baseRate) * 30; // melt even faster in moving water
                    }
                    else
                    {
                        return Math.Abs(baseRate) * 10;
                    }
                }
                else
                {
                    return Math.Abs(baseRate);
                }
            } else {
                rate = aPosition.GetSnowDepth(aMap);
            }

            // Nearby Buldings slow growth
            const int snowCoverRadius = 1;
            for (int xOffset = -snowCoverRadius; xOffset <= snowCoverRadius; xOffset++)
            {
                for (int zOffset = -snowCoverRadius; zOffset <= snowCoverRadius; zOffset++)
                {
                    IntVec3 checkedPosition = aPosition + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(aMap))
                    {
                        foreach (Thing thing in aMap.thingGrid.ThingsListAt(checkedPosition))
                        {
                            if (thing is Building && thing.def.altitudeLayer == AltitudeLayer.Building)
                            {

                                rate = rate * 0.5f;
                            }

                        }
                    }                    
                }
            }

            // Trees on same tile slow growth
            foreach (Thing thing in aMap.thingGrid.ThingsListAt(aPosition))
            {
                if (thing is Plant && thing.def.altitudeLayer == AltitudeLayer.Building)
                {

                    rate = rate * (1 - ((Plant) thing).Growth) * 0.5f;
                }

            }

            // Factor cannot be greater than one or negative
            if (rate > 1)
            { 
                rate = 1f;
            }
            if (rate < 0)
            { 
                rate = 0f;
            }

            // Melt faster in water
            if (aMap.terrainGrid.TerrainAt(aPosition).defName.Contains("Water") && baseRate < 0.05)
            {
                rate = rate * 5;
            }

            return rate;
        }

        public override float GrowthRate
        {
            get
            {
                return base.GrowthRate * grothRateFactor(this.attributes, this.Position, this.Map);
            }
        }

        public new static float GrowthRateAtPos(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap) 
        {
            return DynamicMineral.GrowthRateAtPos(myDef, aPosition, aMap) * grothRateFactor(myDef, aPosition, aMap);
        }
            
    }  



    /// <summary>
    /// ThingDef_SnowDrift class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_SnowDrift : ThingDef_DynamicMineral
    {
        public override void InitNewMap(Map map, float scaling = 1)
        {
            float snowProb = 1f;
            const float minTemp = 10f;

            // Only spawn snow if it is cold out
            if (map.mapTemperature.SeasonalTemp < minTemp)
            {
                snowProb = snowProb * (float)Math.Sqrt(minTemp - map.mapTemperature.SeasonalTemp) / 5;
            }
            else
            {
                snowProb = 0f;
            }

            // Scale by rain amount
            snowProb = snowProb * map.TileInfo.rainfall / 1000;

            Log.Message("Minerals: snow scaling due to temp/precip: " + snowProb);
            base.InitNewMap(map, snowProb);
        }
    }


}



