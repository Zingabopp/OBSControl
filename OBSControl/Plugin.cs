using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Utilities;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;
using OBSControl.OBSComponents;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using OBSControl.UI;
#nullable enable
namespace OBSControl
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static Plugin instance;
        internal static PluginConfig config;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static string Name => "OBSControl";
        internal static bool Enabled;

        [Init]
        public void Init(IPALogger logger, Config conf)
        {
            instance = this;
            Logger.log = logger;
            Logger.log?.Debug("Logger initialized.");

            config = conf.Generated<PluginConfig>();

            OBSWebsocketDotNet.OBSLogger.SetLogger(new OBSLogger());
            BSMLSettings.instance.AddSettingsMenu("OBSControl", "OBSControl.UI.SettingsView.bsml", config);
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += BSEvents_lateMenuSceneLoadedFresh;
        }

        private void BSEvents_lateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            Logger.log?.Warn("Creating control screen.");
            ControlScreenCoordinator.Instance.ShowControlScreen();
        }
        #region IDisablable

        public void AudioTest()
        {
            try
            {
                int count = NAudio.Wave.DirectSoundOut.Devices.Count();
                if (count == 0)
                    Logger.log?.Warn($"No devices.");
                foreach (var dev in NAudio.Wave.DirectSoundOut.Devices)
                {

                    Logger.log?.Info($"Device {dev.Description}: {dev.ModuleName}|{dev.Guid}");
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Debug(ex);
            }
        }

        /// <summary>
        /// Called when the plugin is enabled (including when the game starts if the plugin is enabled).
        /// </summary>
        [OnEnable]
        public void OnEnable()
        {
            //config.Value.FillDefaults();
            Logger.log?.Debug("OnEnable()");
            new GameObject("OBSControl_OBSController").AddComponent<OBSController>();

            AudioTest();

            ApplyHarmonyPatches();
            Enabled = true;

#if !TESTING
        }
        public void SetThings(string matName, string shaderName, string color, float alpha)
        {
        }
#else
            CreateTestPrimitive();
        }
        public void CreateTestPrimitive()
        {
            Primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Renderer renderer = Primitive.GetComponent<Renderer>();
            DefaultMaterial = renderer.material;
            DefaultShader = DefaultMaterial.shader;
            DefaultColor = DefaultMaterial.color;
            GameObject.DontDestroyOnLoad(Primitive);
            Primitive.transform.position = new Vector3(0, 1.8f, -2);
        }
        GameObject? Primitive;
        Material DefaultMaterial;
        Shader DefaultShader;
        Color DefaultColor;
        Color32 OtherColor;
        public void SetThings(string matName, string shaderName, string color, float alpha)
        {
            if(Primitive == null)
            {
                Logger.log?.Warn("Primitive is null");
                return;
            }
            alpha = Mathf.Clamp(alpha, 0f, 1f);
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>().OrderBy(m => m.name).ToArray();
            Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>().OrderBy(m => m.name).ToArray();
            Renderer renderer = Primitive.GetComponent<Renderer>();
            Logger.log?.Info("------------------------------");
            Logger.log?.Info($"Previous Material: '{renderer.sharedMaterial.name}'");
            Logger.log?.Info($"Previous Shader: '{renderer.sharedMaterial.shader.name}'");
            Logger.log?.Info($"Previous Color: '{renderer.sharedMaterial.color}'");
            Logger.log?.Info("------------------------------");
            if (!string.IsNullOrEmpty(matName))
            {
                Material? material = materials.FirstOrDefault(m => m.name == matName);
                if (material != null)
                {
                    renderer.material = material;
                }
                else
                    Logger.log?.Warn($"Could not find {nameof(Material)} '{matName}'");
            }
            else
                renderer.material = DefaultMaterial;
            if (!string.IsNullOrEmpty(shaderName))
            {
                Shader? shader = shaders.FirstOrDefault(m => m.name == shaderName);
                if (shader != null)
                {
                    renderer.sharedMaterial.shader = shader;
                }
                else
                    Logger.log?.Warn($"Could not find {nameof(Shader)} '{shaderName}'");
            }
            else
                renderer.sharedMaterial.shader = DefaultShader;
            if (!string.IsNullOrEmpty(color))
            {
                if (ColorUtility.TryParseHtmlString(color, out Color parsedColor))
                {
                    parsedColor.a = alpha;
                    renderer.sharedMaterial.color = parsedColor;
                }
                else
                    Logger.log?.Warn($"Could not find {nameof(Shader)} '{shaderName}'");
            }
            else
                renderer.sharedMaterial.color = DefaultColor;
            Logger.log?.Info($"Available Materials: {string.Join(", ", materials.Select(m => m.name))}");
            Logger.log?.Info($"Available Shaders: {string.Join(", ", shaders.Select(m => m.name))}");
            Logger.log?.Info("------------------------------");
            Logger.log?.Info($"Current Material: '{renderer.sharedMaterial.name}'");
            Logger.log?.Info($"Current Shader: '{renderer.sharedMaterial.shader.name}'");
            Logger.log?.Info($"Current Color: '{renderer.sharedMaterial.color}'");
        }
#endif
        /// <summary>
        /// Called when the plugin is disabled. It is important to clean up any Harmony patches, GameObjects, and Monobehaviours here.
        /// The game should be left in a state as if the plugin was never started.
        /// </summary>
        [OnDisable]
        public void OnDisable()
        {
            Logger.log?.Debug("OnDisable()");
            RemoveHarmonyPatches();
            GameObject.Destroy(OBSController.instance?.gameObject);
            Enabled = false;
        }
        #endregion

        /// <summary>
        /// Attempts to apply all the Harmony patches in this assembly.
        /// </summary>
        public static void ApplyHarmonyPatches()
        {
            HarmonyPatches.HarmonyManager.ApplyDefaultPatches();
        }

        /// <summary>
        /// Attempts to remove all the Harmony patches that used our HarmonyId.
        /// </summary>
        public static void RemoveHarmonyPatches()
        {
            // Removes all patches with this HarmonyId
            HarmonyPatches.HarmonyManager.UnpatchAll();
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Logger.log?.Debug("OnApplicationQuit");
            if (OBSController.instance != null)
                GameObject.Destroy(OBSController.instance);
        }
    }
}
