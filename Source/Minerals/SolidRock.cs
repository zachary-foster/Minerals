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
	/// SolidRock class
	/// </summary>
	/// <author>zachary-foster</author>
	/// <permission>No restrictions</permission>
	public class SolidRock : StaticMineral
	{
        public new ThingDef_SolidRock attributes
        {
            get
            {
                return base.attributes as ThingDef_SolidRock;
            }
        }


	}       


	/// <summary>
	/// ThingDef_StaticMineral class.
	/// </summary>
	/// <author>zachary-foster</author>
	/// <permission>No restrictions</permission>
	public class ThingDef_SolidRock : ThingDef_StaticMineral
	{
        public override void ReplaceThings(Map map, float scaling = 1)
        {

            if (ThingsToReplace == null || ThingsToReplace.Count == 0 || allowReplaceSetting() == false)
            {
                return;
            }

            // Find spots to spawn it
            map.regionAndRoomUpdater.Enabled = false;
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (!current.InBounds(map))
                {
                    continue;
                }

                // dont spawn expect under mountains 
                if (map.roofGrid.RoofAt(current) == null || map.roofGrid.RoofAt(current).isThickRoof == false)
                {
                    continue;
                }

                // Only spawn if near passable area
                if (!IsNearPassable(map, current, 1))
                {
                    continue;
                }

                // Replace rock under mountains
                Thing ToReplace = ThingToReplaceAtPos(map, current);
                if (ToReplace != null)
                {
                    ToReplace.Destroy();
                    StaticMineral spawned = SpawnAt(map, current, Rand.Range(initialSizeMin, initialSizeMax));
                    map.edificeGrid.Register(spawned);
                }
            }
            map.regionAndRoomUpdater.Enabled = true;
        }

        public bool IsNearPassable(Map map, IntVec3 position, int radius = 1)
        {
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                for (int zOffset = -radius; zOffset <= radius; zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        if (! checkedPosition.Impassable(map))
                        {
                            return true;
                        }

                    }
                }
            }

            return false;

        }

    }

}
