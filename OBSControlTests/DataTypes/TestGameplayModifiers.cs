using OBSControl.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OBSControlTests.DataTypes
{
    public class TestGameplayModifiers : IGameplayModifiers
    {
        public EnergyType EnergyType { get; set; }

        public bool BatteryEnergy { get; set; }

        public bool NoFail { get; set; }

        public bool DemoNoFail { get; set; }

        public bool InstaFail { get; set; }

        public bool FailOnSaberClash { get; set; }

        public EnabledObstacleType EnabledObstacleType { get; set; }

        public bool NoObstacles { get; set; }

        public bool DemoNoObstacles { get; set; }

        public bool FastNotes { get; set; }

        public bool StrictAngles { get; set; }

        public bool DisappearingArrows { get; set; }

        public bool GhostNotes { get; set; }

        public bool NoBombs { get; set; }

        public SongSpeed SongSpeed { get; set; }

        public float SongSpeedMul { get; set; }

        public bool NoArrows { get; set; }
    }
}
