using HarmonyLib;
using IPA.Utilities;
using OBSControl.OBSComponents;
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
#nullable enable
namespace OBSControl.HarmonyPatches
{
    [HarmonyPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), "StartLevel",
        new Type[] {
        typeof(Action),
        typeof(bool)
        })]
    internal class LevelSelectionNavigationController_StartLevel
    {
        internal static FieldAccessor<LevelSelectionNavigationController, LevelCollectionNavigationController>.Accessor AccessNavigationController =
            FieldAccessor<LevelSelectionNavigationController, LevelCollectionNavigationController>.GetAccessor("_levelCollectionNavigationController");
        internal static FieldAccessor<LevelCollectionNavigationController, StandardLevelDetailViewController>.Accessor AccessDetailViewController =
            FieldAccessor<LevelCollectionNavigationController, StandardLevelDetailViewController>.GetAccessor("_levelDetailViewController");
        internal static FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.Accessor AccessDetailView =
            FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.GetAccessor("_standardLevelDetailView");
        public static bool DelayedStartActive { get; private set; }

        /// <summary>
        /// Coroutine to start the level is active.
        /// </summary>
        public static bool WaitingToStart { get; private set; }
        static bool Prefix(SinglePlayerLevelSelectionFlowCoordinator __instance,
            ref Action beforeSceneSwitchCallback, ref bool practice,
            LevelSelectionNavigationController ___levelSelectionNavigationController)
        {
            if (RecordingController.instance == null)
            {
                Logger.log?.Warn($"RecordingController is null, unable to start recording.");
                return true;
            }
            if (!(OBSController.instance?.IsConnected ?? false))
            {
                Logger.log?.Warn($"Not connected to OBS, skipping StartLevel override.");
                return true;
            }
            if (Plugin.config.LevelStartDelay == 0)
            {
                RecordingController.instance.StartRecordingLevel();
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
            Logger.log?.Debug("LevelSelectionNavigationController_StartLevel");
            LevelCollectionNavigationController navigationController = AccessNavigationController(ref ___levelSelectionNavigationController);
            StandardLevelDetailViewController detailViewController = AccessDetailViewController(ref navigationController);
            StandardLevelDetailView levelView = AccessDetailView(ref detailViewController);
            if (levelView != null)
                levelView.actionButton.interactable = false;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, beforeSceneSwitchCallback, practice, levelView?.actionButton));
            return false;
        }

        private static IEnumerator DelayedLevelStart(SinglePlayerLevelSelectionFlowCoordinator coordinator,
            Action beforeSceneSwitchCallback, bool practice,
            UnityEngine.UI.Button? playButton)
        {
            if (playButton != null)
                playButton.interactable = false;
            else
                Logger.log?.Warn($"playButton is null for DelayedLevelStart, unable to disable while waiting.");
            Logger.log?.Debug($"Delaying level start by {Plugin.config.LevelStartDelay} seconds...");
            Task? startTask = RecordingController.instance?.StartRecordingLevel();
            if (startTask != null)
                yield return IPA.Utilities.Async.Coroutines.WaitForTask(startTask);
            yield return new WaitForSeconds(Plugin.config.LevelStartDelay);
            WaitingToStart = false;
            //playButton.interactable = true;
            StartLevel(coordinator, beforeSceneSwitchCallback, practice);
            if (RecordingController.instance != null)
                SharedCoroutineStarter.instance.StartCoroutine(RecordingController.instance.GameStatusSetup());
        }

        private static StartLevelDelegate? _startLevel;
        private static StartLevelDelegate StartLevel
        {
            get
            {
                if (_startLevel == null)
                {
                    MethodInfo presentMethod = typeof(SinglePlayerLevelSelectionFlowCoordinator).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _startLevel = (StartLevelDelegate)Delegate.CreateDelegate(typeof(StartLevelDelegate), presentMethod);
                }
                return _startLevel;
            }
        }
    }
    public delegate void StartLevelDelegate(SinglePlayerLevelSelectionFlowCoordinator coordinator, Action? beforeSceneSwitchCallback, bool practice);



}
