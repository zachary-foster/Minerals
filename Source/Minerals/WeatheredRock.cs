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
	/// WeatheredRock class
	/// </summary>
	/// <author>zachary-foster</author>
	/// <permission>No restrictions</permission>
	public class WeatheredRock : StaticMineral
	{
        public new ThingDef_WeatheredRock attributes
        {
            get
            {
                return base.attributes as ThingDef_WeatheredRock;
            }
        }


	}       


	/// <summary>
	/// ThingDef_StaticMineral class.
	/// </summary>
	/// <author>zachary-foster</author>
	/// <permission>No restrictions</permission>
	public class ThingDef_WeatheredRock : ThingDef_StaticMineral
	{
        // Things this mineral replaces when a map is initialized
		public List<string> ThingsToReplace; 

		public override void InitNewMap(Map map, float scaling = 1)
		{
			// Print to log
			Log.Message("Minerals: " + defName + " will replace unroofed " + ThingsToReplace + ".");

			// Find spots to spawn it
			IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
			foreach (IntVec3 current in allCells)
			{
				if (!current.InBounds(map))
				{
					continue;
				}

				if (current.Roofed(map))
				{
					continue;
				}

				// Replace unroofed rock
				foreach (Thing thing in map.thingGrid.ThingsListAt(current))
				{
					if (thing == null || thing.def == null)
					{
						continue;
					}

					if (ThingsToReplace.Any(thing.def.defName.Equals))
					{
						thing.Destroy();
						SpawnAt(map, current);
					}
				}
			}

			// Call parent function for standard spawning
			base.InitNewMap(map, scaling);
		}

	}

}
