using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using KSP.Localization;
using ProceduralParts;

namespace SmartTank {

	using static ShipIntegrity;

	// Convert from en-UK to en-US
	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// Our main plugin behavior.
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class SmartTank : MonoBehavior {

		/// <summary>
		/// Iniitalize a smart tank behavior
		/// </summary>
		public SmartTank() : base() { }

		/// <summary>
		/// Machine-readable name for this mod.
		/// Use this for directory/file names, etc.
		/// </summary>
		public const string Name = "SmartTank";

		private static readonly bool ProceduralPartsInstalled = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "ProceduralParts");

		/// <summary>
		/// This is called at creation
		/// </summary>
		public void Start()
		{
			if (ProceduralPartsInstalled) {
				Settings.Instance.HideNonProceduralPartsChanged();
				// SmartTank requires the "All Part Upgrades Applied In Sandbox" setting
				HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().PartUpgradesInSandbox = true;
				GameEvents.onDeltaVCalcsCompleted.Add(OnDeltaVCalcsCompleted);
			}
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		public void OnDisable()
		{
			if (ProceduralPartsInstalled) {
				GameEvents.onDeltaVCalcsCompleted.Remove(OnDeltaVCalcsCompleted);
			}
		}

		private void OnDeltaVCalcsCompleted()
		{
			OnSimUpdate(EditorLogic.fetch?.ship?.vesselDeltaV);
		}

		/// <summary>
		/// Return the total wet mass of a part
		/// The mass property is just the dry mass, so
		/// to get the wet mass, we need to add the resource mass.
		/// </summary>
		/// <param name="p">Part to examine</param>
		/// <returns>
		/// Total wet mass of the part
		/// </returns>
		private double partTotalMass(Part p)
		{
			return p.mass + p.GetResourceMass();
		}

		private static bool         paused       = false;
		private static VesselDeltaV pausedDeltaV = null;
		private static PausedView   pausedDialog = null;

		/// <summary>
		/// Whether auto scaling is enabled
		/// </summary>
		public static bool Paused {
			get {
				return paused;
			}
			set {
				if (paused != value) {
					paused = value;
					if (paused) {
						pausedDialog = new PausedView(() => Paused = false);
						pausedDialog.Show(UnityEngine.Object.FindObjectOfType<AppLauncher>().GetAnchor());
					} else {
						pausedDialog?.Dismiss();
						pausedDialog = null;

						UnityEngine.Object.FindObjectOfType<SmartTank>().OnSimUpdate(pausedDeltaV);
						pausedDeltaV = null;
					}
				}
			}
		}

		/// <summary>
		/// Fires when the simulator is updated
		/// Populates the KSPFields for PP tanks so their PartModules can do the scaling
		/// </summary>
		private void OnSimUpdate(VesselDeltaV dvCalc)
		{
			if (dvCalc == null) {
				return;
			}
			if (Paused) {
				pausedDeltaV = dvCalc;
				return;
			}
			string nodesErr = "";
			getNodeStructureError(ref nodesErr);
			double totalMassChange = 0;

			for (int st = dvCalc.OperatingStageInfo.Count - 1; st >= 0; --st) {
				DeltaVStageInfo stage = dvCalc.OperatingStageInfo[st];

				List<SmartTankPart> drained = new List<SmartTankPart>(drainedTanks(dvCalc, stage.stage));
				int numTanks = drained.Count;

				if (stage != null && numTanks > 0) {

					if (stage.thrustVac <= 0) {
						// No thrust on this stage, so fuel doesn't make sense either.
						// Note that IdealTotalMass effectively is optional for the parts
						// to obey; if AutoScale is false, they can ignore it.
						for (int t = 0; t < numTanks; ++t) {
							// Reset all the tanks to minimum size with no thrust
							drained[t].nodesError     = nodesErr;
							drained[t].IdealTotalMass = 0;
							if (drained[0].AutoScale) {
								totalMassChange -= partTotalMass(drained[t].part);
							}
						}

					} else {
						// This stage has thrust that we can balance against the fuel.

						// Add up the current procedural tank mass
						double currentProcTankMass = 0;
						for (int t = 0; t < numTanks; ++t) {
							currentProcTankMass += partTotalMass(drained[t].part);
						}

						// Determine the mass that the procedural parts can't change
						double nonProcMass = stage.startMass - currentProcTankMass + totalMassChange;

						if (nonProcMass < 0) {
							// Sanity check, this is negative a lot
							continue;
						}

						// Get the thrust this stage is configured to use
						double thrust = drained[0].Atmospheric
							? stage.thrustASL
							: stage.thrustVac;

						// Calculate the mass to distribute among this stage's procedural tanks
						// This includes their wet AND dry mass!
						double targetProcTankMass = optimalTankMass(
							thrust,
							drained[0].bodyGravAccel,
							drained[0].targetTWR,
							nonProcMass
						);

						// Assume we'll have our way if auto scaling,
						// otherwise use the existing mass
						if (drained[0].AutoScale) {
							double massChange = targetProcTankMass > 0
								? targetProcTankMass - currentProcTankMass
								: 0;
							totalMassChange += massChange;
						}

						// Distribute the mass evenly
						double massPerTank = targetProcTankMass / numTanks;
						for (int t = 0; t < numTanks; ++t) {
							drained[t].nodesError     = nodesErr;
							drained[t].IdealTotalMass = massPerTank;
						}
					}
				}
			}
		}

		private IEnumerable<SmartTankPart> drainedTanks(VesselDeltaV dvCalc, int stageIndex)
		{
			for (int i = 0; i < dvCalc.PartInfo.Count; ++i) {
				DeltaVPartInfo pi = dvCalc.PartInfo[i];

				DeltaVPartInfo.PartStageFuelMass psfm = null;

				if (pi.part.Modules.Contains<SmartTankPart>()
						&& pi.stageFuelMass.TryGetValue(stageIndex, out psfm)) {

					// The calculator doesn't give us an end mass of 0 for emptied tanks,
					// nor end=start for untouched tanks (rounding errors?).
					// So check whether it's more than half depleted.
					// Mass of 0 indicates decoupling.
					if (psfm.endMass < 0.5 * psfm.startMass && psfm.endMass > 0) {
						yield return pi.part.Modules.GetModule<SmartTankPart>();
					}
				}
			}
		}

		/// <summary>
		/// Calculate the acceleration due to gravity at a body's surface.
		///   F = m * a
		///   F = mu * m / r^2
		///   a = F / m = mu / r^2
		/// </summary>
		/// <param name="b">The CelestialBody to use</param>
		/// <returns>
		/// Acceleration in m/s/s
		/// </returns>
		public static double gravAccel(CelestialBody b)
		{
			return b.gravParameter / Math.Pow(b.Radius, 2);
		}

		/// <summary>
		/// Calculate the mass of fuel needed to achieve the desired TWR for a given thrust and dry mass.
		/// </summary>
		/// <param name="thrust">Thrust in kN</param>
		/// <param name="gravAccel">Acceleration due to gravity opposing the thrust</param>
		/// <param name="desiredTWR">Thrust weight ratio to aim for</param>
		/// <param name="dryMass">Mass in metric tons that will be left when this stage's fuel is gone</param>
		/// <returns>
		/// Mass in metric tons of fuel that should be used
		/// </returns>
		private static double optimalTankMass(double thrust, double gravAccel, double desiredTWR, double dryMass)
		{
			if (desiredTWR > 0 && gravAccel > 0 && dryMass > 0) {
				return Math.Max(
					0,
					thrust / desiredTWR / gravAccel - dryMass
				);
			} else {
				return 0;
			}
		}

	}

}
