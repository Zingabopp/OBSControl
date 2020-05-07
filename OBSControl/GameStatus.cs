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
        public static int MaxModifiedScore;

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

        public static void Setup()
        {
            try
            {
                MaxScore = ScoreModel.MaxRawScoreForNumberOfNotes(DifficultyBeatmap?.beatmapData.notesCount ?? 0);
                Logger.log?.Debug($"MaxScore: {MaxScore}");
                MaxModifiedScore = GameStatus.GpModSO?.GetModifiedScoreForGameplayModifiers(GameStatus.MaxScore, GameSetupData?.gameplayModifiers) ?? 0;
                Logger.log?.Debug($"MaxModifiedScore: {MaxModifiedScore}");
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
