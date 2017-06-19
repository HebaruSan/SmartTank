using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using KSP.Localization;
using KSP.UI.TooltipTypes;

namespace SmartTank {

	using static TooltipExtensions;

	public class SettingsView : DialogGUIVerticalLayout {

		public SettingsView(UnityAction close)
			: base(
				windowWidth, -1, 2,
				winPadding,
				TextAnchor.UpperLeft
			)
		{
			getPlanetList();
			getTextureList();

			AddChild(new DialogGUIHorizontalLayout(
				windowWidth, -1,

				new DialogGUIBox(
					"", boxWidth, boxHeight, null,

					new DialogGUIVerticalLayout(
						boxWidth, boxHeight, boxSpacing, boxPadding,
						TextAnchor.UpperLeft,

						new DialogGUILabel("smartTank_SettingsAutoBoxTitle"),
						DeferTooltip(new DialogGUIToggle(
							() => Settings.Instance.AutoScale,
							"smartTank_AutoScaleSettingPrompt",
							(bool b) => { Settings.Instance.AutoScale = b; }
						) {
							tooltipText = "smartTank_AutoScaleSettingTooltip"
						}),
						DeferTooltip(new DialogGUIToggle(
							() => Settings.Instance.DiameterMatching,
							"smartTank_DiameterMatchingSettingPrompt",
							(bool b) => { Settings.Instance.DiameterMatching = b; }
						) {
							tooltipText = "smartTank_DiameterMatchingSettingTooltip"
						})
					)
				),
				new DialogGUIBox(
					"", boxWidth, boxHeight, null,

					new DialogGUIVerticalLayout(
						boxWidth, boxHeight, boxSpacing, boxPadding,
						TextAnchor.UpperLeft,

						new DialogGUILabel("smartTank_SettingsTWRBoxTitle"),
						new DialogGUIVerticalLayout(
							padding, 1.8f * textFieldHeight, 0, noPadding,
							TextAnchor.MiddleCenter,

							DeferTooltip(new DialogGUISlider(
								() => (float)Math.Log10(Settings.Instance.TargetTWR),
								-1f, 1f,
								false,
								-1, -1,
								(float f) => { Settings.Instance.TargetTWR = (float)Math.Pow(10f, f); }
							) {
								tooltipText = "smartTank_SettingsTWRBoxTooltip"
							}),
							new DialogGUILabel(() => $"{Settings.Instance.TargetTWR:G3}")
						),
						new DialogGUIHorizontalLayout(
							TextAnchor.MiddleLeft,

							new DialogGUILabel("smartTank_TWRAtSettingPrompt"),
							DeferTooltip(new DialogGUIChooseOption(
								planetList,
								() => Settings.Instance.BodyForTWR,
								(string s) => { Settings.Instance.BodyForTWR = s; }
							) {
								tooltipText = "smartTank_TWRAtSettingTooltip"
							}),
							DeferTooltip(new DialogGUIToggle(
								() => Settings.Instance.Atmospheric,
								"smartTank_AtmosphericSettingPrompt",
								(bool b) => { Settings.Instance.Atmospheric = b; }
							) {
								tooltipText = "smartTank_AtmosphericSettingTooltip"
							})
						)
					)
				)
			));
			AddChild(new DialogGUIHorizontalLayout(
				true, true, boxSpacing, boxPadding,
				TextAnchor.MiddleLeft,

				DeferTooltip(new DialogGUIToggle(
					() => Settings.Instance.FuelMatching,
					"smartTank_FuelMatchingPrompt",
					(bool b) => { Settings.Instance.FuelMatching = b; }
				) {
					tooltipText = "smartTank_FuelMatchingTooltip"
				}),
				DeferTooltip(new DialogGUIToggle(
					() => Settings.Instance.HideNonProceduralFuelTanks,
					"smartTank_HideNonProceduralFuelTanksPrompt",
					(bool b) => {
						Settings.Instance.HideNonProceduralFuelTanks = b;
						Settings.Instance.HideNonProceduralFuelTanksChanged();
					}
				) {
					tooltipText = "smartTank_HideNonProceduralFuelTanksTooltip"
				})
			));
			visibleTexture = new DialogGUIImage(
				new Vector2(textureWidth, textureHeight), new Vector2(0, 0), Color.white,
				nameToTexture(Settings.Instance.DefaultTexture)
			);
			AddChild(new DialogGUIBox(
				"", windowWidth - 2 * padding, textureHeight + 2 * padding, null,

				new DialogGUIHorizontalLayout(
					true, false, 4, boxPadding,
					TextAnchor.MiddleLeft,

					new DialogGUIVerticalLayout(
						true, false, padding, noPadding,
						TextAnchor.UpperLeft,

						new DialogGUILabel("smartTank_DefaultTexturePrompt", leftColWidth),
						DeferTooltip(new DialogGUIChooseOption(
							textureList,
							() => Settings.Instance.DefaultTexture,
							(string s) => {
								Settings.Instance.DefaultTexture = s;
								visibleTexture.uiItem.GetComponent<RawImage>().texture = nameToTexture(s);
							}
						) {
							tooltipText = "smartTank_DefaultTextureTooltip"
						}),
						new DialogGUIFlexibleSpace()
					),
					visibleTexture
				)
			));

			AddChild(new DialogGUISpace(padding));
			AddChild(new DialogGUIHorizontalLayout(
				new DialogGUIFlexibleSpace(),
				new DialogGUIButton(
					"smartTank_CloseButtonText",
					() => { close(); },
					200, -1,
					false
				),
				new DialogGUIFlexibleSpace()
			));
		}

