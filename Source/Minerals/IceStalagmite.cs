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
    public class IceStalagmite : DynamicMineral
    {
        public new static bool isRoofConditionOk(ThingDef_StaticMineral myDef, Map map, IntVec3 position)
        {
            // Allow to spawn near roofs
            Predicate<IntVec3> validator = (IntVec3 c) => c.Roofed(map);
            IntVec3 unused;

            if (CellFinder.TryFindRandomCellNear(position, map, 1, validator, out unused))
            {
                return true;
            }

            return DynamicMineral.isRoofConditionOk(myDef, map, position);
        }

    }       

}



