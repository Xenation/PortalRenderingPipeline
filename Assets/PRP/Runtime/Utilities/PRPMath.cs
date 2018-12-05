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

	}
}
