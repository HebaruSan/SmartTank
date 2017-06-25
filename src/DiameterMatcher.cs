using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralParts;

namespace SmartTank {

	public class DiameterMatcher : PartModule {

		public DiameterMatcher() : base() { }

		public override void OnAwake()
		{
			base.OnAwake();

			// Reset to defaults for each new part
			DiameterMatching = Settings.Instance.DiameterMatching;
		}

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
					print($"Enabling DiameterMatcher");
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
					pil.Fields["diameter"].guiActiveEditor = value;
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

				List<Part> parts = EditorLogic?.fetch?.ship?.parts;
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
			if (part.HasModule<ProceduralShapeCylinder>()) {
				SetShape("Cylinder");
				ProceduralShapeCylinder cyl = part.GetModule<ProceduralShapeCylinder>();
				cyl.diameter = diameter;
			} else if (part.HasModule<ProceduralShapePill>()) {
				SetShape("Pill");
				ProceduralShapePill pil = part.GetModule<ProceduralShapePill>();
				pil.diameter = diameter;
			}
		}

		private void SetConeDiameters(float topDiameter, float bottomDiameter)
		{
			if (part.HasModule<ProceduralShapeCone>()) {
				SetShape("Cone");
				ProceduralShapeCone con = part.GetModule<ProceduralShapeCone>();
				con.topDiameter    = topDiameter;
				con.bottomDiameter = bottomDiameter;
			} else {
				SetCylindricalDiameter(Math.Max(topDiameter, bottomDiameter));
			}
		}

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
