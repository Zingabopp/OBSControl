using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RankModel;

namespace OBSControl.Wrappers
{
    public interface ILevelCompletionResults
    {
		int PlayCount { get; }
		int MaxModifiedScore { get; }
		float ScorePercent { get; }
		IGameplayModifiers GameplayModifiers { get; }
		int ModifiedScore { get; }
		int RawScore { get; }
		ScoreRank Rank { get; }
		bool FullCombo { get; }
		LevelEndState LevelEndStateType { get; }
		SongEndAction LevelEndAction { get; }
		int AverageCutScore { get; }
		int GoodCutsCount { get; }
		int BadCutsCount { get; }
		int MissedCount { get; }
		int MaxCombo { get; }
		float EndSongTime { get; }
	}

	public enum ScoreRank
	{
		E = 0,
		D = 1,
		C = 2,
		B = 3,
		A = 4,
		S = 5,
		SS = 6,
		SSS = 7
	}

	public enum LevelEndState
	{
		None = 0,
		Cleared = 1,
		Failed = 2
	}
	public enum SongEndAction
	{
		None = 0,
		Quit = 1,
		Restart = 2,
		LostConnection = 3,
		RoomDestroyed = 4
	}
}
