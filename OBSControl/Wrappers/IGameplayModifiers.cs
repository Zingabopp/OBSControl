using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace OBSControl.Wrappers
{
    public interface IGameplayModifiers
    {
		EnergyType EnergyType { get; }
        bool SmallCubes { get; }
        bool ZenMode { get; }
        bool ProMode { get; }
        SongSpeed SongSpeed { get; }
        bool NoBombs { get; }
        bool GhostNotes { get; }
        bool DisappearingArrows { get; }
        bool NoArrows { get; }
        bool FastNotes { get; }
        EnabledObstacleType EnabledObstacleType { get; }
        bool FailOnSaberClash { get; }
        bool StrictAngles { get; }
        bool InstaFail { get; }
        bool NoFail { get; }
		bool NoObstacles { get; }
		float SongSpeedMul { get; }
        bool BatteryEnergy { get; }
    }

	public enum EnabledObstacleType
	{
		All,
		FullHeightOnly,
		NoObstacles
	}
	
	public enum EnergyType
	{
		Bar,
		Battery
	}
	
	public enum SongSpeed
	{
		Normal = 0,
		Faster = 1,
		Slower = 2,
        SuperFast = 3
	}

	
}