		private const  float      leftColWidth    = 90;
		private const  float      rightColWidth   = 1.5f * leftColWidth;
		private const  float      textFieldHeight = 25;
		private const  float      boxWidth        = leftColWidth + rightColWidth;
		private const  float      boxHeight       = 5 * textFieldHeight;
		private const  float      boxSpacing      = 2;
		private const  int        padding         = 10;
		private static RectOffset boxPadding      = new RectOffset(padding, padding, padding, padding);
		private static RectOffset winPadding      = new RectOffset(2, 2, 2, 2);
		private static RectOffset noPadding       = new RectOffset(0, 0, 0, 0);
		private const  float      windowWidth     = 2 * boxWidth + 2 * padding;
		private const  float      textureWidth    = 200;
		private const  float      textureHeight   = textureWidth;

		private static string[]   planetList      = null;
		private static string[]   textureList     = null;

		private DialogGUIImage visibleTexture;

		private void getPlanetList()
		{
			if (planetList == null) {
				List<string> options = new List<string>();
				for (int i = 0; i < FlightGlobals.Bodies.Count; ++i) {
					CelestialBody b = FlightGlobals.Bodies[i];
					if (b.hasSolidSurface) {
						options.Add(b.name);
					}
				}
				planetList = options.ToArray();
			}
		}

		private void getTextureList()
		{
			if (textureList == null) {
				List<string> options = new List<string>();
				ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("STRETCHYTANKTEXTURES");
				for (int n = 0; n < nodes.Length; ++n) {
					ConfigNode textureInfo = nodes[n];
					for (int t = 0; t < textureInfo.nodes.Count; ++t) {
						options.Add(textureInfo.nodes[t].name);
					}
				}
				options.Sort();
				textureList = options.Distinct().ToArray();
			}
		}

		private static Texture nameToTexture(string name)
		{
			ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("STRETCHYTANKTEXTURES");
			for (int n = 0; n < nodes.Length; ++n) {
				ConfigNode textureInfo = nodes[n];
				for (int t = 0; t < textureInfo.nodes.Count; ++t) {
					if (name == textureInfo.nodes[t].name) {
						return GameDatabase.Instance.GetTexture(textureInfo.nodes[t].GetNode("sides").GetValue("texture"), false);
					}
				}
			}
			return null;
		}

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
