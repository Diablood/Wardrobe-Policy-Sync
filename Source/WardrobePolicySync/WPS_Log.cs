using Verse;

namespace WardrobePolicySync
{
    public static class WPS_Log
    {
        private const string Prefix = "<color=#B86BFF>[Wardrobe Policy Sync]</color>";

        public static void Message(string message)
        {
            Log.Message(Prefix + " " + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(Prefix + " " + message);
        }

        public static void Error(string message)
        {
            Log.Error(Prefix + " " + message);
        }
    }
}
