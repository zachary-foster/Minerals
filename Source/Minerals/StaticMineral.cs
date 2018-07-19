
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
                    myDistFromNeededTerrain = attributes.posDistFromNeededTerrain(this.Map, this.Position);
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
                return this.def as ThingDef_StaticMineral;
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
            this.yieldPct += (float)Mathf.Min(amount, this.HitPoints) / (float)base.MaxHitPoints * miner.GetStatValue(StatDefOf.MiningYield, true);
        }

        // hackish solution since I cant override Mineable.DestroyMined
        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            // Drop gems
            float dropChance = this.size * this.attributes.roughGemDropChance * ((float) Math.Min(dinfo.Amount, this.HitPoints) / (float) this.MaxHitPoints);
            // Log.Message("this.size: " + this.size);
            // Log.Message("this.attributes.roughGemDropChance: " + this.attributes.roughGemDropChance);
            // Log.Message("dinfo.Amount: " + dinfo.Amount);
            // Log.Message("this.HitPoints: " + this.HitPoints);
            // Log.Message("this.MaxHitPoints: " + this.MaxHitPoints);
            // Log.Message("dropChance: " + dropChance);
            if (Rand.Range(0f, 1f) < dropChance)
            {
                Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RoughGem", true), null);
                thing.stackCount = 1;
                GenPlace.TryPlaceThing(thing, base.Position, this.Map, ThingPlaceMode.Near, null);
            }

            // 
            if (this.def.building.mineableThing != null && this.def.building.mineableYieldWasteable && dinfo.Def == DamageDefOf.Mining && dinfo.Instigator != null && dinfo.Instigator is Pawn)
            {
                this.incPctYeild(dinfo.Amount, (Pawn)dinfo.Instigator);
            }
            if (this.size < this.yieldPct)
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
                    IntVec3 checkedPosition = this.Position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(this.Map))
                    {
                        List<Thing> list = this.Map.thingGrid.ThingsListAt(checkedPosition);
                        foreach (Thing item in list)
                        {
                            if (item.def.defName == this.attributes.defName)
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

            float correctedSize = (0.75f - distToTrueCenter) * this.size + (1 - distToNearest) * sizeOfNearest;
            //Log.Message("this.size=" + this.size + " sizeOfNearest=" + sizeOfNearest + " distToNearest=" + distToNearest + " distToTrueCenter=" + distToTrueCenter);
            //Log.Message(this.size + " -> " + correctedSize + "  dist = " + distToNearest);

            return this.attributes.visualSizeRange.LerpThroughRange(correctedSize);
        }

        public static float randPos(float clustering, float spread)
        {
            // Weighted average of normal and uniform distribution
            return (Rand.Gaussian(0, 0.2f) * clustering + Rand.Range(-0.5f, 0.5f) * (1 - clustering)) * spread;
        }

        public override void Print(SectionLayer layer)
        {

            Rand.PushState();
            Rand.Seed = this.Position.GetHashCode() + this.attributes.defName.GetHashCode();
            int numToPrint = Mathf.CeilToInt(this.size * (float)this.attributes.maxMeshCount);
            if (numToPrint < 1)
            {
                numToPrint = 1;
            }
            Vector3 trueCenter = this.TrueCenter();
            for (int i = 0; i < numToPrint; i++)
            {
                // Calculate location
                Vector3 center = trueCenter;
                center.y = this.attributes.Altitude;
                center.x += randPos(this.attributes.visualClustering, this.attributes.visualSpread);
                center.z += randPos(this.attributes.visualClustering, this.attributes.visualSpread);

                // Adjust size for distance from center to other crystals
                float thisSize = GetSizeBasedOnNearest(center);

                // Add random variation
                thisSize = thisSize + (thisSize * Rand.Range(- this.attributes.visualSizeVariation, this.attributes.visualSizeVariation));
                if (thisSize <= 0)
                {
                    continue;
                }

                // Print image
                Material matSingle = this.Graphic.MatSingle;
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
            Rand.PopState();
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Size: " + this.size.ToStringPercent());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.mySize, "mySize", 1, false);
        }


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
        public float minClusterProbability = 0f; 
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
        public List<string> allowedBiomes = new List<string> {};

        // The terrains this can appear on
        public List<string> allowedTerrains = new List<string> {};

        // The terrains this must be near to, but not necessarily on, and how far away it can be
        public List<string> neededNearbyTerrains = new List<string> {};
        public float neededNearbyTerrainRadius = 3f;

        // Controls how extra clusters are added near assocaited ore
        public List<string> associatedOres = new List<string> {};
        public float nearAssociatedOreBonus = 3f;

        // If true, growth rate and initial size depends on distance from needed terrains
        public bool neededNearbyTerrainSizeEffect = true;

        // If true, only grows under roofs
        public bool mustBeUnderRoof = true;
        public bool mustBeUnderThickRoof = false;
//        public bool mustBeUnderNaturalRoof = true;
        public bool mustBeUnroofed = false;

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
        public float roughGemDropChance = 0f;

        // If it can spawn on other things
        public bool canSpawnOnThings = false;


        // ======= Spawning clusters ======= //


        public StaticMineral TryReproduce(Map map, IntVec3 position)
        {
            IntVec3 dest;
            if (! this.TryFindReproductionDestination(map, position, out dest))
            {
                return null;
            }
            return this.TrySpawnAt(dest, map);
        }


        public virtual void SpawnCluster(Map map, IntVec3 position)
        {
            // Make a cluster center
            StaticMineral mineral = this.TrySpawnAt(position, map);
            if (mineral != null)
            {            
                mineral.size = Rand.Range(this.initialSizeMin, this.initialSizeMax);

                // Pick cluster size
                int clusterSize = (int)Rand.Range(this.minClusterSize, this.maxClusterSize);

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
                    newGrowth.size = Rand.Range(1f - this.initialSizeVariation, 1f + this.initialSizeVariation) * sourceMineral.size;
                    GrowCluster(map, newGrowth, times - 1);
                }

            }
        }



        // ======= Spawning conditions ======= //


        public virtual bool CanSpawnAt(Map map, IntVec3 position)
        {
            // Check that location is in the map
            if (! position.InBounds(map))
            {
                return false;
            }

            // Check that the terrain is ok
            if (! IsTerrainOkAt(map, position))
            {
                return false;
            }

            // Check that it is under a roof if it needs to be
            if (! this.isRoofConditionOk(map, position))
            {
                return false;
            }

            // Look for stuff in the way
            if (this.PlaceIsBlocked(map, position))
            {
                return false;
            }

            // Check that it is near any needed terrains
            if (! this.isNearNeededTerrain(map, position))
            {
                return false;
            }

            return true;
        }

        public virtual bool PlaceIsBlocked(Map map, IntVec3 position)
        {
            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (! this.canSpawnOnThings) {
                    // Blocked by pawns, items, and plants
                    if (thing.def.category == ThingCategory.Pawn ||
                        thing.def.category == ThingCategory.Item ||
                        thing.def.category == ThingCategory.Plant)
                    {
                        return true;
                    }
                }

                // Blocked by buildings, except low minerals
                if (thing.def.category == ThingCategory.Building)
                {
                    if (thing is StaticMineral && thing.def.defName != this.defName)
                    {
                        //                        Log.Message("Trying to spawn on mineral " + thing.def.defName);
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

        public virtual bool PosIsAssociatedOre(Map map, IntVec3 position)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            if (this.associatedOres.Any(terrain.defName.Equals))
            {
                return true;
            }

            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (
                    this.associatedOres.Any(thing.def.defName.Equals)
                )
                {
                    return true;
                }
            }
            return false;
        }


        public virtual bool CanSpawnInBiome(Map map) 
        {
            if (this.allowedBiomes == null || this.allowedBiomes.Count == 0)
            {
                return true;
            }
            else
            {
                return this.allowedBiomes.Any(map.Biome.defName.Equals);
            }
        }

        public virtual bool IsTerrainOkAt(Map map, IntVec3 position)
        {
            if (! position.InBounds(map))
            {
                return false;
            }
            if (this.allowedTerrains == null || this.allowedTerrains.Count == 0)
            {
                return true;
            }
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            return this.allowedTerrains.Any(terrain.defName.Equals);
        }

        public virtual bool isNearNeededTerrain(Map map, IntVec3 position)
        {
            if (this.neededNearbyTerrains == null || this.neededNearbyTerrains.Count == 0)
            {
                return true;
            }
            for (int xOffset = -(int)Math.Ceiling(this.neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(this.neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(this.neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(this.neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (this.neededNearbyTerrains.Any(terrain.defName.Equals) && position.DistanceTo(checkedPosition) < this.neededNearbyTerrainRadius)
                        {
                            return true;
                        }
                        foreach (Thing thing in map.thingGrid.ThingsListAt(checkedPosition))
                        {
                            if (this.neededNearbyTerrains.Any(thing.def.defName.Equals) && position.DistanceTo(checkedPosition) < this.neededNearbyTerrainRadius)
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
            float output = -1;

            for (int xOffset = -(int)Math.Ceiling(this.neededNearbyTerrainRadius); xOffset <= (int)Math.Ceiling(this.neededNearbyTerrainRadius); xOffset++)
            {
                for (int zOffset = -(int)Math.Ceiling(this.neededNearbyTerrainRadius); zOffset <= (int)Math.Ceiling(this.neededNearbyTerrainRadius); zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
                        if (this.neededNearbyTerrains.Any(terrain.defName.Equals))
                        {
                            float distanceToPos = position.DistanceTo(checkedPosition);
                            if (output < 0 || output > distanceToPos) 
                            {
                                output = distanceToPos;
                            }
                        }
                        foreach (Thing thing in map.thingGrid.ThingsListAt(checkedPosition))
                        {
                            if (this.neededNearbyTerrains.Any(thing.def.defName.Equals))
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


        public virtual StaticMineral TrySpawnAt(IntVec3 dest, Map map)
        {
            if (this.CanSpawnAt(map, dest))
            {
                return this.SpawnAt(map, dest);
            }
            else
            {
                return null;
            }
        }

        public virtual StaticMineral SpawnAt(Map map, IntVec3 dest)
        {
            ThingCategory originalDef = this.category;
            this.category = ThingCategory.Attachment; // Hack to allow them to spawn on other minerals
            StaticMineral output = (StaticMineral)GenSpawn.Spawn(this, dest, map);
            this.category = originalDef;
            output.size = 0.01f;
            map.mapDrawer.MapMeshDirty(dest, MapMeshFlag.Things);
            return output;
        }
            

        // ======= Reproduction ======= //



        public virtual bool TryFindReproductionDestination(Map map,  IntVec3 position, out IntVec3 foundCell)
        {
            if (! position.InBounds(map))
            {
                foundCell = position;
                return false;
            }

            Predicate<IntVec3> validator = (IntVec3 c) => c.InBounds(map) && position.InHorDistOf(c, this.spawnRadius) && this.CanSpawnAt(map, c);
            return CellFinder.TryFindRandomCellNear(position, map, Mathf.CeilToInt(this.spawnRadius), validator, out foundCell);
        }




        public virtual bool isRoofConditionOk(Map map, IntVec3 position)
        {
            if (this.mustBeUnderRoof && (! position.Roofed(map)))
            {
                return false;
            }
            if (this.mustBeUnderThickRoof && position.GetRoof(map).isThickRoof)
            {
                return false;
            }
            if (this.mustBeUnroofed && position.Roofed(map))
            {
                return false;
            }
            return true;
        }







        // ======= Map initialization ======= //


        public virtual void InitNewMap(Map map, float scaling = 1)
        {
            // Print to log

            // Check that it is a valid biome
            if (! this.CanSpawnInBiome(map))
            {
                Log.Message("Minerals: " + this.defName + " cannot be added to this biome");
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(this.minClusterProbability, this.maxClusterProbability) * StaticMineral.globalMineralAbundance * scaling;

            // Find spots to spawn it
            if (Rand.Range(0f, 1f) <= perMapProbability && spawnProbability > 0)
            {
                Log.Message("Minerals: " + this.defName + " will be spawned at a probability of " + spawnProbability);
                IEnumerable<IntVec3> allCells = map.AllCells.InRandomOrder(null);
                foreach (IntVec3 current in allCells)
                {
                    // Randomly spawn some clusters
                    if (current.InBounds(map) && Rand.Range(0f, 1f) < spawnProbability && this.CanSpawnAt(map, current))
                    {
                        this.SpawnCluster(map, current);
                    }

                    // Spawn near their assocaited ore
                    if (this.PosIsAssociatedOre(map, current))
                    {

                        if (Rand.Range(0f, 1f) < spawnProbability * this.nearAssociatedOreBonus)
                        {

                            if (this.CanSpawnAt(map, current))
                            {
                                this.SpawnCluster(map, current);
                            } else {
                                IntVec3 dest;
                                if (current.InBounds(map) && this.TryFindReproductionDestination(map, current, out dest))
                                {
                                    this.SpawnCluster(map, dest);
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
    }

    /// <summary>
    /// ThingDef_StaticMineral class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_StaticMineralBig : ThingDef_StaticMineral
    {
        public Traversability passibility = Traversability.Impassable; 
    }
}