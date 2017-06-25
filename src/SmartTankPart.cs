using System;
using System.Collections;
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
			FuelMatching     = Settings.Instance.FuelMatching;
			BodyForTWR       = Settings.Instance.BodyForTWR;
			Atmospheric      = Settings.Instance.Atmospheric;
			targetTWR        = Settings.Instance.TargetTWR;
			AutoScale        = Settings.Instance.AutoScale;

			// Set the texture for the preview part in the parts list and newly placed parts
			SetTexture();
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			isEnabled = enabled = false;
			if (state == StartState.Editor) {
				initializeBodies();
				bodyChanged(null, null);
				initializeAutoScale();
				autoScaleChanged(null, null);
				getFuelInfo(part.GetModule<TankContentSwitcher>().tankType);

				// Wait 1 second before initializing so ProceduralPart modules
				// have a chance to re-init after a revert
				StartCoroutine(after(1, () => {
					// Update won't get called without this
					isEnabled = enabled = HighLogic.LoadedSceneIsEditor;
				}));
			}
		}

		private IEnumerator after(float seconds, Callback cb)
		{
			while (true) {
				yield return new WaitForSeconds(seconds);
				cb();
				yield break;
			}
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

		[KSPField(
			guiName         = "smartTank_FuelMatchingPrompt",
			isPersistant    = true,
			guiActive       = false,
			guiActiveEditor = true
		), UI_Toggle(
			scene           = UI_Scene.Editor
		)]
		public bool FuelMatching = Settings.Instance.FuelMatching;

		// TODO - get this on the fly from the ConfigNodes
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
					getFuelInfo(tankType);
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
			guiName         = "Nodes error",
			isPersistant    = false,
			guiActive       = false,
			guiActiveEditor = false
		)]
		public string nodesError;

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
			if (enabled && isEnabled && HighLogic.LoadedSceneIsEditor) {
				if (FuelMatching) {
					MatchFuel();
				}
				if (part.HasModule<TankContentSwitcher>()) {
					part.GetModule<TankContentSwitcher>().Fields["tankType"].guiActiveEditor = !FuelMatching;
				}
				if (AutoScale) {
					ScaleNow();
				}
				allowResourceEditing(!AutoScale);

				Fields["nodesError"].guiActiveEditor = (nodesError.Length > 0);
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

		private class FuelInfo : TankContentSwitcher.TankTypeOption {

			public FuelInfo(ConfigNode fuelNode) : base()
			{
				if (fuelNode != null) {
					Load(fuelNode);

					// Multiply dry mass by this to get number of units of Lf+Ox
					double totalUnitsPerT = 0;
					for (int r = 0; r < resources.Count; ++r) {
						totalUnitsPerT += resources[r].unitsPerT;
					}

					// Multiply volume by this to get the mass of fuel:
					double fuelDensity = fuelMassPerUnit * totalUnitsPerT * dryDensity;
					wetDensity         = dryDensity + fuelDensity;
				}
			}

			// Multiply volume by this to get the wet mass:
			public  readonly double wetDensity;

			// Multiply lf or ox units by this to get the fuel mass in tons:
			private const    double fuelMassPerUnit = 0.005;
		}

		private double wetDensity;

		private ConfigNode getContentSwitcher()
		{
			return part?.partInfo?.partConfig?.GetNode("MODULE", "name", "TankContentSwitcher");
		}

		private ConfigNode getFuelNode(string optionName)
		{
			return getContentSwitcher().GetNode("TANK_TYPE_OPTION", "name", optionName);
		}

		private void getFuelInfo(string optionName)
		{
			FuelInfo fuelType = new FuelInfo(getFuelNode(optionName));
			if (fuelType != null) {
				// Multiply volume by this to get the wet mass:
				wetDensity = fuelType.wetDensity;
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
			if (HighLogic.LoadedSceneIsEditor && wetDensity > 0) {
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
						if (part.GetModule<ProceduralPart>().shapeName == cyl.displayName) {
							cyl.Update();
						}
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
						if (part.GetModule<ProceduralPart>().shapeName == pil.displayName) {
							pil.Update();
						}
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
						if (part.GetModule<ProceduralPart>().shapeName == con.displayName) {
							con.Update();
						}
					}
				}
				// BezierCone shapes not supported because they're too complicated.
				// See ProceduralShapeBezierCone.CalcVolume to see why.
			}
		}

	}

}
