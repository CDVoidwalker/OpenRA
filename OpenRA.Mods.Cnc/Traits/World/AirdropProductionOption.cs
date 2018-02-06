#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the 'Airdrop Production Enabled' checkbox in the lobby options.")]
	public class AirdropProductionOptionInfo : ITraitInfo, ILobbyOptions
	{
		[Translate]
		[Desc("Descriptive label for the airdrop checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Airdrop Production";

		[Translate]
		[Desc("Tooltip description for the airdrop checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Enables aircraft delivery of vehicles on airstrip";

		[Desc("Default value of the airdrop checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the airdrop state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the airdrop checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the airdrop checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 7;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("disableairdropproduction", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public object Create(ActorInitializer init) { return new AirdropProductionOption(this); }
	}

	public class AirdropProductionOption : INotifyCreated
	{
		readonly AirdropProductionOptionInfo info;
		public bool Enabled { get; private set; }

		public AirdropProductionOption(AirdropProductionOptionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("disableairdropproduction", info.CheckboxEnabled);
		}
	}
}
