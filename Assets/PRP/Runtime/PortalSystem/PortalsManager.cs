using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class PortalsManager {
		
		private static PortalsManager _instance;
		public static PortalsManager I {
			get {
				if (_instance == null) {
					_instance = new PortalsManager();
				}
				return _instance;
			}
		}


		private List<Portal> portals = new List<Portal>();

		public PortalsManager() {

		}

		public void RegisterPortal(Portal portal) {
			portals.Add(portal);
		}

		public void UnregisterPortal(Portal portal) {
			portals.Remove(portal);
		}

		public bool CheckThroughPortal(Vector3 previousPos, Vector3 currentPos, out Portal transportPortal) {
			if (previousPos == currentPos) {
				transportPortal = null;
				return false;
			}
			RaycastHit hit;
			Vector3 delta = currentPos - previousPos;
			float length = delta.magnitude;
			Ray ray = new Ray(previousPos, delta.normalized);
			foreach (Portal portal in portals) {
				if (Vector3.Dot(delta, portal.transform.forward) > 0f && portal.collider.Raycast(ray, out hit, length)) {
					transportPortal = portal;
					return true;
				}
			}
			transportPortal = null;
			return false;
		}

		public bool CheckTouchingPortal(Bounds worldBounds, out Portal transportPortal) {
			foreach (Portal portal in portals) {
				if (portal.collider.bounds.Intersects(worldBounds)) { // TODO Not Ideal
					transportPortal = portal;
					return true;
				}
			}
			transportPortal = null;
			return false;
		}

		public void GetPortalsInFrustum(Plane[] frustumCullingPlanes, ref List<Portal> visiblePortals, Portal excluded, Vector3 frustumOrigin) {
			visiblePortals.Clear();
			foreach (Portal portal in portals) {
				if (portal == excluded) continue;
				if (GeometryUtility.TestPlanesAABB(frustumCullingPlanes, portal.bounds) && Vector3.Dot(portal.plane.normal, portal.transform.position - frustumOrigin) < 0f) {
					visiblePortals.Add(portal);
				}
			}
		}

	}
}
