using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Graphics;
using OpenRA.Orders;

namespace OpenRA.Mods.AS.Traits
{
	interface IAbility
	{
		string AbilitySlot { get; }
	}
	public abstract class AbilityInfo : PausableConditionalTraitInfo, Requires<AbilityEnergyInfo>, IAbility
	{
		[Desc("How much does the ability costs in energy to be allowed to cast?")]
		public readonly int EnergyCost = 0;

		[Desc("How long in ticks does pause between casts take?")]
		public readonly int Cooldown = 100;

		[Desc("Which ability slot can trigger the ability (from Ability0 to Ability9)")]
		public readonly string AbilitySlot = "Ability0";

		string IAbility.AbilitySlot { get { return AbilitySlot; } }

		//public readonly string AbilityNoEnergySound;
		//public readonly string AbilityDisabledCannotCastSound;
	}

	abstract class AbilityBase<InfoType> : PausableConditionalTrait<InfoType> where InfoType : AbilityInfo
	{
		public AbilityBase(InfoType info) : base(info) { }
	}

	public class AbilityOrderGenerator : UnitOrderGenerator
	{
		IEnumerable<Actor> actors;
		string ordername;

		public AbilityOrderGenerator(IEnumerable<Actor> actors, string ordername)
		{
			this.actors = actors.Where(a => !a.IsDead);
			this.ordername = ordername;
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return "ability";
		}

		public override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
		{
			yield break;
		}

		public override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			yield break;
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				world.CancelInputMode();

			return OrderInner(world, cell, mi);
		}

		protected virtual IEnumerable<Order> OrderInner(World world, CPos cell, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				world.CancelInputMode();

				var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
				var orderName = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "AssaultMove" : "AttackMove";

				// Cells outside the playable area should be clamped to the edge for consistency with move orders
				cell = world.Map.Clamp(cell);
				foreach (var s in actors)
					yield return new Order(orderName, s, Target.FromCell(world, cell), queued);
			}
		}

		public override void Tick(World world)
		{
			if (actors.All(s => s.IsDead))
				world.CancelInputMode();
		}

		public override bool InputOverridesSelection(WorldRenderer wr, World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}
	}
}
