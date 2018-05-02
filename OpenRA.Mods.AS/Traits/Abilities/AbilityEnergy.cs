using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class AbilityEnergyInfo : PausableConditionalTraitInfo
	{
		public readonly int MaxEnergy = 200;
		public readonly int StartingEnergy = 100;
		public readonly int Step = 1;
		public readonly int PercentageStep = 5;
		public readonly int Delay = 25;

		public override object Create(ActorInitializer init) { return new AbilityEnergy(init.Self, this); }
	}

	class AbilityEnergy : PausableConditionalTrait<AbilityEnergyInfo>, ITick
	{
		Actor self;
		AbilityEnergyInfo info;
		int ticks, energy;

		public AbilityEnergy(Actor self, AbilityEnergyInfo info) : base (info)
		{
			this.self = self;
			this.info = info;
			energy = info.StartingEnergy;
			ticks = info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			if (energy >= info.MaxEnergy)
				return;

			if (--ticks <= 0)
			{
				ticks = Info.Delay;

				// Cast to long to avoid overflow
				var nextstep = (int)((Info.Step + Info.PercentageStep * (long)info.MaxEnergy / 100));
				energy = (energy + nextstep) > info.MaxEnergy ? energy = info.MaxEnergy : energy += nextstep;
			}
		}

		public bool DrainEnergy(int amount)
		{
			if (amount > energy)
				return false;
			else
				energy -= amount;

			return true;
		}
	}
}
