namespace OBSControl
{
    internal class PluginConfig
    {
        public bool RegenerateConfig = true;
        public string ServerAddress { get; set; }
        public string ServerPassword { get; set; }
        public int LevelStartDelay { get; set; }
        public int RecordingStopDelay { get; set; }
        public string RecordingFileFormat { get; set; }

        public void FillDefaults()
        {
            RegenerateConfig = false;
            if (string.IsNullOrEmpty(ServerAddress))
                ServerAddress = "ws://127.0.0.1:4444";
            if (string.IsNullOrEmpty(RecordingFileFormat))
                RecordingFileFormat = "?N-?A_?%<_[?M]><-?F><-?e>";
        }
    }
}
