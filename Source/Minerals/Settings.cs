using Harmony;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace Minerals
{
    public class MineralsSettings : ModSettings
    {
        public float abundanceFactor = 1f;
        public static readonly float minAbundanceFactor = 0f;
        public static readonly float maxAbundanceFactor = 10f;

        public float occuranceFactor = 1f;
        public static readonly float minOccuranceFactor = 0f;
        public static readonly float maxOccuranceFactor = 10f;

        public float clusterFactor = 1f;
        public static readonly float minClusterFactor = 0f;
        public static readonly float maxClusterFactor = 10f;

        public float growthFactor = 1f;
        public static readonly float minGrowthFactor = 0f;
        public static readonly float maxGrowthFactor = 10f;

        public float reproductionFactor = 1f;
        public static readonly float minReproductionFactor = 0f;
        public static readonly float maxReproductionFactor = 10f;

        public float spawningFactor = 1f;
        public static readonly float minSpawningFactor = 0f;
        public static readonly float maxSpawningFactor = 10f;

        public bool replaceRockWalls = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref replaceRockWalls, "replaceRockWalls", true);
            Scribe_Values.Look(ref abundanceFactor, "abundanceFactor", 1f);
            Scribe_Values.Look(ref occuranceFactor, "occuranceFactor", 1f);
            Scribe_Values.Look(ref clusterFactor, "clusterFactor", 1f);
            Scribe_Values.Look(ref growthFactor, "growthFactor", 1f);
            Scribe_Values.Look(ref reproductionFactor, "reproductionFactor", 1f);
            Scribe_Values.Look(ref spawningFactor, "spawningFactor", 1f);
        }


        public void DoWindowContents(Rect inRect)
        {

            var list = new Listing_Standard { ColumnWidth = inRect.width - 34f };
            list.Begin(inRect);

            list.Gap(12f);

            list.Label("abundanceFactor".Translate() + ": " + Math.Round(abundanceFactor, 3), -1f);
            abundanceFactor = list.Slider(abundanceFactor, minAbundanceFactor, maxAbundanceFactor);

            list.Gap(12f);

            list.Label("occuranceFactor".Translate() + ": " + Math.Round(occuranceFactor, 3), -1f);
            occuranceFactor = list.Slider(occuranceFactor, minOccuranceFactor, maxOccuranceFactor);

            list.Gap(12f);

            list.Label("clusterFactor".Translate() + ": " + Math.Round(clusterFactor, 3), -1f);
            clusterFactor = list.Slider(clusterFactor, minClusterFactor, maxClusterFactor);

            list.Gap(12f);

            list.Label("growthFactor".Translate() + ": " + Math.Round(growthFactor, 3), -1f);
            growthFactor = list.Slider(growthFactor, minGrowthFactor, maxGrowthFactor);

            list.Gap(12f);

            list.Label("reproductionFactor".Translate() + ": " + Math.Round(reproductionFactor, 3), -1f);
            reproductionFactor = list.Slider(reproductionFactor, minReproductionFactor, maxReproductionFactor);

            list.Gap(12f);

            list.Label("spawningFactor".Translate() + ": " + Math.Round(spawningFactor, 3), -1f);
            spawningFactor = list.Slider(spawningFactor, minSpawningFactor, maxSpawningFactor);

            list.Gap(12f);

            list.CheckboxLabeled("replaceRockWalls".Translate(), ref replaceRockWalls);

            list.End();
        }
    }
}