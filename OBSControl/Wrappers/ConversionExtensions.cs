using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public static class ConversionExtensions
    {
        public static Difficulty ToBeatmapDifficulty(this BeatmapDifficulty difficulty)
        {
            return difficulty switch
            {
                BeatmapDifficulty.Easy => Difficulty.Easy,
                BeatmapDifficulty.Normal => Difficulty.Normal,
                BeatmapDifficulty.Hard => Difficulty.Hard,
                BeatmapDifficulty.Expert => Difficulty.Expert,
                BeatmapDifficulty.ExpertPlus => Difficulty.ExpertPlus,
                _ => Difficulty.Easy,
            };
        }

        public static ScoreRank ToScoreRank(this RankModel.Rank rank)
        {
            return rank switch
            {
                RankModel.Rank.E => ScoreRank.E,
                RankModel.Rank.D => ScoreRank.D,
                RankModel.Rank.C => ScoreRank.C,
                RankModel.Rank.B => ScoreRank.B,
                RankModel.Rank.A => ScoreRank.A,
                RankModel.Rank.S => ScoreRank.S,
                RankModel.Rank.SS => ScoreRank.SS,
                RankModel.Rank.SSS => ScoreRank.SSS,
                _ => ScoreRank.E,
            };
        }

        public static LevelEndState ToLevelEndState(this LevelCompletionResults.LevelEndStateType endState)
        {
            return endState switch
            {
                LevelCompletionResults.LevelEndStateType.Incomplete => LevelEndState.None,
                LevelCompletionResults.LevelEndStateType.Cleared => LevelEndState.Cleared,
                LevelCompletionResults.LevelEndStateType.Failed => LevelEndState.Failed,
                _ => LevelEndState.None,
            };
        }

        public static SongEndAction ToSongEndAction(this LevelCompletionResults.LevelEndAction endAction)
        {
            return endAction switch
            {
                LevelCompletionResults.LevelEndAction.None => SongEndAction.None,
                LevelCompletionResults.LevelEndAction.Quit => SongEndAction.Quit,
                LevelCompletionResults.LevelEndAction.Restart => SongEndAction.Restart,
                //LevelCompletionResults.LevelEndAction.LostConnection => SongEndAction.LostConnection,
                //LevelCompletionResults.LevelEndAction.MultiplayerInactive => SongEndAction.MultiplayerInactive,
                //LevelCompletionResults.LevelEndAction.StartupFailed => SongEndAction.StartupFailed,
                //LevelCompletionResults.LevelEndAction.HostEndedLevel => SongEndAction.HostEndedLevel,
                //LevelCompletionResults.LevelEndAction.ConnectedAfterLevelEnded => SongEndAction.ConnectedAfterLevelEnded,
                _ => SongEndAction.None,
            };
        }

        public static EnabledObstacleType ToEnabledObstacleType(this GameplayModifiers.EnabledObstacleType type)
        {
            return type switch
            {
                GameplayModifiers.EnabledObstacleType.All => EnabledObstacleType.All,
                GameplayModifiers.EnabledObstacleType.FullHeightOnly => EnabledObstacleType.FullHeightOnly,
                GameplayModifiers.EnabledObstacleType.NoObstacles => EnabledObstacleType.NoObstacles,
                _ => EnabledObstacleType.All,
            };
        }

        public static EnergyType ToEnergyType(this GameplayModifiers.EnergyType energyType)
        {
            return energyType switch
            {
                GameplayModifiers.EnergyType.Bar => EnergyType.Bar,
                GameplayModifiers.EnergyType.Battery => EnergyType.Battery,
                _ => EnergyType.Bar,
            };
        }

        public static SongSpeed ToSongSpeed(this GameplayModifiers.SongSpeed speed)
        {
            return speed switch
            {
                GameplayModifiers.SongSpeed.Normal => SongSpeed.Normal,
                GameplayModifiers.SongSpeed.Faster => SongSpeed.Faster,
                GameplayModifiers.SongSpeed.Slower => SongSpeed.Slower,
                _ => SongSpeed.Normal,
            };
        }


    }
}
