namespace OBSControl
{
    internal class PluginConfig
    {

        public virtual string ServerAddress { get; set; } = "ws://127.0.0.1:4444";
        public virtual string ServerPassword { get; set; } = string.Empty;
        public virtual int LevelStartDelay { get; set; } = 2;
        public virtual int RecordingStopDelay { get; set; } = 4;
        public virtual string RecordingFileFormat { get; set; } = "?N-?A_?%<_[?M]><-?F><-?e>";

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }
    }
}
