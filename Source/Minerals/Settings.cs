using Harmony;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace Minerals
{
    public class MineralsSettings : ModSettings
    {
        public static float maxSetting = 15f;

        public float crystalAbundanceSetting = 1f;
        public float crystalDiversitySetting = 1f;
        public float boulderAbundanceSetting = 1f;
        public float rocksAbundanceSetting = 1f;
        public float mineralGrowthSetting = 1f;
        public float mineralReproductionSetting = 1f;
        public float mineralSpawningSetting = 1f;
        public bool replaceWallsSetting = true;
        public bool replaceChunksSetting = true;
        public bool includeFictionalSetting = true;
        public bool removeStartingChunksSetting = true;
        public bool underwaterMineralsSetting = true;
        public bool mineralsGrowUpWallsSetting = true;
        public bool snowyRockSetting = true;


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref crystalAbundanceSetting, "crystalAbundanceSetting", 1f);
            Scribe_Values.Look(ref crystalDiversitySetting, "crystalDiversitySetting", 1f);
            Scribe_Values.Look(ref boulderAbundanceSetting, "boulderAbundanceSetting", 1f);
            Scribe_Values.Look(ref rocksAbundanceSetting, "rocksAbundanceSetting", 1f);
            Scribe_Values.Look(ref mineralGrowthSetting, "mineralGrowthSetting", 1f);
            Scribe_Values.Look(ref mineralReproductionSetting, "mineralReproductionSetting", 1f);
            Scribe_Values.Look(ref mineralSpawningSetting, "mineralSpawningSetting", 1f);
            Scribe_Values.Look(ref replaceWallsSetting, "replaceWallsSetting", true);
            Scribe_Values.Look(ref replaceChunksSetting, "replaceChunksSetting", true);
            Scribe_Values.Look(ref includeFictionalSetting, "includeFictionalSetting", true);
            Scribe_Values.Look(ref removeStartingChunksSetting, "removeStartingChunksSetting", true);
            Scribe_Values.Look(ref underwaterMineralsSetting, "underwaterMineralsSetting", true);
            Scribe_Values.Look(ref mineralsGrowUpWallsSetting, "mineralsGrowUpWallsSetting", true);
            Scribe_Values.Look(ref snowyRockSetting, "snowyRockSetting", true);
        }


        public void DoWindowContents(Rect inRect)
        {

            var list = new Listing_Standard { ColumnWidth = inRect.width - 34f };
            list.Begin(inRect);

            list.Gap(12f);

            list.Label("crystalAbundanceSetting".Translate() + ": " + Math.Round(crystalAbundanceSetting * 100, 3) + "%", -1f);
            crystalAbundanceSetting = list.Slider(crystalAbundanceSetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("crystalDiversitySetting".Translate() + ": " + Math.Round(crystalDiversitySetting * 100, 3) + "%", -1f);
            crystalDiversitySetting = list.Slider(crystalDiversitySetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("boulderAbundanceSetting".Translate() + ": " + Math.Round(boulderAbundanceSetting * 100, 3) + "%", -1f);
            boulderAbundanceSetting = list.Slider(boulderAbundanceSetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("rocksAbundanceSetting".Translate() + ": " + Math.Round(rocksAbundanceSetting * 100, 3) + "%", -1f);
            rocksAbundanceSetting = list.Slider(rocksAbundanceSetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("mineralGrowthSetting".Translate() + ": " + Math.Round(mineralGrowthSetting * 100, 3) + "%", -1f);
            mineralGrowthSetting = list.Slider(mineralGrowthSetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("mineralReproductionSetting".Translate() + ": " + Math.Round(mineralReproductionSetting * 100, 3) + "%", -1f);
            mineralReproductionSetting = list.Slider(mineralReproductionSetting, 0, maxSetting);

            list.Gap(12f);

            list.Label("mineralSpawningSetting".Translate() + ": " + Math.Round(mineralSpawningSetting * 100, 3) + "%", -1f);
            mineralSpawningSetting = list.Slider(mineralSpawningSetting, 0, maxSetting);

            list.Gap(12f);

            list.CheckboxLabeled("replaceWallsSetting".Translate(), ref replaceWallsSetting);

            list.Gap(12f);

            list.CheckboxLabeled("replaceChunksSetting".Translate(), ref replaceChunksSetting);

            list.Gap(12f);

            list.CheckboxLabeled("removeStartingChunksSetting".Translate(), ref removeStartingChunksSetting);

            list.Gap(12f);

            list.CheckboxLabeled("includeFictionalSetting".Translate(), ref includeFictionalSetting);

            list.Gap(12f);

            list.CheckboxLabeled("underwaterMineralsSetting".Translate(), ref underwaterMineralsSetting);

            list.Gap(12f);

            list.CheckboxLabeled("mineralsGrowUpWallsSetting".Translate(), ref mineralsGrowUpWallsSetting);

            list.Gap(12f);

            list.CheckboxLabeled("snowyRockSetting".Translate(), ref snowyRockSetting);

            list.End();
        }
    }
}