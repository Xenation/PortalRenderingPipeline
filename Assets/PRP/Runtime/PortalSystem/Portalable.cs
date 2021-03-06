﻿using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class Portalable : MonoBehaviour {

		private class PortalableElement {
			private PortalContact portaledClone;

			public GameObject original;
			public MeshFilter originalFilter;
			public MeshRenderer originalRenderer;
			public SlicableMesh originalMesh;
			public GameObject cloned;
			public MeshFilter clonedFilter;
			public MeshRenderer clonedRenderer;
			public SlicableMesh clonedMesh;

			public PortalableContext context = PortalableContext.Nominal;
			
			public PortalableElement(PortalContact portaledClone, GameObject ori) {
				this.portaledClone = portaledClone;
				original = ori;
				originalRenderer = original.GetComponent<MeshRenderer>();
				originalFilter = original.GetComponent<MeshFilter>();
				originalMesh = new SlicableMesh(originalFilter.mesh);
			}

			public void CreateClone() {
				cloned = PRPUtils.InstantiateDummy(original, original.transform.parent, typeof(InstancedColor));
				clonedRenderer = cloned.GetComponent<MeshRenderer>();
				clonedFilter = cloned.GetComponent<MeshFilter>();
				clonedMesh = new SlicableMesh(clonedFilter.mesh);
			}

			public void DestroyClone() {
				if (cloned == null) return;
				originalMesh.Revert();
				originalRenderer.enabled = true;
				cloned.transform.SetParent(null);
				Destroy(cloned);
				cloned = null;
				clonedRenderer = null;
				clonedFilter = null;
				clonedMesh = null;
			}

			public void Update() {
				Portal portal = portaledClone.portal;
				Matrix4x4 originalWTL = original.transform.worldToLocalMatrix;
				Vector3[] originalLocalCorners = {
					originalWTL.MultiplyPoint3x4(portal.corners[0]),
					originalWTL.MultiplyPoint3x4(portal.corners[1]),
					originalWTL.MultiplyPoint3x4(portal.corners[2]),
					originalWTL.MultiplyPoint3x4(portal.corners[3]),
				};
				PortalableContext nContext = originalMesh.GetContext(originalLocalCorners);
				if (context != nContext) { // Context Changes
					//Debug.Log(context + " --> " + nContext);
					switch (nContext) {
						case PortalableContext.Nominal:
							portaledClone.elementCountNonNominal--;
							if (context == PortalableContext.Between) {
								originalMesh.Revert();
								DestroyClone();
							} else if (context == PortalableContext.Portaled) {
								DestroyClone();
								originalRenderer.enabled = true;
							}
							break;
						case PortalableContext.Between:
							portaledClone.elementCountNonNominal++;
							if (context == PortalableContext.Nominal) {
								CreateClone();
							} else if (context == PortalableContext.Portaled) {
								originalRenderer.enabled = true;
							}
							break;
						case PortalableContext.Portaled:
							portaledClone.elementCountNonNominal++;
							if (context == PortalableContext.Between) {
								clonedMesh.Revert();
								originalMesh.Revert();
								originalRenderer.enabled = false;
							} else if (context == PortalableContext.Nominal) {
								originalRenderer.enabled = false;
								CreateClone();
							}
							break;
					}
				}
				context = nContext;

				if (context == PortalableContext.Between || context == PortalableContext.Portaled) {
					cloned.transform.position = portal.TransformPosition(original.transform.position);
					cloned.transform.rotation = Quaternion.LookRotation(portal.TransformDirection(original.transform.forward), portal.TransformDirection(original.transform.up));
				}

				if (context == PortalableContext.Between) {
					Matrix4x4 clonedWTL = cloned.transform.worldToLocalMatrix;
					Vector3[] clonedLocalCorners = {
						clonedWTL.MultiplyPoint3x4(portal.outputPortal.corners[0]),
						clonedWTL.MultiplyPoint3x4(portal.outputPortal.corners[1]),
						clonedWTL.MultiplyPoint3x4(portal.outputPortal.corners[2]),
						clonedWTL.MultiplyPoint3x4(portal.outputPortal.corners[3]),
					};

					clonedMesh.Slice(new Plane(clonedLocalCorners[0], clonedLocalCorners[1], clonedLocalCorners[2]));
					originalMesh.Slice(new Plane(originalLocalCorners[0], originalLocalCorners[1], originalLocalCorners[2]));
					
				}
			}

		}

		private class PortalContact {
			public GameObject original;
			public Portal portal;
			public List<PortalableElement> portalableElements = new List<PortalableElement>();
			public int elementCountNonNominal = 0;

			public List<Collider> subColliders;
			public Collider[] ignored = new Collider[32];
			public int ignoredCount = 0;

			public PortalContact(GameObject original, Portal p, List<Collider> subColliders) {
				this.original = original;
				portal = p;
				portal.GetExcludedColliders(ref ignored, ref ignoredCount);
				this.subColliders = subColliders;
				SetIgnoreState(true);
				InitializeElements(this.original);
			}

			private void InitializeElements(GameObject root) {
				if (root.GetComponent<MeshRenderer>() != null) {
					portalableElements.Add(new PortalableElement(this, root));
				}
				foreach (Transform child in root.transform) {
					InitializeElements(child.gameObject);
				}
			}

			private void SetIgnoreState(bool state) {
				foreach (Collider subCol in subColliders) {
					for (int i = 0; i < ignoredCount; i++) {
						Physics.IgnoreCollision(subCol, ignored[i], state);
					}
					portal.SetWarpedIgnored(subCol, !state);
				}
			}

			public void DestroyClone() {
				SetIgnoreState(false);
				foreach (PortalableElement portalableElem in portalableElements) {
					portalableElem.DestroyClone();
				}
			}

			public void Update() {
				foreach (PortalableElement portalableElem in portalableElements) {
					portalableElem.Update();
				}
			}
		}

		private Vector3 previousPosition;
		private List<MeshRenderer> renderers = new List<MeshRenderer>();
		private Dictionary<Portal, PortalContact> clones = new Dictionary<Portal, PortalContact>();
		private Rigidbody rb;
		private List<Collider> subColliders = new List<Collider>();

		private Collider[] ignoredColliders = new Collider[32];
		private int ignoredColliderCount = 0;

		private void Start() {
			previousPosition = transform.position;
			rb = GetComponent<Rigidbody>();
			transform.GetComponentsInChildren(renderers);
			transform.GetComponentsInChildren(subColliders);
			foreach (Collider col in subColliders) {
				PortalsManager.I.IgnoreFromAllWarpedColliders(col);
			}
		}

		private void Update() {
			Vector3 currentPosition = transform.position;
			Portal transporter;
			if (PortalsManager.I.CheckThroughPortal(previousPosition, currentPosition, out transporter)) {
				transform.position = currentPosition = transporter.TransformPosition(currentPosition);
				transform.rotation = Quaternion.LookRotation(transporter.TransformDirection(transform.forward), transporter.TransformDirection(transform.up));
				if (rb != null) {
					rb.velocity = transporter.TransformDirection(rb.velocity);
				}
			}
			previousPosition = currentPosition;

			UpdateTouching();
			UpdateClones();
		}

		private void UpdateTouching() {
			List<Portal> touched = new List<Portal>();
			foreach (MeshRenderer rend in renderers) {
				Portal portal;
				if (PortalsManager.I.CheckTouchingPortal(rend.bounds, out portal)) {
					touched.Add(portal);
				}
			}
			List<Portal> toRemove = new List<Portal>();
			foreach (KeyValuePair<Portal, PortalContact> pair in clones) {
				if (!touched.Contains(pair.Key)) { // Ends Touching
					//Debug.Log("Touch end");
					pair.Value.DestroyClone();
					toRemove.Add(pair.Key);
					//rb.detectCollisions = true;
				}
			}
			foreach (Portal rm in toRemove) {
				clones.Remove(rm);
			}
			foreach (Portal portal in touched) {
				if (!clones.ContainsKey(portal)) { // Start touching
					//Debug.Log("Touch start");
					clones.Add(portal, new PortalContact(gameObject, portal, subColliders));
					//rb.detectCollisions = false;
				}
			}
		}

		private void UpdateClones() {
			foreach (PortalContact clone in clones.Values) {
				clone.Update();
			}
		}

	}
}
