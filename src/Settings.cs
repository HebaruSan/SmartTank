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

		public void Save()
		{
			ConfigNode.CreateConfigFromObject(this, new ConfigNode(GetType().Name)).Save(path);
		}

		private const           string   settingsSuffix   = "settings";
		private static readonly string   path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{SmartTank.Name}.{settingsSuffix}";
		public  static readonly Settings Instance         = new Settings();

		private const           string   fuelResourceName = "LiquidFuel";

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
		[Persistent] public bool   DiameterMatching           = true;
		[Persistent] public bool   FuelMatching               = true;
		[Persistent] public bool   AutoScale                  = true;
		[Persistent] public bool   Atmospheric                = true;
		[Persistent] public bool   HideNonProceduralParts     = true;
		[Persistent] public string BodyForTWR                 = Planetarium?.fetch?.Home?.name ?? "Kerbin";
		[Persistent] public string DefaultTexture             = "Original";
		[Persistent] public float  TargetTWR                  = 1.5f;
	}

}
