using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public interface IGameplayModifiers
    {
		GameplayModifiers.EnergyType energyType { get; }

		
		
		
		bool batteryEnergy { get; }

		
		
		
		bool noFail { get; }

		
		
		
		bool demoNoFail { get; }

		
		
		
		bool instaFail { get; }

		
		
		
		bool failOnSaberClash { get; }

		
		
		
		GameplayModifiers.EnabledObstacleType enabledObstacleType { get; }

		
		
		
		bool noObstacles { get; }

		
		
		
		bool demoNoObstacles { get; }

		
		
		
		bool fastNotes { get; }

		
		
		
		bool strictAngles { get; }

		
		
		
		bool disappearingArrows { get; }

		
		
		
		bool ghostNotes { get; }

		
		
		
		bool noBombs { get; }

		
		
		
		GameplayModifiers.SongSpeed songSpeed { get; }

		
		
		float songSpeedMul { get; }

		
		
		
		bool noArrows { get; }

		
		
		static GameplayModifiers defaultModifiers { get; }
		
		protected GameplayModifiers.EnergyType _energyType;

		
		
		protected bool _noFail;

		
		
		protected bool _demoNoFail;

		
		
		protected bool _instaFail;

		
		
		protected bool _failOnSaberClash;

		
		
		protected GameplayModifiers.EnabledObstacleType _enabledObstacleType;

		
		
		protected bool _demoNoObstacles;

		
		
		protected bool _noBombs;

		
		
		protected bool _fastNotes;

		
		
		protected bool _strictAngles;

		
		
		protected bool _disappearingArrows;

		
		
		protected bool _ghostNotes;

		
		
		protected GameplayModifiers.SongSpeed _songSpeed;

		
		
		protected bool _noArrows;

		
		
	}

	enum EnabledObstacleType
	{
		
		All,
		
		FullHeightOnly,
		
		NoObstacles
	}

	
	enum EnergyType
	{
		
		Bar,
		
		Battery
	}

	
	enum SongSpeed
	{
		
		Normal,
		
		Faster,
		
		Slower
	}
}
