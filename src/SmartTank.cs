using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using KSP.Localization;
using SmartTank.Simulation;
using ProceduralParts;

namespace SmartTank {

	using static ShipIntegrity;

	// Convert from en-UK to en-US
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

		private static readonly bool ProceduralPartsInstalled = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "ProceduralParts");

		/// <summary>
		/// This is called at creation
		/// </summary>
		public void Start()
		{
			if (ProceduralPartsInstalled) {
				GameEvents.onEditorShipModified.Add(OnShipModified);
				GameEvents.onEditorPartPlaced.Add(OnPartPlaced);
				GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStagingChanged);

				SimManager.OnReady += OnSimUpdate;
				Settings.Instance.HideNonProceduralPartsChanged();
			}
		}

		private void OnShipModified(ShipConstruct sc)
		{
			RunSimulator();
		}

		private void OnPartPlaced(Part p)
		{
			RunSimulator();
		}

		private void OnStagingChanged()
		{
			RunSimulator();
		}

		private void RunSimulator()
		{
			if (ProceduralPartsInstalled) {
				try {
					SimManager.Gravity = gravAccel(FlightGlobals.GetHomeBody());
					SimManager.Atmosphere = FlightGlobals.GetHomeBody().GetPressure(0) * PhysicsGlobals.KpaToAtmospheres;
					SimManager.Mach = 0;
					SimManager.RequestSimulation();
					SimManager.TryStartSimulation();
				} catch (Exception ex) {
					print($"Exception while updating SmartTank: {ex.Message}");
					print($"{ex.StackTrace}");
				}
			}
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		public void OnDisable()
		{
			if (ProceduralPartsInstalled) {
				GameEvents.onEditorShipModified.Remove(OnShipModified);
				GameEvents.onEditorPartPlaced.Remove(OnPartPlaced);
				GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStagingChanged);

				SimManager.OnReady -= OnSimUpdate;
			}
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

		/// <summary>
		/// Fires when the simulator is updated
		/// </summary>
		private void OnSimUpdate()
		{
			string nodesErr = "";
			getNodeStructureError(ref nodesErr);
			double totalMassChange = 0;

			for (int st = 0; st < SimManager.Stages.Length; ++st) {
				Stage stage = SimManager?.Stages[st] ?? null;
				int numTanks = stage.drainedTanks.Count;

				if (stage != null && numTanks > 0) {
					if (stage.thrust <= 0) {
						// No thrust on this stage, so fuel doesn't make sense either.
						// Note that IdealTotalMass effectively is optional for the parts
						// to obey; if AutoScale is false, they can ignore it.
						for (int t = 0; t < numTanks; ++t) {
							// Reset all the tanks to minimum size with no thrust
							stage.drainedTanks[t].nodesError     = nodesErr;
							stage.drainedTanks[t].IdealTotalMass = 0;
							if (stage.drainedTanks[0].AutoScale) {
								totalMassChange -= partTotalMass(stage.drainedTanks[t].part);
							}
						}

					} else {
						// This stage has thrust that we can balance against the fuel.

						// Add up the current procedural tank mass
						double currentProcTankMass = 0;
						for (int t = 0; t < numTanks; ++t) {
							currentProcTankMass += partTotalMass(stage.drainedTanks[t].part);
						}

						// Determine the mass that the procedural parts can't change
						double nonProcMass = stage.totalMass - currentProcTankMass + totalMassChange;

						// Get the thrust this stage is configured to use
						double thrust = stage.drainedTanks[0].Atmospheric
							? stage.thrust
							: stage.vacuumThrust;

						// Calculate the mass to distribute among this stage's procedural tanks
						// This includes their wet AND dry mass!
						double targetProcTankMass = optimalTankMass(
							thrust,
							stage.drainedTanks[0].bodyGravAccel,
							stage.drainedTanks[0].targetTWR,
							nonProcMass
						);

						// Assume we'll have our way if auto scaling,
						// otherwise use the existing mass
						if (stage.drainedTanks[0].AutoScale) {
							double massChange = targetProcTankMass > 0
								? targetProcTankMass - currentProcTankMass
								: 0;
							totalMassChange += massChange;
						}

						// Distribute the mass in the same proportions as it is now
						double perTankRatio = targetProcTankMass / currentProcTankMass;
						if (Math.Abs(perTankRatio - 1) > 0.01) {
							for (int t = 0; t < numTanks; ++t) {
								stage.drainedTanks[t].nodesError     = nodesErr;
								stage.drainedTanks[t].IdealTotalMass = perTankRatio * partTotalMass(stage.drainedTanks[t].part);
							}
						}
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
