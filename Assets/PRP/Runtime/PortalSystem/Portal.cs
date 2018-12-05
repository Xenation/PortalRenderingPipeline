using UnityEngine;

namespace PRP.PortalSystem {
	public class Portal : MonoBehaviour {

		public Portal targetPortal;

		private RenderTexture renderTexture;
		private Camera cameraAtTarget;

		private void OnEnable() {
			renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
			CreateCamera();
		}

		private void Update() { // DEBUG
			SyncCamera();
			Debug.DrawLine(cameraAtTarget.transform.position, cameraAtTarget.transform.position + cameraAtTarget.transform.forward * 5f, Color.green);
		}

		private void OnDisable() {
			renderTexture.Release();
		}

		private void CreateCamera() {
			GameObject camGO = new GameObject("Camera - " + gameObject.name);
			camGO.transform.SetParent(targetPortal.transform);
			cameraAtTarget = camGO.AddComponent<Camera>();
			cameraAtTarget.targetTexture = renderTexture;
		}

		private void SyncCamera() {
			Matrix4x4 mirror = PRPMath.ReflectionMatrix(new Plane(-transform.forward, transform.position));
			Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
			worldToLocal = worldToLocal * mirror;
			cameraAtTarget.transform.localPosition = worldToLocal.MultiplyPoint3x4(Camera.main.transform.position);
			cameraAtTarget.transform.localRotation = Quaternion.FromToRotation(worldToLocal.MultiplyVector(Camera.main.transform.forward), Vector3.forward);

			//Matrix4x4 mirrorMatrix = new Matrix4x4();
			//mirrorMatrix.SetTRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, -1f, -1f));
			//Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
			//worldToLocal = mirrorMatrix * worldToLocal;
			//cameraAtTarget.transform.localPosition = worldToLocal.MultiplyPoint3x4(Camera.main.transform.position);
			////cameraAtTarget.transform.localRotation = Quaternion.FromToRotation(worldToLocal.MultiplyVector(Camera.main.transform.forward), Vector3.forward);


			//return;
			//Vector3 cameraMirrorPos = worldToLocal.MultiplyPoint3x4(Camera.main.transform.position);
			//cameraMirrorPos.z = -cameraMirrorPos.z;
			//cameraAtTarget.transform.localPosition = cameraMirrorPos;
			//Vector3 cameraMirrorRot = transform.worldToLocalMatrix * Camera.main.transform.localRotation.eulerAngles;
			//cameraMirrorRot.y += 180;
			//cameraAtTarget.transform.localRotation = Quaternion.Euler(cameraMirrorRot);
		}

	}
}
