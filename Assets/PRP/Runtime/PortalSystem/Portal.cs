using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PRP.PortalSystem {
	[RequireComponent(typeof(Renderer))]
	public class Portal : MonoBehaviour {

		private static Matrix4x4 portalMirroring = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);

		//public PortalRenderInfo info;
		public Portal outputPortal;
		

		[System.NonSerialized] public new Renderer renderer;

		private Matrix4x4 worldToPortal;

		private void OnEnable() {
			renderer = transform.GetComponentInChildren<Renderer>();
		}

		private void OnPreRender() {
			Synchronize();
		}

		private void OnDisable() {

		}

		public void Synchronize() {
			worldToPortal = portalMirroring * transform.worldToLocalMatrix;
		}

		public Matrix4x4 TransformMatrix(Matrix4x4 mat) {
			return mat * worldToPortal * outputPortal.transform.localToWorldMatrix;
		}

		public Vector3 TransformPosition(Vector3 p) {
			return outputPortal.transform.localToWorldMatrix.MultiplyPoint3x4(worldToPortal.MultiplyPoint3x4(p));
		}
		
		public Vector3 TransformDirection(Vector3 d) {
			return outputPortal.transform.localToWorldMatrix.MultiplyVector(worldToPortal.MultiplyVector(d));
		}

	}
}
