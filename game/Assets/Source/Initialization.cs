using UnityEngine;

namespace Race
{
    public class Initialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetBuiltinCommands()
        {
            Race.Generated.CommandsInitialization.InitializeBuiltinCommands();
        }
    }
}