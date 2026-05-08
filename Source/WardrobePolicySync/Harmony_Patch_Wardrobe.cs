using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WardrobePolicySync
{
    [StaticConstructorOnStartup]
    public static class WPS_Icons
    {
        public static readonly Texture2D Apply = ContentFinder<Texture2D>.Get("UI/Commands/WPS_ApplyPolicy");
        public static readonly Texture2D Clear = ContentFinder<Texture2D>.Get("UI/Commands/WPS_ClearPolicy");
        public static readonly Texture2D Reapply = ContentFinder<Texture2D>.Get("UI/Commands/WPS_ReapplyPolicy");
    }

    [HarmonyPatch(typeof(Building), "GetGizmos")]
    public static class Patch_Building_GetGizmos
    {
        private static Dictionary<Thing, WardrobePolicyData> dataStore = new Dictionary<Thing, WardrobePolicyData>();

        private static WardrobePolicyData GetData(Thing thing)
        {
            if (!dataStore.TryGetValue(thing, out WardrobePolicyData data))
            {
                data = new WardrobePolicyData();
                dataStore[thing] = data;
            }

            NormalizeData(data);
            return data;
        }

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (!IsTargetRack(__instance))
            {
                yield break;
            }

            WardrobePolicyData data = GetData(__instance);

            yield return new Command_Action
            {
                defaultLabel = "WPS_ApplyPolicy".Translate(),
                defaultDesc = "WPS_ApplyPolicyDesc".Translate(),
                icon = WPS_Icons.Apply,
                action = delegate
                {
                    OpenPolicyMenu(__instance);
                }
            };

            if (HasActiveWpsPolicy(data))
            {
                yield return new Command_Action
                {
                    defaultLabel = "WPS_ClearPolicy".Translate(),
                    defaultDesc = "WPS_ClearPolicyDesc".Translate(),
                    icon = WPS_Icons.Clear,
                    action = delegate
                    {
                        ClearPolicyData(data);

                        // After disabling WPS, put the stand back in a safe vanilla/manual state.
                        // This repairs stands from older saves where an empty WPS filter was already applied.
                        TryRestoreRackToSafeManualState(__instance);

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
                    icon = WPS_Icons.Reapply,
                    action = delegate
                    {
                        bool refreshed = RefreshDataFromPolicyLabel(data);
                        bool applied = false;

                        if (refreshed)
                        {
                            applied = TryApplyPolicyToRack(__instance, data);
                        }

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

        private static bool IsTargetRack(Building building)
        {
            return building != null &&
                   building.def != null &&
                   (building.def.defName == "Building_OutfitStand" ||
                    building.def.defName == "Building_KidOutfitStand");
        }

        private static void OpenPolicyMenu(Building building)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<ApparelPolicy> policies = Current.Game?.outfitDatabase?.AllOutfits;

            if (policies == null)
            {
                Messages.Message("WPS_NoPolicies".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (ApparelPolicy policy in policies)
            {
                ApparelPolicy localPolicy = policy;

                options.Add(new FloatMenuOption(localPolicy.label, delegate
                {
                    WardrobePolicyData data = GetData(building);
                    data.isWpsManaged = true;
                    data.selectedPolicyLabel = localPolicy.label;

                    QualityRange qualityRange;
                    FloatRange hitPointsRange;

                    data.allowedApparelDefNames = ExtractAllowedApparel(localPolicy, out qualityRange, out hitPointsRange);
                    data.allowedSpecialFilterDefNames = ExtractAllowedSpecialFilters(localPolicy);
                    data.qualityRange = qualityRange;
                    data.hpRange = hitPointsRange;
                    NormalizeData(data);

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

        private static List<string> ExtractAllowedApparel(
            ApparelPolicy policy,
            out QualityRange qualityRange,
            out FloatRange hitPointsRange
        )
        {
            List<string> result = new List<string>();
            qualityRange = QualityRange.All;
            hitPointsRange = new FloatRange(0f, 1f);

            if (policy == null || policy.filter == null)
            {
                return result;
            }

            qualityRange = policy.filter.AllowedQualityLevels;
            hitPointsRange = policy.filter.AllowedHitPointsPercents;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!def.IsApparel)
                {
                    continue;
                }

                bool allowed;

                try
                {
                    allowed = policy.filter.Allows(def);
                }
                catch
                {
                    allowed = false;
                }

                if (allowed)
                {
                    result.Add(def.defName);
                }
            }

            return result;
        }

        private static List<string> ExtractAllowedSpecialFilters(ApparelPolicy policy)
        {
            List<string> result = new List<string>();

            if (policy == null || policy.filter == null)
            {
                return result;
            }

            foreach (SpecialThingFilterDef specialDef in DefDatabase<SpecialThingFilterDef>.AllDefsListForReading)
            {
                bool allowed;

                try
                {
                    allowed = policy.filter.Allows(specialDef);
                }
                catch
                {
                    allowed = false;
                }

                if (allowed)
                {
                    result.Add(specialDef.defName);
                }
            }

            return result;
        }

        private static bool RefreshDataFromPolicyLabel(WardrobePolicyData data)
        {
            NormalizeData(data);

            if (!HasActiveWpsPolicy(data))
            {
                return false;
            }

            List<ApparelPolicy> policies = Current.Game?.outfitDatabase?.AllOutfits;

            if (policies == null)
            {
                return false;
            }

            ApparelPolicy matchedPolicy = null;

            foreach (ApparelPolicy policy in policies)
            {
                if (policy.label == data.selectedPolicyLabel)
                {
                    matchedPolicy = policy;
                    break;
                }
            }

            if (matchedPolicy == null)
            {
                WPS_Log.Warning("WPS_LogPolicyNotFound".Translate(data.selectedPolicyLabel));
                return false;
            }

            QualityRange qualityRange;
            FloatRange hitPointsRange;

            data.allowedApparelDefNames = ExtractAllowedApparel(matchedPolicy, out qualityRange, out hitPointsRange);
            data.allowedSpecialFilterDefNames = ExtractAllowedSpecialFilters(matchedPolicy);
            data.qualityRange = qualityRange;
            data.hpRange = hitPointsRange;
            data.isWpsManaged = true;

            NormalizeData(data);
            return true;
        }

        private static bool TryApplyPolicyToRack(Building building, WardrobePolicyData data)
        {
            if (building == null || data == null)
            {
                return false;
            }

            NormalizeData(data);

            // Main safety rule: unmanaged stands are vanilla/manual stands.
            // WPS must not touch their filter, otherwise pawns can reject the stand as a storage target.
            if (!HasActiveWpsPolicy(data))
            {
                WPS_Log.Message("WPS_NoSelectedPolicy".Translate());
                return false;
            }

            ThingFilter filter = TryGetStorageFilter(building);

            if (filter == null)
            {
                WPS_Log.Warning("WPS_LogStorageFilterNotFound".Translate(building.def.defName));
                return false;
            }

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
                {
                    continue;
                }

                bool allow = data.allowedApparelDefNames.Contains(def.defName);

                try
                {
                    filter.SetAllow(def, allow);
                }
                catch
                {
                }
            }

            foreach (SpecialThingFilterDef specialDef in DefDatabase<SpecialThingFilterDef>.AllDefsListForReading)
            {
                bool allow = data.allowedSpecialFilterDefNames.Contains(specialDef.defName);

                try
                {
                    filter.SetAllow(specialDef, allow);
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

            WPS_Log.Message(
                "WPS_LogPolicyAppliedToRack".Translate(
                    data.selectedPolicyLabel ?? "Unknown",
                    building.def.defName
                )
            );

            return true;
        }

        private static bool TryRestoreRackToSafeManualState(Building building)
        {
            if (building == null)
            {
                return false;
            }

            StorageSettings currentSettings = TryGetStorageSettings(building);
            StorageSettings defaultSettings = building.def?.building?.defaultStorageSettings;

            if (currentSettings != null && defaultSettings != null)
            {
                try
                {
                    currentSettings.CopyFrom(defaultSettings);
                    WPS_Log.Message("WPS: restored default storage settings for " + building.def.defName + ".");
                    return true;
                }
                catch
                {
                }
            }

            ThingFilter currentFilter = TryGetStorageFilter(building);
            ThingFilter defaultFilter = defaultSettings?.filter;

            if (currentFilter != null && defaultFilter != null)
            {
                try
                {
                    currentFilter.CopyAllowancesFrom(defaultFilter);
                    currentFilter.AllowedQualityLevels = defaultFilter.AllowedQualityLevels;
                    currentFilter.AllowedHitPointsPercents = defaultFilter.AllowedHitPointsPercents;
                    WPS_Log.Message("WPS: restored default storage filter for " + building.def.defName + ".");
                    return true;
                }
                catch
                {
                }
            }

            // Last-resort repair for old saves with an empty poisoned filter.
            // This is only called when the player explicitly clears/disables WPS on the stand.
            if (currentFilter != null)
            {
                try
                {
                    currentFilter.SetDisallowAll(null);

                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (def.IsApparel)
                        {
                            currentFilter.SetAllow(def, true);
                        }
                    }

                    currentFilter.AllowedQualityLevels = QualityRange.All;
                    currentFilter.AllowedHitPointsPercents = new FloatRange(0f, 1f);
                    WPS_Log.Message("WPS: repaired outfit stand filter by allowing apparel.");
                    return true;
                }
                catch
                {
                }
            }

            WPS_Log.Warning("WPS: could not restore manual storage state for " + building.def.defName + ".");
            return false;
        }

        private static StorageSettings TryGetStorageSettings(Building building)
        {
            if (building == null)
            {
                return null;
            }

            if (building is IStoreSettingsParent storeSettingsParent)
            {
                try
                {
                    StorageSettings settings = storeSettingsParent.GetStoreSettings();

                    if (settings != null)
                    {
                        return settings;
                    }
                }
                catch
                {
                }
            }

            if (building is Building_Storage buildingStorage)
            {
                StorageSettings storeSettings = buildingStorage.GetStoreSettings();

                if (storeSettings != null)
                {
                    return storeSettings;
                }
            }

            object filterOrSettings = FindFilterOrStorageSettings(building);

            if (filterOrSettings is StorageSettings directSettings)
            {
                return directSettings;
            }

            if (building.AllComps != null)
            {
                foreach (ThingComp comp in building.AllComps)
                {
                    filterOrSettings = FindFilterOrStorageSettings(comp);

                    if (filterOrSettings is StorageSettings compSettings)
                    {
                        return compSettings;
                    }
                }
            }

            return null;
        }

        private static ThingFilter TryGetStorageFilter(Building building)
        {
            if (building == null)
            {
                return null;
            }

            StorageSettings settings = TryGetStorageSettings(building);

            if (settings?.filter != null)
            {
                return settings.filter;
            }

            object filterOrSettings = FindFilterOrStorageSettings(building);

            if (filterOrSettings is ThingFilter directFilter)
            {
                return directFilter;
            }

            if (filterOrSettings is StorageSettings directSettings && directSettings.filter != null)
            {
                return directSettings.filter;
            }

            if (building.AllComps != null)
            {
                foreach (ThingComp comp in building.AllComps)
                {
                    filterOrSettings = FindFilterOrStorageSettings(comp);

                    if (filterOrSettings is ThingFilter compFilter)
                    {
                        return compFilter;
                    }

                    if (filterOrSettings is StorageSettings compSettings && compSettings.filter != null)
                    {
                        return compSettings.filter;
                    }
                }
            }

            return null;
        }

        private static object FindFilterOrStorageSettings(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            System.Type type = obj.GetType();

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (typeof(StorageSettings).IsAssignableFrom(field.FieldType))
                {
                    object value = field.GetValue(obj);

                    if (value != null)
                    {
                        return value;
                    }
                }

                if (typeof(ThingFilter).IsAssignableFrom(field.FieldType))
                {
                    object value = field.GetValue(obj);

                    if (value != null)
                    {
                        return value;
                    }
                }
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                try
                {
                    if (typeof(StorageSettings).IsAssignableFrom(property.PropertyType))
                    {
                        object value = property.GetValue(obj, null);

                        if (value != null)
                        {
                            return value;
                        }
                    }

                    if (typeof(ThingFilter).IsAssignableFrom(property.PropertyType))
                    {
                        object value = property.GetValue(obj, null);

                        if (value != null)
                        {
                            return value;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool HasActiveWpsPolicy(WardrobePolicyData data)
        {
            return data != null && data.isWpsManaged && !string.IsNullOrEmpty(data.selectedPolicyLabel);
        }

        private static void ClearPolicyData(WardrobePolicyData data)
        {
            if (data == null)
            {
                return;
            }

            data.ClearWpsPolicy();
        }

        private static void NormalizeData(WardrobePolicyData data)
        {
            if (data == null)
            {
                return;
            }

            data.Normalize();
        }

        public static bool TryRefreshAndApply(Building building, WardrobePolicyData data)
        {
            if (!HasActiveWpsPolicy(data))
            {
                return false;
            }

            bool refreshed = RefreshDataFromPolicyLabel(data);

            if (!refreshed)
            {
                return false;
            }

            return TryApplyPolicyToRack(building, data);
        }

        public static bool TryGetStoredData(Thing thing, out WardrobePolicyData data)
        {
            data = null;

            if (dataStore == null || thing == null)
            {
                return false;
            }

            if (!dataStore.TryGetValue(thing, out data))
            {
                return false;
            }

            NormalizeData(data);
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "GetInspectString")]
    public static class Patch_InspectString
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            if (!(__instance is Building building) || !IsTargetRackStatic(building))
            {
                return;
            }

            if (Patch_Building_GetGizmos.TryGetStoredData(building, out WardrobePolicyData data) &&
                data != null &&
                data.isWpsManaged &&
                !string.IsNullOrEmpty(data.selectedPolicyLabel))
            {
                if (!string.IsNullOrEmpty(__result))
                {
                    __result += "\n";
                }

                __result += "WPS_CurrentPolicy".Translate(data.selectedPolicyLabel);
            }
        }

        private static bool IsTargetRackStatic(Building building)
        {
            return building != null &&
                   building.def != null &&
                   (building.def.defName == "Building_OutfitStand" ||
                    building.def.defName == "Building_KidOutfitStand");
        }
    }

    [HarmonyPatch(typeof(Thing), "ExposeData")]
    public static class Patch_Thing_ExposeData
    {
        public static void Postfix(Thing __instance)
        {
            if (__instance is Building building &&
                building.def != null &&
                (building.def.defName == "Building_OutfitStand" ||
                 building.def.defName == "Building_KidOutfitStand"))
            {
                Patch_WardrobePolicyPersistence.ExposeThingData(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "SpawnSetup")]
    public static class Patch_Thing_SpawnSetup
    {
        public static void Postfix(Thing __instance)
        {
            if (__instance is Building building &&
                building.def != null &&
                (building.def.defName == "Building_OutfitStand" ||
                 building.def.defName == "Building_KidOutfitStand"))
            {
                Patch_AutoSyncHelper.TryAutoSync(building);
            }
        }
    }

    public static class Patch_WardrobePolicyPersistence
    {
        public static void ExposeThingData(Thing thing)
        {
            FieldInfo field = typeof(Patch_Building_GetGizmos)
                .GetField("dataStore", BindingFlags.NonPublic | BindingFlags.Static);

            Dictionary<Thing, WardrobePolicyData> store =
                field?.GetValue(null) as Dictionary<Thing, WardrobePolicyData>;

            if (store == null || thing == null)
            {
                return;
            }

            if (!store.TryGetValue(thing, out WardrobePolicyData data))
            {
                data = new WardrobePolicyData();
                store[thing] = data;
            }

            data.Normalize();

            bool wasManagedBeforeLoad = data.isWpsManaged || !string.IsNullOrEmpty(data.selectedPolicyLabel);

            Scribe_Values.Look(ref data.isWpsManaged, "wps_isWpsManaged", wasManagedBeforeLoad);
            Scribe_Values.Look(ref data.selectedPolicyLabel, "wps_selectedPolicyLabel");
            Scribe_Collections.Look(ref data.allowedApparelDefNames, "wps_allowedApparelDefNames", LookMode.Value);
            Scribe_Collections.Look(ref data.allowedSpecialFilterDefNames, "wps_allowedSpecialFilterDefNames", LookMode.Value);
            Scribe_Values.Look(ref data.qualityRange, "wps_qualityRange", QualityRange.All);
            Scribe_Values.Look(ref data.hpRange, "wps_hpRange", new FloatRange(0f, 1f));

            data.Normalize();

            // Backward compatibility for saves made before wps_isWpsManaged existed.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && !string.IsNullOrEmpty(data.selectedPolicyLabel))
            {
                data.isWpsManaged = true;
            }
        }
    }

    public static class Patch_AutoSyncHelper
    {
        public static void TryAutoSync(Building building)
        {
            if (building == null)
            {
                return;
            }

            if (!Patch_Building_GetGizmos.TryGetStoredData(building, out WardrobePolicyData data))
            {
                return;
            }

            // Critical: a stand configured manually/vanilla must never be resynced by WPS.
            if (data == null || !data.isWpsManaged || string.IsNullOrEmpty(data.selectedPolicyLabel))
            {
                return;
            }

            Patch_Building_GetGizmos.TryRefreshAndApply(building, data);
        }
    }
}
