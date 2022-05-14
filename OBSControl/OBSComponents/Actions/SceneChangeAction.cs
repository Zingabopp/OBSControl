using OBSControl.Utilities;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public class SceneChangeAction : ObsAction
    {
        public override ControlEventType EventType => ControlEventType.SceneChange;
        public string SceneName { get; }
        public int Timeout { get; set; } = 5000; // TODO: What if transition > 5 seconds?
        private AsyncEventListenerWithArg<string?, string, string> SceneListener { get; }

        public SceneChangeAction(OBSWebsocket obs, string sceneName)
            : base(obs)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentNullException(nameof(sceneName));
            SceneName = sceneName;
            SceneListener = new AsyncEventListenerWithArg<string?, string, string>((s, sceneName, expectedScene) =>
             {
                 if (string.IsNullOrEmpty(expectedScene))
                     return new EventListenerResult<string?>(null, true);
                 if (sceneName == expectedScene)
                     return new EventListenerResult<string?>(sceneName, true);
                 else
                     return new EventListenerResult<string?>(sceneName, false);
             }, string.Empty, Timeout);
        }

        private void OnSceneChange(object sender, SceneChangeEventArgs e)
        {
            SceneListener.OnEvent(sender, e.NewSceneName);
        }

        protected override async Task ActionAsync(CancellationToken cancellationToken)
        {
            obs.SceneChanged -= OnSceneChange;
            obs.SceneChanged += OnSceneChange;
            SceneListener.Reset(SceneName, cancellationToken);
            SceneListener.StartListening();
            var currentScene = await obs.GetCurrentScene(cancellationToken).ConfigureAwait(false);
            if (currentScene != null && currentScene.Name == SceneName)
                return;
            await SceneListener.Task.ConfigureAwait(false);
        }

        protected override void Cleanup()
        {
            obs.SceneChanged -= OnSceneChange;
            SceneListener.TrySetCanceled();
        }
    }
}
