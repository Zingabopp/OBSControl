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
        private int _playCount;
        public LevelCompletionResultsWrapper(LevelCompletionResults results, int playCount, int maxModifiedScore)
        {
            _results = results;
            PlayCount = playCount;
            MaxModifiedScore = maxModifiedScore;
            if (MaxModifiedScore != 0)
                ScorePercent = ((float)results.rawScore / MaxModifiedScore) * 100f;
        }
        public int PlayCount { get; private set; }
        public int MaxModifiedScore { get; private set; }
        public float ScorePercent { get; private set; }

        public GameplayModifiers gameplayModifiers => _results.gameplayModifiers;

        public int modifiedScore => _results.modifiedScore;

        public int rawScore => _results.rawScore;

        public RankModel.Rank rank => _results.rank;

        public bool fullCombo => _results.fullCombo;

        public LevelCompletionResults.LevelEndStateType levelEndStateType => _results.levelEndStateType;

        public LevelCompletionResults.LevelEndAction levelEndAction => _results.levelEndAction;

        public int averageCutScore => _results.averageCutScore;

        public int goodCutsCount => _results.goodCutsCount;

        public int badCutsCount => _results.badCutsCount;

        public int missedCount => _results.missedCount;

        public int maxCombo => _results.maxCombo;

        public float endSongTime => _results.endSongTime;
    }
}
