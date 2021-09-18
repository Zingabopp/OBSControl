using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#nullable enable
namespace OBSControl
{
    public static class GameStatus
    {
        private static GameplayModifiersModelSO? _gpModSO;
        private static GameplayCoreSceneSetupData? _gameSetupData;
        public static int MaxScore;
        //public static int MaxModifiedScore;

        public static GameplayCoreSceneSetupData? GameSetupData
        {
            get
            {
                if (BS_Utils.Plugin.LevelData.IsSet)
                    _gameSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                return _gameSetupData;
            }
        }

        public static IDifficultyBeatmap? DifficultyBeatmap
        {
            get { return GameSetupData?.difficultyBeatmap; }
        }

        public static IBeatmapLevel? LevelInfo
        {
            get
            {
                return DifficultyBeatmap?.level;
            }
        }

        public static GameplayModifiersModelSO? GpModSO
        {
            get
            {
                if (_gpModSO == null)
                {
                    Logger.log?.Debug("GameplayModifersModelSO is null, getting new one");
                    _gpModSO = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().FirstOrDefault();
                }
                if (_gpModSO == null)
                {
                    Logger.log?.Warn("GameplayModifersModelSO is still null");
                }
                //else
                //    Logger.Debug("Found GameplayModifersModelSO");
                return _gpModSO;
            }
        }

        public static int GetMaxModifiedScore(float endEnergy)
        {
            int maxModifiedScore = 0;
            GameplayModifiersModelSO? gpModSo = GameStatus.GpModSO;
            GameplayModifiers? mods = GameSetupData?.gameplayModifiers;
            List<GameplayModifierParamsSO> modifiers;
            if (gpModSo != null && mods != null)
            {
                modifiers = gpModSo.CreateModifierParamsList(mods);
                maxModifiedScore = gpModSo.GetModifiedScoreForGameplayModifiers(GameStatus.MaxScore, modifiers, endEnergy);
                Logger.log?.Debug($"MaxModifiedScore with energy '{endEnergy}': {maxModifiedScore}");

            }
            else
            {
                Logger.log?.Warn("Could not determine gameplay modifiers.");
            }
            return maxModifiedScore;
        }

        public static void Setup()
        {
            try
            {
                MaxScore = ScoreModel.MaxRawScoreForNumberOfNotes(DifficultyBeatmap?.beatmapData.cuttableNotesCount ?? 0);
                Logger.log?.Debug($"MaxScore: {MaxScore}");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting max scores: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
