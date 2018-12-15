using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class PRPDebugger : MonoBehaviour {

		public List<VirtualPortalCamera> cameras = new List<VirtualPortalCamera>();
		public static List<Vector3> debugPositions = new List<Vector3>();

		public void AddCamera(VirtualPortalCamera cam) {
			cameras.Add(cam);
		}

		public void ClearCameras() {
			cameras.Clear();
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

			for (int i = 0; i < cameras.Count; i++) {
				for (int pi = 0; pi < cameras[i].frustrumPlanes.Length; pi++) {
					DrawPlaneGizmo(cameras[i].frustrumPlanes[pi], new Color(.5f, 1f, 0f, .25f));
				}
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
			Gizmos.DrawCube(Vector3.zero, new Vector3(50f, 50f, 0.0001f));
			Gizmos.matrix = tmpMat;
			Gizmos.color = tmpCol;
		}

	}
}
