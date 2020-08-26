using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Notify;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using OBSControl.OBSComponents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
#nullable enable
[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OBSControl
{
    internal class PluginConfig : INotifiableHost
    {
        [UIValue(nameof(Enabled))]
        public virtual bool Enabled { get; set; } = true;
        [UIValue(nameof(ServerAddress))]
        public virtual string? ServerAddress { get; set; } = "ws://127.0.0.1:4444";
        [UIValue(nameof(ServerPassword))]
        public virtual string? ServerPassword { get; set; } = string.Empty;
        [UIValue(nameof(EnableAutoRecord))]
        public virtual bool EnableAutoRecord { get; set; } = true;

        public void NotifyRecordStartChanged()
        {
            RaisePropertyChanged(nameof(RecordStartOption));
            RaisePropertyChanged(nameof(DelayedLevelStartEnabled));
            RaisePropertyChanged(nameof(SongStartEnabled));
            RaisePropertyChanged(nameof(SceneSequenceEnabled));
            RaisePropertyChanged(nameof(SceneSequenceDisabled));
        }

        [UseConverter(typeof(EnumConverter<RecordStartOption>))]
        [UIValue(nameof(RecordStartOption))]
        public virtual RecordStartOption RecordStartOption
        {
            get => _recordStartOption;
            set
            {
                if (value == _recordStartOption) return;
                _recordStartOption = value;
                NotifyRecordStartChanged();
            }
        }
        [UseConverter(typeof(EnumConverter<RecordStopOption>))]
        [UIValue(nameof(RecordStopOption))]
        public virtual RecordStopOption RecordStopOption { get; set; } = RecordStopOption.ResultsView;

        [Ignore]
        [UIValue(nameof(DelayedLevelStartEnabled))]
        public bool DelayedLevelStartEnabled => RecordStartOption == RecordStartOption.LevelStartDelay;
        [Ignore]
        [UIValue(nameof(SongStartEnabled))]
        public bool SongStartEnabled => RecordStartOption == RecordStartOption.SongStart;

        [UIValue(nameof(SongStartDelay))]
        public virtual float SongStartDelay { get; set; } = 0f;

        [UIValue(nameof(LevelStartDelay))]
        public virtual float LevelStartDelay
        {
            get => _levelStartDelay;
            set
            {
                if (value < 0)
                    value = 0;
                _levelStartDelay = (float)Math.Round(value, 1);
            }
        }
        [UIValue(nameof(RecordingStopDelay))]
        public virtual float RecordingStopDelay
        {
            get => _recordingStopDelay;
            set
            {
                if (value < 0)
                    value = 0;
                _recordingStopDelay = (float)Math.Round(value, 1);
            }
        }

        [UIValue(nameof(AutoStopOnManual))]
        public virtual bool AutoStopOnManual { get; set; } = true;

        [UIValue(nameof(RecordingFileFormat))]
        public virtual string? RecordingFileFormat { get; set; } = "?N{20}-?A{20}_?%<_[?M]><-?F><-?e>";

        [UIValue(nameof(ReplaceSpacesWith))]
        public virtual string? ReplaceSpacesWith { get; set; } = "_";

        [UIValue(nameof(InvalidCharacterSubstitute))]
        public virtual string? InvalidCharacterSubstitute { get; set; } = "_";

        [UIValue(nameof(StartSceneDuration))]
        public virtual float StartSceneDuration
        {
            get => _startSceneDuration;
            set
            {
                if (value < 0)
                    value = 0;
                _startSceneDuration = (float)Math.Round(value, 1);
            }
        }

        private float _endSceneStartDelay;
        [UIValue(nameof(EndSceneStartDelay))]
        public float EndSceneStartDelay
        {
            get { return _endSceneStartDelay; }
            set
            {
                if (value < 0)
                    value = 0;
                _endSceneStartDelay = (float)Math.Round(value, 1);
            }
        }

        [UIValue(nameof(EndSceneDuration))]
        public virtual float EndSceneDuration
        {
            get => _endSceneDuration;
            set
            {
                if (value < 0)
                    value = 0;
                _endSceneDuration = (float)Math.Round(value, 1);
            }
        }

        //[NonNullable]
        //[UIValue(nameof(SceneCollectionName))]
        //public virtual string SceneCollectionName { get; set; } = string.Empty;

        [NonNullable]
        [UIValue(nameof(StartSceneName))]
        public virtual string StartSceneName { get; set; } = string.Empty;
        [NonNullable]
        [UIValue(nameof(GameSceneName))]
        public virtual string GameSceneName { get; set; } = string.Empty;
        [NonNullable]
        [UIValue(nameof(EndSceneName))]
        public virtual string EndSceneName { get; set; } = string.Empty;

        [NonNullable]
        [UIValue(nameof(RestingSceneName))]
        public virtual string RestingSceneName { get; set; } = string.Empty;

        //[NonNullable]
        //public virtual string MaterialName { get; set; } = string.Empty;

        //[NonNullable]
        //public virtual string ShaderName { get; set; } = string.Empty;
        //[NonNullable]
        //public virtual string ColorName { get; set; } = string.Empty;
        //[NonNullable]
        //public virtual float ColorAlpha { get; set; } = 1f;

        #region Floating Screen
        public virtual float ScreenPosX { get; set; } = 0f;
        public virtual float ScreenPosY { get; set; } = 2.9f;
        public virtual float ScreenPosZ { get; set; } = 2.4f;
        public virtual float ScreenRotX { get; set; } = -30f;
        public virtual float ScreenRotY { get; set; } = 0f;
        public virtual float ScreenRotZ { get; set; } = 0f;
        public virtual bool ShowScreenHandle { get; set; } = true;

        #endregion

        public virtual float ObsTimeout { get; set; } = 5000f;

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            //HMMainThreadDispatcher.instance.Enqueue(() =>
            //{
            //    Plugin.instance.SetThings(MaterialName, ShaderName, ColorName, ColorAlpha);
            //});
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            RefreshDropdowns();
            OBSController.instance?.gameObject.SetActive(Enabled);
        }

        /// <summary>
        /// Call this when you want to do multiple changes before saving the file, dispose to save.
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable ChangeTransaction() => null!;

        public void UpdateSceneOptions(IEnumerable<string> newOptions)
        {
            SceneSelectOptions.Clear();
            SceneSelectOptions.Add(string.Empty);
            SceneSelectOptions.AddRange(newOptions);
            TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName, RestingSceneName);
            RefreshDropdowns();
        }
        //public void UpdateSceneCollectionOptions(IEnumerable<KeyValuePair<string, string[]>> newOptions)
        //{
        //    SceneCollectionOptions.Clear();
        //    SceneCollectionOptions.Add(string.Empty);
        //    SceneCollectionOptions.AddRange(newOptions.Select(p => p.Key));

        //    RefreshDropdowns();
        //}

        private void TryAddCurrentNames(params string[]? sceneNames)
        {
            if (sceneNames == null) return;
            foreach (string name in sceneNames)
            {
                if (!SceneSelectOptions.Contains(name))
                    SceneSelectOptions.Add(name);
            }
        }

        public void RefreshDropdowns()
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            DropDownListSetting[]? dropDowns = new DropDownListSetting[] { StartSceneDropDown, GameSceneDropdown, EndSceneDropdown, RestingSceneDropdown };
#pragma warning restore CS8601 // Possible null reference assignment.
            foreach (DropDownListSetting dropDown in dropDowns)
            {
                if (dropDown != null)
                {
                    dropDown.tableView.ReloadData();
                    dropDown.ReceiveValue();
                }
            }
        }
        [UIAction("formatter-seconds")]
        public string floatToSeconds(float val)
        {
            return $"{Math.Round(val, 1)}s";
        }

        [Ignore]
        [UIValue("SceneSelectOptions")]
        public List<object> SceneSelectOptions = new List<object>() { string.Empty };


        [Ignore]
        [UIValue(nameof(RecordStartOptions))]
        public List<object> RecordStartOptions = new List<object>() { RecordStartOption.SongStart, RecordStartOption.LevelStartDelay, RecordStartOption.SceneSequence };

        [Ignore]
        [UIValue(nameof(RecordStopOptions))]
        public List<object> RecordStopOptions = new List<object>() { RecordStopOption.ResultsView, RecordStopOption.SongEnd };

        [Ignore]
        [UIValue(nameof(SceneSequenceEnabled))]
        public bool SceneSequenceEnabled => RecordStartOption == RecordStartOption.SceneSequence;
        [Ignore]
        [UIValue(nameof(SceneSequenceDisabled))]
        public bool SceneSequenceDisabled => !SceneSequenceEnabled;

        //[Ignore]
        //public ConcurrentDictionary<string, string[]> SceneCollections = new ConcurrentDictionary<string, string[]>();

        //[Ignore]
        //[UIValue("SceneCollectionOptions")]
        //public List<object> SceneCollectionOptions = new List<object>() { string.Empty };

        [Ignore]
        [UIComponent("StartSceneDropdown")]
        public DropDownListSetting? StartSceneDropDown;
        [Ignore]
        [UIComponent("GameSceneDropdown")]
        public DropDownListSetting? GameSceneDropdown;
        [Ignore]
        [UIComponent("EndSceneDropdown")]
        public DropDownListSetting? EndSceneDropdown;
        [Ignore]
        [UIComponent("RestingSceneDropdown")]
        public DropDownListSetting? RestingSceneDropdown;

        #region Backing Fields
        private float _levelStartDelay = 3f;
        private float _recordingStopDelay = 4f;
        private float _startSceneDuration = 1f;
        private float _endSceneDuration = 2f;
        private RecordStartOption _recordStartOption = RecordStartOption.SongStart;

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Not used yet.
    /// </summary>
    //public class SceneProfile
    //{
    //    public event EventHandler? SceneListUpdated;
    //    [JsonRequired]
    //    [JsonProperty("SceneCollectionName")]
    //    public string SceneCollectionName { get; protected set; }

    //    [NonNullable]
    //    [JsonProperty("StartSceneName")]
    //    public virtual string StartSceneName { get; set; } = string.Empty;
    //    [NonNullable]
    //    [JsonProperty("GameSceneName")]
    //    public virtual string GameSceneName { get; set; } = string.Empty;
    //    [NonNullable]
    //    [JsonProperty("EndSceneName")]
    //    public virtual string EndSceneName { get; set; } = string.Empty;
    //    [NonNullable]
    //    [JsonProperty("RestingSceneName")]
    //    public virtual string RestingSceneName { get; set; } = string.Empty;

    //    [JsonIgnore]
    //    private HashSet<string> AvailableScenes = new HashSet<string>();

    //    public bool SceneAvailable(string sceneName) => AvailableScenes.Contains(sceneName);

    //    public void UpdateAvailableScenes(IEnumerable<string> sceneList)
    //    {
    //        AvailableScenes.Clear();
    //        foreach (var scene in sceneList)
    //        {
    //            if (!string.IsNullOrEmpty(scene))
    //                AvailableScenes.Add(scene);
    //        }
    //        SceneListUpdated?.Invoke(this, EventArgs.Empty);
    //    }

    //    [JsonConstructor]
    //    public SceneProfile(string sceneCollectionName)
    //    {
    //        SceneCollectionName = sceneCollectionName;
    //    }
    //}

    internal static class ConfigExtensions
    {
        public static Vector3 GetScreenPosition(this PluginConfig config)
        {
            return new Vector3(config.ScreenPosX, config.ScreenPosY, config.ScreenPosZ);
        }
        public static Quaternion GetScreenRotation(this PluginConfig config)
        {
            return Quaternion.Euler(config.ScreenRotX, config.ScreenRotY, config.ScreenRotZ);
        }
    }
}
