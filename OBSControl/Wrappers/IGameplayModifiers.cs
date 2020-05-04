using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public interface IGameplayModifiers
    {
		EnergyType EnergyType { get; }
		bool BatteryEnergy { get; }
		bool NoFail { get; }
		bool DemoNoFail { get; }
		bool InstaFail { get; }
		bool FailOnSaberClash { get; }
		EnabledObstacleType EnabledObstacleType { get; }
		bool NoObstacles { get; }
		bool DemoNoObstacles { get; }
		bool FastNotes { get; }
		bool StrictAngles { get; }
		bool DisappearingArrows { get; }
		bool GhostNotes { get; }
		bool NoBombs { get; }
		SongSpeed SongSpeed { get; }
		float SongSpeedMul { get; }
		bool NoArrows { get; }		
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
		Normal,
		Faster,
		Slower
	}
}
