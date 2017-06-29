using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using ProceduralParts;
using KSP.UI.Screens;

namespace SmartTank {

	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// <summary>
	/// App-wide settings, mainly defaults for part-specific settings.
	/// Patterned after BasicDeltaV_Settings by DMagic.
	/// </summary>
	public class Settings : MonoBehavior {

		private Settings()
		{
			if (File.Exists(path)) {
				ConfigNode.LoadObjectFromConfig(this, ConfigNode.Load(path));
			}
		}

		/// <summary>
		/// Saves the current values of the properties to the setings file
		/// </summary>
		public void Save()
		{
			ConfigNode.CreateConfigFromObject(this, new ConfigNode(GetType().Name)).Save(path);
		}

		private const  string   settingsSuffix   = "settings";
		private static string   path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{SmartTank.Name}.{settingsSuffix}";
		private const  string   fuelResourceName = "LiquidFuel";

		/// <summary>
		/// The singleton instance of this class.
		/// Should be the only way other code accesses it.
		/// Make sure this is after all other static members, so
		/// the constructor can use them!
		/// </summary>
		public  static Settings Instance         = new Settings();

		/// <summary>
		/// Call after changing HideNonProceduralParts.
		/// Shows/hides affected parts and refreshes the parts list.
		/// </summary>
		public void HideNonProceduralPartsChanged()
		{
			PartCategories fromCat, toCat;
			if (HideNonProceduralParts) {
				fromCat = PartCategories.FuelTank;
   				toCat   = PartCategories.none;
			} else {
				fromCat = PartCategories.none;
				toCat   = PartCategories.FuelTank;
			}
			List<AvailablePart> parts = PartLoader.LoadedPartsList;
			for (int p = 0; p < parts.Count; ++p) {
				Part pref = parts[p].partPrefab;
				if (!pref.HasModule<ProceduralPart>()) {
					// Fuel tanks, excluding engines and wings
					// Note, the Mk2 spaceplane tanks are Propulsion instead of FuelTank
					if (parts[p].category == fromCat) {
						for (int r = 0; r < parts[p].resourceInfos.Count; ++r) {
							if (parts[p].resourceInfos[r].resourceName == fuelResourceName) {
								parts[p].category = toCat;
								break;
							}
						}
					}
					// Decouplers, excluding head shields and pylons
					if (pref.HasModule<ModuleDecouple>()
							&& !pref.HasModule<ModuleJettison>()) {
						if (HideNonProceduralParts) {
							if (parts[p].category == PartCategories.Coupling) {
								parts[p].category = PartCategories.none;
							}
						} else {
							parts[p].category = PartCategories.Coupling;
						}
					}
				}
			}
			EditorPartList.Instance.Refresh();
		}

		// These are the actual settings.
		// Access them via Settings.Instance.PropertyName.

		/// <summary>
		/// If true, automatically set the top and bottom diameters to match attached parts.
		/// Includes switching between cylinder and cone if attached parts don't match.
		/// Otherwise leave as-is for manual manipulation.
		/// </summary>
		[Persistent] public bool   DiameterMatching           = true;

		/// <summary>
		/// If true, automatically set the fuel contents to match attached engines.
		/// E.g., LiquidFuel only for NERVA, or Lf+Ox for most others.
		/// Otherwise leave as-is for manual manipulation.
		/// </summary>
		[Persistent] public bool   FuelMatching               = true;

		/// <summary>
		/// If true, automatically set the length to fit TWR.
		/// Otherwise leave as-is for manual manipulation.
		/// </summary>
		[Persistent] public bool   AutoScale                  = true;

		/// <summary>
		/// If true, calculate the TWR based on the engine's thrust
		/// at sea-level at the designated body.
		/// Otherwise use vacuum thrust.
		/// </summary>
		[Persistent] public bool   Atmospheric                = true;

		/// <summary>
		/// Name of body to use for calculating TWR.
		/// </summary>
		[Persistent] public string BodyForTWR                 = Planetarium?.fetch?.Home?.name ?? "Kerbin";

		/// <summary>
		/// Default thrust-to-weight ratio to use when calculating desired
		/// size of tanks.
		/// </summary>
		[Persistent] public float  TargetTWR                  = 1.5f;

		/// <summary>
		/// If true, the
		/// Be sure to call HideNonProceduralPartsChanged after changing this.
		/// </summary>
		[Persistent] public bool   HideNonProceduralParts     = true;

		/// <summary>
		/// Name of default texture to use for newly placed procedural fuel tanks.
		/// Corresponds to ConfigNode data from a cfg file in this format:
		///
		/// STRETCHYTANKTEXTURES
		/// {
		///     NameOfTextureHere
		///     {
		///         sides
		///         {
		///             [Texture data here]
		///         }
		///     }
		/// }
		/// </summary>
		[Persistent] public string DefaultTexture             = "Original";
	}

}
