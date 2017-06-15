using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace SmartTank {

	using MonoBehavior = UnityEngine.MonoBehaviour;

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class AppLauncher : MonoBehavior {

		private const ApplicationLauncher.AppScenes VisibleInScenes =
			ApplicationLauncher.AppScenes.VAB |
			ApplicationLauncher.AppScenes.SPH;
		private static string    AppIconPath = $"{SmartTank.Name}/{SmartTank.Name}";
		private static Texture2D AppIcon     = GameDatabase.Instance.GetTexture(AppIconPath, false);
		private ApplicationLauncherButton launcher;

		public void Start()
		{
			GameEvents.onGUIApplicationLauncherReady.Add(AddLauncher);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncher);
		}

		public void OnDisable()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);
			RemoveLauncher();
		}

		private static string tooltipTitle = $"{SmartTank.Name} Settings";
		private static string tooltipText  = $"Configure defaults for procedural tank auto-scaling";

		private void AddLauncher()
		{
			if (ApplicationLauncher.Ready && launcher == null)
			{
				launcher = ApplicationLauncher.Instance.AddModApplication(
					OnToggleOn,      OnToggleOff,
					null,            null,
					null,            null,
					VisibleInScenes, AppIcon
				);
				SetTooltip(launcher?.gameObject, tooltipTitle, tooltipText);
			}
		}

		private static Tooltip_TitleAndText tooltipPrefab = AssetBase.GetPrefab<Tooltip_TitleAndText>("Tooltip_TitleAndText");

		private static void SetTooltip(GameObject gameObj, string title, string text)
		{
			TooltipController_TitleAndText tt = (gameObj?.GetComponent<TooltipController_TitleAndText>() ?? gameObj?.AddComponent<TooltipController_TitleAndText>());
			if (tt != null) {
				tt.prefab      = tooltipPrefab;
				tt.titleString = title;
				tt.textString  = text;
			}
		}

		private void RemoveLauncher()
		{
			if (launcher != null) {
				ApplicationLauncher.Instance.RemoveModApplication(launcher);
				launcher = null;
			}
		}

		private SettingsView view;

		private void OnToggleOn()
		{
			if (view == null) {
				view = new SettingsView();
			}
			view.Show();
		}

		private void OnToggleOff()
		{
			if (view != null) {
				view.Dismiss();
				view = null;
			}
		}
	}

}
