using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#nullable enable

namespace OBSControl
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class NotificationController : MonoBehaviour
    {
        private HashSet<(string text, DateTime end)> Notifications = new HashSet<(string, DateTime)>();
        private bool ListDirty = false;
        /// <summary>
        /// Post a notification for the give duration (in seconds).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="duration"></param>
        public void Post(string text, float duration)
        {
            if (duration <= 0 || text == null || text.Length == 0)
                return;
            DateTime cancelAfter = DateTime.UtcNow + TimeSpan.FromSeconds(duration);
            Notifications.Add((text, cancelAfter));
            ListDirty = true;
            enabled = true;
        }

        private void UpdateText()
        {
            ListDirty = false;
        }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {

        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }

        private int Interval = 10;
        private int Counter;
        /// <summary>
        /// Called every frame if the script is enabled.
        /// </summary>
        private void Update()
        {
            Counter++;
            if (Counter < Interval)
                return;
            DateTime now = DateTime.UtcNow;
            int numRemoved = Notifications.RemoveWhere(e => e.end > now);
            if (numRemoved > 0)
            {
                ListDirty = true;
            }
            if (ListDirty)
                UpdateText();
        }

        /// <summary>
        /// Called every frame after every other enabled script's Update().
        /// </summary>
        private void LateUpdate()
        {

        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {

        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {

        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {

        }
        #endregion
    }
}
