using System;
using UnityEngine;
using UnityEngine.Events;
using KSP.Localization;

namespace SmartTank {

	public class SettingsView : DialogGUIVerticalLayout {

		public SettingsView(UnityAction close)
		{
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.DiameterMatching,
				"smartTank_DiameterMatchingPrompt",
				(bool b) => { Settings.Instance.DiameterMatching = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.FuelMatching,
				"smartTank_FuelMatchingPrompt",
				(bool b) => { Settings.Instance.FuelMatching = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.AutoScale,
				"smartTank_AutoScalePrompt",
				(bool b) => { Settings.Instance.AutoScale = b; }
			));
			AddChild(new DialogGUIToggle(
				() => Settings.Instance.Atmospheric,
				"smartTank_AtmosphericPrompt",
				(bool b) => { Settings.Instance.Atmospheric = b; }
			));
			AddChild(new DialogGUIHorizontalLayout(
				new DialogGUILabel("smartTank_TargetTWRPrompt"),
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
				new DialogGUILabel("smartTank_TWRAtPrompt"),
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
					"smartTank_CloseButtonText",
					() => {
						Settings.Instance.Save();
						close();
					},
					200f, -1f,
					false
				)
			));
		}

		private PopupDialog dialog;

		public PopupDialog Show()
		{
			dialog = PopupDialog.SpawnPopupDialog(
				new MultiOptionDialog(
					SmartTank.Name,
					"smartTank_settingsSubtitle",
					Localizer.Format("smartTank_SettingsTitle", SmartTank.Name),
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
				Settings.Instance.Save();
				dialog.Dismiss();
				dialog = null;
			}
		}

	}

}
