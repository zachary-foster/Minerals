
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
        protected float sizeWhenLastPrinted = 0f;
        protected int currentTextureIndex = 0;

        // The current size of the mineral
        protected float mySize = 1f;

        // Cache for mineral texture locations
        protected Vector3[] textureLocations;

        // Cache for mineral texture sizes
        protected float[] textureSizes;

        // Cache for mineral texture indexes
        protected int[] textureIndexes;

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

        public virtual void incPctYeild(float amount, Pawn miner)
        {
            // Increase yeild for when it is destroyed
            yieldPct += (float)Mathf.Min(amount, HitPoints) / (float)MaxHitPoints * miner.GetStatValue(StatDefOf.MiningYield, true);

            // Drop resources
            foreach (RandomResourceDrop toDrop in attributes.randomlyDropResources)
            {
                float dropChance = size * toDrop.DropProbability * ((float) Math.Min(amount, HitPoints) / (float) MaxHitPoints) * miner.GetStatValue(StatDefOf.MiningYield, true) * MineralsMain.Settings.resourceDropFreqSetting;
                if (Rand.Range(0f, 1f) < dropChance)
                {
                    ThingDef myThingDef = DefDatabase<ThingDef>.GetNamed(toDrop.ResourceDefName, false);
                    if (myThingDef != null)
                    {
                        int dropNum = (int) Math.Round(toDrop.CountPerDrop * MineralsMain.Settings.resourceDropAmountSetting);
                        if (dropNum >= 1)
                        {
                            Thing thing = ThingMaker.MakeThing(myThingDef, null);
                            thing.stackCount = dropNum;
                            GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near, null);
                        }
                   }

                }

            }

        }


        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {

            if (def.building.mineableThing != null && def.building.mineableYieldWasteable && dinfo.Def == DamageDefOf.Mining && dinfo.Instigator != null && dinfo.Instigator is Pawn)
            {
                incPctYeild(dinfo.Amount, (Pawn)dinfo.Instigator);
            }

            base.PreApplyDamage(ref dinfo, out absorbed);

        }

            

        // ======= Behavior ======= //

