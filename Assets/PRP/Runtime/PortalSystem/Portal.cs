using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PRP.PortalSystem {
	[RequireComponent(typeof(Renderer), typeof(Collider))]
	public class Portal : MonoBehaviour {

		private static Matrix4x4 portalMirroring = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);
		private static Matrix4x4 portalMirroringInverse = portalMirroring.inverse;

		//public PortalRenderInfo info;
		public Portal outputPortal;
		

		[System.NonSerialized] public new Renderer renderer;
		public new Collider collider { get; private set; }

		private Matrix4x4 worldToPortal;
		private Matrix4x4 worldToPortalWorld;
		private Matrix4x4 portalWorldToWorld;
		private Matrix4x4 warpMatrix;
		public Plane plane;

		public Bounds bounds {
			get {
				return collider.bounds;
			}
		}
		
		[System.NonSerialized] public Vector3[] corners = new Vector3[4];
		[System.NonSerialized] public Vector3 middle;

		private void OnEnable() {
			renderer = transform.GetComponentInChildren<Renderer>();
			collider = GetComponent<Collider>();
			ComputeCorners();
			PortalsManager.I.RegisterPortal(this);
		}

		private void Update() {
			Synchronize();
		}

		private void OnDisable() {
			PortalsManager.I.UnregisterPortal(this);
		}

		private void ComputeCorners() {
			Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
			Matrix4x4 rendLocalToWorld = renderer.transform.localToWorldMatrix;
			// TODO Assumes portal mesh is a quad
			Vector3[] vertices = mesh.vertices;
			corners[0] = rendLocalToWorld.MultiplyPoint3x4(vertices[0]);
			corners[1] = rendLocalToWorld.MultiplyPoint3x4(vertices[1]);
			corners[2] = rendLocalToWorld.MultiplyPoint3x4(vertices[2]);
			corners[3] = rendLocalToWorld.MultiplyPoint3x4(vertices[3]);
			middle = renderer.transform.position;
		}

		public void Synchronize() {
			worldToPortal = portalMirroring * transform.worldToLocalMatrix;
			worldToPortalWorld = outputPortal.transform.localToWorldMatrix * portalMirroring * transform.worldToLocalMatrix;
			portalWorldToWorld = transform.localToWorldMatrix * portalMirroringInverse * outputPortal.transform.worldToLocalMatrix;
			warpMatrix = outputPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix;
			plane = new Plane(-transform.forward, transform.position);
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

		public Vector3 WarpPosition(Vector3 p) {
			return warpMatrix.MultiplyPoint3x4(p);
		}

		public Vector3 WarpDirection(Vector3 d) {
			return warpMatrix.MultiplyVector(d);
		}

	}
}
