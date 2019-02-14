using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;
using KSP.Localization;

namespace SmartTank {

	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// <summary>
	/// Plugin to add a application launcher button for our settings.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class AppLauncher : MonoBehavior {

		private const ApplicationLauncher.AppScenes VisibleInScenes =
			ApplicationLauncher.AppScenes.VAB |
			ApplicationLauncher.AppScenes.SPH;
		private static readonly string    AppIconPath  = $"{SmartTank.Name}/{SmartTank.Name}";
		private static readonly Texture2D AppIcon      = GameDatabase.Instance.GetTexture(AppIconPath, false);
		private static readonly string    tooltipTitle = Localizer.Format("smartTank_SettingsTooltipTitle", SmartTank.Name);
		private static readonly string    tooltipText  = "smartTank_SettingsTooltipText";

		private ApplicationLauncherButton launcher;

		/// <summary>
		/// Called when entering VAB or SPH.
		/// Enqueue our events for adding and removing or button.
		/// </summary>
		public void Start()
		{
			GameEvents.onGUIApplicationLauncherReady.Add(AddLauncher);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncher);
		}

		/// <summary>
		/// Called when exiting VAB or SPH.
		/// Remove our event handlers and our button.
		/// </summary>
		public void OnDisable()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);
			RemoveLauncher();
		}

		/// <summary>
		/// Return the location of the button
		/// </summary>
		public Vector3 GetAnchor()
		{
			return launcher?.GetAnchor() ?? Vector3.right;
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
