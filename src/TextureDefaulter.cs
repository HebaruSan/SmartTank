using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralParts;

namespace SmartTank {

	/// <summary>
	/// Part module to:
	///   - Set default texture
	/// </summary>
	public class TextureDefaulter : PartModule {

		public TextureDefaulter() : base() { }

		/// <summary>
		/// Called when part is initially created, including during game load
		/// </summary>
		public override void OnAwake()
		{
			base.OnAwake();

			// Set the texture for the preview part in the parts list and newly placed parts
			SetTexture();
		}

		/// <summary>
		/// Called when the part is instantiated for use
		/// </summary>
		/// <param name="state">Description of when the part is being created</param>
		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// We don't need Updates for texture defaulting
			isEnabled = enabled = false;
		}

		private void SetTexture()
		{
			if (part != null && part.HasModule<ProceduralPart>()) {
				ProceduralPart pp = part.GetModule<ProceduralPart>();
				if (pp != null) {
					pp.textureSet = Settings.Instance.DefaultTexture;
				}
			}
		}

	}

}
