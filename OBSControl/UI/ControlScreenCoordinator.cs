using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BS_Utils.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUIControls;

#nullable enable
namespace OBSControl.UI
{
    public class ControlScreenCoordinator
    {
        private static ControlScreenCoordinator? _instance;
        public static ControlScreenCoordinator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ControlScreenCoordinator();
                return _instance;
            }
        }


        private List<Tab> Tabs = new List<Tab>();
        protected FloatingScreen? _controlScreen;
        protected ControlScreen? ControlScreenView;
        public ControlScreen? ControlScreen => ControlScreenView;
        protected ControlScreenCoordinator()
        {
            BSEvents.earlyMenuSceneLoadedFresh += BSEvents_earlyMenuSceneLoadedFresh;
            BSEvents.gameSceneActive += OnGameSceneActive;
            BSEvents.menuSceneActive += OnMenuSceneActive;
            BSEvents.songPaused += OnSongPaused;
            BSEvents.songUnpaused += OnSongUnpaused;

        }

        private void OnSongUnpaused()
        {
            SetControlScreenLock(true);
        }

        private void OnSongPaused()
        {
            if (ControlScreenView != null)
                SetControlScreenLock(ControlScreenView.WindowLocked);
        }

        public void SetControlScreenLock(bool locked)
        {
            if (_controlScreen != null)
                _controlScreen.ShowHandle = !locked;
        }

        private void OnMenuSceneActive()
        {
            ResetScreenMover(false);
        }

        private void OnGameSceneActive()
        {
            ResetScreenMover(true);
            SetControlScreenLock(true);
        }

        private void BSEvents_earlyMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            if (_controlScreen != null)
            {
                Logger.log?.Warn("Destroying ControlScreen");
                GameObject.Destroy(_controlScreen.gameObject);
                _controlScreen = null;
            }
        }


        public void ShowControlScreen()
        {
            if (_controlScreen == null)
            {
                _controlScreen = CreateFloatingScreen();
                ControlScreenView = BeatSaberUI.CreateViewController<ControlScreen>();
                ControlScreenView.ParentCoordinator = this;
                _controlScreen.SetRootViewController(ControlScreenView, false);
                SetScreenTransform(_controlScreen, Plugin.config);
                Logger.log?.Critical($"Control screen created: {_controlScreen != null}");
            }
            _controlScreen?.gameObject.SetActive(true);
        }

        public FloatingScreen CreateFloatingScreen()
        {
            PluginConfig config = Plugin.config;
            FloatingScreen screen = FloatingScreen.CreateFloatingScreen(
                new Vector2(100, 50), true,
                Vector3.zero,
                Quaternion.identity);

            screen.HandleReleased -= OnRelease;
            screen.HandleReleased += OnRelease;

            if (!config.ShowScreenHandle)
                screen.ShowHandle = false;


            GameObject.DontDestroyOnLoad(screen.gameObject);
            return screen;
        }

        /// <summary>
        /// Fixes floating screen mover not working after changing scenes.
        /// </summary>
        /// <param name="isGameScene"></param>
        private void ResetScreenMover(bool isGameScene)
        {
            FloatingScreenMoverPointer? screenMover = ControlScreen?.screenMover;
            if (ControlScreen != null && screenMover != null)
            {
                VRPointer pointer;
                if (isGameScene)
                    pointer = Resources.FindObjectsOfTypeAll<VRPointer>().LastOrDefault();
                else
                    pointer = Resources.FindObjectsOfTypeAll<VRPointer>().FirstOrDefault();

                if (pointer != null)
                {
                    screenMover.Init(ControlScreen, pointer);
                }
                else
                    Logger.log?.Warn($"Couldn't find VRPointer.");
            }
        }

        internal static void SetScreenTransform(FloatingScreen screen, PluginConfig config)
        {
            screen.transform.position = config.GetScreenPosition();
            screen.transform.rotation = config.GetScreenRotation();
        }

        private void OnRelease(object _, FloatingScreenHandleEventArgs posRot)

        {
            PluginConfig config = Plugin.config;
            using IDisposable t = config.ChangeTransaction();
            Vector3 newPos = posRot.Position;
            Vector3 euler = posRot.Rotation.eulerAngles;

            config.ScreenPosX = newPos.x;
            config.ScreenPosY = newPos.y;
            config.ScreenPosZ = newPos.z;
            config.ScreenRotX = euler.x;
            config.ScreenRotY = euler.y;
            config.ScreenRotZ = euler.z;
        }


    }
}
