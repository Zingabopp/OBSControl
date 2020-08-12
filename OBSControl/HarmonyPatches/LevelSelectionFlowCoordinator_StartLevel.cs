using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using IPA.Utilities;
using OBSControl.OBSComponents;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#nullable enable
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
            StandardLevelDetailViewController detailViewController = AccessDetailViewController(ref ____levelSelectionNavigationController);
            StandardLevelDetailView levelView = AccessDetailView(ref detailViewController);
            Button playButton = levelView.playButton;
            if (playButton != null)
            {
                PreviousText = GetButtonText(playButton);
                levelView.playButton.interactable = false;
                RecordStateChangedAction = new EventHandler<OutputState>((e, OutputState) =>
                {
                    string buttonText = OutputState switch
                    {
                        OutputState.Starting => RecordingText,
                        OutputState.Started => RecordingText,
                        OutputState.Stopping => NotRecording,
                        OutputState.Stopped => NotRecording,
                        _ => DefaultText
                    };
                    SetButtonText(playButton, buttonText);
                });
                SetButtonText(playButton, NotRecording);
            }
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(__instance, difficultyBeatmap, beforeSceneSwitchCallback, practice, playButton));
            return false;
        }
        private static string? PreviousText;
        private static string? GetButtonText(Button button)
        {
            TextMeshProUGUI? tmp = button?.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                return tmp.text;
            return null;
        }

        private static void SetButtonText(Button button, string text)
        {
            if(button == null)
            {
                Logger.log?.Debug("Button was null when trying to set colors.");
                return;
            }
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                button.SetButtonText(text);
            });
            
        }

        private static EventHandler<OutputState>? _recordStateChangedAction;
        internal static EventHandler<OutputState>? RecordStateChangedAction
        {
            get => _recordStateChangedAction;
            set
            {
                if (_recordStateChangedAction == value)
                    return;
                if (OBSController.instance == null)
                    return;
                OBSController.instance.RecordingStateChanged -= _recordStateChangedAction;
                _recordStateChangedAction = value;
                if (value != null)
                {
                    OBSController.instance.RecordingStateChanged -= value;
                    OBSController.instance.RecordingStateChanged += value;
                }
            }
        }

        static string DefaultText = "Play";
        static string NotRecording = "Waiting for OBS";
        static string RecordingText = "Recording";
        private static IEnumerator DelayedLevelStart(LevelSelectionFlowCoordinator coordinator,
            IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice,
            UnityEngine.UI.Button? playButton)
        {
            if (playButton != null)
                playButton.interactable = false;
            else
                Logger.log?.Warn($"playButton is null for DelayedLevelStart, unable to disable while waiting.");
            Logger.log?.Debug($"Delaying level start by {Plugin.config.LevelStartDelay} seconds...");
            RecordingController.instance?.StartRecordingLevel();
            yield return new WaitForSeconds(Plugin.config.LevelStartDelay); ;
            WaitingToStart = false;
            //playButton.interactable = true;
            StartLevel(coordinator, difficultyBeatmap, beforeSceneSwitchCallback, practice);
            if (playButton != null)
            {
                SetButtonText(playButton, PreviousText ?? DefaultText);
                PreviousText = null;
            }
            RecordStateChangedAction = null;
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
                    MethodInfo presentMethod = typeof(LevelSelectionFlowCoordinator).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _startLevel = (StartLevelDelegate)Delegate.CreateDelegate(typeof(StartLevelDelegate), presentMethod);
                }
                return _startLevel;
            }
        }
    }
    public delegate void StartLevelDelegate(LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice);



}
