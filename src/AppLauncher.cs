using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;
using KSP.Localization;

namespace SmartTank {

	using MonoBehavior = UnityEngine.MonoBehaviour;

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class AppLauncher : MonoBehavior {

		private const ApplicationLauncher.AppScenes VisibleInScenes =
			ApplicationLauncher.AppScenes.VAB |
			ApplicationLauncher.AppScenes.SPH;
		private static string    AppIconPath  = $"{SmartTank.Name}/{SmartTank.Name}";
		private static Texture2D AppIcon      = GameDatabase.Instance.GetTexture(AppIconPath, false);
		private static string    tooltipTitle = Localizer.Format("smartTank_SettingsTitle", SmartTank.Name);
		private static string    tooltipText  = "smartTank_SettingsTooltip";

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
				launcher?.gameObject?.SetTooltip(tooltipTitle, tooltipText);
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
				view = new SettingsView(() => { launcher.SetFalse(true); });
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
