using System;
using UnityEngine;

namespace SmartTank {

	public class SettingsView : DialogGUIVerticalLayout {

		public SettingsView()
		{
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.DiameterMatching,
				"Diameter matching",
				(bool b) => { Settings.Instance.DiameterMatching = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.FuelMatching,
				"Fuel matching",
				(bool b) => { Settings.Instance.FuelMatching = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.AutoScale,
				"Auto-scale",
				(bool b) => { Settings.Instance.AutoScale = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.Atmospheric,
				"Atmospheric",
				(bool b) => { Settings.Instance.Atmospheric = b; }
			));
			AddChild(new DialogGUIHorizontalLayout(
				new DialogGUILabel("Target TWR:"),
				new DialogGUIVerticalLayout(
					10, 10, 2, new RectOffset(0, 0, 0, 0),
					TextAnchor.MiddleCenter,
					new DialogGUISlider(
						() => (float)Math.Log10(Settings.Instance.TargetTWR),
						-1f, 1f,
						false,
						-1, -1,
						(float f) => { Settings.Instance.TargetTWR = (float)Math.Pow(10f, f); }
					),
					new DialogGUILabel(() => $"{Settings.Instance.TargetTWR:G2}")
				)
			));
			AddChild(new DialogGUIHorizontalLayout(
				new DialogGUILabel("Body for TWR:"),
				new DialogGUITextInput(
					Settings.Instance.BodyForTWR,
					false,
					15,
					(string s) => {
						Settings.Instance.BodyForTWR = s;
						return s;
					},
					40f
				)
			));
			AddChild(new DialogGUIHorizontalLayout(
				TextAnchor.MiddleCenter,
				new DialogGUIButton(
					"Close",
					() => { },
					200f, -1f,
					true
				)
			));
		}

		private PopupDialog dialog;

		public PopupDialog Show()
		{
			dialog = PopupDialog.SpawnPopupDialog(
				new MultiOptionDialog(
					SmartTank.Name,
					"",
					$"{SmartTank.Name} Settings",
					UISkinManager.defaultSkin,
					this
				),
				false,
				UISkinManager.defaultSkin,
				true
			);
			return dialog;
		}

		public void Dismiss()
		{
			if (dialog != null) {
				dialog.Dismiss();
				dialog = null;
			}
		}

	}

}
