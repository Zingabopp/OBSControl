using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OBSControl
{
    public static class GameStatus
    {
        private static GameplayModifiersModelSO _gpModSO;
        private static GameplayCoreSceneSetupData _gameSetupData;
        public static int MaxScore;
        public static int MaxModifiedScore;

        public static GameplayCoreSceneSetupData gameSetupData
        {
            get
            {
                if (BS_Utils.Plugin.LevelData.IsSet)
                    _gameSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                return _gameSetupData;
            }
        }

        public static IDifficultyBeatmap DifficultyBeatmap
        {
            get { return gameSetupData?.difficultyBeatmap; }
        }

        public static IBeatmapLevel LevelInfo
        {
            get
            {
                return DifficultyBeatmap?.level;
            }
        }

        public static GameplayModifiersModelSO GpModSO
        {
            get
            {
                if (_gpModSO == null)
                {
                    Logger.log.Debug("GameplayModifersModelSO is null, getting new one");
                    _gpModSO = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().FirstOrDefault();
                }
                if (_gpModSO == null)
                {
                    Logger.log.Warn("GameplayModifersModelSO is still null");
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
                MaxScore = ScoreModel.MaxRawScoreForNumberOfNotes(DifficultyBeatmap.beatmapData.notesCount);
                Logger.log.Debug($"MaxScore: {MaxScore}");
                MaxModifiedScore = GameStatus.GpModSO.GetModifiedScoreForGameplayModifiers(GameStatus.MaxScore, gameSetupData.gameplayModifiers);
                Logger.log.Debug($"MaxModifiedScore: {MaxModifiedScore}");
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error getting max scores: {ex}");
                Logger.log.Debug(ex);
            }
        }
    }
}
