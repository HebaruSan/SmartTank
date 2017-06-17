using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using KSP.Localization;

namespace SmartTank {

	public class SettingsView : DialogGUIVerticalLayout {

		public SettingsView(UnityAction close)
			: base(
				windowWidth, -1, 2,
				winPadding,
				TextAnchor.UpperLeft
			)
		{
			AddChild(new DialogGUIHorizontalLayout(
				windowWidth, -1,
				new DialogGUIBox(
					"", boxWidth, boxHeight, null,
					new DialogGUIVerticalLayout(
						boxWidth, boxHeight, boxSpacing, boxPadding,
						TextAnchor.UpperLeft,

						new DialogGUILabel("smartTank_SettingsAutoBoxTitle"),
						new DialogGUIToggle(
							() => Settings.Instance.AutoScale,
							"smartTank_AutoScaleSettingPrompt",
							(bool b) => { Settings.Instance.AutoScale = b; }
						),
						new DialogGUIToggle(
							() => Settings.Instance.DiameterMatching,
							"smartTank_DiameterMatchingSettingPrompt",
							(bool b) => { Settings.Instance.DiameterMatching = b; }
						)
					)
				),
				new DialogGUIBox(
					"", boxWidth, boxHeight, null,
					new DialogGUIVerticalLayout(
						boxWidth, boxHeight, boxSpacing, boxPadding,
						TextAnchor.UpperLeft,

						new DialogGUILabel("smartTank_SettingsTWRBoxTitle"),
						new DialogGUIVerticalLayout(
							10, 1.8f * textFieldHeight, 2, new RectOffset(0, 0, 0, 0),
							TextAnchor.MiddleCenter,
							new DialogGUISlider(
								() => (float)Math.Log10(Settings.Instance.TargetTWR),
								-1f, 1f,
								false,
								-1, -1,
								(float f) => { Settings.Instance.TargetTWR = (float)Math.Pow(10f, f); }
							),
							new DialogGUILabel(() => $"{Settings.Instance.TargetTWR:G3}")
						),
						new DialogGUIHorizontalLayout(
							TextAnchor.MiddleLeft,
							new DialogGUILabel("smartTank_TWRAtSettingPrompt"),
							new DialogGUITextInput(
								Settings.Instance.BodyForTWR,
								false,
								15,
								(string s) => {
									Settings.Instance.BodyForTWR = s;
									return s;
								},
								textFieldHeight
							),
							new DialogGUIToggle(
								() => Settings.Instance.Atmospheric,
								"smartTank_AtmosphericSettingPrompt",
								(bool b) => { Settings.Instance.Atmospheric = b; }
							)
						)
					)
				)
			));
			AddChild(new DialogGUIHorizontalLayout(
				TextAnchor.MiddleLeft,

				new DialogGUIToggle(
					() => Settings.Instance.FuelMatching,
					"smartTank_FuelMatchingPrompt",
					(bool b) => { Settings.Instance.FuelMatching = b; }
				),
				new DialogGUIToggle(
					() => Settings.Instance.HideNonProceduralFuelTanks,
					"smartTank_HideNonProceduralFuelTanksPrompt",
					(bool b) => {
						Settings.Instance.HideNonProceduralFuelTanks = b;
						Settings.Instance.HideNonProceduralFuelTanksChanged();
					}
				)
			));
			AddChild(new DialogGUIHorizontalLayout(
				TextAnchor.MiddleLeft,

				new DialogGUILabel("smartTank_DefaultTexturePrompt", leftColWidth),
				new DialogGUITextInput(
					Settings.Instance.DefaultTexture,
					false,
					15,
					(string s) => {
						Settings.Instance.DefaultTexture = s;
						return s;
					},
					textFieldHeight
				)
			));

			AddChild(new DialogGUISpace(padding));
			AddChild(new DialogGUIHorizontalLayout(
				new DialogGUIFlexibleSpace(),
				new DialogGUIButton(
					"smartTank_CloseButtonText",
					() => { close(); },
					200f, -1f,
					false
				),
				new DialogGUIFlexibleSpace()
			));
		}

		private const  float      leftColWidth    = 90f;
		private const  float      rightColWidth   = 1.5f * leftColWidth;
		private const  float      textFieldHeight = 25f;
		private const  float      boxWidth        = leftColWidth + rightColWidth;
		private const  float      boxHeight       = 4.5f * textFieldHeight;
		private const  float      boxSpacing      = 2;
		private const  int        padding         = 10;
		private static RectOffset boxPadding      = new RectOffset(padding, padding, padding, padding);
		private static RectOffset winPadding      = new RectOffset(2, 2, 2, 2);
		private const  float      windowWidth     = 2 * boxWidth + 2 * padding;

		private PopupDialog dialog;

		public PopupDialog Show()
		{
			dialog = PopupDialog.SpawnPopupDialog(
				new MultiOptionDialog(
					SmartTank.Name,
					"smartTank_SettingsSubtitle",
					Localizer.Format("smartTank_SettingsTitle", SmartTank.Name),
					UISkinManager.defaultSkin,
					windowWidth,
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
