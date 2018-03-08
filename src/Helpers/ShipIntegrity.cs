using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartTank {

	/// <summary>
	/// Ship integrity checks for DEBUG mode.
	/// During development, I found that the AttachNode objects in my ship
	/// tended to get corrupted if I called FindOpposingNode at the wrong time,
	/// and these checks will detect those problems and make them visible in a
	/// field on the procedural tanks.
	/// Only in DEBUG mode!
	/// </summary>
	public static class ShipIntegrity {

		private const string nullStr = "NULL";

		/// <summary>
		/// Print all the parts and attach nodes in the current ship.
		/// </summary>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void printAttachNodes()
		{
			MonoBehaviour.print($"There are {EditorLogic.fetch.ship.parts.Count} parts");
			for (int p = 0; p < EditorLogic.fetch.ship.parts.Count; ++p) {
				Part part = EditorLogic.fetch.ship.parts[p];
				MonoBehaviour.print($"{p} name: {part.partInfo.name}, mode: {part.attachMode.ToString()}, method: {part.attachMethod.ToString()}, parent: {part.parent?.partInfo.name ?? nullStr}");
				for (int n = 0; n < part.attachNodes.Count; ++n) {
					AttachNode an = part.attachNodes[n];
					printAttachNode($"{n}", an);
				}
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private static void printAttachNode(string descrip, AttachNode an)
		{
			if (an != null) {
				if (an.attachedPart != null) {
					MonoBehaviour.print($"	{descrip} id: {an.id}, method: {an.attachMethod.ToString()}, type: {an.nodeType.ToString()}, size: {an.size}, owner: {an.owner?.partInfo.name ?? nullStr}, attached: {an.attachedPart?.partInfo.name ?? nullStr}");
				}
			} else {
				MonoBehaviour.print($"	{descrip} NULL");
			}
		}

		/// <summary>
		/// Check the ship and return any errors in the parameter.
		/// This is void so we can use the Conditional attribute.
		/// </summary>
		/// <param name="err">Will be set to the error if any, otherwise ""</param>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void getNodeStructureError(ref string err)
		{
			err = "";
			err = attachNodeStructureError();
		}

		private static string attachNodeStructureError()
		{
			try {
				List<Part> parts = EditorLogic.fetch?.ship?.parts;
				if (parts == null) {
					return "No parts found";
				} else if (parts.Count < 2) {
					// 0 or 1 parts
					// Make sure the nodes are all empty
					for (int p = 0; p < (parts?.Count ?? 0); ++p) {
						Part part = parts[p];
						for (int n = 0; n < (part.attachNodes?.Count ?? 0); ++n) {
							AttachNode an = part.attachNodes[n];
							if (an.attachedPart != null) {
								return $"{part.partInfo.name}'s node {an.id} is attached";
							}
						}
					}
					return "";
				} else {
					// 2 or more parts
					// Make sure each part has at least one connected node
					// Make sure each connected node has an opposing node
					for (int p = 0; p < (parts?.Count ?? 0); ++p) {
						Part part = parts[p];
						if ((part.attachNodes?.Count ?? 0) > 1) {
							bool anyAttached = false;
							for (int n = 0; n < (part.attachNodes?.Count ?? 0); ++n) {
								AttachNode an = part.attachNodes[n];
								if (an.attachedPart != null && an.nodeType == AttachNode.NodeType.Stack) {
									anyAttached = true;
									if (an.FindOpposingNode() == null) {
										return $"{part.partInfo.name}'s node {an.id} lacks an opposing node";
									}
								}
							}
							if (!anyAttached) {
								return $"{part.partInfo.name} has no attached nodes";
							}
						}
					}
					return "";
				}
			} catch (Exception ex) {
				MonoBehaviour.print($"Oops during node structure check: {ex.Message}");
				MonoBehaviour.print($"{ex.StackTrace}");
				return "Exception: {ex.Message}";
			}
		}

	}

}
