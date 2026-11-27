using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WardrobePolicySync
{
    // Optional integration: no compile-time dependency on HAR.
    [HarmonyPatch]
    public static class WPS_HarCompatibility
    {
        private static readonly System.Type HarCompType = AccessTools.TypeByName("AlienRace.Comp_OutfitStandHAR");
        private static readonly PropertyInfo JuvenileBody = HarCompType == null ? null : AccessTools.Property(HarCompType, "IsJuvenileBodyType");
        private static readonly MethodInfo GizmosMethod = HarCompType == null ? null : AccessTools.Method(HarCompType, "CompGetGizmosExtra");

        private static bool Prepare() => GizmosMethod != null;
        private static MethodBase TargetMethod() => GizmosMethod;

        public static void ApplyBodyFilter(Building building, ThingFilter filter)
        {
            if (HarCompType == null || JuvenileBody == null || building.AllComps == null)
                return;

            foreach (ThingComp comp in building.AllComps)
            {
                if (!HarCompType.IsInstanceOfType(comp))
                    continue;

                bool child = (bool)JuvenileBody.GetValue(comp);
                SpecialThingFilterDef adultFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowAdultOnlyApparel");
                SpecialThingFilterDef childFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowChildOnlyApparel");
                // Preserve policy restrictions; only further restrict incompatible ages.
                if (child && adultFilter != null) filter.SetAllow(adultFilter, false);
                if (!child && childFilter != null) filter.SetAllow(childFilter, false);
                return;
            }
        }

        private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result)
        {
            foreach (Gizmo gizmo in __result)
            {
                if (gizmo is Command command &&
                    command.defaultDesc == "HAR.OutfitStandRaceCommandDesc".Translate().ToString() &&
                    (command.icon == null || command.icon == BaseContent.BadTex))
                    command.icon = WPS_Icons.Apply;

                yield return gizmo;
            }
        }
    }
}
