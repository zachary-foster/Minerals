
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
    public class StaticMineral : Mineable
    {

        // ======= Private Variables ======= //
        protected float yieldPct = 0;
        public static float globalMineralAbundance = 1f;

        // The current size of the mineral
        protected float mySize = 1f;
        public float size
        {
            get
            {
                return mySize;
            }

            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 1)
                {
                    value = 1;
                }
                mySize = value;
            }
        }


        protected float? myDistFromNeededTerrain = null;
        public float distFromNeededTerrain
        {
            get
            {
                if (myDistFromNeededTerrain == null) // not yet set
                {
                    myDistFromNeededTerrain = attributes.posDistFromNeededTerrain(Map, Position);
                }

                return (float)myDistFromNeededTerrain;
            }

            set
            {
                myDistFromNeededTerrain = value;
            }
        }


        public virtual ThingDef_StaticMineral attributes
        {
            get
            {
                return def as ThingDef_StaticMineral;
            }
        }

        // ======= Spawning conditions ======= //



//        public override IntVec3 Position
//        {
//            get
//            {
//                return base.Position;
//            }
//            set
//            {
//                const int maxTrys = 10;
//                for (int i = 0; i < maxTrys; i++)
//                {
//                    if (StaticMineral.PlaceIsBlocked(this.attributes, this.Map, value))
//                    {
//                        value = value.RandomAdjacentCell8Way();
//                    }
//                    else
//                    {
//                        break;
//                    }
//                }
//                base.Position = value;
//            }
//        }







        // ======= Yeilding resources ======= //

        public virtual void incPctYeild(int amount, Pawn miner)
        {
            yieldPct += (float)Mathf.Min(amount, HitPoints) / (float)MaxHitPoints * miner.GetStatValue(StatDefOf.MiningYield, true);
        }


        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            // Drop resources
            foreach (RandomResourceDrop toDrop in attributes.randomlyDropResources)
            {
                float dropChance = size * toDrop.DropProbability * ((float) Math.Min(dinfo.Amount, HitPoints) / (float) MaxHitPoints);
                if (Rand.Range(0f, 1f) < dropChance)
                {
                    Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(toDrop.ResourceDefName), null);
                    thing.stackCount = toDrop.CountPerDrop;
                    GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near, null);
                }

            }


            // 
            if (def.building.mineableThing != null && def.building.mineableYieldWasteable && dinfo.Def == DamageDefOf.Mining && dinfo.Instigator != null && dinfo.Instigator is Pawn)
            {
                incPctYeild(dinfo.Amount, (Pawn)dinfo.Instigator);
            }
            if (size < yieldPct)
            {
                dinfo.SetAmount(0);
            }
            base.PreApplyDamage(dinfo, out absorbed);


        }

            

        // ======= Behavior ======= //

