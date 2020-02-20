using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Utilities;
using Harmony;
using UnityEngine.SceneManagement;
using UnityEngine;
using OBSControl.OBSComponents;
using IPALogger = IPA.Logging.Logger;

namespace OBSControl
{
    public class Plugin : IBeatSaberPlugin, IDisablablePlugin
    {

        internal static string Name => "OBSControl";
        internal static Ref<PluginConfig> config;
        internal static IConfigProvider configProvider;

        public void Init(IPALogger logger, [Config.Prefer("json")] IConfigProvider cfgProvider)
        {
            Logger.log = logger;
            Logger.log.Debug("Logger initialised.");

            configProvider = cfgProvider;

            config = configProvider.MakeLink<PluginConfig>((p, v) =>
            {
                // Build new config file if it doesn't exist or RegenerateConfig is true
                if (v.Value == null || v.Value.RegenerateConfig)
                {
                    Logger.log.Debug("Regenerating PluginConfig");
                    p.Store(v.Value = new PluginConfig()
                    {
                        // Set your default settings here.
                        RegenerateConfig = false,
                        ServerAddress = "ws://127.0.0.1:4444",
                        ServerPassword = string.Empty,
                        LevelStartDelay = 2,
                        RecordingStopDelay = 4,
                        RecordingFileFormat = "?N-?A_?%<_[?M]><-?F><-?e>"
                    });
                }
                config = v;
            });
            HarmonyPatches.HarmonyManager.Initialize();
        }
        #region IDisablable

        /// <summary>
        /// Called when the plugin is enabled (including when the game starts if the plugin is enabled).
        /// </summary>
        public void OnEnable()
        {
            //config.Value.FillDefaults();
            Logger.log.Debug("OnEnable()");
            new GameObject("OBSControl_OBSController").AddComponent<OBSController>();
            new GameObject("OBSControl_RecordingController").AddComponent<RecordingController>();
            ApplyHarmonyPatches();
        }

        /// <summary>
        /// Called when the plugin is disabled. It is important to clean up any Harmony patches, GameObjects, and Monobehaviours here.
        /// The game should be left in a state as if the plugin was never started.
        /// </summary>
        public void OnDisable()
        {
            Logger.log.Debug("OnDisable()");
            RemoveHarmonyPatches();
            GameObject.Destroy(OBSController.instance.gameObject);
            GameObject.Destroy(RecordingController.instance.gameObject);
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

        /// <summary>
        /// Called when the active scene is changed.
        /// </summary>
        /// <param name="prevScene">The scene you are transitioning from.</param>
        /// <param name="nextScene">The scene you are transitioning to.</param>
        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

        }

        /// <summary>
        /// Called when the a scene's assets are loaded.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="sceneMode"></param>
        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {

        }

        public void OnApplicationQuit()
        {
            Logger.log.Debug("OnApplicationQuit");
            if (RecordingController.instance != null)
                GameObject.Destroy(RecordingController.instance);
            if (OBSController.instance != null)
                GameObject.Destroy(OBSController.instance);
        }

        /// <summary>
        /// Runs at a fixed intervalue, generally used for physics calculations. 
        /// </summary>
        public void OnFixedUpdate()
        {

        }

        /// <summary>
        /// This is called every frame.
        /// </summary>
        public void OnUpdate()
        {

        }


        public void OnSceneUnloaded(Scene scene)
        {

        }


        /// <summary>
        /// This should not be used with an IDisablable plugin. 
        /// It will not be called if the plugin starts disabled and is enabled while the game is running.
        /// </summary>
        public void OnApplicationStart()
        { }
    }
}
