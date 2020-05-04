﻿using HarmonyLib;
using IPA.Utilities;
using OBSControl.OBSComponents;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

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
        internal static FieldAccessor<LevelSelectionNavigationController, StandardLevelDetailViewController>.Accessor AccessDetailViewController =
            FieldAccessor<LevelSelectionNavigationController, StandardLevelDetailViewController>.GetAccessor("_levelDetailViewController");
        internal static FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.Accessor AccessDetailView =
            FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.GetAccessor("_standardLevelDetailView");
        public static bool DelayedStartActive { get; private set; }

        /// <summary>
        /// Coroutine to start the level is active.
        /// </summary>
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
            if (Plugin.config.LevelStartDelay == 0)
            {
                RecordingController.instance.StartRecordingLevel(difficultyBeatmap);
                SharedCoroutineStarter.instance.StartCoroutine(RecordingController.instance.GameStatusSetup());
                return true;
            }
            if (DelayedStartActive && WaitingToStart)
                return false; // Ignore this call to StartLevel
            if (!WaitingToStart && DelayedStartActive) // Done waiting, start the level
            {
                DelayedStartActive = false;
                return true;
            }
            DelayedStartActive = true;
            WaitingToStart = true;
            Logger.log.Debug("LevelSelectionNavigationController_StartLevel");
            StandardLevelDetailViewController detailViewController = AccessDetailViewController(ref ____levelSelectionNavigationController);
            StandardLevelDetailView levelView = AccessDetailView(ref detailViewController);
            if (levelView != null)
                levelView.playButton.interactable = false;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, difficultyBeatmap, beforeSceneSwitchCallback, practice, levelView?.playButton));
            return false;
        }

        private static IEnumerator DelayedLevelStart(LevelSelectionFlowCoordinator coordinator,
            IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice,
            UnityEngine.UI.Button playButton)
        {
            IBeatmapLevel levelInfo = difficultyBeatmap.level;
            playButton.interactable = false;
            Logger.log.Debug($"Delaying level start by {Plugin.config.LevelStartDelay} seconds...");
            if (levelInfo != null)
                Logger.log.Debug($"levelInfo is not null: {levelInfo.songName} by {levelInfo.levelAuthorName}");
            else
                Logger.log.Warn($"levelInfo is null, unable to set song file format.");
            RecordingController.instance.StartRecordingLevel(difficultyBeatmap);
            yield return new WaitForSeconds(Plugin.config.LevelStartDelay); ;
            WaitingToStart = false;
            //playButton.interactable = true;
            StartLevel(coordinator, difficultyBeatmap, beforeSceneSwitchCallback, practice);
            SharedCoroutineStarter.instance.StartCoroutine(RecordingController.instance.GameStatusSetup());
        }

        private static StartLevelDelegate _startLevel;
        private static StartLevelDelegate StartLevel
        {
            get
            {
                if (_startLevel == null)
                {
                    MethodInfo presentMethod = typeof(LevelSelectionFlowCoordinator).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _startLevel = (StartLevelDelegate)Delegate.CreateDelegate(typeof(StartLevelDelegate), presentMethod);
                }
                return _startLevel;
            }
        }
    }
    public delegate void StartLevelDelegate(LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice);



}
