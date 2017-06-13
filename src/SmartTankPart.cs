using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralParts;

namespace SmartTank {

	public class SmartTankPart : PartModule {

		public SmartTankPart() : base() { }

		public override void OnAwake()
		{
			base.OnAwake();
			initializeBodies();
			bodyChanged(null, null);
			initializeAutoScale();
			autoScaleChanged(null, null);
			// Update won't get calld without this
			isEnabled = enabled = HighLogic.LoadedSceneIsEditor;
		}

		[KSPField(
			guiName         = "Match attached diameters",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool DiameterMatching = true;

		private void MatchDiameters()
		{
			float topDiameter  = opposingDiameter(topAttachedNode),
				bottomDiameter = opposingDiameter(bottomAttachedNode);
			if (topDiameter > 0 && bottomDiameter > 0) {
				// Parts are attached to both top and bottom
				if (bottomDiameter == topDiameter) {
					// If they're the same, use that diameter for a cylindrical tank
					SetCylindricalDiameter(topDiameter);
				} else {
					// Otherwise, switch to a cone using the respective diameters
					SetConeDiameters(topDiameter, bottomDiameter);
				}
			} else if (topDiameter > 0) {
				// Part at top only: cylinder, use top's diameter
				SetCylindricalDiameter(topDiameter);
			} else if (bottomDiameter > 0) {
				// Part at bottom only: cylinder, use bottom's diameter
				SetCylindricalDiameter(bottomDiameter);
			}
			// If nothing's attached, do nothing
		}

		private AttachNode topAttachedNode { get { return part.FindAttachNode("top"); } }
		private AttachNode bottomAttachedNode { get { return part.FindAttachNode("bottom"); } }
		private float opposingDiameter(AttachNode an)
		{
			AttachNode oppo = an?.FindOpposingNode();
			if (oppo != null) {
				switch (oppo.size) {
					case 0:  return 0.625f;
					default: return 1.25f * oppo.size;
				}
			}
			return 0f;
		}

		private void SetCylindricalDiameter(float diameter)
		{
			// TODO: change shape if not already cylinder
			if (part.HasModule<ProceduralShapeCylinder>()) {
				ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
				cyl.diameter = diameter;
			}
		}

		private void SetConeDiameters(float topDiameter, float bottomDiameter)
		{
			// TODO: change shape if not already cone
			if (part.HasModule<ProceduralShapeCone>()) {
				ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
				con.topDiameter    = topDiameter;
				con.bottomDiameter = bottomDiameter;
			}
		}

		[KSPField(
			guiName         = "Match engine's fuel",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool FuelMatching = true;

		private void MatchFuel()
		{
			// TODO:
			// 1. Check for engine at either top or bottom
			// 2. If found, check what kind of fuel it uses
			// 3. Switch to that kind of fuel
		}

		[KSPField(
			guiName         = "Drains in stage",
			isPersistant    = false,
			guiActive       = false,
			guiActiveEditor = false
		)]
		public int DrainStage = -1;

		[KSPField(
			guiName         = "Ideal wet mass",
			isPersistant    = false,
			guiActive       = false,
			guiActiveEditor = false
		)]
		public double IdealWetMass;

		[KSPField(
			guiName         = "TWR at",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_ChooseOption(
			scene           = UI_Scene.Editor
		)]
		public string BodyForTWR = Planetarium?.fetch?.Home?.name ?? "Kerbin";

		private static string[] planetList = null;

		private void initializeBodies()
		{
			if (FlightGlobals.Bodies != null) {
				BaseField field = Fields["BodyForTWR"];
	            UI_ChooseOption range = (UI_ChooseOption)field.uiControlEditor;
				if (range != null) {
					range.onFieldChanged = bodyChanged;

					if (planetList == null) {
						List<string> options = new List<string>();
						for (int i = 0; i < FlightGlobals.Bodies.Count; ++i) {
							CelestialBody b = FlightGlobals.Bodies[i];
							if (b.hasSolidSurface) {
								options.Add(b.name);
							}
						}
						planetList = options.ToArray();
					}
					range.options = planetList;
				}
			}
		}

		private CelestialBody body {
			get {
				for (int i = 0; i < FlightGlobals.Bodies.Count; ++i) {
					if (FlightGlobals.Bodies[i].name == BodyForTWR) {
						return FlightGlobals.Bodies[i];
					}
				}
				return null;
			}
		}

		private void bodyChanged(BaseField field, object o)
		{
			bodyGravAccel = SmartTank.gravAccel(body);
		}

		public double bodyGravAccel = 0;

		[KSPField(
			guiName         = "Atmospheric",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		bool Atmospheric = true;

		[KSPField(
			guiName         = "Target TWR",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true,
			guiFormat       = "0.0"
		), UI_FloatEdit(
			scene           = UI_Scene.Editor,
			incrementSlide  = 0.1f,
			incrementLarge  = 1f,
			incrementSmall  = 0.1f,
			minValue        = 0.1f,
			maxValue        = 10f,
			sigFigs         = 1
		)]
		public float targetTWR = SmartTank.configuredTWR;

		[KSPField(
			guiName         = "Auto-scale",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool AutoScale = true;

		private void initializeAutoScale()
		{
			BaseField field = Fields["AutoScale"];
			UI_Toggle tog = (UI_Toggle)field.uiControlEditor;
			tog.onFieldChanged = autoScaleChanged;
		}

		private void autoScaleChanged(BaseField field, object o)
		{
			BaseEvent e = Events["ScaleNow"];
			e.guiActiveEditor = !AutoScale;
		}

		public double mass { get { return part.mass; } }

		public void Update()
        {
			if (DiameterMatching) {
				MatchDiameters();
			}
			if (FuelMatching) {
				MatchFuel();
			}
            if (AutoScale) {
				ScaleNow();
			}
        }

		[KSPEvent(
			guiName         = "Scale now",
			guiActive       = false,
			guiActiveEditor = true,
			active          = true
		)]
		public void ScaleNow()
		{
			// These values live in ProceduralParts's cfg files, but they're hidden from us.
			// Multiply the volume in m^3 by this to get the dry mass in tons:
			const double dryDensity = 0.1089;
			// Multiply the dry mass by this to get the liquid fuel units:
			const double lfUnitsPerT = 720;
			// Multiply the dry mass by this to get the oxidizer units:
			const double oxUnitsPerT = 880;
			// Multiply lf or ox units by this to get the fuel mass in tons:
			const double fuelMassPerUnit = 0.005;
			// Multiply volume by this to get the mass of fuel:
			const double fuelDensity = fuelMassPerUnit * (lfUnitsPerT + oxUnitsPerT) * dryDensity;
			// Multiply volume by this to get the wet mass:
			const double wetDensity = dryDensity + fuelDensity;

			if (part.HasModule<ProceduralShapeCylinder>()) {
				ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();

				double idealVolume = IdealWetMass / wetDensity;
				double radius = 0.5 * cyl.diameter;
				double crossSectionArea = Math.PI * radius * radius;
				double idealLength = idealVolume / crossSectionArea;
				if (idealLength < radius) {
					idealLength = radius;
				}
				if (Math.Abs(cyl.length - idealLength) > 0.05) {
					cyl.length = (float)idealLength;
				}
			}
		}

	}

}
