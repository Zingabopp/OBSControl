using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using OBSControl.OBSComponents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
#nullable enable
[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OBSControl
{
    internal class PluginConfig : INotifyPropertyChanged
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
            RaiseNotifyPropertyChanged(nameof(RecordStartOption));
            RaiseNotifyPropertyChanged(nameof(DelayedLevelStartEnabled));
            RaiseNotifyPropertyChanged(nameof(SongStartEnabled));
            RaiseNotifyPropertyChanged(nameof(SceneSequenceEnabled));
            RaiseNotifyPropertyChanged(nameof(SceneSequenceDisabled));
        }
        public void NotifyAudioDevicesChanged(string sourceKey)
        {
            // Note to self: this is probably here to refresh shown configs
            // in the bsml file whose "active" state is backed by a variable
            RaiseNotifyPropertyChanged(nameof(ObsDesktopAudio1));
            RaiseNotifyPropertyChanged(nameof(ObsDesktopAudio2));
            RaiseNotifyPropertyChanged(nameof(ObsMicAux1));
            RaiseNotifyPropertyChanged(nameof(ObsMicAux2));
            RaiseNotifyPropertyChanged(nameof(ObsMicAux3));
            RaiseNotifyPropertyChanged(nameof(ObsMicAux4));
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
        public virtual string? RecordingFileFormat { get; set; } = "?N{20}-?A{20}<_?%><_[?M]><-?F><-?e>";

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
            DropDownListSetting[]? dropDowns = new DropDownListSetting[] {
                StartSceneDropDown, GameSceneDropdown, EndSceneDropdown, RestingSceneDropdown,
                ObsDesktopAudio1Dropdown, ObsDesktopAudio2Dropdown,
                ObsMicAux1Dropdown, ObsMicAux2Dropdown,ObsMicAux3Dropdown,ObsMicAux4Dropdown,
            };
#pragma warning restore CS8601 // Possible null reference assignment.
            foreach (DropDownListSetting dropDown in dropDowns)
            {
                if (dropDown != null)
                {
                    dropDown.dropdown.ReloadData();
                    dropDown.ReceiveValue();
                }
            }
            Logger.log?.Debug("Refreshed Dropdowns");
        }
        [UIAction("formatter-seconds")]
        public string floatToSeconds(float val)
        {
            return $"{Math.Round(val, 1)}s";
        }

        [Ignore]
        [UIValue("SceneSelectOptions")]
        public List<object> SceneSelectOptions = new List<object>() { string.Empty };

        // Using something needing a converter for the config as dropdown-list-setting options
        // seems to break BSML, so this exists and gets filled at runtime
        [Ignore]
        [UIValue(nameof(DesktopAudioDevicesDropdown))]
        public virtual List<object> DesktopAudioDevicesDropdown { get; set; } = new List<object>() { "default" };

        // Using something needing a converter for the config as dropdown-list-setting options
        // seems to break BSML, so this exists and gets filled at runtime
        [Ignore]
        [UIValue(nameof(MicAuxDevicesDropdown))]
        public virtual List<object> MicAuxDevicesDropdown { get; set; } = new List<object>() { "default" };

        [Ignore]
        [UIValue(nameof(RecordStartOptions))]
        public List<object> RecordStartOptions = new List<object>() { RecordStartOption.SongStart, RecordStartOption.LevelStartDelay, RecordStartOption.SceneSequence };

        [Ignore]
        [UIValue(nameof(ObsDesktopAudioDevices))]
        public List<object> ObsDesktopAudioDevices = new List<object>() { "default" };

        [UIValue(nameof(ObsDesktopAudioDevicesHistory))]
        [UseConverter(typeof(CollectionConverter<string, HashSet<string>>))]
        public virtual HashSet<string> ObsDesktopAudioDevicesHistory { get; set; } = new HashSet<string>() { "default" };

        [Ignore]
        [UIValue(nameof(ObsMicAuxDevices))]
        public List<object> ObsMicAuxDevices = new List<object>() { "default" };

        [UIValue(nameof(ObsMicAuxDevicesHistory))]
        [UseConverter(typeof(CollectionConverter<string, HashSet<string>>))]
        public virtual HashSet<string> ObsMicAuxDevicesHistory { get; set; } = new HashSet<string>() { "default" };

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
        [Ignore]
        [UIComponent("ObsDesktopAudio1Dropdown")]
        public DropDownListSetting? ObsDesktopAudio1Dropdown;
        [Ignore]
        [UIComponent("ObsDesktopAudio2Dropdown")]
        public DropDownListSetting? ObsDesktopAudio2Dropdown;
        [Ignore]
        [UIComponent("ObsMicAux1Dropdown")]
        public DropDownListSetting? ObsMicAux1Dropdown;
        [Ignore]
        [UIComponent("ObsMicAux2Dropdown")]
        public DropDownListSetting? ObsMicAux2Dropdown;
        [Ignore]
        [UIComponent("ObsMicAux3Dropdown")]
        public DropDownListSetting? ObsMicAux3Dropdown;
        [Ignore]
        [UIComponent("ObsMicAux4Dropdown")]
        public DropDownListSetting? ObsMicAux4Dropdown;

        [UIAction(nameof(DesktopAudioFormatter))]
        public string DesktopAudioFormatter(string name)
        {
            if (ObsDesktopAudioDevices.Contains(name)) { return name; }
            return AudioDeviceDropdownEntry.notAvailableFormatter(name);
        }

        [UIAction(nameof(MicAuxFormatter))]
        public string MicAuxFormatter(string name)
        {
            if (ObsMicAuxDevices.Contains(name)) return name;
            return AudioDeviceDropdownEntry.notAvailableFormatter(name);
        }

        public void UpdateObsAudioSources(IEnumerable<string> sourceKeys)
        {
            string[]? keys = this.ObsAudioDevices.Keys.ToArray();
            Logger.log?.Debug($"|ADC| Active devices start");
            foreach (string? sourceKey in keys)
            {
                Logger.log?.Debug($"|ADC| {sourceKey} is now {sourceKeys.Contains(sourceKey)}");
                this.ObsAudioDevices[sourceKey].IsAvailable = sourceKeys.Contains(sourceKey);
            }
            Logger.log?.Debug($"|ADC| Active devices end");

            RefreshDropdowns();
        }

        public void UpdateSystemAudioDevices(IEnumerable<string> desktopAudioDeviceNames, IEnumerable<string> micAuxAudioDeviceNames)
        {
            UpdateSystemAudioDevices(desktopAudioDeviceNames, ObsDesktopAudioDevices, ObsDesktopAudioDevicesHistory, DesktopAudioDevicesDropdown);
            UpdateSystemAudioDevices(micAuxAudioDeviceNames, ObsMicAuxDevices, ObsMicAuxDevicesHistory, MicAuxDevicesDropdown);

            RefreshDropdowns();
        }

        private void UpdateSystemAudioDevices(
            IEnumerable<string> deviceNames,
            List<object> currentDevices,
            HashSet<string> devicesHistory,
            List<object> devicesDropdown)
        {
            currentDevices.Clear();
            currentDevices.Add("default");
            currentDevices.AddRange(deviceNames);

            foreach (string? name in deviceNames) { devicesHistory.Add(name); }
            devicesDropdown.Clear();
            foreach (string? name in devicesHistory) { devicesDropdown.Add(name); }
            Changed();
        }

        #region desktopaudio1
        [Ignore]
        [UIValue(nameof(DesktopAudio1Available))]
        public bool DesktopAudio1Available { get => this.ObsAudioDevices["desktop-1"].IsAvailable; }
        [Ignore]
        [UIValue(nameof(DesktopAudio1Missing))]
        public bool DesktopAudio1Missing { get => !this.ObsAudioDevices["desktop-1"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(DesktopAudio1HoverHint))]
        public string DesktopAudio1HoverHint { get => this.ObsAudioDevices["desktop-1"].HoverHint; }

        [Ignore]
        [UIValue(nameof(DesktopAudio1Color))]
        public string DesktopAudio1Color { get => this.ObsAudioDevices["desktop-1"].Color; }
        #endregion

        #region desktopaudio2
        [Ignore]
        [UIValue(nameof(DesktopAudio2Available))]
        public bool DesktopAudio2Available { get => this.ObsAudioDevices["desktop-2"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(DesktopAudio2Missing))]
        public bool DesktopAudio2Missing { get => !this.ObsAudioDevices["desktop-2"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(DesktopAudio2HoverHint))]
        public string DesktopAudio2HoverHint { get => this.ObsAudioDevices["desktop-2"].HoverHint; }

        [Ignore]
        [UIValue(nameof(DesktopAudio2Color))]
        public string DesktopAudio2Color { get => this.ObsAudioDevices["desktop-2"].Color; }
        #endregion

        #region micaux1
        [Ignore]
        [UIValue(nameof(MicAux1Available))]
        public bool MicAux1Available { get => this.ObsAudioDevices["mic-1"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux1Missing))]
        public bool MicAux1Missing { get => !this.ObsAudioDevices["mic-1"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux1HoverHint))]
        public string MicAux1HoverHint { get => this.ObsAudioDevices["mic-1"].HoverHint; }

        [Ignore]
        [UIValue(nameof(MicAux1Color))]
        public string MicAux1Color { get => this.ObsAudioDevices["mic-1"].Color; }
        #endregion

        #region micaux2
        [Ignore]
        [UIValue(nameof(MicAux2Available))]
        public bool MicAux2Available { get => this.ObsAudioDevices["mic-2"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux2Missing))]
        public bool MicAux2Missing { get => !this.ObsAudioDevices["mic-2"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux2HoverHint))]
        public string MicAux2HoverHint { get => this.ObsAudioDevices["mic-2"].HoverHint; }

        [Ignore]
        [UIValue(nameof(MicAux2Color))]
        public string MicAux2Color { get => this.ObsAudioDevices["mic-2"].Color; }
        #endregion

        #region micaux3
        [Ignore]
        [UIValue(nameof(MicAux3Available))]
        public bool MicAux3Available { get => this.ObsAudioDevices["mic-3"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux3Missing))]
        public bool MicAux3Missing { get => !this.ObsAudioDevices["mic-3"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux3HoverHint))]
        public string MicAux3HoverHint { get => this.ObsAudioDevices["mic-3"].HoverHint; }

        [Ignore]
        [UIValue(nameof(MicAux3Color))]
        public string MicAux3Color { get => this.ObsAudioDevices["mic-3"].Color; }
        #endregion

        #region micaux4
        [Ignore]
        [UIValue(nameof(MicAux4Available))]
        public bool MicAux4Available { get => this.ObsAudioDevices["mic-4"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux4Missing))]
        public bool MicAux4Missing { get => !this.ObsAudioDevices["mic-4"].IsAvailable; }

        [Ignore]
        [UIValue(nameof(MicAux4HoverHint))]
        public string MicAux4HoverHint { get => this.ObsAudioDevices["mic-4"].HoverHint; }

        [Ignore]
        [UIValue(nameof(MicAux4Color))]
        public string MicAux4Color { get => this.ObsAudioDevices["mic-4"].Color; }
        #endregion

        [UIValue(nameof(ObsDesktopAudio1))]
        public virtual string ObsDesktopAudio1
        {
            get => ObsAudioDevices["desktop-1"].deviceName;
            set => this.HandleAudioDeviceSelection("desktop-1", value);
        }

        [UIValue(nameof(ObsDesktopAudio2))]
        public virtual string ObsDesktopAudio2
        {
            get => ObsAudioDevices["desktop-2"].deviceName;
            set => this.HandleAudioDeviceSelection("desktop-2", value);
        }

        [UIValue(nameof(ObsMicAux1))]
        public virtual string ObsMicAux1
        {
            get => ObsAudioDevices["mic-1"].deviceName;
            set => this.HandleAudioDeviceSelection("mic-1", value);
        }

        [UIValue(nameof(ObsMicAux2))]
        public virtual string ObsMicAux2
        {
            get => ObsAudioDevices["mic-2"].deviceName;
            set => this.HandleAudioDeviceSelection("mic-2", value);
        }

        [UIValue(nameof(ObsMicAux3))]
        public virtual string ObsMicAux3
        {
            get => ObsAudioDevices["mic-3"].deviceName;
            set => this.HandleAudioDeviceSelection("mic-3", value);
        }

        [UIValue(nameof(ObsMicAux4))]
        public virtual string ObsMicAux4
        {
            get => ObsAudioDevices["mic-4"].deviceName;
            set => this.HandleAudioDeviceSelection("mic-4", value);
        }
        private async void HandleAudioDeviceSelection(string sourceKey, string deviceName)
        {
            Logger.log?.Debug($"|ADC| Handling \"{sourceKey}\" - \"{deviceName}\"");

            ObsAudioDevices.TryGetValue(sourceKey, out AudioDeviceDropdownEntry oldDevice);
            if (oldDevice.deviceName!= deviceName) {
                await oldDevice.TrySetDevice(sourceKey, deviceName);
            }

            ObsAudioDevices[sourceKey].deviceName = deviceName;
            Changed();
            NotifyAudioDevicesChanged(sourceKey);
            Logger.log?.Debug($"|ADC| Handled \"{sourceKey}\" - \"{deviceName}\"");
        }

        #region Backing Fields
        private float _levelStartDelay = 3f;
        private float _recordingStopDelay = 4f;
        private float _startSceneDuration = 1f;
        private float _endSceneDuration = 2f;
        private RecordStartOption _recordStartOption = RecordStartOption.SongStart;
        private readonly Dictionary<string, AudioDeviceDropdownEntry> ObsAudioDevices = new Dictionary<string, AudioDeviceDropdownEntry>()
        {
            { "desktop-1", new AudioDeviceDropdownEntry("desktop-1", "Desktop Audio", false) },
            { "desktop-2", new AudioDeviceDropdownEntry("desktop-2", "Desktop Audio 2", false) },
            { "mic-1", new AudioDeviceDropdownEntry("mic-1", "Mic/Auxiliary Audio", false) },
            { "mic-2", new AudioDeviceDropdownEntry("mic-2", "Mic/Auxiliary Audio 2", false) },
            { "mic-3", new AudioDeviceDropdownEntry("mic-3", "Mic/Auxiliary Audio 3", false) },
            { "mic-4", new AudioDeviceDropdownEntry("mic-4", "Mic/Auxiliary Audio 4", false) },
        };
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaiseNotifyPropertyChanged([CallerMemberName] string? propertyName = null)
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