//        public override bool BlocksPawn(Pawn p)
//        {
//            return this.size >= 0.8f;
//        }

            
        // ======= Appearance ======= //

        public float GetSizeBasedOnNearest(Vector3 subcenter)
        {
            float distToTrueCenter = Vector3.Distance(this.TrueCenter(), subcenter);
            float sizeOfNearest = 0;
            float distToNearest = 1;
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    if (xOffset == 0 & zOffset == 0)
                    {
                        continue;
                    }
                    IntVec3 checkedPosition = Position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(Map))
                    {
                        List<Thing> list = Map.thingGrid.ThingsListAt(checkedPosition);
                        foreach (Thing item in list)
                        {
                            if (item.def.defName == attributes.defName)
                            {
                                float distanceToPos = Vector3.Distance(item.TrueCenter(), subcenter);

                                if (distToNearest > distanceToPos & distanceToPos <= 1) 
                                {
                                    distToNearest = distanceToPos;
                                    sizeOfNearest = ((StaticMineral) item).size;
                                }
                            }
                        }
                    }
                }
            }

            float correctedSize = (0.75f - distToTrueCenter) * size + (1 - distToNearest) * sizeOfNearest;
            //Log.Message("this.size=" + this.size + " sizeOfNearest=" + sizeOfNearest + " distToNearest=" + distToNearest + " distToTrueCenter=" + distToTrueCenter);
            //Log.Message(this.size + " -> " + correctedSize + "  dist = " + distToNearest);

            return attributes.visualSizeRange.LerpThroughRange(correctedSize);
        }

        public static float randPos(float clustering, float spread)
        {
            // Weighted average of normal and uniform distribution
            return (Rand.Gaussian(0, 0.2f) * clustering + Rand.Range(-0.5f, 0.5f) * (1 - clustering)) * spread;
        }

        public override void Print(SectionLayer layer)
        {
			Rand.PushState();
			Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode();
            if (this.attributes.graphicData.graphicClass.Name != "Graphic_Random" || this.attributes.graphicData.linkType == LinkDrawerType.CornerFiller) {
				base.Print(layer);
			} else {
				int numToPrint = Mathf.CeilToInt(size * (float)attributes.maxMeshCount);
				if (numToPrint < 1)
				{
					numToPrint = 1;
				}
				Vector3 trueCenter = this.TrueCenter();
				for (int i = 0; i < numToPrint; i++)
				{
					// Calculate location
					Vector3 center = trueCenter;
					center.y = attributes.Altitude;
					center.x += randPos(attributes.visualClustering, attributes.visualSpread);
					center.z += randPos(attributes.visualClustering, attributes.visualSpread);

					// Adjust size for distance from center to other crystals
					float thisSize = GetSizeBasedOnNearest(center);

					// Add random variation
					thisSize = thisSize + (thisSize * Rand.Range(- attributes.visualSizeVariation, attributes.visualSizeVariation));
					if (thisSize <= 0)
					{
						continue;
					}

					// Print image
					Material matSingle = Graphic.MatSingle;
					Vector2 sizeVec = new Vector2(thisSize, thisSize);
					Material mat = matSingle;
					Printer_Plane.PrintPlane(layer, center, sizeVec, mat, 0, Rand.Bool);
				}
				//            if (this.attributes.graphicData.shadowData != null)
				//            {
				//                Vector3 center2 = a + this.attributes.graphicData.shadowData.offset * num2;
				//                if (flag)
				//                {
				//                    center2.z = this.Position.ToVector3Shifted().z + this.attributes.graphicData.shadowData.offset.z;
				//                }
				//                center2.y -= 0.046875f;
				//                Vector3 volume = this.attributes.graphicData.shadowData.volume * num2;
				//                Printer_Shadow.PrintShadow(layer, center2, volume, Rot4.North);
				//            }
			}
			Rand.PopState();

        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Size: " + size.ToStringPercent());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref mySize, "mySize", 1);
        }


        public override Graphic Graphic
        {
            get
            {
                // Get paths to textures
                string textureName = System.IO.Path.GetFileName(this.attributes.graphicData.texPath);
                List<Graphic> textures = new List<Graphic> { };
                List<string> versions = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
                foreach (string letter in versions)
                {
                    string a_path = System.IO.Path.Combine(this.attributes.graphicData.texPath, textureName + letter);

                    if (ContentFinder<Texture2D>.Get(a_path, false) != null)
                    {
                        Graphic graphic = GraphicDatabase.Get<Graphic_Single>(a_path, ShaderDatabase.ShaderFromType(attributes.graphicData.shaderType));
                        textures.Add(graphic);
                    }
                }

                // Pick a random path 
                //Rand.PushState();
                //Rand.Seed = Position.GetHashCode();
                Graphic printedTexture = textures.RandomElement();
                //Rand.PopState();

                // get graphic
                //Graphic printedTexture = GraphicDatabase.Get<Graphic_Single>(printedTexturePath, ShaderDatabase.ShaderFromType(attributes.graphicData.shaderType));

                // conver to corner filler if needed
                printedTexture = GraphicDatabase.Get<Graphic_Single>(printedTexture.path, printedTexture.Shader, printedTexture.drawSize, DrawColor, DrawColorTwo, printedTexture.data);
                if (attributes.graphicData.linkType == LinkDrawerType.CornerFiller)
                {
                     return new Graphic_LinkedCornerFiller(printedTexture);
                }
                else
                {
                    return  printedTexture;

                }

                //return printedTexture.GetColoredVersion(printedTexture.Shader, DrawColor, DrawColorTwo);
            }
        }

        public override Color DrawColor {
            get
            {
                if (this.attributes.coloredByTerrain)
                {
                    TerrainDef terrain = this.Position.GetTerrain(this.Map);
                    return terrain.graphic.Color;
                }
                return base.DrawColor;
            }
        }

        public override Color DrawColorTwo
        {
            get
            {
                return base.DrawColorTwo;
            }
        }
    }       



    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class RandomResourceDrop
    {
        public string ResourceDefName;
        public float DropProbability;
        public int CountPerDrop = 1;
    }




    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_StaticMineral : ThingDef
    {
        // How far away it can spawn from an existing location
        // Even though it is a static mineral, the map initialization uses "reproduction" to make clusters 
        public int spawnRadius = 1; 

        // The probability that this mineral type will be spawned at all on a given map
        public float perMapProbability = 0.5f; 

        // For a given map, the minimum/maximum probablility a cluster will spawn for every possible location
        public float minClusterProbability; 
        public float maxClusterProbability = 0.001f;

        // How  many squares each cluster will be
        public int minClusterSize = 1;
        public int maxClusterSize = 10;

        // The range of starting sizes of individuals in clusters
        public float initialSizeMin = 0.3f;
        public float initialSizeMax = 0.3f;

        // How much initial sizes of individuals randomly vary
        public float initialSizeVariation = 0.3f;

        // The biomes this can appear in
        public List<string> allowedBiomes;

        // The terrains this can appear on
        public List<string> allowedTerrains;

        // The terrains this must be near to, but not necessarily on, and how far away it can be
        public List<string> neededNearbyTerrains;
        public float neededNearbyTerrainRadius = 3f;

        // Controls how extra clusters are added near assocaited ore
        public List<string> associatedOres;
        public float nearAssociatedOreBonus = 3f;

        // If true, growth rate and initial size depends on distance from needed terrains
        public bool neededNearbyTerrainSizeEffect = true;

        // If true, only grows under roofs
        public bool mustBeUnderRoof = true;
        public bool mustBeUnderThickRoof = false;
//        public bool mustBeUnderNaturalRoof = true;
        public bool mustBeUnroofed = false;
        public bool mustBeNotUnderThickRoof = false;

        // The maximum number of images that will be printed per square
        public int maxMeshCount = 1;

        // The size range of images printed
        public FloatRange visualSizeRange  = new FloatRange(0.3f, 1.0f);
        public float visualClustering = 0.5f;
        public float visualSpread = 1.2f;
        public float visualSizeVariation = 0.1f;

        // The amount of resource returned if the mineral is its maximum size
        public int maxMinedYeild = 10;

        // Other resources it might drop
        public List<RandomResourceDrop> randomlyDropResources;

        // If it can spawn on other things
        public bool canSpawnOnThings = false;

        // Things this mineral replaces when a map is initialized
        public List<string> ThingsToReplace; 

        // If the primary color is based on the stone below it
        public bool coloredByTerrain = false;

        // ======= Spawning clusters ======= //


        public StaticMineral TryReproduce(Map map, IntVec3 position)
        {
            IntVec3 dest;
            if (! TryFindReproductionDestination(map, position, out dest))
            {
                return null;
            }
            return TrySpawnAt(dest, map, 0.01f);
        }


        public virtual void SpawnCluster(Map map, IntVec3 position)
        {
            // Make a cluster center
            StaticMineral mineral = TrySpawnAt(position, map, Rand.Range(initialSizeMin, initialSizeMax));
            if (mineral != null)
            {            
                // Pick cluster size
                int clusterSize = Rand.Range(minClusterSize, maxClusterSize);

                // Grow cluster 
                GrowCluster(map, mineral, clusterSize);

            }
        }


        public virtual void GrowCluster(Map map, StaticMineral sourceMineral, int times)
        {
            if (times > 0)
            {
                StaticMineral newGrowth = sourceMineral.attributes.TryReproduce(map, sourceMineral.Position);
                if (newGrowth != null)
                {
                    newGrowth.size = Rand.Range(1f - initialSizeVariation, 1f + initialSizeVariation) * sourceMineral.size;
                    GrowCluster(map, newGrowth, times - 1);
                }

            }
        }


        public virtual Thing ThingToReplaceAtPos(Map map, IntVec3 position)
        {
            if (ThingsToReplace == null || ThingsToReplace.Count == 0)
            {
                return(null);
            }
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

        // ======= Spawning conditions ======= //


        public virtual bool CanSpawnAt(Map map, IntVec3 position)
        {
//            Log.Message("CanSpawnAt: " + position + " " + map);
            // Check that location is in the map
            if (! position.InBounds(map))
            {
                return false;
            }

//            Log.Message("CanSpawnAt: location is in the map " + position);
            // Check that the terrain is ok
            if (! IsTerrainOkAt(map, position))
            {
                return false;
            }
//            Log.Message("CanSpawnAt: terrain is ok " + position);

            // Check that it is under a roof if it needs to be
            if (! isRoofConditionOk(map, position))
            {
                return false;
            }
//            Log.Message("CanSpawnAt: roof is ok " + position);

            // Look for stuff in the way
            if (PlaceIsBlocked(map, position))
            {
                return false;
            }
//            Log.Message("CanSpawnAt: not plocked " + position);

            // Check that it is near any needed terrains
            if (! isNearNeededTerrain(map, position))
            {
                return false;
            }
//            Log.Message("CanSpawnAt: can spawn " + position);

            return true;
        }

        public virtual bool PlaceIsBlocked(Map map, IntVec3 position)
        {
//            Log.Message("PlaceIsBlocked: base");
            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (thing == null || thing.def == null)
                {
                    continue;
                }

                if (! canSpawnOnThings) {
                    // Blocked by pawns, items, and plants
                    if (thing.def.category == ThingCategory.Pawn ||
                        thing.def.category == ThingCategory.Item ||
                        thing.def.category == ThingCategory.Plant)
                    {
                        return true;
                    }
                }

                // Blocked by buildings, except low minerals (NOT REALLY)
                if (thing.def.category == ThingCategory.Building)
                {
                    if (thing is StaticMineral && thing.def.defName != defName)
                    {
                        //                        Log.Message("Trying to spawn on mineral " + thing.def.defName);
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                    //                    if (!(thing is StaticMineral && (thing.def.altitudeLayer == AltitudeLayer.Floor || thing.def.altitudeLayer == AltitudeLayer.FloorEmplacement || myDef.altitudeLayer == AltitudeLayer.Floor || myDef.altitudeLayer == AltitudeLayer.FloorEmplacement)))
                    //                    {
                    //                        return true;
                    //                    }

                }

                // Blocked by impassible things, inlcuding assocaited minerals
                if (thing.def.passability == Traversability.Impassable)
                {
                    return true;
                }

            }
            return false;
        }

		public static bool PosHasThing(Map map, IntVec3 position, List<string> things)
		{
			if (things == null || things.Count == 0)
			{
				return false;
			}

			TerrainDef terrain = map.terrainGrid.TerrainAt(position);
			if (things.Any(terrain.defName.Equals))
			{
				return true;
			}

			foreach (Thing thing in map.thingGrid.ThingsListAt(position))
			{
				if (thing == null || thing.def == null)
				{
					continue;
				}

				if (things.Any(thing.def.defName.Equals))
				{
					return true;
				}
			}
			return false;
		}

        public virtual bool PosIsAssociatedOre(Map map, IntVec3 position)
        {
			return PosHasThing(map, position, associatedOres);
        }


        public virtual bool CanSpawnInBiome(Map map) 
        {
            if (allowedBiomes == null || allowedBiomes.Count == 0)
            {
                return true;
            }
            else
            {
                return allowedBiomes.Any(map.Biome.defName.Equals);
            }
        }

        public virtual bool IsTerrainOkAt(Map map, IntVec3 position)
        {
            if (! position.InBounds(map))
            {
                return false;
            }
            if (allowedTerrains == null || allowedTerrains.Count == 0)
            {
                return true;
            }
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            return allowedTerrains.Any(terrain.defName.Equals);
        }

        public virtual bool isNearNeededTerrain(Map map, IntVec3 position)
        {
            if (neededNearbyTerrains == null || neededNearbyTerrains.Count == 0)
            {
                return true;
            }

            for (int xOffset = -(int)Math.Ceiling(neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (neededNearbyTerrains.Any(terrain.defName.Equals) && position.DistanceTo(checkedPosition) < neededNearbyTerrainRadius)
                        {
                            return true;
                        }
                        foreach (Thing thing in map.thingGrid.ThingsListAt(checkedPosition))
                        {
                            if (neededNearbyTerrains.Any(thing.def.defName.Equals) && position.DistanceTo(checkedPosition) < neededNearbyTerrainRadius)
                            {
                                return true;
                            }
                        }

                    }
                }
            }

            return false;
        }


        // The distance a position is from a needed terrain type
        // A little slower than `isNearNeededTerrain` because all squares are checked
        public virtual float posDistFromNeededTerrain(Map map, IntVec3 position)
        {
            if (neededNearbyTerrains == null || neededNearbyTerrains.Count == 0)
            {
                return 0;
            }

            float output = -1;

            for (int xOffset = -(int)Math.Ceiling(neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (neededNearbyTerrains.Any(terrain.defName.Equals))
                        {
                            float distanceToPos = position.DistanceTo(checkedPosition);
                            if (output < 0 || output > distanceToPos) 
                            {
                                output = distanceToPos;
                            }
                        }
                        foreach (Thing thing in map.thingGrid.ThingsListAt(checkedPosition))
                        {
                            if (neededNearbyTerrains.Any(thing.def.defName.Equals))
                            {
                                float distanceToPos = position.DistanceTo(checkedPosition);
                                if (output < 0 || output > distanceToPos) 
                                {
                                    output = distanceToPos;
                                }
                            }
                        }

                    }
                }
            }

            return output;
        }

        // ======= Spawning individuals ======= //


        public virtual StaticMineral TrySpawnAt(IntVec3 dest, Map map, float size)
        {
            if (CanSpawnAt(map, dest))
            {
                return SpawnAt(map, dest, size);
            }
            else
            {
                return null;
            }
        }

        public virtual StaticMineral SpawnAt(Map map, IntVec3 dest, float size)
        {
            ThingCategory originalDef = category;
            category = ThingCategory.Attachment; // Hack to allow them to spawn on other minerals
            //StaticMineral output = (StaticMineral)GenSpawn.Spawn(this, dest, map);
            StaticMineral output = (StaticMineral)ThingMaker.MakeThing(this);
            GenSpawn.Spawn(output, dest, map);
            category = originalDef;
            output.size = size;
            map.mapDrawer.MapMeshDirty(dest, MapMeshFlag.Buildings);
            return output;
        }
            

        // ======= Reproduction ======= //



        public virtual bool TryFindReproductionDestination(Map map,  IntVec3 position, out IntVec3 foundCell)
        {
            if (( ! position.InBounds(map) ) || position.DistanceToEdge(map) <= Mathf.CeilToInt(spawnRadius))
            {
                foundCell = position;
                return false;
            }
//            Log.Message("TryFindReproductionDestination: " + position + " " + map + "  " + Mathf.CeilToInt(this.spawnRadius));
            Predicate<IntVec3> validator = c => c.InBounds(map) && CanSpawnAt(map, c);
            return CellFinder.TryFindRandomCellNear(position, map, Mathf.CeilToInt(spawnRadius), validator, out foundCell);
        }




        public virtual bool isRoofConditionOk(Map map, IntVec3 position)
        {
            if (mustBeUnderRoof && (! position.Roofed(map)))
            {
                return false;
            }
            if (mustBeUnderThickRoof && position.GetRoof(map).isThickRoof)
            {
                return false;
            }
            if (mustBeUnroofed && position.Roofed(map))
            {
                return false;
            }
            return true;
        }







        // ======= Map initialization ======= //


        public virtual void InitNewMap(Map map, float scaling = 1)
        {
            ReplaceThings(map, scaling);
            InitialSpawn(map, scaling);
        }


        public virtual void InitialSpawn(Map map, float scaling = 1)
        {

            // Check that it is a valid biome
            if (! CanSpawnInBiome(map))
            {
                Log.Message("Minerals: " + defName + " cannot be added to this biome");
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(minClusterProbability, maxClusterProbability) * StaticMineral.globalMineralAbundance * scaling;

            // Find spots to spawn it
            if (Rand.Range(0f, 1f) <= perMapProbability && spawnProbability > 0)
            {
                Log.Message("Minerals: " + defName + " will be spawned at a probability of " + spawnProbability);
                IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
                foreach (IntVec3 current in allCells)
                {
                    if (!current.InBounds(map))
                    {
                        continue;
                    }

                    // Randomly spawn some clusters
                    if (Rand.Range(0f, 1f) < spawnProbability && CanSpawnAt(map, current))
                    {
                        SpawnCluster(map, current);
                    }

                    // Spawn near their assocaited ore
                    if (PosIsAssociatedOre(map, current))
                    {

                        if (Rand.Range(0f, 1f) < spawnProbability * nearAssociatedOreBonus)
                        {

                            if (CanSpawnAt(map, current))
                            {
                                SpawnCluster(map, current);
                            } else {
                                IntVec3 dest;
                                if (current.InBounds(map) && TryFindReproductionDestination(map, current, out dest))
                                {
                                    SpawnCluster(map, dest);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //                Log.Message("Minerals: " + this.defName + " will not be spawned in this map.");
            }

        }


        public virtual void ReplaceThings(Map map, float scaling = 1)
        {
            if (ThingsToReplace == null || ThingsToReplace.Count == 0)
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

                // roof filters
                if (map.roofGrid.RoofAt(current) != null)
                {

                    if (mustBeUnderThickRoof && (! map.roofGrid.RoofAt(current).isThickRoof))
                    {
                        continue;
                    }

                    if (mustBeNotUnderThickRoof && map.roofGrid.RoofAt(current).isThickRoof)
                    {
                        continue;
                    }

                    if (mustBeUnderRoof && (! map.roofGrid.Roofed(current)))
                    {
                        continue;
                    }

                    if (mustBeUnroofed && map.roofGrid.Roofed(current))
                    {
                        continue;
                    }

                }
                    
                Thing ToReplace = ThingToReplaceAtPos(map, current);
                if (ToReplace != null)
                {

                    ToReplace.Destroy(DestroyMode.Vanish);
                    StaticMineral spawned = SpawnAt(map, current, Rand.Range(initialSizeMin, initialSizeMax));
                    map.edificeGrid.Register(spawned);
                }
            }
            map.regionAndRoomUpdater.Enabled = true;

        }

    }
        
}