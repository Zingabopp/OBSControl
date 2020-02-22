using System;
using System.Collections.Generic;
using OBSControl.Wrappers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

namespace OBSControl.Utilities
{
    public static class FileRenaming
    {
        public static readonly ReadOnlyDictionary<char, LevelDataType> LevelDataSubstitutions = new ReadOnlyDictionary<char, LevelDataType>(new Dictionary<char, LevelDataType>()
        {
            {'B', LevelDataType.BeatsPerMinute },
            {'D', LevelDataType.DifficultyName },
            {'d', LevelDataType.DifficultyShortName },
            {'A', LevelDataType.LevelAuthorName },
            {'a', LevelDataType.SongAuthorName },
            {'I', LevelDataType.LevelId },
            {'J', LevelDataType.NoteJumpSpeed },
            {'L', LevelDataType.SongDurationLabeled },
            {'l', LevelDataType.SongDurationNoLabels },
            {'N', LevelDataType.SongName },
            {'n', LevelDataType.SongSubName },
            //------CompletionResults----------
            {'1', LevelDataType.FirstPlay },
            {'b', LevelDataType.BadCutsCount },
            {'T', LevelDataType.EndSongTimeLabeled },
            {'t', LevelDataType.EndSongTimeNoLabels },
            {'F', LevelDataType.FullCombo },
            {'M', LevelDataType.Modifiers },
            {'m', LevelDataType.MissedCount },
            {'G', LevelDataType.GoodCutsCount },
            {'E', LevelDataType.LevelEndType },
            {'e', LevelDataType.LevelIncompleteType },
            {'C', LevelDataType.MaxCombo },
            {'S', LevelDataType.RawScore },
            {'s', LevelDataType.ModifiedScore },
            {'R', LevelDataType.Rank },
            {'%', LevelDataType.ScorePercent }
        });

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
            SongDurationNoLabels,
            SongDurationLabeled,
            SongName,
            SongSubName,
            FirstPlay,
            BadCutsCount,
            EndSongTimeNoLabels,
            EndSongTimeLabeled,
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
            // Can't use difficulty name extensions outside the game.
#if DEBUG
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
#else
            return shortName ? difficulty.ShortName() : difficulty.Name();
#endif
        }

        public static string GetLevelDataString(LevelDataType levelDataType, IDifficultyBeatmap difficultyBeatmap, 
            ILevelCompletionResults levelCompletionResults)
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
                case LevelDataType.SongDurationNoLabels:
                    difficultyBeatmap.level.songDuration.MinutesAndSeconds(out int durMin, out int durSec);
                    return durMin + "." + durSec.ToString("00");
                case LevelDataType.SongDurationLabeled:
                    difficultyBeatmap.level.songDuration.MinutesAndSeconds(out int durMinL, out int durSecL);
                    return durMinL + "m." + durSecL.ToString("00") + "s";
                case LevelDataType.SongName:
                    return difficultyBeatmap.level.songName;
                case LevelDataType.SongSubName:
                    return difficultyBeatmap.level.songSubName;
                case LevelDataType.FirstPlay:
                    if (levelCompletionResults.PlayCount == 0)
                        return "1st";
                    else
                        return string.Empty;
                case LevelDataType.BadCutsCount:
                    return levelCompletionResults.badCutsCount.ToString();
                case LevelDataType.EndSongTimeNoLabels:
                    levelCompletionResults.endSongTime.MinutesAndSeconds(out int endMin, out int endSec);
                    return endMin + "." + endSec.ToString("00");
                case LevelDataType.EndSongTimeLabeled:
                    levelCompletionResults.endSongTime.MinutesAndSeconds(out int endMinL, out int endSecL);
                    return endMinL + "m." + endSecL.ToString("00") + "s";
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
                    string scoreStr = levelCompletionResults.ScorePercent.ToString("F3");
                    return scoreStr.Substring(0, scoreStr.Length - 1); // Game rounds down
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

        /// <summary>
        /// Creates a file name string from a base string substituting characters prefixed by '?' with data from the game.
        /// </summary>
        /// <param name="baseString"></param>
        /// <param name="difficultyBeatmap"></param>
        /// <param name="levelCompletionResults"></param>
        /// <param name="maxModifiedScore"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="difficultyBeatmap"/> or <paramref name="levelCompletionResults"/> is null.</exception>
        public static string GetFilenameString(string baseString, IDifficultyBeatmap difficultyBeatmap, ILevelCompletionResults levelCompletionResults)
        {
            if (difficultyBeatmap == null)
                throw new ArgumentNullException(nameof(difficultyBeatmap), "difficultyBeatmap cannot be null for GetFilenameString.");
            if (levelCompletionResults == null)
                throw new ArgumentNullException(nameof(levelCompletionResults), "levelCompletionResults cannot be null for GetFilenameString.");

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
                                string data;
                                try
                                {
                                    data = GetLevelDataString(LevelDataSubstitutions[ch], difficultyBeatmap, levelCompletionResults);
                                }
                                catch
                                { 
                                    data = "INVLD"; 
                                }
                                if (!string.IsNullOrEmpty(data))
                                {
                                    ignoreGroup = false;
                                    groupString += data;
                                }
                            }
                            else
                            {
                                try
                                {
                                    stringBuilder.Append(GetLevelDataString(LevelDataSubstitutions[ch], difficultyBeatmap, levelCompletionResults));
                                }
                                catch 
                                { 
                                    stringBuilder.Append("INVLD"); 
                                }
                                
                            }
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
