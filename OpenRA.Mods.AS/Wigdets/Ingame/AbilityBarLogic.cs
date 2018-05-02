using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.AS.Traits;

namespace OpenRA.Mods.AS.Wigdets.Ingame
{
	class AbilityBarLogic : ChromeLogic
	{
		World world;
		Actor[] selectedActors = { };

		int[] abilityHighlighted = new int[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		bool[] abilityDisabled = new bool[9] { false, false, false, false, false, false, false, false, false };

		public AbilityBarLogic(World world, Widget widget) //, Dictionary<string, MiniYaml> logicArgs
		{
			this.world = world;

			/*var highlightOnButtonPress = false;
			if (logicArgs.ContainsKey("HighlightOnButtonPress"))
				highlightOnButtonPress = FieldLoader.GetValue<bool>("HighlightOnButtonPress", logicArgs["HighlightOnButtonPress"].Value);*/

			var ability0 = widget.GetOrNull<ButtonWidget>("ABILITY_0");
			if (ability0 != null)
			{
				BindButtonIcon(ability0);

				ability0.IsDisabled = () => { UpdateState(); return abilityDisabled[0]; };
				ability0.IsHighlighted = () => world.OrderGenerator is AbilityOrderGenerator;

				Action<bool> toggle = allowCancel =>
				{
					if (ability0.IsHighlighted())
					{
						if (allowCancel)
							world.CancelInputMode();
					}
					else
						world.OrderGenerator = new AbilityOrderGenerator(selectedActors, "Ability0");
				};

				ability0.OnClick = () => toggle(true);
				ability0.OnKeyPress = _ => toggle(false);
			}
		}

		void UpdateState()
		{
			selectedActors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead)
				.ToArray();
		}

		public override void Tick()
		{
			for (int i = 0; i < 9; i++)
			{
				if (abilityHighlighted[i] > 0)
					abilityHighlighted[i]--;
			}

			base.Tick();
		}

		void BindButtonIcon(ButtonWidget button)
		{
			var icon = button.Get<ImageWidget>("ICON");
			var hasDisabled = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-disabled") != null;
			var hasActive = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-active") != null;
			var hasActiveHover = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-active-hover") != null;
			var hasHover = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-hover") != null;

			icon.GetImageName = () => hasActive && button.IsHighlighted() ?
						(hasActiveHover && Ui.MouseOverWidget == button ? icon.ImageName + "-active-hover" : icon.ImageName + "-active") :
					hasDisabled && button.IsDisabled() ? icon.ImageName + "-disabled" :
					hasHover && Ui.MouseOverWidget == button ? icon.ImageName + "-hover" : icon.ImageName;
		}
	}
}
