using System;
using System.IO;
using System.Reflection;

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

		private const  string settingsSuffix = "settings";
		private static string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{SmartTank.Name}.{settingsSuffix}";
		public static Settings Instance { get; private set; } = new Settings();

		// These are the actual settings.
		// Access them via Settings.Instance.PropertyName.
		[Persistent] public bool   DiameterMatching = true;
		[Persistent] public bool   FuelMatching     = true;
		[Persistent] public bool   AutoScale        = true;
		[Persistent] public bool   Atmospheric      = true;
		[Persistent] public string BodyForTWR       = Planetarium?.fetch?.Home?.name ?? "Kerbin";
		[Persistent] public float  TargetTWR        = 1.5f;
	}

}
