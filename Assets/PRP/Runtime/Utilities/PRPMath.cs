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

	}
}
