using System;
using System.Collections.Generic;
using OBSControl.Wrappers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OBSControl.Utilities
{
    public static class FileRenaming
    {
        private static readonly Dictionary<char, LevelDataType> LevelDataSubstitutions = new Dictionary<char, LevelDataType>()
        {
            {'B', LevelDataType.BeatsPerMinute },
            {'d', LevelDataType.DifficultyShortName },
            {'D', LevelDataType.DifficultyName },
            {'A', LevelDataType.LevelAuthorName },
            {'I', LevelDataType.LevelId },
            {'J', LevelDataType.NoteJumpSpeed },
            {'a', LevelDataType.SongAuthorName },
            {'L', LevelDataType.SongDuration },
            {'N', LevelDataType.SongName },
            {'n', LevelDataType.SongSubName },
            //------CompletionResults----------
            {'b', LevelDataType.BadCutsCount },
            {'t', LevelDataType.EndSongTime },
            {'F', LevelDataType.FullCombo },
            {'M', LevelDataType.Modifiers },
            {'G', LevelDataType.GoodCutsCount },
            {'E', LevelDataType.LevelEndType },
            {'e', LevelDataType.LevelIncompleteType },
            {'C', LevelDataType.MaxCombo },
            {'m', LevelDataType.MissedCount },
            {'s', LevelDataType.ModifiedScore },
            {'R', LevelDataType.Rank },
            {'S', LevelDataType.RawScore },
            {'%', LevelDataType.ScorePercent }
        };

        public enum LevelDataType
        {
            None,
            BeatsPerMinute,
            DifficultyShortName,
            DifficultyName,
            LevelAuthorName,
            LevelId,
            NoteJumpSpeed,
            SongAuthorName,
            SongDuration,
            SongName,
            SongSubName,
            BadCutsCount,
            EndSongTime,
            FullCombo,
            Modifiers,
            GoodCutsCount,
            LevelEndType,
            LevelIncompleteType,
            MaxCombo,
            MissedCount,
            ModifiedScore,
            Rank,
            RawScore,
            ScorePercent
        }

        public static string GetDifficultyName(BeatmapDifficulty difficulty, bool shortName = false)
        {
            if (!shortName)
                return difficulty.ToString();
            switch (difficulty)
            {
                case BeatmapDifficulty.Easy:
                    return "E";
                case BeatmapDifficulty.Normal:
                    return "N";
                case BeatmapDifficulty.Hard:
                    return "H";
                case BeatmapDifficulty.Expert:
                    return "E";
                case BeatmapDifficulty.ExpertPlus:
                    return "E+";
                default:
                    return "NA";
            }
        }

        public static string GetLevelDataString(LevelDataType levelDataType, IDifficultyBeatmap difficultyBeatmap, ILevelCompletionResults levelCompletionResults, int maxModifiedScore)
        {
            switch (levelDataType)
            {
                case LevelDataType.None:
                    return string.Empty;
                case LevelDataType.BeatsPerMinute:
                    return difficultyBeatmap.level.beatsPerMinute.ToString("N2").TrimEnd('0').TrimEnd('.').TrimEnd(',');
                case LevelDataType.DifficultyShortName:
                    return GetDifficultyName(difficultyBeatmap.difficulty, true);
                case LevelDataType.DifficultyName:
                    return GetDifficultyName(difficultyBeatmap.difficulty, false);
                case LevelDataType.LevelAuthorName:
                    return difficultyBeatmap.level.levelAuthorName;
                case LevelDataType.LevelId:
                    return difficultyBeatmap.level.levelID;
                case LevelDataType.NoteJumpSpeed:
                    return difficultyBeatmap.noteJumpMovementSpeed.ToString("N2").TrimEnd('0').TrimEnd('.').TrimEnd(',');
                case LevelDataType.SongAuthorName:
                    return difficultyBeatmap.level.songAuthorName;
                case LevelDataType.SongDuration:
                    difficultyBeatmap.level.songDuration.MinutesAndSeconds(out int durMin, out int durSec);
                    return durMin + "." + durSec;
                case LevelDataType.SongName:
                    return difficultyBeatmap.level.songName;
                case LevelDataType.SongSubName:
                    return difficultyBeatmap.level.songSubName;
                case LevelDataType.BadCutsCount:
                    return levelCompletionResults.badCutsCount.ToString();
                case LevelDataType.EndSongTime:
                    levelCompletionResults.endSongTime.MinutesAndSeconds(out int endMin, out int endSec);
                    return endMin + "." + endSec;
                case LevelDataType.FullCombo:
                    return levelCompletionResults.fullCombo ? "FC" : string.Empty;
                case LevelDataType.Modifiers:
                    return GetModifierString(levelCompletionResults.gameplayModifiers);
                case LevelDataType.GoodCutsCount:
                    return levelCompletionResults.goodCutsCount.ToString();
                case LevelDataType.LevelEndType:
                    if (levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Quit
                        || levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
                        return "Quit";
                    switch (levelCompletionResults.levelEndStateType)
                    {
                        case LevelCompletionResults.LevelEndStateType.None:
                            return "Unknown";
                        case LevelCompletionResults.LevelEndStateType.Cleared:
                            return "Cleared";
                        case LevelCompletionResults.LevelEndStateType.Failed:
                            return "Failed";
                        default:
                            return "Unknown";
                    }
                case LevelDataType.LevelIncompleteType:
                    if (levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Quit
                        || levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
                        return "Quit";

                    string submissionProlongedDisabledByMods = BS_Utils.Gameplay.ScoreSubmission.ProlongedModString;
                    switch (levelCompletionResults.levelEndStateType)
                    {
                        case LevelCompletionResults.LevelEndStateType.None:
                            return "Unknown";
                        case LevelCompletionResults.LevelEndStateType.Cleared:
                            return string.Empty;
                        case LevelCompletionResults.LevelEndStateType.Failed:
                            return "Failed";
                        default:
                            return string.Empty;
                    }
                case LevelDataType.MaxCombo:
                    return levelCompletionResults.maxCombo.ToString();
                case LevelDataType.MissedCount:
                    return levelCompletionResults.missedCount.ToString();
                case LevelDataType.ModifiedScore:
                    return levelCompletionResults.modifiedScore.ToString();
                case LevelDataType.Rank:
                    return levelCompletionResults.rank.ToString();
                case LevelDataType.RawScore:
                    return levelCompletionResults.rawScore.ToString();
                case LevelDataType.ScorePercent:
                    float scorePercent = ((float)levelCompletionResults.rawScore / maxModifiedScore) * 100f;
                    string scoreStr = scorePercent.ToString("N2");
                    return scoreStr;
                default:
                    return "NA";
            }
        }

        public static string GetModifierString(GameplayModifiers modifiers, string separator = "_")
        {
            List<string> activeModifiers = new List<string>();
            if (modifiers.songSpeed != GameplayModifiers.SongSpeed.Normal)
            {
                if (modifiers.songSpeed == GameplayModifiers.SongSpeed.Faster)
                    activeModifiers.Add("FS");
                else
                    activeModifiers.Add("SS");
            }
            if (modifiers.disappearingArrows)
                activeModifiers.Add("DA");
            if (modifiers.ghostNotes)
                activeModifiers.Add("GN");
            if (modifiers.batteryEnergy)
                activeModifiers.Add("BE");
            if (modifiers.demoNoFail)
                activeModifiers.Add("DNF");
            if (modifiers.demoNoObstacles)
                activeModifiers.Add("DNO");
            if (modifiers.enabledObstacleType != GameplayModifiers.EnabledObstacleType.All)
            {
                if (modifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.FullHeightOnly)
                    activeModifiers.Add("FHO");
                else
                    activeModifiers.Add("NO");
            }
            //if (modifiers.energyType == GameplayModifiers.EnergyType.Battery)
            //    activeModifiers.Add("BE");
            if (modifiers.failOnSaberClash)
                activeModifiers.Add("FSC");
            if (modifiers.fastNotes)
                activeModifiers.Add("FN");
            if (modifiers.instaFail)
                activeModifiers.Add("IF");
            if (modifiers.noArrows)
                activeModifiers.Add("NA");
            if (modifiers.noBombs)
                activeModifiers.Add("NB");
            if (modifiers.noFail)
                activeModifiers.Add("NF");
            //if (modifiers.noObstacles)
            //    activeModifiers.Add("NO");
            if (modifiers.strictAngles)
                activeModifiers.Add("SA");
            return string.Join(separator, activeModifiers);
        }

        public static string GetFileNameString(string baseString, IDifficultyBeatmap difficultyBeatmap, ILevelCompletionResults levelCompletionResults, int maxModifiedScore)
        {
            if (!baseString.Contains("?"))
                return baseString;
            StringBuilder stringBuilder = new StringBuilder(baseString.Length);
            bool substituteNext = false;
            bool processingGroup = false;
            bool ignoreGroup = true;
            string groupString = string.Empty;
            foreach (char ch in baseString)
            {
                switch (ch)
                {
                    case '<':
                        processingGroup = true;
                        continue;
                    case '>':
                        processingGroup = false;
                        if (!ignoreGroup && !string.IsNullOrEmpty(groupString))
                            stringBuilder.Append(groupString);
                        groupString = string.Empty;
                        ignoreGroup = true;
                        continue;
                    case '?':
                        substituteNext = true;
                        continue;
                    default:
                        if (substituteNext)
                        {
                            if (processingGroup)
                            {
                                string data = GetLevelDataString(LevelDataSubstitutions[ch], difficultyBeatmap, levelCompletionResults, maxModifiedScore);
                                if (!string.IsNullOrEmpty(data))
                                {
                                    ignoreGroup = false;
                                    groupString += data;
                                }
                            }
                            else
                                stringBuilder.Append(GetLevelDataString(LevelDataSubstitutions[ch], difficultyBeatmap, levelCompletionResults, maxModifiedScore));
                            substituteNext = false;
                        }
                        else
                        {
                            if (processingGroup)
                                groupString += ch;
                            else
                                stringBuilder.Append(ch);
                        }
                        break;
                }
            }
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string retStr = stringBuilder.ToString();
            for(int i = 0; i < invalidChars.Length; i++)
                retStr.Replace(invalidChars[i], '_');
            return retStr;
        }
    }
}
