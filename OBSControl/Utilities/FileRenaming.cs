using System;
using System.Collections.Generic;
using OBSControl.Wrappers;
using OBSControl.Utilities;
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
            {'@', LevelDataType.Date },
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
            Date,
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

        public static string GetDifficultyName(Difficulty difficulty, bool shortName = false)
        {
            if (!shortName)
                return difficulty.ToString();
            return difficulty switch
            {
                Difficulty.Easy => "E",
                Difficulty.Normal => "N",
                Difficulty.Hard => "H",
                Difficulty.Expert => "E",
                Difficulty.ExpertPlus => "E+",
                _ => "NA",
            };
        }

        public static string GetLevelDataString(LevelDataType levelDataType, ILevelData levelData, 
            ILevelCompletionResults levelCompletionResults, string? data = null)
        {
            switch (levelDataType)
            {
                case LevelDataType.None:
                    return string.Empty;
                case LevelDataType.BeatsPerMinute:
                    return levelData.BeatsPerMinute.ToString("N2").TrimEnd('0').TrimEnd('.').TrimEnd(',');
                case LevelDataType.DifficultyShortName:
                    return GetDifficultyName(levelData.Difficulty, true);
                case LevelDataType.DifficultyName:
                    return GetDifficultyName(levelData.Difficulty, false);
                case LevelDataType.LevelAuthorName:
                    return levelData.LevelAuthorName;
                case LevelDataType.LevelId:
                    return levelData.LevelID;
                case LevelDataType.NoteJumpSpeed:
                    return levelData.NoteJumpMovementSpeed.ToString("N2").TrimEnd('0').TrimEnd('.').TrimEnd(',');
                case LevelDataType.SongAuthorName:
                    return levelData.SongAuthorName;
                case LevelDataType.SongDurationNoLabels:
                    levelData.SongDuration.MinutesAndSeconds(out int durMin, out int durSec);
                    return durMin + "." + durSec.ToString("00");
                case LevelDataType.SongDurationLabeled:
                    levelData.SongDuration.MinutesAndSeconds(out int durMinL, out int durSecL);
                    return durMinL + "m." + durSecL.ToString("00") + "s";
                case LevelDataType.SongName:
                    return levelData.SongName;
                case LevelDataType.SongSubName:
                    return levelData.SongSubName;
                case LevelDataType.Date:
                    return DateTime.Now.ToString(data ?? "yyyyMMddHHmm");
                case LevelDataType.FirstPlay:
                    if (levelCompletionResults.PlayCount == 0)
                        return "1st";
                    else
                        return string.Empty;
                case LevelDataType.BadCutsCount:
                    return levelCompletionResults.BadCutsCount.ToString();
                case LevelDataType.EndSongTimeNoLabels:
                    levelCompletionResults.EndSongTime.MinutesAndSeconds(out int endMin, out int endSec);
                    return endMin + "." + endSec.ToString("00");
                case LevelDataType.EndSongTimeLabeled:
                    levelCompletionResults.EndSongTime.MinutesAndSeconds(out int endMinL, out int endSecL);
                    return endMinL + "m." + endSecL.ToString("00") + "s";
                case LevelDataType.FullCombo:
                    return levelCompletionResults.FullCombo ? "FC" : string.Empty;
                case LevelDataType.Modifiers:
                    return GetModifierString(levelCompletionResults.GameplayModifiers);
                case LevelDataType.GoodCutsCount:
                    return levelCompletionResults.GoodCutsCount.ToString();
                case LevelDataType.LevelEndType:
                    if (levelCompletionResults.LevelEndAction == SongEndAction.Quit
                        || levelCompletionResults.LevelEndAction == SongEndAction.Restart)
                        return "Quit";
                    return levelCompletionResults.LevelEndStateType switch
                    {
                        LevelEndState.None => "Unknown",
                        LevelEndState.Cleared => "Cleared",
                        LevelEndState.Failed => "Failed",
                        _ => "Unknown",
                    };
                case LevelDataType.LevelIncompleteType:
                    if (levelCompletionResults.LevelEndAction == SongEndAction.Quit
                        || levelCompletionResults.LevelEndAction == SongEndAction.Restart)
                        return "Quit";
                    return levelCompletionResults.LevelEndStateType switch
                    {
                        LevelEndState.None => "Unknown",
                        LevelEndState.Cleared => string.Empty,
                        LevelEndState.Failed => "Failed",
                        _ => string.Empty,
                    };
                case LevelDataType.MaxCombo:
                    return levelCompletionResults.MaxCombo.ToString();
                case LevelDataType.MissedCount:
                    return levelCompletionResults.MissedCount.ToString();
                case LevelDataType.ModifiedScore:
                    return levelCompletionResults.ModifiedScore.ToString();
                case LevelDataType.Rank:
                    return levelCompletionResults.Rank.ToString();
                case LevelDataType.RawScore:
                    return levelCompletionResults.RawScore.ToString();
                case LevelDataType.ScorePercent:
                    string scoreStr = levelCompletionResults.ScorePercent.ToString("F3");
                    return scoreStr.Substring(0, scoreStr.Length - 1); // Game rounds down
                default:
                    return "NA";
            }
        }

        public static string GetModifierString(IGameplayModifiers modifiers, string separator = "_")
        {
            List<string> activeModifiers = new List<string>();
            if (modifiers.SongSpeed != SongSpeed.Normal)
            {
                if (modifiers.SongSpeed == SongSpeed.Faster)
                    activeModifiers.Add("FS");
                else
                    activeModifiers.Add("SS");
            }
            if (modifiers.DisappearingArrows)
                activeModifiers.Add("DA");
            if (modifiers.GhostNotes)
                activeModifiers.Add("GN");
            if (modifiers.BatteryEnergy)
                activeModifiers.Add("BE");
            if (modifiers.DemoNoFail)
                activeModifiers.Add("DNF");
            if (modifiers.DemoNoObstacles)
                activeModifiers.Add("DNO");
            if (modifiers.EnabledObstacleType != EnabledObstacleType.All)
            {
                if (modifiers.EnabledObstacleType == EnabledObstacleType.FullHeightOnly)
                    activeModifiers.Add("FHO");
                else
                    activeModifiers.Add("NO");
            }
            //if (modifiers.energyType == GameplayModifiers.EnergyType.Battery)
            //    activeModifiers.Add("BE");
            if (modifiers.FailOnSaberClash)
                activeModifiers.Add("FSC");
            if (modifiers.FastNotes)
                activeModifiers.Add("FN");
            if (modifiers.InstaFail)
                activeModifiers.Add("IF");
            if (modifiers.NoArrows)
                activeModifiers.Add("NA");
            if (modifiers.NoBombs)
                activeModifiers.Add("NB");
            if (modifiers.NoFail)
                activeModifiers.Add("NF");
            //if (modifiers.noObstacles)
            //    activeModifiers.Add("NO");
            if (modifiers.StrictAngles)
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
        public static string GetFilenameString(string baseString, ILevelData levelData, ILevelCompletionResults levelCompletionResults)
        {
            if (levelData == null)
                throw new ArgumentNullException(nameof(levelData), "difficultyBeatmap cannot be null for GetFilenameString.");
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
                                    data = GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults);
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
                                    stringBuilder.Append(GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults));
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
            for(int i = 0; i < invalidChars.Length; i++)
                stringBuilder.Replace(invalidChars[i], '_');
            return stringBuilder.ToString();
        }
    }
}
