using UnityEngine;
using KSP.UI.TooltipTypes;

namespace SmartTank {

	public static class TooltipExtensions {

		private static Tooltip_TitleAndText titleAndTextTooltipPrefab = AssetBase.GetPrefab<Tooltip_TitleAndText>("Tooltip_TitleAndText");

		public static void SetTooltip(this GameObject gameObj, string title, string text)
		{
			if (gameObj != null) {
				TooltipController_TitleAndText tt = (gameObj?.GetComponent<TooltipController_TitleAndText>() ?? gameObj?.AddComponent<TooltipController_TitleAndText>());
				if (tt != null) {
					tt.prefab      = titleAndTextTooltipPrefab;
					tt.titleString = title;
					tt.textString  = text;
				}
			}
		}

		private static Tooltip_Text textTooltipPrefab = AssetBase.GetPrefab<Tooltip_Text>("Tooltip_Text");

		public static bool SetTooltip(this GameObject gameObj, string tooltip)
		{
			if (gameObj != null) {
				TooltipController_Text tt = (gameObj.GetComponent<TooltipController_Text>() ?? gameObj.AddComponent<TooltipController_Text>());
				if (tt != null) {
					tt.textString = tooltip;
					tt.prefab     = textTooltipPrefab;
					return true;
				}
			}
			return false;
		}

		public static DialogGUIBase DeferTooltip(DialogGUIBase gb)
		{
			if (gb.tooltipText != "") {
				gb.OnUpdate = () => {
					if (gb.uiItem != null
							&& gb.uiItem.SetTooltip(gb.tooltipText)) {
						gb.OnUpdate = () => {};
					}
				};
			}
			return gb;
		}

	}

}
