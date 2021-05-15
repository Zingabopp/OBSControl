using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using OBSControl.OBSComponents;
using UnityEngine;
#nullable enable
/// <summary>
/// See https://github.com/pardeike/Harmony/wiki for a full reference on Harmony.
/// </summary>
namespace OBSControl.HarmonyPatches
{
    /// <summary>
    /// This patches ClassToPatch.MethodToPatch(Parameter1Type arg1, Parameter2Type arg2)
    /// </summary>
    [HarmonyPatch(typeof(GameSongController), "waitUntilIsReadyToStartTheSong_get",
        new Type[] { })]
    public class GameSongController_ReadyToStart
    {
        /// <summary>
        /// Delay level start until OBS is recording or timeout.
        /// </summary>
        static void Postfix(GameSongController __instance, ref AudioTimeSyncController ____audioTimeSyncController, ref WaitUntil __result)
        {
            RecordingController? recordingController = OBSController.instance?.GetOBSComponent<RecordingController>();
            AudioTimeSyncController audioTimeSyncController = ____audioTimeSyncController;
            if (!(recordingController?.ActiveAndConnected ?? false))
            {
                return;
            }
            AudioDevicesController? audioDeviceController = OBSController.instance?.GetOBSComponent<AudioDevicesController>();
            if (audioDeviceController?.ActiveAndConnected ?? false)
            {
                audioDeviceController.SetDevicesFromConfig();
            }
            RecordStartOption recordStartOption = recordingController.RecordStartOption;
            if (recordStartOption != RecordStartOption.SongStart)
                return;
            TimeSpan delay;
            float startDelay = Plugin.config.SongStartDelay;
            if (startDelay > 0)
                delay = TimeSpan.FromSeconds(startDelay);
            else
                delay = TimeSpan.Zero;
            DateTime now = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(5) + delay;
            Logger.log?.Debug($"Song Start delay enabled, waiting for recording to start and delaying by {delay.TotalSeconds}s, timing out after {timeout.TotalSeconds}s");
            // TODO: Add fallback for other recording start options that should've started recording by now?
            if (recordStartOption == RecordStartOption.SongStart)
            {
                __result = new WaitUntil(() =>
                {
                    if (!audioTimeSyncController.isAudioLoaded)
                    {
                        //Logger.log?.Debug($"Audio not loaded, delaying song.");
                        return false;
                    }
                    if (now + timeout < DateTime.UtcNow) // Wait timed out, continue anyway.
                    {
                        Logger.log?.Critical($"Level start wait timed out, starting song.");
                        return true;
                    }
                    if (recordingController.OutputState == OBSWebsocketDotNet.Types.OutputState.Started)
                    {
                        if (delay > TimeSpan.Zero)
                        {
                            if (recordingController.RecordStartTime + delay > DateTime.UtcNow)
                            {

                                //Logger.log?.Debug($"Level start delay not met, delaying song.");
                                return false;
                            }
                        }

                        Logger.log?.Debug($"Level start conditions met, starting song.");
                        return true;
                    }
                    //Logger.log?.Debug($"OBS recording not started, delaying song.");
                    return false;
                });
            }
        }

    }
}