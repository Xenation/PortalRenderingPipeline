using PRP.PortalSystem;
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


		public static void NarrowFrustumPlanes(Vector3 frustumOrigin, ref Plane[] planes, Bounds narrowingBounds, ref Matrix4x4 worldToClip, ref Matrix4x4 clipToWorld) {
			Vector3 minClip = worldToClip.MultiplyPoint3x4(narrowingBounds.min);
			Vector3 maxClip = worldToClip.MultiplyPoint3x4(narrowingBounds.max);
			//Vector3 minCamera, maxCamera;
			if (minClip.x > maxClip.x) {
				float tmpX = minClip.x;
				minClip.x = maxClip.x;
				maxClip.x = tmpX;
			}
			if (minClip.y > maxClip.y) {
				float tmpY = minClip.y;
				minClip.y = maxClip.y;
				maxClip.y = tmpY;
			}
			Vector3 worldBotLeft = clipToWorld.MultiplyPoint3x4(minClip);
			Vector3 worldTopLeft = clipToWorld.MultiplyPoint3x4(new Vector3(minClip.x, maxClip.y, minClip.z));
			Vector3 worldTopRight = clipToWorld.MultiplyPoint3x4(maxClip);
			Vector3 worldBotRight = clipToWorld.MultiplyPoint3x4(new Vector3(maxClip.x, minClip.y, maxClip.z));
			PRPDebugger.debugPositions.Add(worldBotLeft);
			PRPDebugger.debugPositions.Add(worldTopLeft);
			PRPDebugger.debugPositions.Add(worldBotRight);
			PRPDebugger.debugPositions.Add(worldTopRight);
			// 0: left  1: right  2: down  3: up  4: near  5: far
			planes[0] = new Plane(frustumOrigin, worldTopLeft, worldBotLeft);
			planes[1] = new Plane(frustumOrigin, worldBotRight, worldTopRight);
			planes[2] = new Plane(frustumOrigin, worldBotLeft, worldBotRight);
			planes[3] = new Plane(frustumOrigin, worldTopRight, worldTopLeft);
		}

		public static void NarrowFrustumPlanesCam(Vector3 frustumOrigin, ref Plane[] planes, Bounds narrowingBounds, ref Matrix4x4 worldToCamera, ref Matrix4x4 cameraToWorld) {
			Vector3 minCamera = worldToCamera.MultiplyPoint3x4(narrowingBounds.min);
			Vector3 maxCamera = worldToCamera.MultiplyPoint3x4(narrowingBounds.max);
			//Vector3 minCamera, maxCamera;
			if (minCamera.x > maxCamera.x) {
				float tmpX = minCamera.x;
				minCamera.x = maxCamera.x;
				maxCamera.x = tmpX;
			}
			if (minCamera.y > maxCamera.y) {
				float tmpY = minCamera.y;
				minCamera.y = maxCamera.y;
				maxCamera.y = tmpY;
			}
			if (minCamera.z < maxCamera.z) {
				minCamera.z = maxCamera.z;
			} else {
				maxCamera.z = minCamera.z;
			}
			Vector3 worldBotLeft = cameraToWorld.MultiplyPoint3x4(minCamera);
			Vector3 worldTopLeft = cameraToWorld.MultiplyPoint3x4(new Vector3(minCamera.x, maxCamera.y, minCamera.z));
			Vector3 worldTopRight = cameraToWorld.MultiplyPoint3x4(maxCamera);
			Vector3 worldBotRight = cameraToWorld.MultiplyPoint3x4(new Vector3(maxCamera.x, minCamera.y, maxCamera.z));
			PRPDebugger.debugPositions.Add(worldBotLeft);
			PRPDebugger.debugPositions.Add(worldTopLeft);
			PRPDebugger.debugPositions.Add(worldBotRight);
			PRPDebugger.debugPositions.Add(worldTopRight);
			// 0: left  1: right  2: down  3: up  4: near  5: far
			planes[0] = new Plane(frustumOrigin, worldTopLeft, worldBotLeft);
			planes[1] = new Plane(frustumOrigin, worldBotRight, worldTopRight);
			planes[2] = new Plane(frustumOrigin, worldBotLeft, worldBotRight);
			planes[3] = new Plane(frustumOrigin, worldTopRight, worldTopLeft);
		}

		public static void CameraSpaceMinMax(Bounds bounds, ref Matrix4x4 worldToCamera, ref Vector3 min, ref Vector3 max) {
			min = worldToCamera.MultiplyPoint3x4(bounds.min);
			max = worldToCamera.MultiplyPoint3x4(bounds.max);
			if (min.x > max.x) {
				float tmpX = min.x;
				min.x = max.x;
				max.x = tmpX;
			}
			if (min.y > max.y) {
				float tmpY = min.y;
				min.y = max.y;
				max.y = tmpY;
			}
			if (min.z < max.z) {
				min.z = max.z;
			} else {
				max.z = min.z;
			}
		}

		public static void FrustumPlanesFromCameraSpaceMinMax(ref Matrix4x4 cameraToWorld, Vector3 frustumOrigin, ref Plane[] planes, Vector3 min, Vector3 max) {
			Vector3 worldBotLeft = cameraToWorld.MultiplyPoint3x4(min);
			Vector3 worldTopLeft = cameraToWorld.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
			Vector3 worldTopRight = cameraToWorld.MultiplyPoint3x4(max);
			Vector3 worldBotRight = cameraToWorld.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
			PRPDebugger.debugPositions.Add(worldBotLeft);
			PRPDebugger.debugPositions.Add(worldTopLeft);
			PRPDebugger.debugPositions.Add(worldBotRight);
			PRPDebugger.debugPositions.Add(worldTopRight);
			// 0: left  1: right  2: down  3: up  4: near  5: far
			planes[0] = new Plane(frustumOrigin, worldTopLeft, worldBotLeft);
			planes[1] = new Plane(frustumOrigin, worldBotRight, worldTopRight);
			planes[2] = new Plane(frustumOrigin, worldBotLeft, worldBotRight);
			planes[3] = new Plane(frustumOrigin, worldTopRight, worldTopLeft);
		}
	}
}
