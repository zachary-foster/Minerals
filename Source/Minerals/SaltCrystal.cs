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
    public class SaltCrystal : DynamicMineral
    {
        public static float GrowthRateBonus(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap)
        {
            float bonus = 1f;
            TerrainDef terrain = aMap.terrainGrid.TerrainAt(aPosition);

            if (terrain.defName == "TKKN_SandBeachWetSalt") // Grows faster on wet sand
            {
                bonus = bonus * 3;
            } else if (terrain.defName == "WaterOceanShallow") // melts in water
            {
                bonus = Math.Abs(bonus) * -1;
            }

            return bonus;
        }

        public override float GrowthRate
        {
            get
            {
                return base.GrowthRate * GrowthRateBonus(this.attributes, this.Position, this.Map);
            }
        }

        public new static float GrowthRateAtPos(ThingDef_DynamicMineral myDef, IntVec3 aPosition, Map aMap) 
        {
            return DynamicMineral.GrowthRateAtPos(myDef, aPosition, aMap) * GrowthRateBonus(myDef, aPosition, aMap);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
            if (this.Map.terrainGrid.TerrainAt(this.Position).defName == "WaterOceanShallow") // melts in water
            {
                stringBuilder.AppendLine("Dissolving in water.");
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

     }       
        
}



