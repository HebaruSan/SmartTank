using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralParts;

namespace SmartTank {

	public class SmartTankPart : PartModule {

		public SmartTankPart() : base() { }

		public override void OnAwake()
		{
			base.OnAwake();

			// Reset to defaults for each new part
			DiameterMatching = Settings.Instance.DiameterMatching;
			FuelMatching     = Settings.Instance.FuelMatching;
			BodyForTWR       = Settings.Instance.BodyForTWR;
			Atmospheric      = Settings.Instance.Atmospheric;
			targetTWR        = Settings.Instance.TargetTWR;
			AutoScale        = Settings.Instance.AutoScale;
		}

		public void Start()
		{
			initializeBodies();
			bodyChanged(null, null);
			initializeAutoScale();
			autoScaleChanged(null, null);
			initializeDiameter();
			diameterChanged(null, null);
			SetTexture();
			// Update won't get called without this
			isEnabled = enabled = HighLogic.LoadedSceneIsEditor;
		}

		private void SetTexture()
		{
			if (part.HasModule<ProceduralPart>()) {
				ProceduralPart pp = part.GetModule<ProceduralPart>();
				pp.textureSet = Settings.Instance.DefaultTexture;
			}
		}

		[KSPField(
			guiName         = "smartTank_DiameterMatchingPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool DiameterMatching = Settings.Instance.DiameterMatching;

		private void initializeDiameter()
		{
			BaseField field = Fields["DiameterMatching"];
			UI_Toggle tog = (UI_Toggle)field.uiControlEditor;
			tog.onFieldChanged = diameterChanged;
			// Note whether ProceduralParts found enough shapes to enable the setting
			shapeNameActiveDefault = shapeNameActive;
		}

		private void diameterChanged(BaseField field, object o)
		{
			diameterActive  = !DiameterMatching;
			shapeNameActive = !DiameterMatching;
		}

		private bool diameterActive {
			set {
				if (part.HasModule<ProceduralShapeCylinder>()) {
					ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
					cyl.Fields["diameter"].guiActiveEditor = value;
				}
				if (part.HasModule<ProceduralShapePill>()) {
					ProceduralShapePill pil = part.GetModule<ProceduralShapePill>();
					pil.Fields["length"].guiActiveEditor = value;
				}
				if (part.HasModule<ProceduralShapeCone>()) {
					ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
					con.Fields["topDiameter"   ].guiActiveEditor = value;
					con.Fields["bottomDiameter"].guiActiveEditor = value;
				}
			}
		}

		// ProceduralPart sets shapeName active and inactive itself.
		// We don't want to enable it when they've disabled it, so we need
		// to cache their setting.
		private bool shapeNameActiveDefault = false;

		private bool shapeNameActive {
			get {
				if (part.HasModule<ProceduralPart>()) {
					ProceduralPart pp = part.GetModule<ProceduralPart>();
					return pp.Fields["shapeName"].guiActiveEditor;
				}
				return false;
			}
			set {
				if (shapeNameActiveDefault && part.HasModule<ProceduralPart>()) {
					ProceduralPart pp = part.GetModule<ProceduralPart>();
					pp.Fields["shapeName"].guiActiveEditor = value;
				}
			}
		}

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

		private AttachNode topAttachedNode    { get { return part.FindAttachNode("top");    } }
		private AttachNode bottomAttachedNode { get { return part.FindAttachNode("bottom"); } }
		private float opposingDiameter(AttachNode an)
		{
			AttachNode oppo = ReallyFindOpposingNode(an);
			if (oppo != null) {
				switch (oppo.size) {
					case 0:  return 0.625f;
					default: return 1.25f * oppo.size;
				}
			}
			return 0f;
		}

		private AttachNode ReallyFindOpposingNode(AttachNode an)
		{
			Part opposingPart = an?.attachedPart;
			if (opposingPart != null) {
				for (int i = 0; i < (opposingPart?.attachNodes?.Count ?? 0); ++i) {
					AttachNode otherNode = opposingPart.attachNodes[i];
					if (an.owner == otherNode.attachedPart) {
						return otherNode;
					}
				}
			}

			for (int p = 0; p < EditorLogic.fetch.ship.parts.Count; ++p) {
				Part otherPart = EditorLogic.fetch.ship.parts[p];
				for (int n = 0; n < (otherPart?.attachNodes?.Count ?? 0); ++n) {
					AttachNode otherNode = otherPart.attachNodes[n];
					if (an.owner == otherNode.attachedPart
							&& otherNode.nodeType == AttachNode.NodeType.Stack
							&& an.id != otherNode.id) {
						return otherNode;
					}
				}
			}
			return null;
		}

		private void SetShape(string shapeName)
		{
			if (part.HasModule<ProceduralPart>()) {
				ProceduralPart pp = part.GetModule<ProceduralPart>();
				if (shapeName != pp.shapeName) {
					pp.shapeName = shapeName;
					// Give the module a chance to update before we do anything else
					pp.Update();
				}
			}
		}

		private void SetCylindricalDiameter(float diameter)
		{
			SetShape("Cylinder");
			if (part.HasModule<ProceduralShapeCylinder>()) {
				ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
				cyl.diameter = diameter;
			}
		}

		private void SetConeDiameters(float topDiameter, float bottomDiameter)
		{
			SetShape("Cone");
			if (part.HasModule<ProceduralShapeCone>()) {
				ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
				con.topDiameter    = topDiameter;
				con.bottomDiameter = bottomDiameter;
			}
		}

		[KSPField(
			guiName         = "smartTank_FuelMatchingPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool FuelMatching = Settings.Instance.FuelMatching;

		private const string LfOxTypeName = "Mixed";

		private string EngineTankType(Part enginePart)
		{
			if (enginePart != null && enginePart.HasModule<ModuleEngines>()) {
				List<PartResourceDefinition> resources = enginePart.GetModule<ModuleEngines>().GetConsumedResources();
				if (resources.Count == 1) {
					return resources[0].name;
				} else {
					return LfOxTypeName;
				}
			} else {
				return LfOxTypeName;
			}
		}

		private Part findEngine()
		{
			for (int n = 0; n < part.attachNodes.Count; ++n) {
				AttachNode an = part.attachNodes[n];
				if (an?.attachedPart?.HasModule<ModuleEngines>() ?? false) {
					return an.attachedPart;
				}
				Part opown = an?.FindOpposingNode()?.owner;
				if (opown?.HasModule<ModuleEngines>() ?? false) {
					return opown;
				}
				List<Part> parts = new List<Part>();
				part.attachNodes[n].FindAttachedPart(parts);
				for (int p = 0; p < parts.Count; ++p) {
					if (parts[p].HasModule<ModuleEngines>()) {
						return parts[p];
					}
				}
			}
			return null;
		}

		private void MatchFuel()
		{
			if (part.HasModule<TankContentSwitcher>()) {
				TankContentSwitcher tcs = part.GetModule<TankContentSwitcher>();

				string tankType = EngineTankType(findEngine());
				if (tcs.tankType != tankType) {
					tcs.tankType = tankType;
				}
			}
		}

		[KSPField(
			guiName         = "smartTank_DrainsInStagePrompt",
			isPersistant    = false,
			guiActive       = false,
			guiActiveEditor = false
		)]
		public int DrainStage = -1;

		[KSPField(
			guiName         = "smartTank_IdealWetMassPrompt",
			isPersistant    = false,
			guiActive       = false,
			guiActiveEditor = false
		)]
		public double IdealWetMass;

		[KSPField(
			guiName         = "smartTank_TWRAtPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_ChooseOption(
			scene           = UI_Scene.Editor
		)]
		public string BodyForTWR = Settings.Instance.BodyForTWR;

		private static string[] planetList = null;

		private void getPlanetList()
		{
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
		}

		private void initializeBodies()
		{
			if (FlightGlobals.Bodies != null) {
				BaseField field = Fields["BodyForTWR"];
				UI_ChooseOption range = (UI_ChooseOption)field.uiControlEditor;
				if (range != null) {
					getPlanetList();
					range.onFieldChanged = bodyChanged;
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
			guiName         = "smartTank_AtmosphericPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool Atmospheric = Settings.Instance.Atmospheric;

		[KSPField(
			guiName         = "smartTank_TargetTWRPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true,
			guiFormat       = "G2"
		), UI_FloatEdit(
			scene           = UI_Scene.Editor,
			incrementSlide  = 0.1f,
			incrementLarge  = 1f,
			incrementSmall  = 0.1f,
			minValue        = 0.1f,
			maxValue        = 10f,
			sigFigs         = 1
		)]
		public float targetTWR = Settings.Instance.TargetTWR;

		[KSPField(
			guiName         = "smartTank_AutoScalePrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool AutoScale = Settings.Instance.AutoScale;

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
			lengthActive = !AutoScale;
		}

		private bool lengthActive {
			set {
				if (part.HasModule<ProceduralShapeCylinder>()) {
					ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
					cyl.Fields["length"].guiActiveEditor = value;
				}
				if (part.HasModule<ProceduralShapePill>()) {
					ProceduralShapePill pil = part.GetModule<ProceduralShapePill>();
					pil.Fields["length"].guiActiveEditor = value;
				}
				if (part.HasModule<ProceduralShapeCone>()) {
					ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
					con.Fields["length"].guiActiveEditor = value;
				}
			}
		}

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
			} else {
				allowResourceEditing(true);
			}
		}

		private void allowResourceEditing(bool allowEdit)
		{
			for (int i = 0; i < part.Resources.Count; ++i) {
				PartResource r = part.Resources[i];
				if (!allowEdit) {
					r.amount   = r.maxAmount;
				}
				r.isTweakable  = allowEdit;
			}
		}

		[KSPEvent(
			guiName         = "smartTank_ScaleNowPrompt",
			guiActive       = false,
			guiActiveEditor = true,
			active          = true
		)]
		public void ScaleNow()
		{
			allowResourceEditing(false);
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
			// Volume of fuel to use:
			double idealVolume = IdealWetMass / wetDensity;

			if (part.HasModule<ProceduralShapeCylinder>()) {
				ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
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

			if (part.HasModule<ProceduralShapePill>()) {
				// We won't try to change the "fillet", so we can treat it as a constant
				// Diameter is likewise a constant here
				ProceduralShapePill pil = part.GetModule<ProceduralShapePill>();
				double fillet = pil.fillet, diameter = pil.diameter;
				double idealLength = (idealVolume * 24f / Math.PI - (10f - 3f * Math.PI) * fillet * fillet * fillet - 3f * (Math.PI - 4) * diameter * fillet * fillet) / (6f * diameter * diameter);
				if (idealLength < 1) {
					idealLength = 1;
				}
				if (Math.Abs(pil.length - idealLength) > 0.05) {
					pil.length = (float)idealLength;
				}
			}

			if (part.HasModule<ProceduralShapeCone>()) {
				ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
				double topDiameter = con.topDiameter, bottomDiameter = con.bottomDiameter;
				double idealLength = idealVolume * 12f / (Math.PI * (topDiameter * topDiameter + topDiameter * bottomDiameter + bottomDiameter * bottomDiameter));
				if (idealLength < 1) {
					idealLength = 1;
				}
				if (Math.Abs(con.length - idealLength) > 0.05) {
					con.length = (float)idealLength;
				}
			}

			// BezierCone shapes not supported because they're too complicated.
			// See ProceduralShapeBezierCone.CalcVolume to see why.
		}

	}

}
