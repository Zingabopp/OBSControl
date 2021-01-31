using CSCore.CoreAudioAPI;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents
{
    public class AudioDevicesController : OBSComponent
    {
        public static List<string> obsSourceKeys = new List<string>{
            "desktop-1", "desktop-2", "mic-1", "mic-2", "mic-3", "mic-4"
        };
        // Devices available to the system (as returned by NAudio)
        private MMDevice? systemDefaultOutputDevice;
        private MMDevice? systemDefaultInputDevice;
        private MMDeviceCollection? systemDevices;
        private MMDeviceCollection? systemInputDevices;
        private MMDeviceCollection? systemOutputDevices;
        private Dictionary<string, string> shortDeviceNames = new Dictionary<string, string>();

        // Current configuration of OBS. Keys are "SpecialSource" keys ("desktop-1", "mic-1", etc.)
        // obs special source keys mapped to system devices
        private Dictionary<string, MMDevice> obsDevices = new Dictionary<string, MMDevice>();
        // obs special source keys mapped to OBS Source Names ("Desktop Audio", "Mic/Aux", etc.)
        private Dictionary<string, string> obsSourceNames = new Dictionary<string, string>();
        // Stores the sourceKeys of active OBS Sources.
        // Sources can be disabled completely in OBS settings and setting them won't work 
        public HashSet<string> obsActiveSources = new HashSet<string>();


        private string? getDeviceNameFromConfig(string sourceKey)
        {
            PluginConfig? conf = Plugin.config;
            return sourceKey switch
            {
                "desktop-1" => conf.ObsDesktopAudio1,
                "desktop-2" => conf.ObsDesktopAudio2,
                "mic-1" => conf.ObsMicAux1,
                "mic-2" => conf.ObsMicAux2,
                "mic-3" => conf.ObsMicAux3,
                "mic-4" => conf.ObsMicAux4,
                _ => null,
            };
        }

        public async void setDevicesFromConfig()
        {
            Logger.log?.Info("|ADC| Setting devices from config");
            foreach (string sourceKey in obsSourceKeys)
            {
                Logger.log?.Info($"|ADC| Trying to get device name for \"{sourceKey}\"");
                string? deviceName = this.getDeviceNameFromConfig(sourceKey);
                if (deviceName != null)
                {
                    Logger.log?.Info($"|ADC| source \"{sourceKey}\" configured to \"{deviceName}\"");
                    try
                    {
                        await setSourceToDeviceByName(sourceKey, deviceName);
                    }
                    catch (Exception e)
                    {
                        Logger.log?.Info($"|ADC| Setting \"{sourceKey}\" configured to \"{deviceName}\" failed");
                        Logger.log?.Info($"|ADC| {e}");
                    }
                }
                else
                    Logger.log?.Warn($"|ADC| Could not get a device name using sourceKey '{sourceKey}'");
            }
            Logger.log?.Info("|ADC| Setting devices from config done");
        }

        private void listSystemDevices()
        {
            if (systemDevices == null)
            {
                Logger.log?.Info("|ADC| MMDeviceCollection systemDevices is null.");
                return;
            }
            Logger.log?.Info("|ADC| system devices start");
            foreach (MMDevice? device in this.systemDevices)
            {
                Logger.log?.Info($"|ADC| FriendlyName: \"{device.FriendlyName}\"");
                Logger.log?.Info($"|ADC| DeviceID: \"{device.DeviceID}\"");
            }
            Logger.log?.Info("|ADC| system devices end");
        }
        public void refreshSystemDevices()
        {
            Logger.log?.Debug("|ADC| refreshSystemDevices called.");
            MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
            this.systemDefaultOutputDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            this.systemDefaultInputDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

            this.systemDevices = deviceEnumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.Active);
            this.systemDevices.DefaultIfEmpty(null);
            this.listSystemDevices();

            this.systemInputDevices = deviceEnumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            this.systemInputDevices.DefaultIfEmpty(this.systemDefaultInputDevice);

            this.systemOutputDevices = deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            this.systemOutputDevices.DefaultIfEmpty(this.systemDefaultOutputDevice);

            this.shortDeviceNames.Clear();
            this.generateShortOutputDeviceNames();
            this.generateShortInputDeviceNames();

            Logger.log?.Debug("|ADC| refreshSystemDevices finished.");
        }

        private MMDevice? defaultDeviceByKey(string sourceKey)
        {
            if (sourceKey.StartsWith("mic"))
            {
                return this.systemDefaultInputDevice;
            }
            if (sourceKey.StartsWith("desktop"))
            {
                return this.systemDefaultOutputDevice;
            }
            Logger.log?.Error($"|ADC| Got source Key {sourceKey}. Can't resolve default Device");
            return null;
        }

        public async Task refreshOBSDevices(OBSWebsocket obs)
        {
            Logger.log?.Debug("|ADC| refreshOBSDevices called.");
            Dictionary<string, string> obsSources;

            this.obsDevices = new Dictionary<string, MMDevice>();
            this.obsSourceNames = new Dictionary<string, string>();
            this.obsActiveSources = new HashSet<string>();
            try
            {
                obsSources = await obs.GetSpecialSources();
            }
            catch (Exception)
            {
                Logger.log?.Debug("|ADC| refreshOBSDevices failed.");
                return;
            }
            foreach (KeyValuePair<string, string> source in obsSources)
            {
                this.obsActiveSources.Add(source.Key);
                Logger.log?.Debug($"|ADC| Special device {source.Value} start");
                OBSWebsocketDotNet.Types.SourceSettings? sourceSettings = await obs.GetSourceSettings(source.Value);
                string? deviceID = sourceSettings.Settings.GetValue("device_id")?.ToString();
                Logger.log?.Debug($"|ADC| Device ID: \"{deviceID}\"");
                MMDevice? device = this.systemDevices.FirstOrDefault((d) => d.DeviceID == deviceID);
                if (device == null)
                {
                    if (deviceID == "default")
                    {
                        device = this.defaultDeviceByKey(source.Key);
                        Logger.log?.Debug($"|ADC| Source set to default: {device?.FriendlyName ?? "<NULL>"}");
                    }
                    else
                    {
                        // This case is weird, because here OBS tries to use a device that
                        // does not exist and will default to whatever "default" means at the
                        // time, which depends on Windows Audio settings. Technically
                        // we should just store "default" somewhere here, but I'm not rewriting
                        // everything to handle an MMDevices AND maybe the string "default"
                        // so we are going with what the default device is right now...
                        device = this.defaultDeviceByKey(source.Key);
                        Logger.log?.Debug($"|ADC| Source set to default because device was missing");
                    }
                }
                if (device != null)
                {
                    this.obsDevices[source.Key] = device;
                    Logger.log?.Debug($"|ADC| obsDevices[{source.Key}] = {device.FriendlyName}");
                    this.obsSourceNames[source.Key] = sourceSettings.SourceName;
                    Logger.log?.Debug($"|ADC| obsSourceNames[{source.Key}] = \"{sourceSettings.SourceName}\"");
                    Logger.log?.Debug($"|ADC| Special device {source.Value} end\n");
                }
                else
                    Logger.log?.Warn($"|ADC| OBS audio device {source.Key} ({sourceSettings.SourceName}) is null.");
            }
        }

        public async Task setSourceToDeviceByName(string sourceKey, string friendlyName)
        {
            Logger.log?.Debug($"|ADC| Now: Setting source by device Name: \"{sourceKey}\" => \"{friendlyName}\"");
            if (friendlyName == "default")
            {
                await this.SetSourceToDefault(sourceKey);
                return;
            }
            MMDevice? device = this.getDeviceByFriendlyName(friendlyName);
            if (device == null)
            {
                Logger.log?.Info($"|ADC| device not found :(");
                this.listSystemDevices();
                return;
            }
            await SetSourceToDevice(sourceKey, device);
            Logger.log?.Debug($"|ADC| Done Setting source by device Name: {sourceKey} => {friendlyName}");
        }

        public async Task SetSourceToDefault(string sourceKey)
        {
            string? obsSourceName = null;
            if (!this.obsSourceNames.TryGetValue(sourceKey, out obsSourceName))
            {
                Logger.log?.Info($"|ADC| Aborting, obsSourceNames not yet initialized.");
                return;
            }
            JObject settings = new JObject { { "device_id", "default" } };

            try
            {
                OBSWebsocket? obs = Obs.GetConnectedObs();
                if (obs == null)
                {
                    Logger.log?.Debug($"|ADC| obs is null");
                }
                else if (obs.IsConnected)
                {
                    await obs.SetSourceSettings(obsSourceName, settings, null); 
                    this.obsDevices[sourceKey] = this.defaultDeviceByKey(sourceKey);
                    Logger.log?.Debug($"|ADC| Set \"{sourceKey}\" to \"default\"");
                }
                else
                {
                    Logger.log?.Debug($"|ADC| obs not connected");
                }
            }
            catch (Exception e)
            {
                Logger.log?.Debug($"|ADC| Setting \"{sourceKey}\" to \"default\" didn't work:");
                Logger.log?.Debug($"|ADC| {e}");
            }
        }

        public async Task SetSourceToDevice(string sourceKey, MMDevice device)
        {
            Logger.log?.Info($"|ADC| Trying to set \"{sourceKey}\" to \"{device.FriendlyName}\"");
            string? obsSourceName;
            if (!this.obsSourceNames.TryGetValue(sourceKey, out obsSourceName))
            {
                Logger.log?.Info($"|ADC| Aborting, obsSourceNames not yet initialized.");
                return;
            }
            JObject settings = new JObject { { "device_id", device.DeviceID } };
            try
            {
                OBSWebsocket? obs = Obs.GetConnectedObs();
                if (obs == null)
                {
                    Logger.log?.Debug($"|ADC| obs is null");
                }
                else if (obs.IsConnected)
                {
                    await obs.SetSourceSettings(obsSourceName, settings, null); ;
                    this.obsDevices[sourceKey] = device;
                    Logger.log?.Debug($"|ADC| Set \"{sourceKey}\" to \"{device.FriendlyName}\"");
                }
                else
                {
                    Logger.log?.Debug($"|ADC| obs not connected");
                }
            }
            catch (Exception e)
            {
                Logger.log?.Debug($"|ADC| Setting \"{sourceKey}\" to \"{device.FriendlyName}\" didn't work:");
                Logger.log?.Debug($"|ADC| {e}");
            }
        }

        // public MMDevice getInputDeviceByID(string id) => this.systemInputDevices.FirstOrDefault((device) => device.DeviceID.Equals(id));
        // public MMDevice getOutputDeviceByID(string id) => this.systemOutputDevices.FirstOrDefault((device) => device.DeviceID.Equals(id));
        public MMDevice getDeviceByFriendlyName(string name)
        {
            try
            {
                string? longName = this.shortDeviceNames[name];
                if (longName != null) name = longName;
            }
            catch (Exception) { }
            return this.systemDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));
        }
        // public MMDevice getInputDeviceByFriendlyName(string name) => this.systemInputDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));
        // public MMDevice getOutputDeviceByFriendlyName(string name) => this.systemOutputDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));

        private IEnumerable<string> getShortDeviceNamesFrom(MMDeviceCollection col)
        {
            IEnumerable<string>? names = col.Select(d => d.FriendlyName);
            return names.Select((name) =>
            {
                try
                {
                    string? shortName = this.shortDeviceNames[name];
                    if (shortName != null) return shortName;
                }
                catch (Exception) { };
                return name;
            });
        }
        private IEnumerable<string> getOutputDeviceNamesForConfig() 
            => systemOutputDevices != null ? this.getShortDeviceNamesFrom(this.systemOutputDevices) : Array.Empty<string>();
        private IEnumerable<string> getInputDeviceNamesForConfig() 
            => systemInputDevices != null ? this.getShortDeviceNamesFrom(this.systemInputDevices) : Array.Empty<string>();

        private void generateShortDeviceName(string pattern, MMDeviceCollection col)
        {
            IEnumerable<string>? names = col.Select(d => d.FriendlyName);
            Dictionary<string, string> nameMapping = new Dictionary<string, string>();
            IEnumerable<string>? shortNames = names.Select(name =>
            {
                Match m = Regex.Match(name, pattern);
                if (!m.Success) return name;
                string shortName = m.Result("${name}");
                nameMapping.Add(shortName, name);
                nameMapping.Add(name, shortName);
                return shortName;
            });

            if (shortNames.GroupBy(n => n).Any(c => c.Count() > 1))
            {
                // If short names have duplicates we can't map back to long names,
                // In that case, just don't use short names for now.
                Logger.log?.Debug($"|ADC| Short Device names had duplicates, not using short names");
                return;
            }

            nameMapping.ToList().ForEach(x => this.shortDeviceNames.Add(x.Key, x.Value));
            Logger.log?.Debug($"|ADC| Generated short Device names");
        }

        private void generateShortOutputDeviceNames()
        {
            if (systemOutputDevices == null)
                return;
            string pattern = @"(?<name>.*) \(.*\)";
            this.generateShortDeviceName(pattern, this.systemOutputDevices);
        }
        private void generateShortInputDeviceNames()
        {
            if (systemInputDevices == null)
                return;
            string pattern = @".* \((?<name>.*)\)";
            this.generateShortDeviceName(pattern, this.systemInputDevices);
        }

        public void UpdateSystemDevices(bool forceCurrentUpdate = true)
        {
            Logger.log?.Debug("|ADC| UpdateSystemDevices called.");
            refreshSystemDevices();
            List<string> inputDeviceNames = this.getInputDeviceNamesForConfig().ToList();
            List<string> outputDeviceNames = this.getOutputDeviceNamesForConfig().ToList();
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                try
                {
                    Plugin.config.UpdateSystemAudioDevices(outputDeviceNames, inputDeviceNames);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error Updating System Audio devices: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            });
        }
        public async Task UpdateOBSDevices(bool forceCurrentUpdate = true)
        {
            Logger.log?.Debug("|ADC| UpdateOBSDevices called.");
            try
            {
                OBSWebsocket? obs = Obs.GetConnectedObs();
                if(obs == null)
                {
                    Logger.log?.Warn("|ADC| Unable get OBS devices. OBS not connected.");
                    return;
                }
                await refreshOBSDevices(obs);
                Logger.log?.Debug("|ADC| OBS Devices refreshed");
            }
            catch (Exception)
            {
                Logger.log?.Warn("|ADC| Unable get OBS devices. OBS not connected.");
            }
            Logger.log?.Debug("|ADC| Updating config data with OBS information");
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                try
                {
                    Plugin.config.UpdateObsAudioSources(this.obsActiveSources);
                }
                catch (Exception e)
                {
                    Logger.log?.Debug($"|ADC| Failed to push active sources to config:");
                    Logger.log?.Debug($"|ADC| {e}");
                }
            });
            Logger.log?.Debug("|ADC| Updating OBS devices finished");
            // Thread.Sleep(2000);
            // setDevicesFromConfig();
        }

        #region Setup/Teardown
        public override async Task InitializeAsync(OBSController obs)
        {
            await base.InitializeAsync(obs);
            UpdateSystemDevices();
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(cancellationToken);
            await UpdateOBSDevices().ConfigureAwait(false);
        }

        protected override void OnDisconnect()
        {
            base.OnDisconnect();
            // CurrentScene = null;
        }

        protected override void SetEvents(OBSController obs)
        {
            base.SetEvents(obs);
            //BS_Utils.Plugin.LevelDidFinishEvent += OnLevelDidFinish;
            // StartLevelPatch.LevelStarting += OnLevelStarting;
        }

        protected override void RemoveEvents(OBSController obs)
        {
            base.RemoveEvents(obs);
            // BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelDidFinish;
            // StartLevelPatch.LevelStarting -= OnLevelStarting;
        }

        protected override void SetEvents(OBSWebsocket obs)
        {
            RemoveEvents(obs);
            // obs.SceneListChanged += OnObsSceneListChanged;
            // obs.SceneChanged += OnObsSceneChanged;
        }

        protected override void RemoveEvents(OBSWebsocket obs)
        {
            // obs.SceneListChanged -= OnObsSceneListChanged;
            // obs.SceneChanged -= OnObsSceneChanged;
        }
        #endregion


        #region Monobehaviour Messages
        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            SetEvents(Obs);
            // refreshSystemDevices();
            // _ = refreshOBSDevices();
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveEvents(Obs);
        }

        #endregion
    }
}
