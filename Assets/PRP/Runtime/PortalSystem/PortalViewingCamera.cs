using UnityEngine;

namespace PRP.PortalSystem {
	[RequireComponent(typeof(Camera)), ExecuteInEditMode]
	public class PortalViewingCamera : MonoBehaviour {

		public Portal[] viewablePortals;

		private new Camera camera;

		private void OnEnable() {
			camera = GetComponent<Camera>();
			camera.depthTextureMode = DepthTextureMode.Depth;
		}

	}
}
