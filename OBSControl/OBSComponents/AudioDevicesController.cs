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
        // Devices available to the system (as returned by NAudio)
        private MMDevice systemDefaultOutputDevice;
        private MMDevice systemDefaultInputDevice;
        private MMDeviceCollection systemDevices;
        private MMDeviceCollection systemInputDevices;
        private MMDeviceCollection systemOutputDevices;
        private Dictionary<string, string> shortDeviceNames = new Dictionary<string, string>();

        // Current configuration of OBS. Keys are "SpecialSource" keys ("desktop-1", "mic-1", etc.)
        // obs special source keys mapped to system devices
        private Dictionary<String, MMDevice> obsDevices;
        // obs special source keys mapped to OBS Source Names ("Desktop Audio", "Mic/Aux", etc.)
        private Dictionary<String, String> obsSourceNames;

        private void listSystemDevices()
        {
            Logger.log?.Info("|ADC| system devices start");
            foreach (var device in this.systemDevices)
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
            Logger.log?.Debug("|ADC| refreshSystemDevices finished.");
        }

        private MMDevice defaultDeviceByKey(string sourceKey)
        {
            if (sourceKey.StartsWith("mic")) {
                return this.systemDefaultInputDevice;
            }
            if (sourceKey.StartsWith("desktop")) {
                return this.systemDefaultOutputDevice;
            }
            Logger.log?.Error($"|ADC| Got source Key {sourceKey}. Can't resolve default Device");
            return null;
        }

        public async Task refreshOBSDevices(OBSWebsocket obs)
        {
            Logger.log?.Info("|ADC| refreshOBSDevices called.");
            var obsSources = await obs.GetSpecialSources();
            this.obsDevices = new Dictionary<String, MMDevice>();
            this.obsSourceNames = new Dictionary<String, String>();
            foreach (var source in obsSources)
            {
                Logger.log?.Info($"|ADC| Special device {source.Value} start");
                var sourceSettings = await obs.GetSourceSettings(source.Value);
                var did = sourceSettings.Settings.GetValue("device_id").ToString();
                Logger.log?.Info($"|ADC| Device ID: \"{did}\"");
                MMDevice device = this.systemDevices.FirstOrDefault((d) => d.DeviceID == did);
                if (device == null)
                {
                    if (did == "default")
                    {
                        device = this.defaultDeviceByKey(source.Key);
                        Logger.log?.Info($"|ADC| Source set to default: {device.FriendlyName}");
                    }
                    else
                    {
                        // Device doesn't exist any more. Use default for now
                        // later, change this to be something from the config
                        device = this.defaultDeviceByKey(source.Key);
                        Logger.log?.Info($"|ADC| Source set to default because device was missing");
                    }
                }
                this.obsDevices[source.Key] = device;
                Logger.log?.Info($"|ADC| obsDevices[{source.Key}] = {device.FriendlyName}");
                this.obsSourceNames[source.Key] = sourceSettings.SourceName;
                Logger.log?.Info($"|ADC| obsSourceNames[{source.Key}] = \"{sourceSettings.SourceName}\"");
                Logger.log?.Info($"|ADC| Special device {source.Value} end\n");
            }
        }

        public async void setSourceToDeviceByName(string sourceKey, string friendlyName)
        {
            Logger.log?.Debug($"|ADC| Now: Setting source by device Name: \"{sourceKey}\" => \"{friendlyName}\"");
            if (friendlyName == "default") {
                await this.SetSourceToDefault(sourceKey);
                return;
            }
            var device = this.getDeviceByFriendlyName(friendlyName);
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
            String obsSourceName = this.obsSourceNames[sourceKey];
            JObject settings = new JObject { { "device_id", "default" } };
            await Obs.GetConnectedObs().SetSourceSettings(obsSourceName, settings, null);
            this.obsDevices[sourceKey] = this.defaultDeviceByKey(sourceKey);
        }

        public async Task SetSourceToDevice(string sourceKey, MMDevice device)
        {
            String obsSourceName = this.obsSourceNames[sourceKey];
            JObject settings = new JObject { { "device_id", device.DeviceID } };
            await Obs.GetConnectedObs().SetSourceSettings(obsSourceName, settings, null); ;
            this.obsDevices[sourceKey] = device;
        }

        // public MMDevice getInputDeviceByID(string id) => this.systemInputDevices.FirstOrDefault((device) => device.DeviceID.Equals(id));
        // public MMDevice getOutputDeviceByID(string id) => this.systemOutputDevices.FirstOrDefault((device) => device.DeviceID.Equals(id));
        public MMDevice getDeviceByFriendlyName(string name)
        {
            try {
                var longName = this.shortDeviceNames[name];
                if (longName != null) name = longName;
            }
            catch (Exception) { }
            return this.systemDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));
        }
        // public MMDevice getInputDeviceByFriendlyName(string name) => this.systemInputDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));
        // public MMDevice getOutputDeviceByFriendlyName(string name) => this.systemOutputDevices.FirstOrDefault((device) => device.FriendlyName.Equals(name));

        private IEnumerable<string> shortOutputDeviceNames()
        {
            var names = this.systemOutputDevices.Select(d => d.FriendlyName);
            var shortNames = names.Select(name => {
                string pattern = @"(?<name>.*) \(.*\)";
                Match m = Regex.Match(name, pattern);
                if (!m.Success) return name;
                string shortName = m.Result("${name}");
                this.shortDeviceNames[shortName] = name;
                return shortName;
            });
            if (shortNames.GroupBy(n => n).Any(c => c.Count() > 1))
            {
                // Just return list with long names if short names have duplicates
                return names;
            }
            Logger.log?.Debug($"|ADC| Returning short names");
            return shortNames;
        }
        private IEnumerable<string> shortInputDeviceNames()
        {
            var names = this.systemInputDevices.Select(d => d.FriendlyName);
            var shortNames = names.Select(name => {
                string pattern = @".* \((?<name>.*)\)";
                Match m = Regex.Match(name, pattern);
                if (!m.Success) return name;
                string shortName = m.Result("${name}");
                this.shortDeviceNames[shortName] = name;
                return shortName;
            });
            if (shortNames.GroupBy(n => n).Any(c => c.Count() > 1))
            {
                // Just return list with long names if short names have duplicates
                return names;
            }
            Logger.log?.Debug($"|ADC| Returning short names");
            return shortNames;
        }

        public async Task UpdateAudioDevices(bool forceCurrentUpdate = true)
        {
            Logger.log?.Debug("|ADC| UpdateAudioDevices called.");
            refreshSystemDevices();
            List<string> inputDeviceNames = this.shortInputDeviceNames().ToList();
            List<string> outputDeviceNames = this.shortOutputDeviceNames().ToList();
            Plugin.config.UpdateSystemAudioDevices(outputDeviceNames, inputDeviceNames);

            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Warn("|ADC| Unable get OBS devices. OBS not connected.");
                return;
            }
            await refreshOBSDevices(obs);
            Logger.log?.Debug("|ADC| OBS Devices refreshed");
        }

        #region Setup/Teardown
        public override async Task InitializeAsync(OBSController obs)
        {
            await base.InitializeAsync(obs);
            await UpdateAudioDevices().ConfigureAwait(false);
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            await base.OnConnectAsync(cancellationToken);
            await UpdateAudioDevices().ConfigureAwait(false);
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
