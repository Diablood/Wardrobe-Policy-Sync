using HarmonyLib;
using Verse;

namespace WardrobePolicySync
{
    [StaticConstructorOnStartup]
    public static class ModStartup
    {
        static ModStartup()
        {
            Harmony harmony = new Harmony("diablood.wardrobepolicysync");
            harmony.PatchAll();
            WPS_Log.Message("WPS_LogLoaded".Translate());
        }
    }
}