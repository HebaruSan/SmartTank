using UnityEngine;
using UnityEngine.Events;
using KSP.UI.Screens;

namespace SmartTank {

	using static TooltipExtensions;

	/// <summary>
	/// Small popup that indicates that auto-scaling is globally paused
	/// and allows it to be resumed.
	/// </summary>
	public class PausedView: DialogGUIVerticalLayout {

		/// <summary>
		/// Iniitalize a tiny popup containing a pause button
		/// </summary>
		/// <param name="onClick">Action to call when user clicks the button</param>
		public PausedView(UnityAction onClick)
			: base()
		{
			AddChild(DeferTooltip(new DialogGUIToggleButton(
				true, "||", b => onClick(),
				buttonWidth, buttonWidth
			) {
				tooltipText = "smartTank_PausedButtonTooltip"
			}));
		}

		/// <summary>
		/// Create a dialog and display it
		/// </summary>
		/// <param name="where">Vector describing location on screen for the popup</param>
		/// <returns>
		/// Reference to the dialog
		/// </returns>
		public PopupDialog Show(Vector3 where)
		{
			dialog = PopupDialog.SpawnPopupDialog(
				Vector2.right,
				Vector2.right,
				new MultiOptionDialog(
					$"{SmartTank.Name} Paused", "", "",
					UISkinManager.defaultSkin,
					new Rect(
						where.x / Screen.width  + 0.5f,
						where.y / Screen.height + 0.5f,
						dialogWidth, dialogWidth
					),
					this
				),
				false,
				UISkinManager.defaultSkin,
				false
			);
			return dialog;
		}

		/// <summary>
		/// Close the dialog
		/// </summary>
		public void Dismiss()
		{
			if (dialog != null) {
				dialog.Dismiss();
				dialog = null;
			}
		}

		private const int buttonWidth = 30;
		private const int pad         = 5;
		private const int dialogWidth = buttonWidth + 2 * pad;

		private PopupDialog dialog;
	}

}
