using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace OBSControl.HarmonyPatches
{
    public static class HarmonyManager
    {
        public static readonly string HarmonyId = "com.github.Zingabopp.OBSControl";
        private static Harmony? _harmony;
        internal static Harmony Harmony
        {
            get
            {
                return _harmony ??= new Harmony(HarmonyId);
            }
        }
        private static readonly BindingFlags allBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static HarmonyPatchInfo? LevelDelayPatch;
        private static HarmonyPatchInfo? ReadyToStartPatch;
        internal readonly static HashSet<HarmonyPatchInfo> AppliedPatches = new HashSet<HarmonyPatchInfo>();

        public static bool ApplyPatch(HarmonyPatchInfo patchInfo)
        {
            return patchInfo.ApplyPatch(Harmony);
        }

        public static bool RemovePatch(HarmonyPatchInfo patchInfo)
        {
            return patchInfo.RemovePatch(Harmony);
        }

        public static bool ApplyPatch(Harmony harmony, MethodInfo original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null)
        {
            try
            {
                string? patchTypeName = null;
                if (prefix != null)
                    patchTypeName = prefix.method.DeclaringType?.Name;
                else if (postfix != null)
                    patchTypeName = postfix.method.DeclaringType?.Name;
                Logger.log?.Debug($"Harmony patching {original.Name} with {patchTypeName}");
                harmony.Patch(original, prefix, postfix);
                return true;
            }
            catch (Exception e)
            {
                Logger.log?.Error($"Unable to patch method {original.Name}: {e.Message}");
                Logger.log?.Debug(e);
                return false;
            }
        }

        public static void ApplyDefaultPatches()
        {

        }

        public static void UnpatchAll()
        {
            foreach (var patch in AppliedPatches.ToList())
            {
                patch.RemovePatch();
            }
            Harmony.UnpatchAll(HarmonyId);
        }
        public static HarmonyPatchInfo GetLevelDelayPatch()
        {
            if(LevelDelayPatch == null)
            {
                MethodInfo original = typeof(LevelSelectionFlowCoordinator).GetMethod("StartLevel", allBindingFlags);
                HarmonyMethod prefix = new HarmonyMethod(typeof(StartLevelPatch).GetMethod("Prefix", allBindingFlags));
                LevelDelayPatch = new HarmonyPatchInfo(Harmony, original, prefix, null);
            }
            return LevelDelayPatch;
        }
        public static HarmonyPatchInfo GetReadyToStartPatch()
        {
            if (ReadyToStartPatch == null)
            {
                MethodInfo original = typeof(GameSongController).GetMethod("get_waitUntilIsReadyToStartTheSong", allBindingFlags);
                HarmonyMethod postFix = new HarmonyMethod(typeof(GameSongController_ReadyToStart).GetMethod("Postfix", allBindingFlags));
                ReadyToStartPatch = new HarmonyPatchInfo(Harmony, original, null, postFix);
            }
            return ReadyToStartPatch;
        }
    }
}
