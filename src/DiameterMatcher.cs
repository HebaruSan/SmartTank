using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralParts;

namespace SmartTank {

	/// <summary>
	/// Part module that automatically sets the size and diameters of a
	/// procedural part based on its attached neighbor parts.
	/// </summary>
	public class DiameterMatcher : PartModule {

		/// <summary>
		/// Initialize a diameter matcher module
		/// </summary>
		public DiameterMatcher() : base() { }

		/// <summary>
		/// Called when part is first created and during load of game assets
		/// </summary>
		public override void OnAwake()
		{
			base.OnAwake();

			// Reset to defaults for each new part
			DiameterMatching = Settings.Instance.DiameterMatching;
		}

		/// <summary>
		/// Called when part is created for use in editor or flight.
		/// </summary>
		/// <param name="state">Description of context where part will be used</param>
		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			isEnabled = enabled = false;
			if (state == StartState.Editor) {
				initializeDiameter();
				diameterChanged(null, null);

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

		/// <summary>
		/// Field to toggle this module's functionality
		/// </summary>
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
				if (part.Modules.Contains<ProceduralShapeCylinder>()) {
					ProceduralShapeCylinder cyl = part.Modules.GetModule<ProceduralShapeCylinder>();
					cyl.Fields["diameter"].guiActiveEditor = value;
				}
				if (part.Modules.Contains<ProceduralShapePill>()) {
					ProceduralShapePill pil = part.Modules.GetModule<ProceduralShapePill>();
					pil.Fields["diameter"].guiActiveEditor = value;
				}
				if (part.Modules.Contains<ProceduralShapeCone>()) {
					ProceduralShapeCone con = part.Modules.GetModule<ProceduralShapeCone>();
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
				if (part.Modules.Contains<ProceduralPart>()) {
					ProceduralPart pp = part.Modules.GetModule<ProceduralPart>();
					return pp.Fields["shapeName"].guiActiveEditor;
				}
				return false;
			}
			set {
				if (shapeNameActiveDefault && part.Modules.Contains<ProceduralPart>()) {
					ProceduralPart pp = part.Modules.GetModule<ProceduralPart>();
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
				if (oppo.owner != null && oppo.owner.Modules.Contains<ProceduralPart>()) {
					ProceduralPart oppoPP = oppo.owner.Modules.GetModule<ProceduralPart>();
					switch (oppoPP.shapeName) {
						case "Cylinder":
							return oppo.owner.Modules.GetModule<ProceduralShapeCylinder>().diameter;
						case "Curved": // Only used by the decoupler
						case "Fillet Cylinder":
							return oppo.owner.Modules.GetModule<ProceduralShapePill>().diameter;
						case "Cone":
							switch (an.id) {
								case "top":
									return oppo.owner.Modules.GetModule<ProceduralShapeCone>().bottomDiameter;
								case "bottom":
									return oppo.owner.Modules.GetModule<ProceduralShapeCone>().topDiameter;
							}
							break;
						case "Smooth Cone":
							switch (an.id) {
								case "top":
									return oppo.owner.Modules.GetModule<ProceduralShapeCone>().bottomDiameter;
								case "bottom":
									return oppo.owner.Modules.GetModule<ProceduralShapeCone>().topDiameter;
							}
							break;
					}
				}
				switch (oppo.size) {
					case 0:  return 0.625f;
					case 2:  return treat2As1p5(oppo) ? 1.875f : 2.5f;
					default: return 1.25f * oppo.size;
				}
			}
			return 0f;
		}

		/// <summary>
		/// 1.875m parts are treated as 2.5m parts in the AttachNode.size property,
		/// since it's an integer.
		/// To identify nodes that are actually 1.875m, we have to check
		/// the AvailablePart.bulkheadProfiles property:
		///
		/// Diameter | size | bulkheadProfiles
		/// ---------+------+------------------
		/// 0.625m   | 0    | size0
		/// 1.25m    | 1    | size1
		/// 1.875m   | 2    | size1p5*
		/// 2.5m     | 2    | size2
		/// 3.75m    | 3    | size3
		/// 5m       | 4    | size4
		/// * The Cheetah has a bulkheadProfiles of size1 despite being 1.875m,
		///   so we don't require it to contain size1p5.
		///
		/// Note that 100% correct determination is impossible in the case of
		/// an adapter that's 1.875m on one side and 2.5m on the other, since
		/// both have size=2 and bulkheadProfiles will contain both size1p5 and size2.
		/// </summary>
		/// <param name="an">Attach node to check</param>
		/// <returns>
		/// True if a size of 2 should be treated as 1.875m on this part,
		/// false if it should be treated as 2.5m.
		/// </returns>
		private bool treat2As1p5(AttachNode an)
		{
			return !an?.owner?.partInfo?.bulkheadProfiles?.Contains("size2") ?? false;
		}

		private AttachNode ReallyFindOpposingNode(AttachNode an)
		{
			if (an != null) {
				Part opposingPart = an.attachedPart;
				if (opposingPart != null) {
					for (int i = 0; i < (opposingPart?.attachNodes?.Count ?? 0); ++i) {
						AttachNode otherNode = opposingPart.attachNodes[i];
						if (an.owner == otherNode.attachedPart) {
							return otherNode;
						}
					}
				}

				List<Part> parts = EditorLogic.fetch?.ship?.parts;
				if (parts != null) {
					for (int p = 0; p < parts.Count; ++p) {
						Part otherPart = parts[p];
						for (int n = 0; n < (otherPart?.attachNodes?.Count ?? 0); ++n) {
							AttachNode otherNode = otherPart.attachNodes[n];
							if (an.owner == otherNode.attachedPart
							&& otherNode.nodeType == AttachNode.NodeType.Stack
							&& an.id != otherNode.id) {
								return otherNode;
							}
						}
					}
				}
			}
			return null;
		}

		private bool SetShape<T>() where T : ProceduralAbstractShape
		{
			if (part.Modules.Contains<ProceduralPart>() && part.Modules.Contains<T>()) {
				ProceduralPart pp = part.Modules.GetModule<ProceduralPart>();
				T shape = part.Modules.GetModule<T>();
				if (pp.shapeName != shape.displayName) {
					pp.shapeName = shape.displayName;
					// Give the module a chance to update before we do anything else
					pp.Update();
				}
				return true;
			} else {
				return false;
			}
		}

		private void SetCylindricalDiameter(float diameter)
		{
			if (SetShape<ProceduralShapeCylinder>()) {
				ProceduralShapeCylinder cyl = part.Modules.GetModule<ProceduralShapeCylinder>();
				cyl.diameter = diameter;
			} else if (SetShape<ProceduralShapePill>()) {
				ProceduralShapePill pil = part.Modules.GetModule<ProceduralShapePill>();
				pil.diameter = diameter;
			}
		}

		private void SetConeDiameters(float topDiameter, float bottomDiameter)
		{
			if (SetShape<ProceduralShapeCone>()) {
				ProceduralShapeCone con = part.Modules.GetModule<ProceduralShapeCone>();
				con.topDiameter    = topDiameter;
				con.bottomDiameter = bottomDiameter;
			} else {
				SetCylindricalDiameter(Math.Max(topDiameter, bottomDiameter));
			}
		}

		/// <summary>
		/// Called on each UI tick
		/// </summary>
		public void Update()
		{
			if (enabled && isEnabled && HighLogic.LoadedSceneIsEditor) {
				if (DiameterMatching) {
					MatchDiameters();
				}
			}
		}

	}

}
