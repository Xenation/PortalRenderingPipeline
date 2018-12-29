using PRP.PortalSystem;
using System.Collections.Generic;
using UnityEngine;

namespace PRP {
	public static class PRPMath {

		public static Matrix4x4 ReflectionMatrix(Plane reflectionPlane) {
			Vector3 n = reflectionPlane.normal;
			float d = reflectionPlane.distance;
			//float a = n.x;
			//float b = n.y;
			//float c = n.z;
			return new Matrix4x4 {
				m00 = 1f - 2f * n.x * n.x,	m01 = -2f * n.x * n.y,		m02 = -2f * n.x * n.z,		m03 = -2f * n.x * d,
				m10 = -2f * n.x * n.y,		m11 = 1f - 2f * n.y * n.y,	m12 = -2f * n.y * n.z,		m13 = -2f * n.y * d,
				m20 = -2f * n.x * n.z,		m21 = -2f * n.y * n.z,		m22 = 1f - 2f * n.z * n.z,	m23 = -2f * n.z * d,
				m30 = 0f,					m31 = 0f,					m32 = 0f,					m33 = 1f
			};
		}

		public static void SetObliqueNearPlane(ref Matrix4x4 projMatrix, Vector4 plane) {
			Vector4 q;
			q.x = (Mathf.Sign(plane.x) + projMatrix.m02) / projMatrix.m00;
			q.y = (Mathf.Sign(plane.y) + projMatrix.m12) / projMatrix.m11;
			q.z = -1f;
			q.w = (1f + projMatrix.m22) / projMatrix.m23;

			Vector4 c = plane * (2f / Vector4.Dot(plane, q));

			projMatrix.m20 = c.x;
			projMatrix.m21 = c.y;
			projMatrix.m22 = c.z + 1f;
			projMatrix.m23 = c.w;
		}

		public static void SetObliqueNearPlane(ref Matrix4x4 projMatrix, Plane plane) {
			SetObliqueNearPlane(ref projMatrix, new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance));
		}

		
		public static void OrganizeCorners(ref Matrix4x4 worldToCamera, ref Matrix4x4 worldToClip, Vector3[] corners, Vector3 middle, ref Vector3 botLeft, ref Vector3 topLeft, ref Vector3 topRight, ref Vector3 botRight) {
			Vector3[] camSpaceCorners = {
				worldToCamera.MultiplyPoint3x4(corners[0]),
				worldToCamera.MultiplyPoint3x4(corners[1]),
				worldToCamera.MultiplyPoint3x4(corners[2]),
				worldToCamera.MultiplyPoint3x4(corners[3]),
			};
			Vector4[] clipSpaceCorners = {
				worldToClip * corners[0],
				worldToClip * corners[1],
				worldToClip * corners[2],
				worldToClip * corners[3],
			};
			Vector4 clipSpaceMiddle = worldToClip * middle;
			PRPDebugger.debugPositions.Add(middle);

			// We want top{left, right} bot{left, right}
			// Separate Top/Bottom first
			int tmp;
			List<int> topIndices = new List<int>(2);
			List<int> botIndices = new List<int>(2);
			List<int> onLine = new List<int>(2);
			float yMiddle = clipSpaceMiddle.y;
			for (int i = 0; i < 4; i++) {
				if (clipSpaceCorners[i].y > yMiddle) {
					topIndices.Add(i);
				} else if (clipSpaceCorners[i].y < yMiddle) {
					botIndices.Add(i);
				} else { // When on separation line
					onLine.Add(i);
				}
			}
			foreach (int i in onLine) { // Fix not ideal, may produce incorrect plane normals
				if (topIndices.Count < 2) {
					topIndices.Add(i);
				} else {
					botIndices.Add(i);
				}
			}

			// Separate Left/Right
			float xMiddleTop = (clipSpaceCorners[topIndices[0]].x + clipSpaceCorners[topIndices[1]].x) / 2f;
			if (clipSpaceCorners[topIndices[0]].x > xMiddleTop) {
				tmp = topIndices[0];
				topIndices[0] = topIndices[1];
				topIndices[1] = tmp;
			}
			float xMiddleBot = (clipSpaceCorners[botIndices[0]].x + clipSpaceCorners[botIndices[1]].x) / 2f;
			if (clipSpaceCorners[botIndices[0]].x > xMiddleBot) {
				tmp = botIndices[0];
				botIndices[0] = botIndices[1];
				botIndices[1] = tmp;
			}

			topLeft = camSpaceCorners[topIndices[0]];
			topRight = camSpaceCorners[topIndices[1]];
			botLeft = camSpaceCorners[botIndices[0]];
			botRight = camSpaceCorners[botIndices[1]];
		}

