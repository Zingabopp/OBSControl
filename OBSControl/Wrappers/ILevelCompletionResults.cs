using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public interface ILevelCompletionResults
    {
		int PlayCount { get; }
		int MaxModifiedScore { get; }
		float ScorePercent { get; }
		GameplayModifiers gameplayModifiers { get; }
		int modifiedScore { get; }
		int rawScore { get; }
		RankModel.Rank rank { get; }
		bool fullCombo { get; }
		LevelCompletionResults.LevelEndStateType levelEndStateType { get; }
		LevelCompletionResults.LevelEndAction levelEndAction { get; }
		int averageCutScore { get; }
		int goodCutsCount { get; }
		int badCutsCount { get; }
		int missedCount { get; }
		int maxCombo { get; }
		float endSongTime { get; }
	}
}
