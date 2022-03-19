using UnityEngine;

namespace Race
{
    public static class GlobalInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetBuiltinCommands()
        {
            Race.Generated.CommandsInitialization.InitializeBuiltinCommands();
        }
    }
}