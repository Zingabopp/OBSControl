using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using BS_Utils;
using static OBSControl.Utilities.ReflectionUtil;

namespace OBSControl.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionFlowCoordinator), "HandleLevelSelectionNavigationControllerDidPressPlayButton",
        new Type[] {
        typeof(LevelSelectionNavigationController)
        })]
    class LevelSelectionNavigationController_DidPressPlay
    {
        static bool Prefix(LevelSelectionFlowCoordinator __instance, ref LevelSelectionNavigationController viewController)
        {
            if (!OBSController.instance.IsConnected)
            {
                Logger.log.Warn($"Not connected to OBS, skipping Play button override.");
                return true; 
            }
            Logger.log.Debug("In StandardLevelDetailViewController.HandleLevelDetailViewControllerDidPressPlayButton()");
            var detailViewController = viewController.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
            var levelView = detailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
            if (levelView != null)
                levelView.playButton.interactable = false;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, viewController.selectedDifficultyBeatmap.level, levelView?.playButton));
            return false;
        }
        private static WaitForSeconds LevelStartDelay = new WaitForSeconds(Plugin.config.Value.LevelStartDelay);
        private static IEnumerator DelayedLevelStart(LevelSelectionFlowCoordinator coordinator, IBeatmapLevel levelInfo, UnityEngine.UI.Button playButton)
        {
            playButton.interactable = false;
            Logger.log.Debug($"Delaying level start by {Plugin.config.Value.LevelStartDelay} seconds...");
            if (levelInfo != null)
                Logger.log.Debug($"levelInfo is not null: {levelInfo.songName} by {levelInfo.levelAuthorName}");
            else
                Logger.log.Warn($"levelInfo is null, unable to set song file format.");
            SharedCoroutineStarter.instance.StartCoroutine(OBSController.instance.GetFileFormat(levelInfo));
            OBSController.instance.recordingCurrentLevel = true;
            yield return LevelStartDelay;
            //playButton.interactable = true;
            coordinator.InvokePrivateMethod("StartLevelOrShow360Warning", null, false);
            SharedCoroutineStarter.instance.StartCoroutine(OBSController.instance.GameStatusSetup());
        }
    }




}