		public static void FrustumPlanesFromOrganizedCorners(ref Matrix4x4 cameraToWorld, Vector3 frustumOrigin, ref Plane[] planes, Vector3 botLeft, Vector3 topLeft, Vector3 topRight, Vector3 botRight) {
			Vector3 worldBotLeft = cameraToWorld.MultiplyPoint3x4(botLeft);
			Vector3 worldTopLeft = cameraToWorld.MultiplyPoint3x4(topLeft);
			Vector3 worldBotRight = cameraToWorld.MultiplyPoint3x4(botRight);
			Vector3 worldTopRight = cameraToWorld.MultiplyPoint3x4(topRight);
			PRPDebugger.debugPositions.Add(worldBotLeft);
			PRPDebugger.debugPositions.Add(worldTopLeft);
			PRPDebugger.debugPositions.Add(worldBotRight);
			PRPDebugger.debugPositions.Add(worldTopRight);
			// 0: left  1: right  2: down  3: up  4: near  5: far
			if (planes[0].GetSide(worldTopLeft) && planes[0].GetSide(worldBotLeft)) {
				planes[0] = new Plane(frustumOrigin, worldTopLeft, worldBotLeft);
			}
			if (planes[1].GetSide(worldBotRight) && planes[1].GetSide(worldTopRight)) {
				planes[1] = new Plane(frustumOrigin, worldBotRight, worldTopRight);
			}
			if (planes[2].GetSide(worldBotLeft) && planes[2].GetSide(worldBotRight)) {
				planes[2] = new Plane(frustumOrigin, worldBotLeft, worldBotRight);
			}
			if (planes[3].GetSide(worldTopRight) && planes[3].GetSide(worldTopLeft)) {
				planes[3] = new Plane(frustumOrigin, worldTopRight, worldTopLeft);
			}
		}

		public static bool IsInPlanes(Vector3 point, params Plane[] planes) {
			foreach (Plane plane in planes) {
				if (!plane.GetSide(point)) return false;
			}
			return true;
		}

		public static bool PlaneBoundsIntersects(Plane plane, Bounds bounds) {
			Vector3 center = bounds.center;
			Vector3 extents = bounds.extents;

			float projectedExtents = extents.x * Mathf.Abs(plane.normal.x) + extents.y * Mathf.Abs(plane.normal.y) + extents.z * Mathf.Abs(plane.normal.z);

			return Mathf.Abs(plane.GetDistanceToPoint(center)) <= projectedExtents;
		}

		public static bool PlanesIntersect(Plane p0, Plane p1, Plane p2, out Vector3 intersect) {
			float denom = Vector3.Dot(Vector3.Cross(p0.normal, p1.normal), p2.normal);
			if (denom == 0.0f) {
				intersect = Vector3.zero;
				return false;
			}

			intersect = (-(p0.distance * Vector3.Cross(p1.normal, p2.normal)) - (p1.distance * Vector3.Cross(p2.normal, p0.normal)) - (p2.distance * Vector3.Cross(p0.normal, p1.normal))) / denom;
			return true;
		}

	}
}
