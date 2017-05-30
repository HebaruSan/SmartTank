using System;
using UnityEngine;
using KSP;
using System.Collections.Generic;
using KSP.UI.Screens;
using KSP.Localization;
using KerbalEngineer.VesselSimulator;

namespace SmartTank {

	// We speak American in this house, young lady!
	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// Our main plugin behavior.
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class SmartTank : MonoBehavior {

		public SmartTank() : base() { }

		/// <summary>
		/// Machine-readable name for this mod.
		/// Use this for directory/file names, etc.
		/// </summary>
		public const string Name = "SmartTank";

		private const double configuredTWR = 1.5f;

		/// <summary>
		/// This is called at creation
		/// </summary>
		public void Start()
		{
			SimManager.OnReady += OnSimUpdate;
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		public void OnDisable()
		{
			SimManager.OnReady -= OnSimUpdate;
		}

		/// <summary>
		/// Fires when KER tells us the simulator is updated
		/// </summary>
		public void OnSimUpdate()
		{
			for (int st = 0; st < SimManager.Stages.Length; ++st) {
				Stage stage = SimManager.Stages[st];
				Debug.Log(string.Format(
					"Stage {0}: resourceMass = {1}, optimal fuel mass = {2}",
					st,
					stage.resourceMass,
					optimalFuelMass(stage.thrust, configuredTWR, stage.totalMass - stage.resourceMass)
				));
			}
		}

		private static double gravAccel = FlightGlobals?.GetHomeBody()?.GeeASL
			?? KerbalEngineer.Helpers.Units.GRAVITY;

		private double optimalFuelMass(double thrust, double desiredTWR, double dryMass)
		{
			return Math.Max(
				0,
				thrust / desiredTWR / gravAccel - dryMass
			);
		}

	}

}
