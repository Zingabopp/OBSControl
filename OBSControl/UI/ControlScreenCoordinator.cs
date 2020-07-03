using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OBSControl.UI
{
    public class ControlScreenCoordinator
    {
        private static ControlScreenCoordinator _instance;
        public static ControlScreenCoordinator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ControlScreenCoordinator();
                return _instance;
            }
        }
        protected ControlScreenCoordinator()
        {
        }
        private List<Tab> Tabs = new List<Tab>();
        protected FloatingScreen ControlScreen;
        protected ControlScreen_Main ControlScreen_Main;

        public void ShowControlScreen()
        {
            if (ControlScreen == null)
            {
                ControlScreen = CreateFloatingScreen();

                ControlScreen_Main = BeatSaberUI.CreateViewController<ControlScreen_Main>();
                ControlScreen.SetRootViewController(ControlScreen_Main, false);
                Logger.log.Critical($"Control screen created: {ControlScreen != null}");
            }
            ControlScreen.gameObject.SetActive(true);
        }
        public FloatingScreen CreateFloatingScreen()
        {
            FloatingScreen screen = FloatingScreen.CreateFloatingScreen(new Vector2(100, 50), true, new Vector3(0f, 2.9f, 2.4f), Quaternion.Euler(-30f, 0f, 0f));
            GameObject.DontDestroyOnLoad(screen.gameObject);
            return screen;
        }
    }
}
