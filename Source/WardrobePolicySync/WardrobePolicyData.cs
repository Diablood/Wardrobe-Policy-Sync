using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WardrobePolicySync
{
    public class WardrobePolicyData : IExposable
    {
        public bool isWpsManaged = false;
        public string selectedPolicyLabel;
        public List<string> allowedApparelDefNames = new List<string>();
        public List<string> allowedSpecialFilterDefNames = new List<string>();
        public QualityRange qualityRange = QualityRange.All;
        public FloatRange hpRange = new FloatRange(0f, 1f);

        public bool HasActivePolicy => isWpsManaged && !string.IsNullOrEmpty(selectedPolicyLabel);

        public void ExposeData()
        {
            Scribe_Values.Look(ref isWpsManaged, "isWpsManaged", false);
            Scribe_Values.Look(ref selectedPolicyLabel, "selectedPolicyLabel");
            Scribe_Collections.Look(ref allowedApparelDefNames, "allowedApparelDefNames", LookMode.Value);
            Scribe_Collections.Look(ref allowedSpecialFilterDefNames, "allowedSpecialFilterDefNames", LookMode.Value);
            Scribe_Values.Look(ref qualityRange, "qualityRange");
            Scribe_Values.Look(ref hpRange, "hpRange");

            Normalize();

            // Backward compatibility for saves made before isWpsManaged existed.
            // A stand with a stored selected policy was previously considered WPS-managed.
            if (!string.IsNullOrEmpty(selectedPolicyLabel))
            {
                isWpsManaged = true;
            }
        }

        public void Normalize()
        {
            if (allowedApparelDefNames == null)
            {
                allowedApparelDefNames = new List<string>();
            }

            if (allowedSpecialFilterDefNames == null)
            {
                allowedSpecialFilterDefNames = new List<string>();
            }
        }

        public void ClearWpsPolicy()
        {
            isWpsManaged = false;
            selectedPolicyLabel = null;

            Normalize();
            allowedApparelDefNames.Clear();
            allowedSpecialFilterDefNames.Clear();

            qualityRange = QualityRange.All;
            hpRange = new FloatRange(0f, 1f);
        }
    }
}
