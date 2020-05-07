using System;
using System.Collections.Generic;
using OBSControl.Wrappers;
using OBSControl.Utilities;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
#nullable enable
namespace OBSControl.Utilities
{
    public static class FileRenaming
    {
        public const string DefaultDateTimeFormat = "yyyyMMddHHmm";
        public static string GetDefaultFilename() => DateTime.Now.ToString("yyyyMMddHHmmss");
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
            string? retVal = null;
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
                    retVal = levelData.LevelAuthorName;
                    if (int.TryParse(data, out int mapperLimit) && retVal.Length > mapperLimit)
                        retVal = retVal.Substring(0, mapperLimit);
                    return retVal;
                case LevelDataType.LevelId:
                    return levelData.LevelID;
                case LevelDataType.NoteJumpSpeed:
                    return levelData.NoteJumpMovementSpeed.ToString("N2").TrimEnd('0').TrimEnd('.').TrimEnd(',');
                case LevelDataType.SongAuthorName:
                    retVal = levelData.SongAuthorName;
                    if (int.TryParse(data, out int authorLimit) && retVal.Length > authorLimit)
                        retVal = retVal.Substring(0, authorLimit);
                    return retVal;
                case LevelDataType.SongDurationNoLabels:
                    levelData.SongDuration.MinutesAndSeconds(out int durMin, out int durSec);
                    return durMin + "." + durSec.ToString("00");
                case LevelDataType.SongDurationLabeled:
                    levelData.SongDuration.MinutesAndSeconds(out int durMinL, out int durSecL);
                    return durMinL + "m." + durSecL.ToString("00") + "s";
                case LevelDataType.SongName:
                    retVal = levelData.SongName;
                    if (int.TryParse(data, out int songNameLimit) && retVal.Length > songNameLimit)
                        retVal = retVal.Substring(0, songNameLimit);
                    return retVal;
                case LevelDataType.SongSubName:
                    retVal = levelData.SongSubName;
                    if (int.TryParse(data, out int subNameLimit) && retVal.Length > subNameLimit)
                        retVal = retVal.Substring(0, subNameLimit);
                    return retVal;
                case LevelDataType.Date:
                    return DateTime.Now.ToString(data ?? DefaultDateTimeFormat);
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
                    return levelCompletionResults.GameplayModifiers.ToModifierString();
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

        /// <summary>
        /// Creates a file name string from a base string substituting characters prefixed by '?' with data from the game.
        /// </summary>
        /// <param name="baseString"></param>
        /// <param name="levelData"></param>
        /// <param name="levelCompletionResults"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="difficultyBeatmap"/> or <paramref name="levelCompletionResults"/> is null.</exception>
        public static string GetFilenameString(string? baseString, ILevelData levelData, ILevelCompletionResults levelCompletionResults)
        {
            if (levelData == null)
                throw new ArgumentNullException(nameof(levelData), "difficultyBeatmap cannot be null for GetFilenameString.");
            if (levelCompletionResults == null)
                throw new ArgumentNullException(nameof(levelCompletionResults), "levelCompletionResults cannot be null for GetFilenameString.");
            if(string.IsNullOrEmpty(baseString) || baseString == null)
                return string.Empty;
            if (!baseString.Contains("?"))
                return baseString;
            StringBuilder stringBuilder = new StringBuilder(baseString.Length);
            StringBuilder section = new StringBuilder(20);
            bool substituteNext = false;
            bool inProcessingGroup = false; // Group that is skipped if there's no data
            bool ignoreGroup = true; // False if the processingGroup contains data
            for(int i = 0; i < baseString.Length; i++)
            {
                char ch = baseString[i];
                switch (ch)
                {
                    case '<':
                        section.Clear();
                        inProcessingGroup = true;
                        continue;
                    case '>':
                        inProcessingGroup = false;
                        if (!ignoreGroup && section.Length > 0)
                            stringBuilder.Append(section.ToString());
                        section.Clear();
                        ignoreGroup = true;
                        continue;
                    case '?':
                        substituteNext = true;
                        continue;
                    default:
                        if (substituteNext)
                        {
                            string? dataString = null;
                            int nextIndex = i + 1;
                            if (nextIndex < baseString.Length && baseString[nextIndex] == '{')
                            {
                                nextIndex++;
                                int lastIndex = baseString.IndexOf('}', nextIndex);
                                if(lastIndex > 0)
                                {
                                    dataString = baseString.Substring(nextIndex, lastIndex - nextIndex);
                                    i = lastIndex;
                                }
                            }
                            if (inProcessingGroup)
                            {
                                string data;
                                try
                                {
                                    data = GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults, dataString);
                                }
                                catch
                                { 
                                    data = "INVLD"; 
                                }
                                if (!string.IsNullOrEmpty(data))
                                {
                                    ignoreGroup = false;
                                    section.Append(data);
                                }
                            }
                            else
                            {
                                try
                                {
                                    stringBuilder.Append(GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults, dataString));
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
                            if (inProcessingGroup)
                                section.Append(ch);
                            else
                                stringBuilder.Append(ch);
                        }
                        break;
                }
            }
            Utilities.GetSafeFilename(ref stringBuilder);
            return stringBuilder.ToString();
        }
    }
}
