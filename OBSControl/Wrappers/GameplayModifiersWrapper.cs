﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public class GameplayModifiersWrapper : IGameplayModifiers
    {
        private GameplayModifiers _modifiers;
        private readonly float _endEnergy;
        public GameplayModifiersWrapper(GameplayModifiers modifiers, float endEnergy)
        {
            _modifiers = modifiers;
            _endEnergy = endEnergy;
        }

        bool IGameplayModifiers.SmallCubes => _modifiers.smallCubes;
        bool IGameplayModifiers.ZenMode => _modifiers.zenMode;
        bool IGameplayModifiers.ProMode => _modifiers.proMode;
        public EnergyType EnergyType => _modifiers.energyType.ToEnergyType();
        public bool BatteryEnergy => _modifiers.energyType == GameplayModifiers.EnergyType.Battery;
        public bool NoFail => _modifiers.noFailOn0Energy && _endEnergy < float.Epsilon;
        public bool InstaFail => _modifiers.instaFail;
        public bool FailOnSaberClash => _modifiers.failOnSaberClash;
        public EnabledObstacleType EnabledObstacleType => _modifiers.enabledObstacleType.ToEnabledObstacleType();
        public bool NoObstacles => _modifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles;
        public bool FastNotes => _modifiers.fastNotes;
        public bool StrictAngles => _modifiers.strictAngles;
        public bool DisappearingArrows => _modifiers.disappearingArrows;
        public bool GhostNotes => _modifiers.ghostNotes;
        public bool NoBombs => _modifiers.noBombs;
        public SongSpeed SongSpeed => _modifiers.songSpeed.ToSongSpeed();
        public float SongSpeedMul => _modifiers.songSpeedMul;
        public bool NoArrows => _modifiers.noArrows;

        public override string ToString()
        {
            return this.ToModifierString();
        }
    }
}
