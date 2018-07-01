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
                return baseRate;
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

                    rate = rate * (1 - ((Plant) thing).Growth);
                }

            }

            // Factor cannot be greater than one
            if (rate > 1)
            { 
                rate = 1f;
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

}



