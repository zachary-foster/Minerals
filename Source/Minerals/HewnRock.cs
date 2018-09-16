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
    /// HewnRock class
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class HewnRock : StaticMineral
    {
        public new ThingDef_HewnRock attributes
        {
            get
            {
                return base.attributes as ThingDef_HewnRock;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                // Get paths to textures
                string textureName = System.IO.Path.GetFileName(this.attributes.graphicData.texPath);
                List<string> texturePaths = new List<string> { };
                List<string> versions = new List<string> { "A", "B", "C" };
                foreach (string letter in versions)
                {
                    texturePaths.Add(System.IO.Path.Combine(this.attributes.graphicData.texPath, textureName + letter));
                }

                // Pick a random path 
                Rand.PushState();
                Rand.Seed = Position.GetHashCode();
                string printedTexturePath = texturePaths.RandomElement();
                Rand.PopState();

                // get graphic
                Graphic graphic = GraphicDatabase.Get<Graphic_Single>(printedTexturePath, ShaderDatabase.DefaultShader);
                Graphic_LinkedCornerFiller glGraphic = new Graphic_LinkedCornerFiller(graphic);

                return glGraphic.GetColoredVersion(glGraphic.Shader, this.DrawColor, this.DrawColor);
            }
        }


    }       


    /// <summary>
    /// ThingDef_HewnRock class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_HewnRock : ThingDef_StaticMineral
    {
        public List<string> ThingsToReplace; 

        public virtual Thing ThingToReplaceAtPos(Map map, IntVec3 position)
        {
            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (thing == null || thing.def == null)
                {
                    continue;
                }

                if (ThingsToReplace.Any(thing.def.defName.Equals))
                {
                    return(thing);
                }
            }
            return(null);
        }

        public override void InitNewMap(Map map, float scaling = 1)
        {

            // Find spots to spawn it
            map.regionAndRoomUpdater.Enabled = false;
            IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
            foreach (IntVec3 current in allCells)
            {
                if (!current.InBounds(map))
                {
                    continue;
                }

                if (map.roofGrid.RoofAt(current) == null || map.roofGrid.RoofAt(current).isThickRoof == false)
                {
                    continue;
                }

                // Replace rock under mountains
                Thing ToReplace = ThingToReplaceAtPos(map, current);
                if (ToReplace != null)
                {
                    //                    Log.Message("Minerals: spawning " + defName + " at " + current + " on " + ToReplace.def.defName);

                    ToReplace.Destroy();
                    StaticMineral spawned = SpawnAt(map, current, Rand.Range(initialSizeMin, initialSizeMax));
                    map.edificeGrid.Register(spawned);
                }
            }
            map.regionAndRoomUpdater.Enabled = true;


            // Call parent function for standard spawning
            base.InitNewMap(map, scaling);
        }



    }

}
