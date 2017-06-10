#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides docking procedures for helicopters")]
	class HelipadInfo : ITraitInfo, Requires<DockManagerInfo>
	{
		public object Create(ActorInitializer init)
		{
			return new Helipad(init);
		}
	}

	public class Helipad : IAcceptDock
	{
		readonly Actor host;
		readonly DockManager dockManager;
		readonly RallyPoint rallyPoint;

		public Helipad(ActorInitializer init)
		{
			host = init.Self;
			dockManager = host.Trait<DockManager>();
			rallyPoint = host.TraitOrDefault<RallyPoint>();
		}

		void IAcceptDock.OnUndock(Actor client, Dock dock, Activity parameters)
		{
			client.SetTargetLine(Target.FromCell(client.World, rallyPoint.Location), Color.Green, false);

			// ResupplyAircraft handles this.
			// Take off and move to RP.
			if (rallyPoint != null)
				client.QueueActivity(client.Trait<IMove>().MoveTo(rallyPoint.Location, 2));
		}

		void IAcceptDock.QueueDockActivity(Actor client, Dock dock, Activity parameters)
		{
			client.SetTargetLine(Target.FromCell(client.World, dock.Location), Color.Green, false);

			// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
			client.QueueActivity(new ResupplyAircraft(client));

			// I know this depreciates AbortOnResupply activity but it is a bug to reuse NextActivity!
			// client.Info.TraitInfo<AircraftInfo>().AbortOnResupply ? null : client.CurrentActivity.NextActivity));
		}

		Activity IAcceptDock.ApproachDockActivity(Actor client, Dock dock, Activity parameters)
		{
			return ActivityUtils.SequenceActivities(
				new HeliFly(client, Target.FromPos(dock.CenterPosition)),
				new Turn(client, dock.Info.DockAngle),
				new HeliLand(client, false));
		}
	}
}
