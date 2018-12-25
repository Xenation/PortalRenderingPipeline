using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class Portalable : MonoBehaviour {

		private class PortalableMeshFilter {
			public MeshFilter originalFilter;
			public MeshRenderer originalRenderer;
			public SlicableMesh originalMesh;
			public MeshFilter clonedFilter;
			public SlicableMesh clonedMesh;
			public Portal transporter;

			public PortalableMeshFilter(MeshFilter oriFilter, MeshRenderer oriRenderer) {
				originalFilter = oriFilter;
				originalRenderer = oriRenderer;
				originalMesh = new SlicableMesh(originalFilter.mesh);
			}

			public void CreatePortaledClone() {
				GameObject go = Instantiate(originalFilter.gameObject, originalFilter.transform);
				go.name = "PortalableMeshClone";
				go.hideFlags = HideFlags.HideAndDontSave;
				clonedFilter = go.GetComponent<MeshFilter>();
				clonedMesh = new SlicableMesh(clonedFilter.mesh);
				Portalable p = go.GetComponent<Portalable>();
				if (p != null) {
					Destroy(p);
				}
			}

			public void DestroyPortaledClone() {
				Destroy(clonedFilter.gameObject);
				clonedFilter = null;
				clonedMesh = null;
				originalMesh.Revert();
			}

			public void Update() {
				clonedFilter.gameObject.transform.position = transporter.TransformPosition(originalFilter.transform.position);
				clonedFilter.gameObject.transform.rotation = Quaternion.LookRotation(transporter.TransformDirection(originalFilter.transform.forward));
				Matrix4x4 toClonedLocal = clonedFilter.transform.worldToLocalMatrix;
				Matrix4x4 toOriginalLocal = originalFilter.transform.worldToLocalMatrix;
				Plane planeOut = new Plane(toClonedLocal.MultiplyVector(transporter.outputPortal.plane.normal), toClonedLocal.MultiplyPoint3x4(transporter.outputPortal.plane.ClosestPointOnPlane(Vector3.zero)));
				Plane planeIn = new Plane(toOriginalLocal.MultiplyVector(transporter.plane.normal), toOriginalLocal.MultiplyPoint3x4(transporter.plane.ClosestPointOnPlane(Vector3.zero)));
				clonedMesh.Slice(planeOut);
				originalMesh.Slice(planeIn);
			}

		}

		private Vector3 previousPosition;

		private List<PortalableMeshFilter> portalableFilters = new List<PortalableMeshFilter>();

		private void Awake() {
			previousPosition = transform.position;
			List<MeshRenderer> meshRenderer = new List<MeshRenderer>();
			transform.GetComponentsInChildren(meshRenderer);
			InitializeMeshes(meshRenderer);
		}

		private void Update() {
			Vector3 currentPosition = transform.position;
			Portal transporter;
			if (PortalsManager.I.CheckThroughPortal(previousPosition, currentPosition, out transporter)) {
				transform.position = currentPosition = transporter.TransformPosition(currentPosition);
				transform.rotation = Quaternion.LookRotation(transporter.TransformDirection(transform.forward));
			}
			previousPosition = currentPosition;

			UpdateMeshes();
		}

		private void InitializeMeshes(List<MeshRenderer> meshRenderers) {
			foreach (MeshRenderer meshRenderer in meshRenderers) {
				portalableFilters.Add(new PortalableMeshFilter(meshRenderer.GetComponent<MeshFilter>(), meshRenderer));
			}
		}

		private void UpdateMeshes() {
			foreach (PortalableMeshFilter portalableFilter in portalableFilters) {
				if (PortalsManager.I.CheckTouchingPortal(portalableFilter.originalRenderer.bounds, out portalableFilter.transporter)) {
					if (portalableFilter.clonedFilter == null) { // Starts Touching
						portalableFilter.CreatePortaledClone();
					}
					portalableFilter.Update();
				} else {
					if (portalableFilter.clonedFilter != null) { // Ends Touching
						portalableFilter.DestroyPortaledClone();
					}
				}
			}
		}

	}
}
