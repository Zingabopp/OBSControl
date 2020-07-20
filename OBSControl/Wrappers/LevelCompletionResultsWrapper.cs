using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public class LevelCompletionResultsWrapper : ILevelCompletionResults
    {
        private LevelCompletionResults _results;
        public LevelCompletionResultsWrapper(LevelCompletionResults results, int playCount, int maxModifiedScore)
        {
            _results = results;
            GameplayModifiers = new GameplayModifiersWrapper(results.gameplayModifiers);
            PlayCount = playCount;
            MaxModifiedScore = maxModifiedScore;
            if (MaxModifiedScore != 0) // Should never be 0, but check anyway to be safe.
                ScorePercent = ((float)results.modifiedScore / MaxModifiedScore) * 100f;
        }
        public int PlayCount { get; private set; }
        public int MaxModifiedScore { get; private set; }
        public float ScorePercent { get; private set; }

        public IGameplayModifiers GameplayModifiers { get; }

        public int ModifiedScore => _results.modifiedScore;

        public int RawScore => _results.rawScore;

        public ScoreRank Rank => _results.rank.ToScoreRank();

        public bool FullCombo => _results.fullCombo;

        public LevelEndState LevelEndStateType => _results.levelEndStateType.ToLevelEndState();

        public SongEndAction LevelEndAction => _results.levelEndAction.ToSongEndAction();

        public int AverageCutScore => _results.averageCutScore;

        public int GoodCutsCount => _results.goodCutsCount;

        public int BadCutsCount => _results.badCutsCount;

        public int MissedCount => _results.missedCount;

        public int MaxCombo => _results.maxCombo;

        public float EndSongTime => _results.endSongTime;
    }
}
