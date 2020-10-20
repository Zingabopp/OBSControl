using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using HMUI;
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
    // TODO: Patch MenuTransitionHelpers.StartStandardLevel instead/also?
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
        private static object _ctsLock = new object();
        static CancellationTokenSource _cts = new CancellationTokenSource();
        static CancellationTokenSource CTS
        {
            get
            {
                lock (_ctsLock)
                {
                    return _cts;
                }
            }
        }
        public static void Cancel()
        {
            lock (_ctsLock)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
        }
        private static void OnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            Button? playButton = PlayButton;
            if (playButton != null)
                playButton.interactable = true;
        }

        public static event EventHandler<LevelStartingEventArgs>? LevelStarting;
        /// <summary>
        /// 
        /// </summary>
        public static event EventHandler<LevelStartEventArgs>? LevelStart;

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
            CancellationToken cancellationToken = CTS.Token;
            WaitingToStart = true;
            Logger.log?.Debug("LevelSelectionNavigationController_StartLevel");
            OBSController? obs = OBSController.instance;
            if (obs == null || !obs.IsConnected)
            {
                Logger.log?.Warn($"Skipping StartLevel sequence, OBS is unavailable.");
                return true;
            }
            StandardLevelDetailViewController detailViewController = AccessDetailViewController(ref ____levelSelectionNavigationController);
            StandardLevelDetailView levelView = AccessDetailView(ref detailViewController);
            Button playButton = levelView.actionButton;
            PlayButton = playButton;
            PreviousText = playButton.GetComponentInChildren<TextMeshProUGUI>()?.text;
            playButton.interactable = false;
            levelView.hidePracticeButton = true;
            bool returnValue = false;
            try
            {
                var handler = LevelStarting;
                if (handler == null)
                {
                    returnValue = true;
                    return true;
                }
                EventHandler<LevelStartingEventArgs>[] invocations = handler.GetInvocationList().Select(d => (EventHandler<LevelStartingEventArgs>)d).ToArray();
                LevelStartResponse response = LevelStartResponse.None;
                LevelStartingEventArgs args = new LevelStartingEventArgs(difficultyBeatmap, practice);
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
                LevelStartEventArgs startEventArgs = new LevelStartEventArgs(args, StartLevel, __instance, difficultyBeatmap,
                    beforeSceneSwitchCallback, practice, playButton, PreviousText ?? DefaultText);
                if (response == LevelStartResponse.None)
                {
                    Logger.log?.Debug($"No LevelStartResponse, skipping delayed start.");
                    Utilities.Utilities.RaiseEventSafe(LevelStart, __instance, startEventArgs, nameof(LevelStart));
                    returnValue = true;
                    return true;
                }
                else
                {
                    
                }
                if (response == LevelStartResponse.Immediate)
                {
                    Logger.log?.Debug("LevelStartResponse is Immediate, skipping delayed start.");
                    FadeOutPreview();
                    Utilities.Utilities.RaiseEventSafe(LevelStart, __instance, startEventArgs, nameof(LevelStart));
                    returnValue = true;
                    return true;
                }
                if (response == LevelStartResponse.Handled)
                {
                    Logger.log?.Debug($"LevelStartResponse is handled, skipping delayed start.");
                    FadeOutPreview();
                    Utilities.Utilities.RaiseEventSafe(LevelStart, __instance, startEventArgs, nameof(LevelStart));
                    returnValue = false;
                    return false;
                }
                if (response == LevelStartResponse.Delayed)
                {
                    Logger.log?.Info($"Starting delayed level start sequence.");
                    _ = StartDelayedLevelStart(__instance, startEventArgs, () =>
                    {
                        LevelStartEventArgs levelStartInfo = startEventArgs;
                        StartLevel(levelStartInfo.Coordinator, levelStartInfo.DifficultyBeatmap, levelStartInfo.BeforeSceneSwitchCallback, levelStartInfo.Practice);
                        if (levelStartInfo.PlayButton != null)
                        {
                            levelStartInfo.PlayButton.interactable = true;
                            string? prevText = PreviousText;
                            if (prevText != null && prevText.Length > 0)
                            {
                                levelStartInfo.PlayButton.SetButtonText(PreviousText);
                                PreviousText = null;
                            }
                        }
                    });
                    returnValue = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in StartLevel patch: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            finally
            {
                if (returnValue)
                {
                    playButton.interactable = true;
                    levelView.hidePracticeButton = true;
                }
            }
            playButton.interactable = true;
            levelView.hidePracticeButton = true;
            return true;
        }

        private static void FadeOutPreview()
        {
            try
            {
                SongPreviewPlayer? previewPlayer = GameObject.FindObjectsOfType<SongPreviewPlayer>().FirstOrDefault();
                if (previewPlayer != null)
                {
                    previewPlayer.FadeOut();
                }
                else
                    Logger.log?.Debug($"Couldn't find SongPreviewPlayer.");
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error fading out song preview: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }
        private async static Task StartDelayedLevelStart(LevelSelectionFlowCoordinator sender, LevelStartEventArgs args, Action continuation)
        {
            FadeOutPreview();
            await Task.Delay(500);
            Utilities.Utilities.RaiseEventSafe(LevelStart, sender, args, nameof(LevelStart));
            TimeSpan levelStartDelay = TimeSpan.FromSeconds(Plugin.config.LevelStartDelay);
            if (levelStartDelay > TimeSpan.Zero)
                await Task.Delay(levelStartDelay);
            try
            {
                continuation?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Starting level in delayed level start continuation: {ex.Message}");
                Logger.log?.Debug(ex);
            }
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
    public class LevelStartEventArgs : EventArgs
    {
        public readonly StartLevelDelegate StartLevel;
        public readonly LevelSelectionFlowCoordinator Coordinator;
        public readonly IDifficultyBeatmap DifficultyBeatmap;
        public readonly Action? BeforeSceneSwitchCallback;
        public readonly bool Practice;
        public readonly Button? PlayButton;
        public readonly string PreviousPlayButtonText;
        public readonly LevelStartResponse StartResponseType;
        public readonly string? StartHandlerName;
        public readonly int StartDelay;

        public void ResetPlayButton()
        {
            if (PlayButton != null)
            {
                PlayButton.interactable = true;
                if (PreviousPlayButtonText != null && PreviousPlayButtonText.Length > 0)
                {
                    PlayButton.SetButtonText(PreviousPlayButtonText);
                }
            }
        }

        public LevelStartEventArgs(LevelStartingEventArgs args, StartLevelDelegate startLevelDelegate, LevelSelectionFlowCoordinator coordinator, IDifficultyBeatmap difficultyBeatmap, Action? beforeSceneSwitchCallback, bool practice, Button? playButton, string previousPlayText)
        {
            StartResponseType = args.StartResponse;
            StartHandlerName = args.ResponseSource;
            if (args.StartResponse == LevelStartResponse.Delayed)
                StartDelay = args.Delay;
            StartLevel = startLevelDelegate;
            Coordinator = coordinator;
            DifficultyBeatmap = difficultyBeatmap;
            BeforeSceneSwitchCallback = beforeSceneSwitchCallback;
            Practice = practice;
            PlayButton = playButton;
            PreviousPlayButtonText = previousPlayText;
        }
    }
    public class LevelStartingEventArgs : EventArgs
    {
        public readonly IDifficultyBeatmap DifficultyBeatmap;
        public readonly bool Practice;

        public string? ResponseSource => Response.SourceName;

        /// <summary>
        /// How the StartLevel patch should behave.
        /// </summary>
        public LevelStartResponse StartResponse => Response.LevelStartResponse;
        /// <summary>
        /// Delay in milliseconds, ignored if there's is a <see cref="LevelStartResponse.Handled"/> response added.
        /// </summary>
        public int Delay { get; protected set; }

        protected List<StartResponse>? StartResponses;

        public StartResponse[] GetResponses() => StartResponses?.ToArray() ?? Array.Empty<StartResponse>();

        protected StartResponse Response;

        protected bool SetResponse(StartResponse response)
        {
            if (StartResponses == null)
                StartResponses = new List<StartResponse>(1);
            StartResponses.Add(response);
            if (StartResponse < response.LevelStartResponse)
            {
                Response = response;
                return true;
            }
            else
                return false;
        }

        public void SetResponse(string sourceName, int delayMs = 0)
        {
            LevelStartResponse startResponse = delayMs > 0 ? LevelStartResponse.Delayed : LevelStartResponse.Immediate;
            Delay = delayMs;
            StartResponse response = new StartResponse(sourceName, startResponse);
            SetResponse(response);
        }

        public void SetHandledResponse(string sourceName)
        {
            StartResponse response = new StartResponse(sourceName, LevelStartResponse.Handled);

            if (!SetResponse(response))
            {
                Logger.log?.Debug($"StartLevel handled response from '{sourceName}', already have a handled response from '{Response.SourceName}'");
            }
        }

        public LevelStartingEventArgs(IDifficultyBeatmap difficultyBeatmap, bool practice)
        {
            DifficultyBeatmap = difficultyBeatmap;
            Practice = practice;
        }

    }
    public struct StartResponse
    {
        public readonly string? SourceName;
        public readonly LevelStartResponse LevelStartResponse;

        public StartResponse(string sourceName, LevelStartResponse response)
        {
            SourceName = sourceName;
            LevelStartResponse = response;
        }
    }

    public enum LevelStartResponse
    {
        /// <summary>
        /// No responses, level should start normally.
        /// </summary>
        None = 0,
        /// <summary>
        /// Level should start normally.
        /// </summary>
        Immediate = 1,
        /// <summary>
        /// Level should start after delay.
        /// </summary>
        Delayed = 2,
        /// <summary>
        /// Another component will handle starting the level.
        /// </summary>
        Handled = 3
    }

}