//        public override bool BlocksPawn(Pawn p)
//        {
//            return this.size >= 0.8f;
//        }

            
        // ======= Appearance ======= //

        public float GetSizeBasedOnNearest(Vector3 subcenter, float baseSize)
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

            float correctedSize = (0.75f - distToTrueCenter) * baseSize + (1 - distToNearest) * sizeOfNearest;
            //Log.Message("this.size=" + this.size + " sizeOfNearest=" + sizeOfNearest + " distToNearest=" + distToNearest + " distToTrueCenter=" + distToTrueCenter);
            //Log.Message(this.size + " -> " + correctedSize + "  dist = " + distToNearest);

            return attributes.visualSizeRange.LerpThroughRange(correctedSize);
        }

        public static float randPos(float clustering, float spread)
        {
            // Weighted average of normal and uniform distribution
            return (Rand.Gaussian(0, 0.2f) * clustering + Rand.Range(-0.5f, 0.5f) * (1 - clustering)) * spread;
        }

        public virtual float submersibleFactor()
        {
            // Check that underwater minerals are enabled
            if (!MineralsMain.Settings.underwaterMineralsSetting)
            {
                return 1f;
            }
            // Check that it is submersible
            if (attributes.submergedSize >= 1)
            {
                return 1f;
            }

            // Check if is on dry land
            TerrainDef myTerrain = Map.terrainGrid.TerrainAt(Position);

            if (!(myTerrain.defName.Contains("Water") || myTerrain.defName.Contains("IceShallow") || myTerrain.defName.Contains("MuddyIce")))
            {
                return 1f;
            }

            // count number of dry cells aroud it
            float dryCount = 0;
            float spotsChecked = 0;
            for (int xOffset = -attributes.submergedRadius; xOffset <= attributes.submergedRadius; xOffset++)
            {
                for (int zOffset = -attributes.submergedRadius; zOffset <= attributes.submergedRadius; zOffset++)
                {
                    spotsChecked = spotsChecked + 1;
                    IntVec3 checkedPosition = Position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(Map))
                    {
                        TerrainDef terrain = Map.terrainGrid.TerrainAt(checkedPosition);
                        if (!(terrain.defName.Contains("Water") || myTerrain.defName.Contains("IceShallow") || myTerrain.defName.Contains("MuddyIce")))
                        {
                            dryCount = dryCount + 1;
                        }
                    }
                }
            }

            // calculate 
            float propDry = dryCount / spotsChecked;
            return attributes.submergedSize + (1 - attributes.submergedSize) * propDry;
        }

        public virtual float printSizeFactor()
        {
            float effectiveSize = 1f;
            effectiveSize = effectiveSize * submersibleFactor();
            return effectiveSize;
        }

        public virtual float printSize()
        {
            return printSizeFactor() * size;
        }

        public virtual void initializeTextureLocations()
        {

            Rand.PushState();
            Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode();

            // initalize the array if it has not already been initalized
            if (textureLocations == null)
            {
                textureLocations = new Vector3[attributes.maxMeshCount];
            }

            // Calculate the location of each texture
            Vector3 trueCenter = this.TrueCenter();
            for (int i = 0; i < textureLocations.Length; i++)
            {
                Vector3 pos = trueCenter;
                pos.x += randPos(attributes.visualClustering, attributes.visualSpread * MineralsMain.Settings.visualSpreadFactor);
                pos.z += randPos(attributes.visualClustering, attributes.visualSpread * MineralsMain.Settings.visualSpreadFactor);
                pos.z += attributes.verticalOffset;
                pos.y = attributes.Altitude;
                textureLocations[i] = pos;
            }

            // The size effects the altitude, which is a location attribute, so:
            initializeTextureSizes();

            Rand.PopState();
        }

        public virtual Vector3 getTextureLocation(int index)
        {
            // initalize the array if it has not already been initalized
            if (textureLocations == null)
            {
                initializeTextureLocations();
            }

            // Return per-calculated location
            return(textureLocations[index]);
        }

        public virtual float customAltitude(int i) {
//            float zProportionOfTextureBottom = 1f - (getTextureLocation(i).z - (getTextureSize(i) / 2f)) / Map.Size.z;
//            float xPropDistToEven = Math.Abs(1f - ((getTextureLocation(i).x + 0.5f) % 2f));
            return attributes.Altitude;// + zProportionOfTextureBottom * 0.01f + xPropDistToEven * 0.001f / Map.Size.z;
        } 

        public virtual void initializeTextureSizes() {
        
            Rand.PushState();
            Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode();

            // initalize the array if it has not already been initalized
            if (textureSizes == null)
            {
                textureSizes = new float[attributes.maxMeshCount];
            }

            // Calculate the size of each texture
            for (int i = 0; i < textureLocations.Length; i++)
            {
                // Get location of texture
                Vector3 pos = getTextureLocation(i);

                // Adjust size for distance from center to other crystals
                float thisSize = GetSizeBasedOnNearest(pos, size);

                // Add random variation
                thisSize = thisSize + (thisSize * Rand.Range(- attributes.visualSizeVariation, attributes.visualSizeVariation));

                // Make large textures appear on top
                if (attributes.largeTexturesOnTop)
                {
                    textureLocations[i].y = customAltitude(i) + 0.01f * thisSize;
                }
                else
                {
                    textureLocations[i].y = customAltitude(i);
                }

                textureSizes[i] = thisSize;

            }

            Rand.PopState();

        }

        public virtual float getTextureSize(int index)
        {
            // initalize the array if it has not already been initalized
            if (textureSizes == null)
            {
                initializeTextureSizes();
            }

            // Return per-calculated location
            return(textureSizes[index]);
        }

        public virtual void initializeTextures() {

            Rand.PushState();
            Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode();

            // initalize the array if it has not already been initalized
            if (textureIndexes == null)
            {
                textureIndexes = new int[attributes.maxMeshCount];
            }
                
            List<int> possibilities = Enumerable.Range(0, attributes.getTexturePaths().Count).OrderBy(order=>Rand.Range(0, 100)).ToList();
            for (int i = 0; i < attributes.maxMeshCount; i++)
            {
                // get a new random set of textures if run out of options
                if (possibilities.Count == 0)
                {
                    possibilities = Enumerable.Range(0, attributes.getTexturePaths().Count).OrderBy(order=>Rand.Range(0, 100)).ToList();
                }
                textureIndexes[i] = possibilities[0];
                possibilities.RemoveAt(0);
            }

            Rand.PopState();

        }

        public virtual string getTexturePath()
        {
            // initalize the array if it has not already been initalized
            if (textureIndexes == null)
            {
                initializeTextures();
            }
                
            return(attributes.getTexturePaths()[textureIndexes[currentTextureIndex]]);
        }

        // https://stackoverflow.com/questions/2742276/how-do-i-check-if-a-type-is-a-subtype-or-the-type-of-an-object/2742288
        public static bool isSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                || potentialDescendant == potentialBase;
        }

        public static bool isMineral(Thing thing)
        {
            return isSameOrSubclass(typeof(StaticMineral), thing.GetType());
        }

        public static Thing isMineralWall(Map map, IntVec3 pos)
        {
            if (pos.InBounds(map))
            {
                List<Thing> list = pos.GetThingList(map);
                foreach (Thing item in list)
                {

                    if (isMineral(item) && item.def.passability == Traversability.Impassable)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public virtual float interactWithWalls(int i, ref Vector3 center, float size)
        {
            if (MineralsMain.Settings.mineralsGrowUpWallsSetting && attributes.growsUpWalls)
            {
                Vector3 squareCenter = this.TrueCenter();
                float leftOverlap = (squareCenter.x - 0.5f) - (center.x - size / 2);
                if (leftOverlap > 0) // left
                {
                    IntVec3 leftSide = Position - new IntVec3(1, 0, 0);
                    Thing leftWall = isMineralWall(Map, leftSide);
                    if (leftWall != null)
                    {
                        // Put half of the textures on the front of the wall
                        if (Rand.Bool)
                        {
                            center.y = leftWall.def.Altitude + 0.1f;
                        }
                        // make textures higher up the wall show on top
                        center.y = center.y + Math.Min(leftOverlap / size, 1f) * 0.1f;

                        // rotate based on proportion of texture overlapping
                        return Math.Min(90f * (leftOverlap / size), 90f);
                    }
                }
                float rightOverlap = (center.x + size / 2) - (squareCenter.x + 0.5f);
                if (rightOverlap > 0)
                {
                    IntVec3 rightSide = Position + new IntVec3(1, 0, 0);
                    Thing rightWall = isMineralWall(Map, rightSide);
                    if (rightWall != null)
                    {
                        // Put half of the textures on the front of the wall
                        if (Rand.Bool)
                        {
                            center.y = rightWall.def.Altitude + 0.1f;
                        }
                        // make textures higher up the wall show on top
                        center.y = center.y + Math.Min(rightOverlap / size, 1f) * 0.1f;

                        // rotate based on proportion of texture overlapping
                        return -Math.Min(90f * (rightOverlap / size), 90f);
                    }
                }
                float topOverlap = (center.z + size / 2) - (squareCenter.z + 0.5f);
                if (topOverlap > 0)
                {
                    IntVec3 topSide = Position + new IntVec3(0, 0, 1);
                    Thing topWall = isMineralWall(Map, topSide);
                    if (topWall != null)
                    {
                        center.y = topWall.def.Altitude + 0.1f;
                        return 180;
                    }
                }
            }
            if (attributes.printOverWalls)
            {
                Vector3 squareCenter = this.TrueCenter();
                float topOverlap = (center.z + size / 2) - (squareCenter.z + 0.4f);
                if (topOverlap > 0)
                {
                    IntVec3 topSide = Position + new IntVec3(0, 0, 1);
                    Thing topWall = isMineralWall(Map, topSide);
                    if (topWall != null)
                    {
                        center.y = topWall.def.Altitude + 0.001f;
                        return 0f;
                    }
                }
            }

            return 0f;
        }

        public virtual bool hiddenInSnow(int i)
        {
            return snowLevel() > attributes.snowTextureThreshold + (attributes.hideAtSnowDepth - attributes.snowTextureThreshold) * getTextureSize(i) / attributes.visualSizeRange.max;
        }

        public virtual void printSubTexture(SectionLayer layer, int i, float sizeFactor = 1f)
        {
            Rand.PushState();
            Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode() + i.GetHashCode();

            // Get location
            Vector3 center = getTextureLocation(i);

            // Get size
            float thisSize = getTextureSize(i) * sizeFactor;
            if (thisSize <= 0)
            {
                Rand.PopState();
                return;
            }

            // Check if snow is covering it
            if (hiddenInSnow(i))
            {
                Rand.PopState();
                return;
            }

            // Get rotation
            float thisRotation = interactWithWalls(i, ref center, thisSize);

            // Print image
            Material matSingle = Graphic.MatSingle;
            Vector2 sizeVec = new Vector2(thisSize, thisSize);
            Printer_Plane.PrintPlane(layer, center, sizeVec, matSingle, thisRotation, Rand.Bool, null, null, attributes.topVerticesAltitudeBias * thisSize, 0f);

            Rand.PopState();
        }


        public override void Print(SectionLayer layer)
        {

            // get print size
            float sizeFactor = printSizeFactor();

            if (sizeFactor <= 0)
            {
                return;
            }
 
            if (this.attributes.graphicData.graphicClass.Name != "Graphic_Random" || this.attributes.graphicData.linkType == LinkDrawerType.CornerFiller) {
                Rand.PushState();
                Rand.Seed = Position.GetHashCode() + attributes.defName.GetHashCode();
                currentTextureIndex = 0;
				base.Print(layer);
                Rand.PopState();
			} else {
                int numToPrint = Mathf.CeilToInt(printSize() * (float)attributes.maxMeshCount);
				if (numToPrint < 1)
				{
					numToPrint = 1;
				}
                currentTextureIndex = 0;
				for (int i = 0; i < numToPrint; i++)
				{
                    printSubTexture(layer, i, sizeFactor);
                    currentTextureIndex += 1;
				}
			}

        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (DebugSettings.godMode)
            {
                stringBuilder.AppendLine("Size: " + size.ToStringPercent());
                float propSubmerged = 1 - submersibleFactor();
                if (propSubmerged > 0)
                {
                    stringBuilder.AppendLine("Submerged: " + propSubmerged.ToStringPercent());
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref mySize, "mySize", 1);
        }

        public virtual float snowLevel()
        {
            if (Map == null)
            {
                return 0f;
            }
            if (attributes.passability == Traversability.Impassable)
            {
                if (Position.Roofed(Map))
                {
                    return 0f;
                }

                float total = 0f;
                int numChecked = 0;
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int zOffset = -1; zOffset <= 1; zOffset++)
                    {
                        IntVec3 checkedPosition = Position + new IntVec3(xOffset, 0, zOffset);
                        if (checkedPosition.InBounds(Map) && (! checkedPosition.Impassable(Map)))
                        {
                            total += checkedPosition.GetSnowDepth(Map);
                            numChecked += 1;
                        }
                    }
                }
                if (numChecked == 0)
                {
                    return 0f;
                }
                else
                {
                    return total / numChecked;
                }
            }
            else
            {
                return Position.GetSnowDepth(Map);
            }
        }


        public override Graphic Graphic
        {
            get
            {
      
                // Pick a random path 
                string printedTexturePath = getTexturePath();

                // Check if it should be snowy
                if (attributes.hasSnowyTextures && snowLevel() > attributes.snowTextureThreshold)
                {
                    printedTexturePath = printedTexturePath + "_s";
                }
                Graphic printedTexture = GraphicDatabase.Get<Graphic_Single>(printedTexturePath, attributes.graphicData.shaderType.Shader);

                // convert to corner filler if needed
                printedTexture = GraphicDatabase.Get<Graphic_Single>(printedTexture.path, printedTexture.Shader, printedTexture.drawSize, DrawColor, DrawColorTwo, printedTexture.data);
                if (attributes.graphicData.linkType == LinkDrawerType.CornerFiller)
                {
                     return new Graphic_LinkedCornerFiller(printedTexture);
                }
                else
                {
                    return  printedTexture;

                }

             }
        }

        public virtual float RandomColorProb(Color colorUsed) {
            Rand.PushState();
            Rand.Seed = Map.GetHashCode() + colorUsed.GetHashCode();
            float output = Rand.Range(0.1f, 1f);
            Rand.PopState();
            return output * output * output;
        }

        public override Color DrawColor {
            get
            {
                if (this.attributes.coloredByTerrain)
                {
                    TerrainDef terrain = this.Position.GetTerrain(this.Map);
                    if (terrain.graphic.Color == Color.white)
                    {
                        return base.DrawColor;
                    }
                    else
                    {
                        return terrain.graphic.Color;
                    }
                }

                if (this.attributes.randomColorsOne != null && this.attributes.randomColorsOne.Count > 0)
                {
                    if (attributes.seedRandomColorByMap)
                    {
                        return this.attributes.randomColorsOne.RandomElementByWeight(RandomColorProb);
                    }
                    else
                    {
                        return this.attributes.randomColorsOne.RandomElement();
                    }
          
                }

                return base.DrawColor;
            }
        }

        public override Color DrawColorTwo
        {
            get
            {
                if (this.attributes.randomColorsTwo != null && this.attributes.randomColorsTwo.Count > 0)
                {
                    if (attributes.seedRandomColorByMap)
                    {
                        return this.attributes.randomColorsTwo.RandomElementByWeight(RandomColorProb);
                    }
                    else
                    {
                        return this.attributes.randomColorsTwo.RandomElement();
                    }

                }

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
        public bool mustBeUnroofed = false;
        public bool mustBeNotUnderThickRoof = false;
        public bool mustBeNearPassable = false; 

        // The maximum number of images that will be printed per square
        public int maxMeshCount = 1;

        // The size range of images printed
        public FloatRange visualSizeRange  = new FloatRange(0.3f, 1.0f);
        public float visualClustering = 0.5f;
        public float visualSpread = 1.2f;
        public float visualSizeVariation = 0.1f;

        // If graphic overlapping with nearby wall textures are rotated
        public bool growsUpWalls = false;

        // If textures overlapping walls above them should be printed on top
        public bool printOverWalls = false;

        // If largest textures are printed on top, ro if vertical order matters
        public bool largeTexturesOnTop = false;

        // Other resources it might drop
        public List<RandomResourceDrop> randomlyDropResources;

        // If it can spawn on other things
        public bool canSpawnOnThings = false;

        // Things this mineral replaces when a map is initialized
        public List<string> ThingsToReplace; 

        // If it replaces everything
        public bool replaceAll = true;

        // If it must replace something in order to spawned
        public bool mustReplace = false;

        // If the primary color is based on the stone below it
        public bool coloredByTerrain = false;

        // If defined, randomly pick colors from this set
        public List<Color> randomColorsOne;
        public List<Color> randomColorsTwo;
        // If true, then the probability of each color is randomly chosen for each map, so each map has distinctive colors.
        public bool seedRandomColorByMap = false;

        // If smaller than 1, it looks smaller in water
        public float submergedSize = 1;
        public int submergedRadius = 2;

        // Tags which determine how some options behave
        public List<string> tags;

        // Has something to do with how textures on the same layer get stacked
        public float topVerticesAltitudeBias = 0.01f;

        public List<string> texturePaths;
        public List<string> snowTexturePaths;
        public bool hasSnowyTextures = false;
        // at what snow depth the snow texture is used, if it exists
        public float snowTextureThreshold = 0.5f;

        // How much to change the vertical position of the texture. Positive is up
        public float verticalOffset = 0f;


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


        public virtual StaticMineral SpawnCluster(Map map, IntVec3 position, float size, int clusterCount)
        {
            StaticMineral mineral = TrySpawnAt(position, map, size);
            if (mineral != null)
            {             
                GrowCluster(map, mineral, clusterCount);

            }
            return mineral;
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
            //if (defName == "BigColdstoneCrystal") Log.Message("ThingToReplaceAtPos: checking for " + defName +  " at " + position, true);
            if (ThingsToReplace == null || ThingsToReplace.Count == 0)
            {
                //if (defName == "BigColdstoneCrystal") Log.Message("ThingToReplaceAtPos: no replacement defined", true);
                return(null);
            }
            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (thing == null || thing.def == null)
                {
                    continue;
                }
                //if (defName == "BigColdstoneCrystal") Log.Message("ThingToReplaceAtPos: found " + thing.def.defName + " at " + position, true);
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
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: checking for " + defName + " at " + position, true);

            // Check that location is in the map
            if (! position.InBounds(map))
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: is in bounds" + position + " " + map, true);

            // Check that the terrain is ok
            if (! IsTerrainOkAt(map, position))
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: terrain is ok " + position, true);

            // Check that it is under a roof if it needs to be
            if (! isRoofConditionOk(map, position))
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: roof is ok " + position, true);

            // Check for things it must replace
            if (mustReplace && ThingToReplaceAtPos(map, position) == null)
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: replacement is ok " + position, true);

            // Look for stuff in the way
            if (PlaceIsBlocked(map, position))
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: not blocked " + position, true);

            // Check that it is near any needed terrains
            if (! isNearNeededTerrain(map, position))
            {
                return false;
            }
            //if (defName == "BigColdstoneCrystal") Log.Message("CanSpawnAt: can spawn " + position, true);

            return true;
        }

        public virtual bool PlaceIsBlocked(Map map, IntVec3 position)
        {
            if (ThingToReplaceAtPos(map, position) != null)
            {
                return false;
            }
            foreach (Thing thing in map.thingGrid.ThingsListAt(position))
            {
                if (thing == null || thing.def == null)
                {
                    continue;
                }

                // Blocked by pawns, items, and plants
                if (! canSpawnOnThings) {
                    if (thing.def.category == ThingCategory.Pawn ||
                        thing.def.category == ThingCategory.Item ||
                        thing.def.category == ThingCategory.Plant ||
                        thing.def.category == ThingCategory.Building
                    )
                    {
                        return true;
                    }
                }

                // Blocked by impassible things
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
                //if (defName == "BigColdstoneCrystal") Log.Message("IsTerrainOkAt: out of bounds", true);
                return false;
            }
            if (allowedTerrains == null || allowedTerrains.Count == 0)
            {
                //if (defName == "BigColdstoneCrystal") Log.Message("IsTerrainOkAt: no terrain needed", true);
                return true;
            }
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            // if (defName == "SmallFossils") Log.Message("IsTerrainOkAt: found terrain " + terrain.defName + ". checking if it is one of: " + String.Join(", ", allowedTerrains.ToArray()), true);
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
            // Remove things to replace
            Thing thingToRemove = ThingToReplaceAtPos(map, dest);
            if (thingToRemove != null)
            {
                thingToRemove.Destroy(DestroyMode.Vanish);
            }

            //ThingCategory originalDef = category;
            //category = ThingCategory.Attachment; // Hack to allow them to spawn on other minerals
            StaticMineral output = (StaticMineral)ThingMaker.MakeThing(this);
            GenSpawn.Spawn(output, dest, map, WipeMode.Vanish);
            //category = originalDef;
            output.size = size;
            map.mapDrawer.MapMeshDirty(dest, MapMeshFlag.Buildings);
            //Log.Message("Spawned " + defName + " at " + dest);
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
            Predicate<IntVec3> validator = c => c.DistanceTo(position) <= spawnRadius && CanSpawnAt(map, c);
            return CellFinder.TryFindRandomCellNear(position, map, Mathf.CeilToInt(spawnRadius), validator, out foundCell);
        }
            

        public virtual bool isRoofConditionOk(Map map, IntVec3 position)
        {
            if (mustBeUnderThickRoof && (map.roofGrid.RoofAt(position) == null || (! map.roofGrid.RoofAt(position).isThickRoof)))
            {
                return false;
            }

            if (mustBeNotUnderThickRoof && (map.roofGrid.RoofAt(position) != null && map.roofGrid.RoofAt(position).isThickRoof))
            {
                return false;
            }

            if (mustBeUnderRoof && (! map.roofGrid.Roofed(position)))
            {
                return false;
            }

            if (mustBeUnroofed && map.roofGrid.Roofed(position))
            {
                return false;
            }

            return true;
        }







        // ======= Map initialization ======= //


        public virtual void InitNewMap(Map map, float scaling = 1)
        {
            //Log.Message("Initializing mineral: " + this.defName);
            ReplaceThings(map, scaling);
            InitialSpawn(map, scaling);
        }

        public virtual float abundanceSettingFactor()
        {
            float factor = 1f;
            if (tags == null || tags.Count <= 0)
            {
                return factor;
            }
            if (tags.Contains("crystal"))
            {
                factor = factor * MineralsMain.Settings.crystalAbundanceSetting;
            }
            if (tags.Contains("boulder"))
            {
                factor = factor * MineralsMain.Settings.boulderAbundanceSetting;
            }
            if (tags.Contains("small_rock"))
            {
                factor = factor * MineralsMain.Settings.rocksAbundanceSetting;
            }
            if (tags.Contains("wall") && MineralsMain.Settings.replaceWallsSetting == false)
            {
                factor = 0f;
            }
            if (tags.Contains("fictional") && MineralsMain.Settings.includeFictionalSetting == false)
            {
                factor = 0f;
            }
            return factor;
        }

        public virtual float diversitySettingFactor()
        {
            float factor = 1f;
            if (tags == null || tags.Count <= 0)
            {
                return factor;
            }
            if (tags.Contains("crystal"))
            {
                factor = factor * MineralsMain.Settings.crystalDiversitySetting;
            }
            return factor;
        }

        public virtual void InitialSpawn(Map map, float scaling = 1)
        {

            // Check that it is a valid biome
            if (! CanSpawnInBiome(map))
            {
                //Log.Message("Minerals: " + defName + " cannot be added to this biome");
                return;
            }

            // Select probability of spawing for this map
            float spawnProbability = Rand.Range(minClusterProbability, maxClusterProbability) * scaling * abundanceSettingFactor();

            // Find spots to spawn it
            if (Rand.Range(0f, 1f) <= perMapProbability * diversitySettingFactor() && spawnProbability > 0)
            {
                //Log.Message("Minerals: " + defName + " will be spawned at a probability of " + spawnProbability);
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
                        SpawnCluster(map, current, Rand.Range(initialSizeMin, initialSizeMax), Rand.Range(minClusterSize, maxClusterSize));
                    }

                    // Spawn near their assocaited ore
                    if (PosIsAssociatedOre(map, current))
                    {

                        if (Rand.Range(0f, 1f) < spawnProbability * nearAssociatedOreBonus)
                        {

                            if (CanSpawnAt(map, current))
                            {
                                SpawnCluster(map, current, Rand.Range(initialSizeMin, initialSizeMax), Rand.Range(minClusterSize, maxClusterSize));
                            } else {
                                IntVec3 dest;
                                if (current.InBounds(map) && TryFindReproductionDestination(map, current, out dest))
                                {
                                    SpawnCluster(map, dest, Rand.Range(initialSizeMin, initialSizeMax), Rand.Range(minClusterSize, maxClusterSize));
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

        public virtual bool allowReplaceSetting()
        {
            bool output = true;
            if (replaceAll == false)
            {
                output = false;
            }
            if (tags.Contains("wall") && MineralsMain.Settings.replaceWallsSetting == false)
            {
                output = false;
            }
            if (tags.Contains("chunk_replacer") && MineralsMain.Settings.replaceChunksSetting == false)
            {
                output = false;
            }
            return output;
        }


        public virtual void ReplaceThings(Map map, float scaling = 1)
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

                // roof filters
                if (! isRoofConditionOk(map, current))
                {
                    continue;
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

        public virtual List<string> getTexturePaths()
        {
            if (texturePaths == null)
            {
                initTexturePaths();
            }
            return texturePaths;
        }

        public virtual void initTexturePaths()
        {
            // Get paths to textures
            string textureName = System.IO.Path.GetFileName(graphicData.texPath);
            texturePaths = new List<string> { };
            snowTexturePaths = new List<string> { };
            List<string> versions = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };
            foreach (string letter in versions)
            {
                string a_path = graphicData.texPath + "/" + textureName + letter;
                if (ContentFinder<Texture2D>.Get(a_path, false) != null)
                {
                    texturePaths.Add(a_path);
                    string snow_path = a_path + "_s";
                    if (ContentFinder<Texture2D>.Get(snow_path, false) != null)
                    {
                        hasSnowyTextures = true;
                        snowTexturePaths.Add(snow_path);
                    }
                }
            }

            // Check that there are enough snowy textures
            if (texturePaths.Count > 0 && snowTexturePaths.Count > 0 && texturePaths.Count != snowTexturePaths.Count)
            {
                Log.Warning("Minerals: Not an equal number of snowy and non-snowy textures for '" + graphicData.texPath + "'");
                hasSnowyTextures = false;
            }
            
        }

    }
        
}