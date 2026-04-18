using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WardrobePolicySync
{
    [HarmonyPatch(typeof(Building), "GetGizmos")]
    public static class Patch_Building_GetGizmos
    {
        private static Dictionary<Thing, WardrobePolicyData> dataStore = new Dictionary<Thing, WardrobePolicyData>();

        private static WardrobePolicyData GetData(Thing t)
        {
            WardrobePolicyData data;
            if (!dataStore.TryGetValue(t, out data))
            {
                data = new WardrobePolicyData();
                dataStore[t] = data;
            }
            return data;
        }

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building __instance)
        {
            foreach (var g in __result)
                yield return g;

            if (IsTargetRack(__instance))
            {
                var data = GetData(__instance);

                yield return new Command_Action
                {
                    defaultLabel = "WPS_ApplyPolicy".Translate(),
                    defaultDesc = "WPS_ApplyPolicyDesc".Translate(),
                    action = delegate
                    {
                        OpenPolicyMenu(__instance);
                    }
                };

                if (!string.IsNullOrEmpty(data.selectedPolicyLabel))
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "WPS_ClearPolicy".Translate(),
                        defaultDesc = "WPS_ClearPolicyDesc".Translate(),
                        action = delegate
                        {
                            data.selectedPolicyLabel = null;
                            data.allowedApparelDefNames.Clear();
                            data.qualityRange = QualityRange.All;
                            data.hpRange = new FloatRange(0f, 1f);

                            TryApplyPolicyToRack(__instance, data);

                            Messages.Message(
                                "WPS_PolicyCleared".Translate(),
                                MessageTypeDefOf.TaskCompletion
                            );
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "WPS_ReapplyPolicy".Translate(),
                        defaultDesc = "WPS_ReapplyPolicyDesc".Translate(),
                        action = delegate
                        {
                            bool refreshed = RefreshDataFromPolicyLabel(data);
                            bool applied = false;

                            if (refreshed)
                                applied = TryApplyPolicyToRack(__instance, data);

                            if (refreshed && applied)
                            {
                                Messages.Message(
                                    "WPS_PolicyReapplied".Translate(data.selectedPolicyLabel),
                                    MessageTypeDefOf.TaskCompletion
                                );
                            }
                            else
                            {
                                Messages.Message(
                                    "WPS_PolicyStoredButNotApplied".Translate(data.selectedPolicyLabel ?? "Unknown"),
                                    MessageTypeDefOf.CautionInput
                                );
                            }
                        }
                    };
                }
            }
        }

        private static bool IsTargetRack(Building building)
        {
            return building != null &&
                   (building.def.defName == "Building_OutfitStand" ||
                    building.def.defName == "Building_KidOutfitStand");
        }

        private static void OpenPolicyMenu(Building building)
        {
            var options = new List<FloatMenuOption>();
            var policies = Current.Game?.outfitDatabase?.AllOutfits;

            if (policies == null)
            {
                Messages.Message("WPS_NoPolicies".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (var policy in policies)
            {
                var localPolicy = policy;

                options.Add(new FloatMenuOption(localPolicy.label, delegate
                {
                    var data = GetData(building);
                    data.selectedPolicyLabel = localPolicy.label;

                    QualityRange q;
                    FloatRange hp;
                    data.allowedApparelDefNames = ExtractAllowedApparel(localPolicy, out q, out hp);
                    data.qualityRange = q;
                    data.hpRange = hp;

                    bool applied = TryApplyPolicyToRack(building, data);

                    if (applied)
                    {
                        Messages.Message(
                            "WPS_PolicyApplied".Translate(localPolicy.label),
                            MessageTypeDefOf.TaskCompletion
                        );
                    }
                    else
                    {
                        Messages.Message(
                            "WPS_PolicyStoredButNotApplied".Translate(localPolicy.label),
                            MessageTypeDefOf.CautionInput
                        );
                    }
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static List<string> ExtractAllowedApparel(ApparelPolicy policy, out QualityRange qualityRange, out FloatRange hpRange)
        {
            List<string> result = new List<string>();

            qualityRange = QualityRange.All;
            hpRange = new FloatRange(0f, 1f);

            if (policy == null || policy.filter == null)
                return result;

            qualityRange = policy.filter.AllowedQualityLevels;
            hpRange = policy.filter.AllowedHitPointsPercents;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!def.IsApparel)
                    continue;

                bool allowed = false;

                try
                {
                    allowed = policy.filter.Allows(def);
                }
                catch
                {
                    allowed = false;
                }

                if (allowed)
                    result.Add(def.defName);
            }

            return result;
        }

        private static bool RefreshDataFromPolicyLabel(WardrobePolicyData data)
        {
            if (data == null || string.IsNullOrEmpty(data.selectedPolicyLabel))
                return false;

            var policies = Current.Game?.outfitDatabase?.AllOutfits;
            if (policies == null)
                return false;

            ApparelPolicy matchedPolicy = null;

            foreach (var policy in policies)
            {
                if (policy.label == data.selectedPolicyLabel)
                {
                    matchedPolicy = policy;
                    break;
                }
            }

            if (matchedPolicy == null)
                return false;

            QualityRange q;
            FloatRange hp;

            data.allowedApparelDefNames = ExtractAllowedApparel(matchedPolicy, out q, out hp);
            data.qualityRange = q;
            data.hpRange = hp;

            return true;
        }

        private static bool TryApplyPolicyToRack(Building building, WardrobePolicyData data)
        {
            if (building == null || data == null)
                return false;

            ThingFilter filter = TryGetStorageFilter(building);
            if (filter == null)
                return false;

            try
            {
                filter.SetDisallowAll(null);
            }
            catch
            {
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!def.IsApparel)
                    continue;

                bool allow = data.allowedApparelDefNames.Contains(def.defName);

                try
                {
                    filter.SetAllow(def, allow);
                }
                catch
                {
                }
            }

            try
            {
                filter.AllowedQualityLevels = data.qualityRange;
                filter.AllowedHitPointsPercents = data.hpRange;
            }
            catch
            {
            }

            return true;
        }

        private static ThingFilter TryGetStorageFilter(Building building)
        {
            if (building == null)
                return null;

            if (building is Building_Storage buildingStorage)
            {
                if (buildingStorage.GetStoreSettings() != null &&
                    buildingStorage.GetStoreSettings().filter != null)
                {
                    return buildingStorage.GetStoreSettings().filter;
                }
            }

            object filterOrSettings = FindFilterOrStorageSettings(building);
            if (filterOrSettings is ThingFilter directFilter)
                return directFilter;

            if (filterOrSettings is StorageSettings settings && settings.filter != null)
                return settings.filter;

            if (building.AllComps != null)
            {
                foreach (ThingComp comp in building.AllComps)
                {
                    filterOrSettings = FindFilterOrStorageSettings(comp);

                    if (filterOrSettings is ThingFilter compFilter)
                        return compFilter;

                    if (filterOrSettings is StorageSettings compSettings && compSettings.filter != null)
                        return compSettings.filter;
                }
            }

            return null;
        }

        private static object FindFilterOrStorageSettings(object obj)
        {
            if (obj == null)
                return null;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = obj.GetType();

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (typeof(StorageSettings).IsAssignableFrom(field.FieldType))
                {
                    object value = field.GetValue(obj);
                    if (value != null)
                        return value;
                }

                if (typeof(ThingFilter).IsAssignableFrom(field.FieldType))
                {
                    object value = field.GetValue(obj);
                    if (value != null)
                        return value;
                }
            }

            foreach (PropertyInfo prop in type.GetProperties(flags))
            {
                if (!prop.CanRead)
                    continue;

                try
                {
                    if (typeof(StorageSettings).IsAssignableFrom(prop.PropertyType))
                    {
                        object value = prop.GetValue(obj, null);
                        if (value != null)
                            return value;
                    }

                    if (typeof(ThingFilter).IsAssignableFrom(prop.PropertyType))
                    {
                        object value = prop.GetValue(obj, null);
                        if (value != null)
                            return value;
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(Thing), "GetInspectString")]
    public static class Patch_InspectString
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            if (!(__instance is Building b) || !IsTargetRackStatic(b))
                return;

            var field = typeof(Patch_Building_GetGizmos)
                .GetField("dataStore", BindingFlags.NonPublic | BindingFlags.Static);

            var store = field.GetValue(null) as Dictionary<Thing, WardrobePolicyData>;
            if (store == null)
                return;

            WardrobePolicyData data;
            if (store.TryGetValue(b, out data) && !string.IsNullOrEmpty(data.selectedPolicyLabel))
            {
                if (!string.IsNullOrEmpty(__result))
                    __result += "\n";

                __result += "WPS_CurrentPolicy".Translate(data.selectedPolicyLabel);
            }
        }

        private static bool IsTargetRackStatic(Building building)
        {
            return building != null &&
                   (building.def.defName == "Building_OutfitStand" ||
                    building.def.defName == "Building_KidOutfitStand");
        }
    }

    [HarmonyPatch(typeof(Building_OutfitStand), "ExposeData")]
    public static class Patch_Building_OutfitStand_ExposeData
    {
        public static void Postfix(Building_OutfitStand __instance)
        {
            Patch_WardrobePolicyPersistence.ExposeThingData(__instance);
        }
    }

    [HarmonyPatch(typeof(Building_KidOutfitStand), "ExposeData")]
    public static class Patch_Building_KidOutfitStand_ExposeData
    {
        public static void Postfix(Building_KidOutfitStand __instance)
        {
            Patch_WardrobePolicyPersistence.ExposeThingData(__instance);
        }
    }

    public static class Patch_WardrobePolicyPersistence
    {
        public static void ExposeThingData(Thing thing)
        {
            var field = typeof(Patch_Building_GetGizmos)
                .GetField("dataStore", BindingFlags.NonPublic | BindingFlags.Static);

            var store = field.GetValue(null) as Dictionary<Thing, WardrobePolicyData>;
            if (store == null)
                return;

            WardrobePolicyData data;
            if (!store.TryGetValue(thing, out data))
            {
                data = new WardrobePolicyData();
                store[thing] = data;
            }

            Scribe_Values.Look(ref data.selectedPolicyLabel, "wps_selectedPolicyLabel");
            Scribe_Collections.Look(ref data.allowedApparelDefNames, "wps_allowedApparelDefNames", LookMode.Value);
            Scribe_Values.Look(ref data.qualityRange, "wps_qualityRange");
            Scribe_Values.Look(ref data.hpRange, "wps_hpRange");

            if (data.allowedApparelDefNames == null)
                data.allowedApparelDefNames = new List<string>();
        }
    }

    [HarmonyPatch(typeof(Building_OutfitStand), "SpawnSetup")]
    public static class Patch_OutfitStand_SpawnSetup
    {
        public static void Postfix(Building_OutfitStand __instance)
        {
            Patch_AutoSyncHelper.TryAutoSync(__instance);
        }
    }

    [HarmonyPatch(typeof(Building_KidOutfitStand), "SpawnSetup")]
    public static class Patch_KidOutfitStand_SpawnSetup
    {
        public static void Postfix(Building_KidOutfitStand __instance)
        {
            Patch_AutoSyncHelper.TryAutoSync(__instance);
        }
    }

    public static class Patch_AutoSyncHelper
    {
        public static void TryAutoSync(Building building)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;

            var field = typeof(Patch_Building_GetGizmos).GetField("dataStore", flags);
            var store = field.GetValue(null) as Dictionary<Thing, WardrobePolicyData>;
            if (store == null)
                return;

            WardrobePolicyData data;
            if (!store.TryGetValue(building, out data))
                return;

            if (string.IsNullOrEmpty(data.selectedPolicyLabel))
                return;

            var refreshMethod = typeof(Patch_Building_GetGizmos)
                .GetMethod("RefreshDataFromPolicyLabel", flags);

            var applyMethod = typeof(Patch_Building_GetGizmos)
                .GetMethod("TryApplyPolicyToRack", flags);

            if (refreshMethod == null || applyMethod == null)
                return;

            object refreshResult = refreshMethod.Invoke(null, new object[] { data });
            bool refreshed = refreshResult is bool b && b;

            if (!refreshed)
                return;

            applyMethod.Invoke(null, new object[] { building, data });
        }
    }
}