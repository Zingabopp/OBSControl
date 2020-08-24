using HarmonyLib;
using OBSControl.Utilities;
using System;
#nullable enable
namespace OBSControl.HarmonyPatches
{

    [HarmonyPatch(typeof(LevelSelectionFlowCoordinator), "HandleStandardLevelDidFinish",
        new Type[] {
        typeof(LevelCompletionResults),
        typeof(IDifficultyBeatmap),
        typeof(GameplayModifiers),
        typeof(bool)
        })]
    internal class HandleStandardLevelDidFinishPatch
    {
        public static event EventHandler? LevelDidFinish;
        static void Postfix(LevelSelectionFlowCoordinator __instance)
        {
            // Does not trigger in Multiplayer
            LevelSelectionFlowCoordinator flowCoordinator = __instance;
            LevelDidFinish.RaiseEventSafe(flowCoordinator, nameof(LevelDidFinish));
        }
    }
}
