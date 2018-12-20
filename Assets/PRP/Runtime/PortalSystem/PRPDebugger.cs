using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class PRPDebugger : MonoBehaviour {

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
