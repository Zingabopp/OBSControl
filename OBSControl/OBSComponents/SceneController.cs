using OBSControl.Utilities;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#nullable enable

namespace OBSControl.OBSComponents
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneController : OBSComponent
    {
        private readonly object _availableSceneLock = new object();
        #region Exposed Events
        public event EventHandler<string>? SceneChanged;
        public event EventHandler? SceneListUpdated;
        #endregion


        #region OBS Properties
        private string? _currentScene;

        public string? CurrentScene
        {
            get { return _currentScene; }
            protected set
            {
                if (_currentScene == value) return;
                _currentScene = value;
                if (value != null)
                    SceneChanged?.Invoke(this, value);
            }
        }

        protected readonly List<string> AvailableScenes = new List<string>();
        public string[] GetAvailableScenes()
        {
            string[] scenes;
            lock (_availableSceneLock)
            {
                scenes = AvailableScenes.ToArray();
            }
            return scenes;
        }

        protected void UpdateSceneList(IEnumerable<string> scenes)
        {
            lock (_availableSceneLock)
            {
                AvailableScenes.Clear();
                AvailableScenes.AddRange(scenes);
            }
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                try
                {
                    Plugin.config.UpdateSceneOptions(scenes);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error setting scene list for config: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            });
            SceneListUpdated?.Invoke(this, null);
        }

        #endregion

        #region OBS Actions

        public async Task SetScene(string sceneName)
        {
            OBSWebsocket obs = Obs.Obs ?? throw new InvalidOperationException("OBSWebsocket is unavailable.");

            AsyncEventListener<bool,string> SceneListener = new AsyncEventListener<bool,string>((s, name) =>
            {
                if (sceneName == name)
                    return true;
                return false;
            }, 0, AllTasksCancelSource.Token);
            try
            {
                obs.SceneChanged += SceneListener.OnEvent;
                await obs.SetCurrentScene(sceneName).ConfigureAwait(false);
                string? current = await UpdateCurrentScene().ConfigureAwait(false);
                if (current == sceneName)
                    return;
                await SceneListener.Task.ConfigureAwait(false);
            }catch(Exception ex)
            {
                Logger.log?.Error($"Error setting scene to '{sceneName}': {ex.Message}");
                Logger.log?.Debug(ex);
            }
            finally
            {
                obs.SceneChanged -= SceneListener.OnEvent;
            }
        }

        public async Task<string?> UpdateCurrentScene()
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Warn("Unable to update current scene. OBS not connected.");
                return null;
            }
            try
            {
                string currentScene = (await obs.GetCurrentScene().ConfigureAwait(false)).Name;
                CurrentScene = currentScene;
                return currentScene;
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting current scene: {ex.Message}");
                Logger.log?.Debug(ex);
                return null;
            }
        }

        public async Task UpdateScenes(bool forceCurrentUpdate = true)
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Warn("Unable to update current scene. OBS not connected.");
                return;
            }
            string[] availableScenes = null!;
            try
            {
                GetSceneListInfo sceneData = await obs.GetSceneList().ConfigureAwait(false);
                availableScenes = (sceneData).Scenes.Select(s => s.Name).ToArray();
                Logger.log?.Info($"OBS scene list updated: {string.Join(", ", availableScenes)}");
                try
                {
                    string? current = sceneData.CurrentScene;
                    if (current != null && current.Length > 0)
                        CurrentScene = current;
                    else if (forceCurrentUpdate)
                    {
                        current = await UpdateCurrentScene().ConfigureAwait(false);
                        if (current != null && current.Length > 0)
                            CurrentScene = current;
                    }
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error getting current scene: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
                UpdateSceneList(availableScenes);
            }
            catch (Exception ex)
            {
                if (availableScenes == null)
                    availableScenes = Array.Empty<string>();
                Logger.log?.Error($"Error getting scene list: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }

        #endregion

        #region Setup/Teardown
        public override async Task InitializeAsync(OBSController obs)
        {
            await base.InitializeAsync(obs);
            await UpdateScenes().ConfigureAwait(false);
        }

        protected override void SetEvents(OBSWebsocket obs)
        {
            RemoveEvents(obs);
            obs.SceneListChanged += OnObsSceneListChanged;
            obs.SceneChanged += OnObsSceneChanged;
        }


        protected override void RemoveEvents(OBSWebsocket obs)
        {
            obs.SceneListChanged -= OnObsSceneListChanged;
            obs.SceneChanged -= OnObsSceneChanged;
        }
        #endregion

        #region OBS Websocket Event Handlers
        private async void OnObsSceneListChanged(object sender, EventArgs? e)
        {
            try
            {
                await UpdateScenes().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in 'OnObsSceneListChanged' handler: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }
        private void OnObsSceneChanged(OBSWebsocket sender, string newSceneName)
        {
            CurrentScene = newSceneName;
        }
        #endregion

        #region Monobehaviour Messages


        #endregion
    }
}
