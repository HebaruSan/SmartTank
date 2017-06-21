using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartTank {

	class DialogGUIChooseOption : DialogGUIBox {

		public DialogGUIChooseOption(string[] Choices, Func<string> GetChoice, Callback<string> SetChoice, float width = 100f, float height = 40f)
			: base("", width, height, null)
		{
			choices   = Choices;
			getChoice = GetChoice;
			setChoice = SetChoice;

			AddChild(new DialogGUIHorizontalLayout(
				width, height, 1, new RectOffset(outerBorder, outerBorder, outerBorder, outerBorder), TextAnchor.MiddleCenter,

				new DialogGUIButton(
					"<<",
					PreviousSelection,
					buttonWidth, height - 2 * outerBorder,
					false
				),
				new DialogGUIVerticalLayout(
					-1, height - 5, 0, noPadding, TextAnchor.MiddleCenter,

					new DialogGUIContentSizer(
						ContentSizeFitter.FitMode.Unconstrained,
						ContentSizeFitter.FitMode.PreferredSize,
						true
					),
					new DialogGUILabel(getChoice),
					new DialogGUISlider(
						GetSelection,
						0, Choices.Length - 1,
						true,
						-1, 0.4f * height,
						SetSelection
					)
				),
				new DialogGUIButton(
					">>",
					NextSelection,
					buttonWidth, height - 2 * outerBorder,
					false
				)
			));
		}

		private const           int        outerBorder = 1;
		private const           int        buttonWidth = 20;
		private static readonly RectOffset noPadding   = new RectOffset(0, 0, 0, 0);

		private string[]          choices;
		private Func<string>      getChoice;
		private Callback<string>  setChoice;

		private float GetSelection()
		{
			string active = getChoice();
			for (int i = 0; i < choices.Length; ++i) {
				if (choices[i] == active) {
					return i;
				}
			}
			return 0;
		}

		private void SetSelection(float val)
		{
			setChoice(choices[(int)Math.Floor(val)]);
		}

		private void PreviousSelection()
		{
			if (choices.Length > 0) {
				SetSelection((GetSelection() + choices.Length - 1) % choices.Length);
			}
		}

		private void NextSelection()
		{
			if (choices.Length > 0) {
				SetSelection((GetSelection() + 1) % choices.Length);
			}
		}

	}

}
