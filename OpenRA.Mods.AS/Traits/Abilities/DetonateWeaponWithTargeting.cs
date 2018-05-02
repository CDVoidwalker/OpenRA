#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Traits;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Traits
{
	public class DetonateWeaponWithTargetingInfo : AbilityInfo
	{
		public readonly int Cooldown = 25;
		public readonly int EnergyCost = 100;

		public readonly bool IsChanneled = false;
		public readonly int ChannelCastInterval = 25;
		public readonly int PerChannelIntervalCost = 25;
		public readonly int FacingTolerance = 10;

		[WeaponReference] [FieldLoader.Require]
		public readonly string Weapon;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DetonateWeaponWithTargeting(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
			WeaponInfo weapon;
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
			WeaponInfo = weapon;
		}
	}

	class DetonateWeaponWithTargeting : AbilityBase<DetonateWeaponWithTargetingInfo>, IIssueOrder, IResolveOrder, ITick, IRenderAboveShroudWhenSelected
	{
		Actor self;
		[Sync]
		int chargeTick = 0, channelTick = 0;
		public int ChannelTick { get { return channelTick; } }
		public int ChargeTick { get { return chargeTick; } }

		AbilityEnergy energyTrait;
		WPos targetpos;
		SpreadDamageWarhead warhead;

		public DetonateWeaponWithTargeting(Actor self, DetonateWeaponWithTargetingInfo info) : base(info)
		{
			this.self = self;
			energyTrait = self.Trait<AbilityEnergy>();

			warhead = (SpreadDamageWarhead)Info.WeaponInfo.Warheads.Where(x => x is SpreadDamageWarhead).
				MaxBy(x => ((SpreadDamageWarhead)x).Spread);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DetonateWeaponWithTargetingTargeter(this);
			}
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }

		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (targetpos == null || self.Owner != self.World.LocalPlayer)
				yield break;

			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			foreach (var unit in UnitsInRange(xy))
			{
				var bounds = unit.TraitsImplementing<IDecorationBounds>().FirstNonEmptyBounds(unit, wr);
				yield return new SelectionBoxRenderable(unit, bounds, Color.Red);
			}

			yield return new RangeCircleRenderable(targetpos, warhead.Spread, 0, Color.Red, Color.Yellow);
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Ability0")
				throw new Exception("Ability0 IssueOrder");

			if (order.OrderID == "Ability1")
				throw new Exception("Ability1 IssueOrder");

			if (order.OrderID == "DetonateWeaponWithTargetingOrder")
			{
				targetpos = target.CenterPosition;
				return new Order(order.OrderID, self, target, queued);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Ability0")
				throw new Exception("Ability0 ResolveOrder");

			if (order.OrderString == "Ability1")
				throw new Exception("Ability1 ResolveOrder");

			if (order.OrderString == "DetonateWeaponWithTargetingOrder" && chargeTick == 0)
			{
				self.QueueActivity(new DetonateWeaponWithTargetingActivity(self, order.Target, true, Info.FacingTolerance, Info.WeaponInfo, this));
			}
		}

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;

			if (channelTick > 0)
				channelTick--;
		}

		internal void StartCooldown()
		{
			chargeTick = Info.Cooldown;
		}

		internal void StartChannelingDelay()
		{
			channelTick = Info.ChannelCastInterval;
		}

		internal bool CanChannelAnotherTime()
		{
			return energyTrait.DrainEnergy(Info.PerChannelIntervalCost);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var tiles = self.World.Map.FindTilesInCircle(xy, warhead.Spread.Length);
			var units = new List<Actor>();

			foreach (var t in tiles)
				units.AddRange(self.World.ActorMap.GetActorsAt(t));

			return units.Distinct().Where(a =>
			{
				if (!a.Owner.IsAlliedWith(self.Owner))
					return false;

				return a.TraitsImplementing<Targetable>()
					.Any(t => warhead.IsValidAgainst(a, self));
			});
		}
	}

	class DetonateWeaponWithTargetingTargeter : IOrderTargeter
	{
		DetonateWeaponWithTargeting trait;

		public DetonateWeaponWithTargetingTargeter(DetonateWeaponWithTargeting trait) { this.trait = trait; }

		public string OrderID { get { return "DetonateWeaponWithTargetingOrder"; } }
		public int OrderPriority { get { return 5; } }
		public bool IsQueued { get; protected set; }

		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			cursor = "ability";

			return modifiers.HasModifier(TargetModifiers.None);
		}
	}

	class DetonateWeaponWithTargetingActivity : Attack
	{
		WeaponInfo winfo;
		Target target;
		Actor self;
		readonly IMove move;
		readonly IFacing facing;
		readonly int facingTolerance;

		DetonateWeaponWithTargeting trait;
		Activity turnActivity;
		Activity moveActivity;
		AttackStatus attackStatus = AttackStatus.UnableToAttack;

		public DetonateWeaponWithTargetingActivity(Actor self, Target target, bool allowMovement, int facingTolerance, WeaponInfo winfo, DetonateWeaponWithTargeting trait)
			:base(self, target, allowMovement, allowMovement,facingTolerance)
		{
			this.self = self;
			this.winfo = winfo;
			this.target = target;
			move = allowMovement ? self.TraitOrDefault<IMove>() : null;
			this.facingTolerance = facingTolerance;
			facing = self.Trait<IFacing>();
			this.trait = trait;
		}

		public override Activity Tick(Actor self)
		{
			turnActivity = moveActivity = null;
			attackStatus = AttackStatus.UnableToAttack;

			var status = TickAttack(self);

			if (attackStatus.HasFlag(AttackStatus.Attacking))
				return this;

			if (attackStatus.HasFlag(AttackStatus.NeedsToTurn))
				return turnActivity;

			if (attackStatus.HasFlag(AttackStatus.NeedsToMove))
				return moveActivity;

			return NextActivity;
		}

		AttackStatus TickAttack(Actor self)
		{
			if (IsCanceled || trait.ChargeTick != 0)
				return AttackStatus.UnableToAttack;

			// Update ranges
			var minRange = winfo.MinRange;
			var maxRange = winfo.Range;

			var pos = self.CenterPosition;
			var mobile = move as Mobile;
			if (!Target.IsInRange(pos, maxRange)
				|| (minRange.Length != 0 && Target.IsInRange(pos, minRange))
				|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
			{
				// Try to move within range, drop the target otherwise
				if (move == null)
					return AttackStatus.UnableToAttack;

				attackStatus |= AttackStatus.NeedsToMove;
				moveActivity = ActivityUtils.SequenceActivities(move.MoveWithinRange(Target, minRange, maxRange), this);
				return AttackStatus.NeedsToMove;
			}

			var targetedPosition = target.CenterPosition;
			var desiredFacing = (targetedPosition - pos).Yaw.Facing;
			if (!OpenRA.Mods.Common.Util.FacingWithinTolerance(facing.Facing, desiredFacing, facingTolerance))
			{
				attackStatus |= AttackStatus.NeedsToTurn;
				turnActivity = ActivityUtils.SequenceActivities(new Turn(self, desiredFacing), this);
				return AttackStatus.NeedsToTurn;
			}

			attackStatus |= AttackStatus.Attacking;

			if (trait.ChannelTick == 0)
				winfo.Impact(target, this.self, new int[1] { 100 });

			if (trait.Info.IsChanneled && trait.CanChannelAnotherTime())
			{
				trait.StartChannelingDelay();
				return AttackStatus.Attacking;
			}

			trait.StartCooldown();
			return AttackStatus.UnableToAttack;
		}
	}
}
