using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using IPA.Utilities;
using OBSControl.OBSComponents;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    internal class StartLevelPatch
    {
        internal static FieldAccessor<LevelSelectionNavigationController, StandardLevelDetailViewController>.Accessor AccessDetailViewController =
            FieldAccessor<LevelSelectionNavigationController, StandardLevelDetailViewController>.GetAccessor("_levelDetailViewController");
        internal static FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.Accessor AccessDetailView =
            FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.GetAccessor("_standardLevelDetailView");
        static StartLevelPatch()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private static void OnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            Button? playButton = PlayButton;
            if (playButton != null)
                playButton.interactable = true;
        }

        public static bool DelayedStartActive { get; private set; }
        public static event EventHandler<LevelStartEventArgs>? LevelStarting;
        public static event EventHandler? DelayedLevelStarting;
        public static Button? PlayButton;

        /// <summary>
        /// Coroutine to start the level is active.
        /// </summary>
        public static bool WaitingToStart { get; private set; }
        static bool Prefix(LevelSelectionFlowCoordinator __instance, ref IDifficultyBeatmap difficultyBeatmap,
            ref Action beforeSceneSwitchCallback, ref bool practice,
            LevelSelectionNavigationController ____levelSelectionNavigationController)
        {

            if (WaitingToStart)
            {
                WaitingToStart = false;
                return true;
            }
            WaitingToStart = true;
            Logger.log?.Debug("LevelSelectionNavigationController_StartLevel");
            OBSController? obs = OBSController.instance;
            SceneController? sceneController = obs?.GetOBSComponent<SceneController>();
            if (obs == null || !obs.IsConnected)
            {
                Logger.log?.Warn($"Skipping StartLevel sequence, OBS is unavailable.");
                return true;
            }
            if (sceneController == null || !sceneController.ActiveAndConnected)
            {
                Logger.log?.Warn($"Skipping StartLevel sequence, SceneController is unavailable.");
                return true;
            }
            if(!sceneController.GetSceneSequenceEnabled())
            {
                Logger.log?.Warn($"Skipping StartLevel sequence, SceneController SceneSequence is not enabled.");
                return true;
            }
            StandardLevelDetailViewController detailViewController = AccessDetailViewController(ref ____levelSelectionNavigationController);
            StandardLevelDetailView levelView = AccessDetailView(ref detailViewController);
            Button playButton = levelView.playButton;
            PlayButton = playButton;
            playButton.interactable = false;
#if DEBUG
            //void StartingHandler(object sender, LevelStartEventArgs e)
            //{
            //    e.SetResponse(LevelStartResponse.Delayed);
            //    Logger.log?.Info($"Setting LevelStartResponse: {e.StartResponse}");
            //}
            //LevelStarting += StartingHandler;
            try
            {
                var handler = LevelStarting;
                if (handler == null)
                {
                    return true;
                }
                EventHandler<LevelStartEventArgs>[] invocations = handler.GetInvocationList().Select(d => (EventHandler<LevelStartEventArgs>)d).ToArray();
                LevelStartResponse response = LevelStartResponse.None;
                LevelStartEventArgs args = new LevelStartEventArgs(__instance, difficultyBeatmap, beforeSceneSwitchCallback, practice, playButton);
                for (int i = 0; i < invocations.Length; i++)
                {
                    try
                    {
                        invocations[i].Invoke(__instance, args);
                    }
                    catch (Exception ex)
                    {
                        Logger.log?.Error($"Error invoking handler '{invocations[i]?.Method.Name}': {ex.Message}");
                        Logger.log?.Debug(ex);
                    }
                }
                response = args.StartResponse;
                if (response == LevelStartResponse.None)
                {
                    Logger.log?.Debug($"No LevelStartResponse, skipping delayed start.");
                    return true;
                }
                if (response == LevelStartResponse.Handled)
                {
                    Logger.log?.Debug($"LevelStartResponse is handled, skipping delayed start.");
                    return false;
                }
                // Do delayed level start
                Logger.log?.Info($"Starting delayed level start sequence.");
                _ = sceneController.StartIntroSceneSequence(CancellationToken.None).ContinueWith(result =>
                {
                    LevelStartEventArgs levelStartInfo = args;
                    StartLevel(levelStartInfo.Coordinator, levelStartInfo.DifficultyBeatmap, levelStartInfo.BeforeSceneSwitchCallback, levelStartInfo.Practice);
                    if (levelStartInfo.PlayButton != null)
                    {
                        levelStartInfo.PlayButton.interactable = true;
                        string prevText = PreviousText;
                        if (prevText != null && prevText.Length > 0)
                        {
                            levelStartInfo.PlayButton.SetButtonText(PreviousText);
                            PreviousText = null;
                        }
                    }
                });
                return false;
            }
            finally
            {
                //LevelStarting -= StartingHandler;
            }
            return true;
#else
            RecordingController? recordingController = OBSController.instance.GetOBSComponent<RecordingController>();
            if (recordingController == null)
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
                recordingController.StartRecordingLevel();
                SharedCoroutineStarter.instance.StartCoroutine(recordingController.GameStatusSetup());
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
            SharedCoroutineStarter.instance.StartCoroutine(DelayedLevelStart(recordingController, __instance, difficultyBeatmap, beforeSceneSwitchCallback, practice, playButton));
            return false;
#endif
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
            if (button == null)
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
        private static IEnumerator DelayedLevelStart(RecordingController recordingController, LevelSelectionFlowCoordinator coordinator,
            IDifficultyBeatmap difficultyBeatmap, Action beforeSceneSwitchCallback, bool practice,
            UnityEngine.UI.Button? playButton)
        {
            if (playButton != null)
                playButton.interactable = false;
            else
                Logger.log?.Warn($"playButton is null for DelayedLevelStart, unable to disable while waiting.");
            Logger.log?.Debug($"Delaying level start by {Plugin.config.LevelStartDelay} seconds...");
            recordingController?.StartRecordingLevel();
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
            if (recordingController != null)
                SharedCoroutineStarter.instance.StartCoroutine(recordingController.GameStatusSetup());
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
    public delegate void StartLevelDelegate(LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action? beforeSceneSwitchCallback, bool practice);

    public class LevelStartEventArgs
    {
        public readonly LevelSelectionFlowCoordinator Coordinator;
        public readonly IDifficultyBeatmap DifficultyBeatmap;
        public readonly Action? BeforeSceneSwitchCallback;
        public readonly bool Practice;
        public readonly Button? PlayButton;
        /// <summary>
        /// How the StartLevel patch should behave.
        /// </summary>
        public LevelStartResponse StartResponse { get; protected set; }
        /// <summary>
        /// Delay in milliseconds.
        /// </summary>
        public int Delay { get; protected set; }
        protected List<LevelStartResponse>? StartResponses;
        public LevelStartResponse[] GetResponses() => StartResponses?.ToArray() ?? Array.Empty<LevelStartResponse>();
        public void SetResponse(LevelStartResponse response, int delayMs = 0)
        {
            if (StartResponses == null)
                StartResponses = new List<LevelStartResponse>(1);
            StartResponses.Add(response);
            if (StartResponse < response)
                StartResponse = response;
        }

        public LevelStartEventArgs(LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action? beforeSceneSwitchCallback, bool practice, Button? playButton)
        {
            Coordinator = coordinator;
            DifficultyBeatmap = difficultyBeatmap;
            BeforeSceneSwitchCallback = beforeSceneSwitchCallback;
            Practice = practice;
            PlayButton = playButton;
        }
    }

    public enum LevelStartResponse
    {
        /// <summary>
        /// Level should start normally.
        /// </summary>
        None = 0,
        /// <summary>
        /// Level should start after delay.
        /// </summary>
        Delayed = 1,
        /// <summary>
        /// Another component will handle starting the level.
        /// </summary>
        Handled = 2
    }

}
