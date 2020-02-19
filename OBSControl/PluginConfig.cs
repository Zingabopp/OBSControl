namespace OBSControl
{
    internal class PluginConfig
    {
        public bool RegenerateConfig = true;
        public string ServerIP { get; set; }
        public string ServerPassword { get; set; }
        public int LevelStartDelay { get; set; }
        public int RecordingStopDelay { get; set; }
        public string RecordingFileFormat { get; set; }
    }
}
