using System.Collections.Generic;
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

		private BoxCollider exclusionZone;
		private BoxCollider inclusionZone;
		private Collider[] includedColliders = new Collider[64];
		private int includedColliderCount = 0;

		private Transform warpedCollidersParent;
		private List<Collider> warpedColliders = new List<Collider>();
		private bool warpedInitialized = false; // TODO synchronize warped colliders elegantly
		private bool synchronized = false;

		private void OnEnable() {
			renderer = transform.GetComponentInChildren<Renderer>();
			collider = GetComponent<Collider>();
			exclusionZone = transform.Find("ExclusionZone").GetComponent<BoxCollider>();
			exclusionZone.enabled = false;
			inclusionZone = transform.Find("InclusionZone").GetComponent<BoxCollider>();
			inclusionZone.enabled = false;
			ComputeCorners();
			PortalsManager.I.RegisterPortal(this);
		}

		private void Update() {
			ComputeCorners();
			Synchronize();
			if (!warpedInitialized && outputPortal.synchronized) {
				InitializeWarpedColliders();
				warpedInitialized = true;
			}
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
			synchronized = true;
			worldToPortal = portalMirroring * transform.worldToLocalMatrix;
			worldToPortalWorld = outputPortal.transform.localToWorldMatrix * portalMirroring * transform.worldToLocalMatrix;
			portalWorldToWorld = transform.localToWorldMatrix * portalMirroringInverse * outputPortal.transform.worldToLocalMatrix;
			warpMatrix = outputPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix;
			plane = new Plane(-transform.forward, transform.position);
		}

		public void InitializeWarpedColliders() {
			GameObject warpedColGO = new GameObject("WarpedColliders");
			warpedCollidersParent = warpedColGO.transform;
			warpedCollidersParent.SetParent(transform);
			includedColliderCount = Physics.OverlapBoxNonAlloc(outputPortal.inclusionZone.transform.position, outputPortal.inclusionZone.size / 2f, includedColliders, outputPortal.inclusionZone.transform.rotation, ~LayerMask.GetMask("Portals", "Portalable", "Player"));
			for (int i = 0; i < includedColliderCount; i++) {
				warpedColliders.Add(CreateWarpedCopy(includedColliders[i]));
			}
		}

		private Collider CreateWarpedCopy(Collider col) {
			GameObject go = new GameObject("WarpedCollider");
			Transform warpedTransform = go.transform;
			warpedTransform.SetParent(warpedCollidersParent);
			warpedTransform.position = outputPortal.TransformPosition(col.transform.position);
			//warpedTransform.rotation = Quaternion.LookRotation(outputPortal.TransformDirection(col.transform.forward), outputPortal.TransformDirection(col.transform.up));
			warpedTransform.localScale = col.transform.lossyScale; // Assumes no scaling from any of the warped object parents
			Collider warpedCol = go.AddComponent(col.GetType()) as Collider;
			warpedCol.CopyFrom(col);
			return warpedCol;
		}

		public void SetWarpedIgnored(Collider col, bool ignored) {
			foreach (Collider warped in warpedColliders) {
				Physics.IgnoreCollision(col, warped, ignored);
			}
		}
		
		public void GetExcludedColliders(ref Collider[] excluded, ref int excludedCount) {
			excludedCount = Physics.OverlapBoxNonAlloc(exclusionZone.transform.position, exclusionZone.size / 2f, excluded, exclusionZone.transform.rotation, ~LayerMask.GetMask("Portals"));
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
