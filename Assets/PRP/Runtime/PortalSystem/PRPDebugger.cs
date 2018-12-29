using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class PRPDebugger : MonoBehaviour {
		
		public struct FrustumCorners {
			public Vector3 topNearLeft;
			public Vector3 topFarLeft;
			public Vector3 topFarRight;
			public Vector3 topNearRight;
			public Vector3 botNearLeft;
			public Vector3 botFarLeft;
			public Vector3 botFarRight;
			public Vector3 botNearRight;

			// 0: left  1: right  2: down  3: up  4: near  5: far
			public FrustumCorners(Plane[] planes) {
				PRPMath.PlanesIntersect(planes[3], planes[4], planes[0], out topNearLeft);
				PRPMath.PlanesIntersect(planes[3], planes[5], planes[0], out topFarLeft);
				PRPMath.PlanesIntersect(planes[3], planes[5], planes[1], out topFarRight);
				PRPMath.PlanesIntersect(planes[3], planes[4], planes[1], out topNearRight);
				PRPMath.PlanesIntersect(planes[2], planes[4], planes[0], out botNearLeft);
				PRPMath.PlanesIntersect(planes[2], planes[5], planes[0], out botFarLeft);
				PRPMath.PlanesIntersect(planes[2], planes[5], planes[1], out botFarRight);
				PRPMath.PlanesIntersect(planes[2], planes[4], planes[1], out botNearRight);
			}
		}

		private static PRPDebugger instance;
		public static PRPDebugger I {
			get {
				if (instance == null) {
					instance = FindObjectOfType<PRPDebugger>();
				}
				return instance;
			}
		}

		public List<VirtualPortalCamera> cameras = new List<VirtualPortalCamera>();
		public static List<Vector3> debugPositions = new List<Vector3>();
		public static List<Plane> debugPlanes = new List<Plane>();
		public static List<Color> debugPlanesColor = new List<Color>();
		public static List<FrustumCorners> debugCullingFrustums = new List<FrustumCorners>();
		public static List<Color> debugCullingFrustumColors = new List<Color>();

		public void AddCamera(VirtualPortalCamera cam) {
			cameras.Add(cam);
		}

		public void ClearCameras() {
			cameras.Clear();
		}

		public static void AddDebugPlane(Plane plane, Color color) {
			debugPlanes.Add(plane);
			debugPlanesColor.Add(color);
		}

		public static void AddDebugPlanes(Plane[] planes, Color color) {
			for (int i = 0; i < planes.Length; i++) {
				debugPlanes.Add(planes[i]);
				debugPlanesColor.Add(color);
			}
		}

		public static void ClearDebugPlanes() {
			debugPlanes.Clear();
			debugPlanesColor.Clear();
		}

		public static void AddDebugCullingFrustum(Plane[] planes, Color color) {
			debugCullingFrustums.Add(new FrustumCorners(planes));
			debugCullingFrustumColors.Add(color);
		}

		public static void ClearDebugCullingFrustums() {
			debugCullingFrustums.Clear();
			debugCullingFrustumColors.Clear();
		}

		private void OnDrawGizmos() {
			Matrix4x4 tmpMat = Gizmos.matrix;
			Color tmpCol = Gizmos.color;

			Gizmos.color = Color.red;
			for (int i = 0; i < cameras.Count; i++) {
				Gizmos.DrawSphere(cameras[i].position, .5f);
			}

			Gizmos.color = Color.white;
			for (int i = 0; i < cameras.Count; i++) {
				Gizmos.matrix = cameras[i].properties.cameraToWorld * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, -1f));
				Gizmos.DrawFrustum(Vector3.zero, Camera.main.fieldOfView, Camera.main.farClipPlane, Camera.main.nearClipPlane, Camera.main.aspect);
			}
			Gizmos.matrix = tmpMat;

			//for (int i = 0; i < cameras.Count; i++) {
			//	for (int pi = 0; pi < cameras[i].frustrumPlanes.Length; pi++) {
			//		DrawPlaneGizmo(cameras[i].frustrumPlanes[pi], new Color(.5f, 1f, 0f, .25f));
			//	}
			//}

			for (int i = 0; i < debugPlanes.Count; i++) {
				DrawPlaneGizmo(debugPlanes[i], debugPlanesColor[i]);
			}
			
			for (int i = 0; i < debugCullingFrustums.Count; i++) {
				Gizmos.color = debugCullingFrustumColors[i];
				Gizmos.DrawLine(debugCullingFrustums[i].topNearLeft, debugCullingFrustums[i].topFarLeft);
				Gizmos.DrawLine(debugCullingFrustums[i].topNearRight, debugCullingFrustums[i].topFarRight);
				Gizmos.DrawLine(debugCullingFrustums[i].botNearLeft, debugCullingFrustums[i].botFarLeft);
				Gizmos.DrawLine(debugCullingFrustums[i].botNearRight, debugCullingFrustums[i].botFarRight);

				Gizmos.DrawLine(debugCullingFrustums[i].topNearLeft, debugCullingFrustums[i].botNearLeft);
				Gizmos.DrawLine(debugCullingFrustums[i].topFarLeft, debugCullingFrustums[i].botFarLeft);
				Gizmos.DrawLine(debugCullingFrustums[i].topFarRight, debugCullingFrustums[i].botFarRight);
				Gizmos.DrawLine(debugCullingFrustums[i].topNearRight, debugCullingFrustums[i].botNearRight);

				Gizmos.DrawLine(debugCullingFrustums[i].topNearLeft, debugCullingFrustums[i].topNearRight);
				Gizmos.DrawLine(debugCullingFrustums[i].topFarLeft, debugCullingFrustums[i].topFarRight);
				Gizmos.DrawLine(debugCullingFrustums[i].botNearLeft, debugCullingFrustums[i].botNearRight);
				Gizmos.DrawLine(debugCullingFrustums[i].botFarLeft, debugCullingFrustums[i].botFarRight);
			}


			Gizmos.color = Color.green;
			foreach (Vector3 pos in debugPositions) {
				Gizmos.DrawCube(pos, Vector3.one * .25f);
			}

			Gizmos.color = tmpCol;
		}

		private void DrawPlaneGizmo(Plane plane, Color color) {
			Quaternion rot = Quaternion.LookRotation(plane.normal);
			Matrix4x4 tmpMat = Gizmos.matrix;
			Color tmpCol = Gizmos.color;
			Gizmos.matrix = Matrix4x4.TRS(plane.ClosestPointOnPlane(Vector3.zero), rot, Vector3.one);
			Gizmos.color = color;
			Gizmos.DrawCube(Vector3.zero, new Vector3(20f, 20f, 0.0001f));
			color.a = 1f;
			Gizmos.color = color;
			Gizmos.DrawLine(Vector3.zero, Vector3.forward * 5f);
			Gizmos.matrix = tmpMat;
			Gizmos.color = tmpCol;
		}

	}
}
