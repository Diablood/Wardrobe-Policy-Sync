using System.Collections.Generic;
using Verse;
using RimWorld;

namespace WardrobePolicySync
{
    public class WardrobePolicyData : IExposable
    {
        public string selectedPolicyLabel;

        public List<string> allowedApparelDefNames = new List<string>();
        public List<string> allowedSpecialFilterDefNames = new List<string>();

        public QualityRange qualityRange = QualityRange.All;
        public FloatRange hpRange = new FloatRange(0f, 1f);

        public void ExposeData()
        {
            Scribe_Values.Look(ref selectedPolicyLabel, "selectedPolicyLabel");

            Scribe_Collections.Look(ref allowedApparelDefNames, "allowedApparelDefNames", LookMode.Value);
            Scribe_Collections.Look(ref allowedSpecialFilterDefNames, "allowedSpecialFilterDefNames", LookMode.Value);

            Scribe_Values.Look(ref qualityRange, "qualityRange");
            Scribe_Values.Look(ref hpRange, "hpRange");

            if (allowedApparelDefNames == null)
                allowedApparelDefNames = new List<string>();

            if (allowedSpecialFilterDefNames == null)
                allowedSpecialFilterDefNames = new List<string>();
        }
    }
}