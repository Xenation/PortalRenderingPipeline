using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PRP.PortalSystem {
	[RequireComponent(typeof(Renderer))]
	public class Portal : MonoBehaviour {

		private static Matrix4x4 portalMirroring = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);
		private static Matrix4x4 portalMirroringInverse = portalMirroring.inverse;

		//public PortalRenderInfo info;
		public Portal outputPortal;
		

		[System.NonSerialized] public new Renderer renderer;

		private Matrix4x4 worldToPortal;
		private Matrix4x4 worldToPortalWorld;
		private Matrix4x4 portalWorldToWorld;
		public Plane portalPlane;

		private void OnEnable() {
			renderer = transform.GetComponentInChildren<Renderer>();
		}

		private void OnPreRender() {
			Synchronize();
		}

		private void OnDisable() {

		}

		private void OnDrawGizmos() {
			Color tmpCol = Gizmos.color;
			Gizmos.color = GetComponentInChildren<InstancedColor>().instancedColor;
			Gizmos.DrawWireCube(TransformPosition(Camera.main.transform.position), Vector3.one);
			Gizmos.color = tmpCol;
		}

		public void Synchronize() {
			worldToPortal = portalMirroring * transform.worldToLocalMatrix;
			worldToPortalWorld = outputPortal.transform.localToWorldMatrix * portalMirroring * transform.worldToLocalMatrix;
			portalWorldToWorld = transform.localToWorldMatrix * portalMirroringInverse * outputPortal.transform.worldToLocalMatrix;
			portalPlane = new Plane(-transform.forward, transform.position);
		}

		public Matrix4x4 TransformInverseMatrix(Matrix4x4 mat) {
			return mat * portalWorldToWorld;
		}

		public Vector3 TransformPosition(Vector3 p) {
			//return outputPortal.transform.localToWorldMatrix.MultiplyPoint3x4(portalMirroring.MultiplyPoint3x4(transform.worldToLocalMatrix.MultiplyPoint3x4(p)));
			return worldToPortalWorld.MultiplyPoint3x4(p);
		}
		
		public Vector3 TransformDirection(Vector3 d) {
			return worldToPortalWorld.MultiplyVector(d);
		}

	}
}
