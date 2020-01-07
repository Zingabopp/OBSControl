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
using System.Reflection;

namespace OBSControl.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionFlowCoordinator), "StartLevel",
        new Type[] {
        typeof(IDifficultyBeatmap),
        typeof(Action),
        typeof(bool)
        })]
    internal class LevelSelectionNavigationController_StartLevel
    {
        public static bool DelayedStartActive { get; private set; }
        public static bool WaitingToStart { get; private set; }
        static bool Prefix(LevelSelectionFlowCoordinator __instance, ref IDifficultyBeatmap difficultyBeatmap,
            ref Action beforeSceneSwitchCallback, ref bool practice,
            LevelSelectionNavigationController ____levelSelectionNavigationController)
        {
            if (!OBSController.instance.IsConnected)
            {
                Logger.log.Warn($"Not connected to OBS, skipping StartLevel override.");
                return true; 
            }
            if (DelayedStartActive && WaitingToStart)
                return false; // Ignore this call to StartLevel
            if(!WaitingToStart && DelayedStartActive) // Done waiting, start the level
            {
                DelayedStartActive = false;
                return true;
            }
            DelayedStartActive = true;
            WaitingToStart = true;
            Logger.log.Debug("LevelSelectionNavigationController_StartLevel");
            var detailViewController = ____levelSelectionNavigationController.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
            var levelView = detailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
            if (levelView != null)
                levelView.playButton.interactable = false;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, difficultyBeatmap, beforeSceneSwitchCallback, practice, levelView?.playButton));
            return false;
        }
        private static WaitForSeconds LevelStartDelay = new WaitForSeconds(Plugin.config.Value.LevelStartDelay);
        private static IEnumerator DelayedLevelStart(LevelSelectionFlowCoordinator coordinator,
            IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice,
            UnityEngine.UI.Button playButton)
        {
            IBeatmapLevel levelInfo = difficultyBeatmap.level;
            playButton.interactable = false;
            Logger.log.Debug($"Delaying level start by {Plugin.config.Value.LevelStartDelay} seconds...");
            if (levelInfo != null)
                Logger.log.Debug($"levelInfo is not null: {levelInfo.songName} by {levelInfo.levelAuthorName}");
            else
                Logger.log.Warn($"levelInfo is null, unable to set song file format.");
            SharedCoroutineStarter.instance.StartCoroutine(OBSController.instance.GetFileFormat(difficultyBeatmap));
            OBSController.instance.recordingCurrentLevel = true;
            yield return LevelStartDelay;
            WaitingToStart = false;
            //playButton.interactable = true;
            StartLevel(coordinator, difficultyBeatmap, beforeSceneSwitchCallback, practice);
            SharedCoroutineStarter.instance.StartCoroutine(OBSController.instance.GameStatusSetup());
        }

        private static StartLevelDelegate _startLevel;
        private static StartLevelDelegate StartLevel
        {
            get
            {
                if (_startLevel == null)
                {
                    var presentMethod = typeof(LevelSelectionFlowCoordinator).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _startLevel = (StartLevelDelegate)Delegate.CreateDelegate(typeof(StartLevelDelegate), presentMethod);
                }
                return _startLevel;
            }
        }
    }
    public delegate void StartLevelDelegate(LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice);



}
