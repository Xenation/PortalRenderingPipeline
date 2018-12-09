using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class PRPDebugger : MonoBehaviour {

		public List<VirtualPortalCamera> cameras = new List<VirtualPortalCamera>();

		public void AddCamera(VirtualPortalCamera cam) {
			cameras.Add(cam.Copy());
		}

		public void ClearCameras() {
			cameras.Clear();
		}

		private void OnDrawGizmos() {
			for (int i = 0; i < cameras.Count; i++) {
				if (cameras[i] == null) continue;
				Gizmos.DrawSphere(cameras[i].position, .5f);
			}

			Matrix4x4 tmpMat = Gizmos.matrix;
			Color tmpCol = Gizmos.color;

			for (int i = 0; i < cameras.Count; i++) {
				if (cameras[i] == null) continue;
				Gizmos.matrix = cameras[i].properties.cameraToWorld * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, -1f));
				Gizmos.DrawFrustum(Vector3.zero, Camera.main.fieldOfView, Camera.main.farClipPlane, Camera.main.nearClipPlane, Camera.main.aspect);
			}

			Gizmos.matrix = tmpMat;
			Gizmos.color = tmpCol;
		}

	}
}
