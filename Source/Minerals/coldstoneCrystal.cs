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
	public class ColdstoneCrystal : Plant
    {
        // Constants
    
    
        public ThingDef_ColdstoneCrystal attributes
        {
            get
            {
                return this.def as ThingDef_ColdstoneCrystal;
            }
        }



		public static bool posIsNearIce(ThingDef_ColdstoneCrystal myDef, Map map, IntVec3 position)
		{
			for (int xOffset = -myDef.iceOffsetRadius; xOffset <= myDef.iceOffsetRadius; xOffset++)
			{
				for (int zOffset = -myDef.iceOffsetRadius; zOffset <= myDef.iceOffsetRadius; zOffset++)
				{
					IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
					if (checkedPosition.InBounds(map))
					{
                        TerrainDef terrain = map.terrainGrid.TerrainAt(checkedPosition);
						if (terrain.defName.Contains("Ice") || terrain.defName.Contains("ice"))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

             
             
        public bool isNearIce
        {
            get
            {
                return posIsNearIce(this.attributes, this.Map, this.Position);
            }

        }
        
        
       // Fertility.
        public float fertilityGrowthRateFactor
        {

            get
            {
				float my_fert = this.Map.fertilityGrid.FertilityAt(this.Position);
				if (my_fert < this.attributes.minFertility || my_fert > this.attributes.maxFertility)
				{
					return 0f;
				} else
				{
					return 1 - (this.attributes.maxFertility - my_fert) / this.attributes.maxFertility ;
				}
	
            }
        }
        
        

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
        
        
        public float lightGrowthRateFactor
        {
            get
            {
                float light = this.Map.glowGrid.GameGlowAt(this.Position);
				if (light >= this.attributes.minGrowLight)
				{
					return (light - this.attributes.minGrowLight) / (1 - this.attributes.minGrowLight);
				}
				else
				{
					return 0f;
				}
            }
        }
        
        
		public float sizeGrowthRateFactor
		{
			get
			{
				if (this.growthInt >= 1)
				{
					return 0f;
				}
				else
				{
					return (1.1f - this.growthInt) / 1.1f;
				}
			}
		}


		public override float GrowthRate
        {
            get
            {
				if (this.temperatureGrowthRateFactor > 0) // If not melting
				{
					return this.fertilityGrowthRateFactor * this.temperatureGrowthRateFactor * this.lightGrowthRateFactor * this.sizeGrowthRateFactor;
				}
				else
				{
					return this.temperatureGrowthRateFactor;
				}

                
            }
        }
        
       public new float GrowthPerTick
        {
            get
            {
//                if (this.LifeStage != PlantLifeStage.Growing)
//                {
//                    return 0f;
//                }
                float growthPerTick = (1f / (GenDate.TicksPerDay * this.def.plant.growDays));
                return growthPerTick * this.GrowthRate;
            }
        }


		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("PercentGrowth".Translate(new object[]
					{
						this.GrowthPercentString
					}));
			stringBuilder.AppendLine("GrowthRate".Translate() + ": " + this.GrowthRate.ToStringPercent());

//				stringBuilder.AppendLine("temperatureGrowthRateFactor".Translate() + ": " + this.temperatureGrowthRateFactor);
//				stringBuilder.AppendLine("fertilityGrowthRateFactor".Translate() + ": " + this.fertilityGrowthRateFactor);
//				stringBuilder.AppendLine("lightGrowthRateFactor".Translate() + ": " + this.lightGrowthRateFactor);


			if (this.temperatureGrowthRateFactor < 0)
			{
				stringBuilder.AppendLine("Evaporating in the heat.".Translate());
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}

//        
//        
//        // ===================== Setup Work =====================
//
//        /// <summary>
//        /// Save and load internal state variables (stored in savegame data).
//        /// </summary>
//        public override void ExposeData()
//        {
//            base.ExposeData();
//            //Scribe_References.Look<Cluster>(ref this.cluster, "cluster");
//        }
//
//
        // ===================== Main Work Function =====================
        public override void TickLong()
		{

			bool plantWasAlreadyMature = (this.LifeStage == PlantLifeStage.Mature);
			this.growthInt += this.GrowthPerTick * GenTicks.TickLongInterval;

			if (this.growthInt > 1)
			{
				this.growthInt = 1;
			}

			if (!plantWasAlreadyMature
			                 && (this.LifeStage == PlantLifeStage.Mature))
			{
				// Plant just became mature.
				this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things);
			}

			if (this.growthInt > this.attributes.minReproductionSize)
            {
                this.ageInt += GenTicks.TickLongInterval;
				if (Rand.MTBEventOccurs(this.def.plant.reproduceMtbDays, 60000, 2000))
				{
					ColdstoneCrystalReproduction.TryReproduceFrom(this, base.Position, this.attributes, this.attributes.spawnRadius, base.Map);
				}
            }

			if (this.growthInt <= 0)
			{
				base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 100000, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
			}
//            this.cachedLabelMouseover = null;
        }


        // ===================== Static exported functions =====================
		public static bool IsFertilityConditionOkAt(ThingDef_ColdstoneCrystal plantDef, Map map, IntVec3 position)
        {
            float fertility = map.fertilityGrid.FertilityAt(position);
            return ((fertility >= plantDef.minFertility) && (fertility <= plantDef.maxFertility));
        }

		public static bool IsLightConditionOkAt(ThingDef_ColdstoneCrystal plantDef, Map map, IntVec3 position)
        {
			return (map.glowGrid.GameGlowAt(position) >= plantDef.minGrowLight);
        }

		public static bool IsTemperatureConditionOkAt(ThingDef_ColdstoneCrystal plantDef, Map map, IntVec3 position)
        {
            float temperature = position.GetTemperature(map);
            return ((temperature >= plantDef.minGrowTemperature)
                && (temperature <= plantDef.maxGrowTemperature));
        }

		public static bool CanTerrainSupportPlantAt(ThingDef_ColdstoneCrystal myDef, Map map, IntVec3 position)
        {
            if (IsFertilityConditionOkAt(myDef, map, position) == false)
            {
                return false;
            }
            if (IsTemperatureConditionOkAt(myDef, map, position) == false)
            {
                return false;
            }
            if (myDef.mustBeNextToIce && posIsNearIce(myDef, map, position) == false)
            {
                return false;
            }
			List<Thing> list = map.thingGrid.ThingsListAt(position);
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing = list[i];
				if (thing.def.BlockPlanting)
				{
					return false;
				}
				if (myDef.passability == Traversability.Impassable && (thing.def.category == ThingCategory.Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Plant))
				{
					return false;
				}
			}
                
            return true;
        }

        public static bool CanBiomeSuppoprtPlantAt(ThingDef_ColdstoneCrystal myDef, Map map) 
        {
            return myDef.allowedBiomes.Any(map.Biome.defName.Contains);
        }

        public static void spawnColdstoneCluster(Map map, IntVec3 position)
        {
            // Make a cluster center
            ThingDef_ColdstoneCrystal thingDef = (ThingDef_ColdstoneCrystal)ThingDef.Named("ColdstoneCrystal");
            ColdstoneCrystal crystal = (ColdstoneCrystal)ThingMaker.MakeThing(thingDef, null);
            crystal.Growth = Rand.Range(thingDef.minReproductionSize, 1);
            GenSpawn.Spawn(crystal, position, map);

            // Pick cluster size
            int clusterSize = (int)Rand.Range(thingDef.minClusterSize, thingDef.maxClusterSize);

            // Grow cluster 
            growCluster(map, crystal, position, clusterSize);
        }


        public static void growCluster(Map map, ColdstoneCrystal crystal, IntVec3 position, int times)
        {
            if (times > 0)
            {
                ColdstoneCrystal newGrowth = (ColdstoneCrystal)ColdstoneCrystalReproduction.TryReproduceFrom(crystal, position, crystal.attributes, crystal.attributes.spawnRadius, map);
                if (newGrowth != null)
                {
                    newGrowth.Growth = Rand.Range(0.05f, 1f);
                    growCluster(map, newGrowth, newGrowth.Position, times - 1);
                }

            }
        }


     }       
      
      

        

        
        
    /// <summary>
    /// ThingDef_ColdstoneCrystal class.
    /// </summary>
    /// <author>zachary-foster</author>
    /// <permission>No restrictions</permission>
    public class ThingDef_ColdstoneCrystal : ThingDef
    {
        public int maxStableTemperature = 5; // Will evaporate above this temperature
		public int maxGrowTemperature = -1; // Will not grow above this temperature
		public int minGrowTemperature = -100; // Will not grow below this temperature
        public int idealGrowTemperature = -30; // Grows fastest at this temperature
		public float meltRate = 0.5f; // How quickly it melts when above maxStableTemperature
        
		public float minGrowLight = 0.9f; // Must have this much light to grow
        
        public float minFertility = 0.1f;
        public float maxFertility = 0.6f;

        public bool mustBeNextToIce = true; // Must be within `iceOffsetRadius` to spawn
		public int iceOffsetRadius = 4;
		public int spawnRadius = 1;
		public float minReproductionSize = 0.5f;

		public float minClusterPorbability = 0.0001f; 
		public float maxClusterPorbability = 0.01f; 
		public int minClusterSize = 4;
		public int maxClusterSize = 10;

        public List<string> allowedBiomes = new List<string> { "IceSheet", "Tundra" };
    }





	public static class ColdstoneCrystalReproduction
	{
		public static bool TryFindReproductionDestination(ColdstoneCrystal crystal, IntVec3 source, ThingDef_ColdstoneCrystal plantDef, int radius, Map map, out IntVec3 foundCell)
		{
			Predicate<IntVec3> validator = (IntVec3 c) => source.InHorDistOf(c, radius) && GenSight.LineOfSight(source, c, map, true, null, 0, 0) && ColdstoneCrystal.CanTerrainSupportPlantAt(plantDef, map, c);
			return CellFinder.TryFindRandomCellNear(source, map, Mathf.CeilToInt(radius), validator, out foundCell);
		}

		public static Plant TryReproduceFrom(ColdstoneCrystal crystal, IntVec3 source, ThingDef_ColdstoneCrystal plantDef, int radius, Map map)
		{
			IntVec3 dest;
			if (!ColdstoneCrystalReproduction.TryFindReproductionDestination(crystal, source, plantDef, radius, map, out dest))
			{
				return null;
			}
			return ColdstoneCrystalReproduction.TryReproduceInto(dest, plantDef, map);
		}

		public static Plant TryReproduceInto(IntVec3 dest, ThingDef_ColdstoneCrystal myDef, Map map)
		{
			if (ColdstoneCrystal.CanTerrainSupportPlantAt(myDef, map, dest))
			{
				return (ColdstoneCrystal)GenSpawn.Spawn(myDef, dest, map);
			}
			else
			{
				return null;
			}
		}
	}


}



