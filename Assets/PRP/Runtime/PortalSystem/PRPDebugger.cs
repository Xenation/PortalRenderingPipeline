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

			for (int i = 0; i < cameras.Count; i++) {
				if (cameras[i] == null) continue;
				Gizmos.matrix = cameras[i].worldToCamera;
				Gizmos.DrawFrustum(Vector3.zero, Camera.main.fieldOfView, Camera.main.farClipPlane, Camera.main.nearClipPlane, Camera.main.aspect);
			}

			Gizmos.matrix = tmpMat;
		}

	}
}
