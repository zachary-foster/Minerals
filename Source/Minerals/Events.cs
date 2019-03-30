
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions 
using Verse;         // RimWorld universal objects 

namespace Minerals
{
    public class IncidentWorker_NoticeMineral : IncidentWorker
    {
    
        ThingDef_StaticMineral typeToSpawn;
        Pawn finder;

            
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            Map map = (Map)parms.target;

            int maxMineSkill = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (pawn.IsColonist && pawn.skills.GetSkill(SkillDefOf.Mining).Level > maxMineSkill)
                {
                    maxMineSkill = pawn.skills.GetSkill(SkillDefOf.Mining).Level;
                    finder = pawn;
                }
            }
            if (maxMineSkill < 5)
            {
                return false;
            }
            if (maxMineSkill < 10 && Rand.Bool) {
                return false;
            }

            IntVec3 outPos;
            return this.TryFindRootCell(map, out outPos);
        }

        protected bool CanSpawnAt(IntVec3 c, Map map)
        {
            if (c.Fogged(map) || c.GetSnowDepth(map) > 0)
            {
                return false;
            }

            bool noticed = false;
            foreach (Pawn pawn in map.mapPawns.AllPawns.InRandomOrder())
            {
                if (pawn.IsColonist)
                {
                    int mineSkill = pawn.skills.GetSkill(SkillDefOf.Mining).Level;
                    if (mineSkill < 5 || (mineSkill < 10 && Rand.Bool) || pawn.Position.DistanceTo(c) > mineSkill)
                    {
                        continue;
                    }
                    noticed = true;
                    finder = pawn;
                }
            }
            if (noticed == false)
            {
                return false;
            }

            foreach (ThingDef_StaticMineral mineralType in DefDatabase<ThingDef_StaticMineral>.AllDefs.InRandomOrder())
            {
                if (mineralType.tags.Contains("NoticeMineral_Event") && mineralType.CanSpawnAt(map, c))
                {
                    typeToSpawn = mineralType;
                    return true;
                }
            }


            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 rootPos;
            if (!this.TryFindRootCell(map, out rootPos))
            {
                return false;
            }
            Thing thing = typeToSpawn.SpawnCluster(map, rootPos, Rand.Range(typeToSpawn.initialSizeMin, typeToSpawn.initialSizeMax), Rand.Range(typeToSpawn.minClusterSize, typeToSpawn.maxClusterSize));
            if (thing == null)
            {
                return false;
            }
            string text = string.Format(this.def.letterText, finder.Name, thing.def.label).CapitalizeFirst();
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, text, LetterDefOf.PositiveEvent, new TargetInfo(rootPos, map, false), null, null);
            return true;
        }

        private bool TryFindRootCell(Map map, out IntVec3 cell)
        {
            return CellFinderLoose.TryFindRandomNotEdgeCellWith(10, (IntVec3 x) => this.CanSpawnAt(x, map) && x.GetRoom(map, RegionType.Set_Passable).CellCount >= 64, map, out cell);
        }
    }


    public class IncidentWorker_BlueSnow : IncidentWorker_MakeGameCondition
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            if (MineralsMain.Settings.includeFictionalSetting == false)
            {
                return false;
            }

            Map map = (Map)parms.target;

            if (map.mapTemperature.OutdoorTemp > 0f || map.mapTemperature.OutdoorTemp < -20 || map.weatherManager.SnowRate < 0.1f)
            {
                return false;
            }

            return true;
        }


    }


    public class GameCondition_BlueSnow : GameCondition
    {

        public ThingDef_DynamicMineral coldstoneDef = DefDatabase<ThingDef_DynamicMineral>.GetNamed("ColdstoneCrystal");
        public int ticksPerSpawn = 100;
        public int currentTick = 1;

        public override float SkyGazeChanceFactor(Map map)
        {
            return base.SkyGazeChanceFactor(map) * 2;
        }

        public override float SkyGazeJoyGainFactor(Map map)
        {
            return base.SkyGazeJoyGainFactor(map) * 2;
        }
            
        public override void GameConditionTick()
        {
            currentTick += 1;
            foreach (Map aMap in this.AffectedMaps)
            {
                if (aMap.weatherManager.curWeather.defName != "BlueSnow")
                {
                    int previousWeatherAge = aMap.weatherManager.curWeatherAge;
                    aMap.weatherManager.TransitionTo(DefDatabase<WeatherDef>.GetNamed("BlueSnow"));
                    if (previousWeatherAge < 4000)
                    {
                        aMap.weatherManager.curWeatherAge = 4000 - previousWeatherAge;
                    }
                }
                if (aMap.mapTemperature.OutdoorTemp > 5f || aMap.mapTemperature.OutdoorTemp < -40)
                {
                    this.End();
                }
                if (currentTick > ticksPerSpawn)
                {
                    IntVec3 spawnPos = CellFinder.RandomCell(aMap);
                    coldstoneDef.TrySpawnAt(spawnPos, aMap, 0.2f);
                    currentTick = 1;
                }
            }
        }
            
    }


    public class WeatherOverlay_BlueSnow : WeatherOverlay_SnowHard
    {

        static Material SnowOverlayWorld;

        public WeatherOverlay_BlueSnow()
        {
            this.worldOverlayMat = WeatherOverlay_BlueSnow.SnowOverlayWorld;
            this.worldOverlayPanSpeed1 = 0.005f;
            this.worldPanDir1 = new Vector2(-0.3f, -1f);
            this.worldPanDir1.Normalize();
            this.worldOverlayPanSpeed2 = 0.006f;
            this.worldPanDir2 = new Vector2(-0.29f, -1f);
            this.worldPanDir2.Normalize();
            this.OverlayColor = new Color(0.3f,0.3f,1f);
        }

        static WeatherOverlay_BlueSnow()
        {
            WeatherOverlay_BlueSnow.SnowOverlayWorld = MatLoader.LoadMat("Weather/SnowOverlayWorld", -1);
        }
    }


}
