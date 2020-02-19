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
        public LevelCompletionResultsWrapper(LevelCompletionResults results)
        {
            _results = results;
        }

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
