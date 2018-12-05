using UnityEngine;

namespace PRP.PortalSystem {
	public class Portal : MonoBehaviour {

		public Portal targetPortal;
		public GameObject test;
		public GameObject test2;

		private RenderTexture renderTexture;
		[System.NonSerialized] public Camera cameraAtTarget;

		private void OnEnable() {
			renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
			CreateCamera();
		}

		private void Update() { // DEBUG
			SyncCamera();
			if (test != null && test2 != null)
				test2.transform.rotation = test.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
			Debug.DrawLine(cameraAtTarget.transform.position, cameraAtTarget.transform.position + cameraAtTarget.transform.forward * 5f, Color.green);
		}

		private void OnDisable() {
			renderTexture.Release();
			if (cameraAtTarget != null) {
				Destroy(cameraAtTarget.gameObject);
			}
		}

		private void CreateCamera() {
			GameObject camGO = new GameObject("Camera - " + gameObject.name);
			camGO.transform.SetParent(targetPortal.transform);
			cameraAtTarget = camGO.AddComponent<Camera>();
			cameraAtTarget.targetTexture = renderTexture;
		}

		private void SyncCamera() {
			Matrix4x4 worldToPortal = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one) * transform.worldToLocalMatrix;
			cameraAtTarget.transform.localPosition = worldToPortal.MultiplyPoint3x4(Camera.main.transform.position);
			cameraAtTarget.transform.localRotation = Quaternion.Inverse(transform.rotation) * Camera.main.transform.rotation;
			Vector3 euler = cameraAtTarget.transform.localRotation.eulerAngles; // ugly af
			cameraAtTarget.transform.localRotation = Quaternion.Euler(euler.x, euler.y + 180f, euler.z);
		}

	}
}
