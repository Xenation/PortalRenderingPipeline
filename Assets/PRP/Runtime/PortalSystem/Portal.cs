using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PRP.PortalSystem {
	[RequireComponent(typeof(Renderer))]
	public class Portal : MonoBehaviour {

		private static Matrix4x4 portalMirroring = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);

		//public PortalRenderInfo info;
		public Portal outputPortal;
		

		[System.NonSerialized] public new Renderer renderer;

		private Matrix4x4 worldToPortal;

		private void OnEnable() {
			renderer = transform.GetComponentInChildren<Renderer>();
		}

		private void OnPreRender() {
			Synchronize();
		}

		private void OnDisable() {

		}

		public void Synchronize() {
			worldToPortal = portalMirroring * transform.worldToLocalMatrix;
		}

		public Matrix4x4 TransformMatrix(Matrix4x4 mat) {
			return mat * worldToPortal * outputPortal.transform.localToWorldMatrix;
		}

		public Vector3 TransformPosition(Vector3 p) {
			return outputPortal.transform.localToWorldMatrix.MultiplyPoint3x4(worldToPortal.MultiplyPoint3x4(p));
		}
		
		public Vector3 TransformDirection(Vector3 d) {
			return outputPortal.transform.localToWorldMatrix.MultiplyVector(worldToPortal.MultiplyVector(d));
		}

	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct HCamProp {
		private const int kNumLayers = 32;

		public Rect screenRect;
		public Vector3 viewDir;
		public float projectionNear;
		public float projectionFar;
		public float cameraNear;
		public float cameraFar;
		public float cameraAspect;

		public Matrix4x4 cameraToWorld;
		public Matrix4x4 actualWorldToClip;
		public Matrix4x4 cameraClipToWorld;
		public Matrix4x4 cameraWorldToClip;
		public Matrix4x4 implicitProjection;
		public Matrix4x4 stereoWorldToClipLeft;
		public Matrix4x4 stereoWorldToClipRight;
		public Matrix4x4 worldToCamera;

		public Vector3 up;
		public Vector3 right;
		public Vector3 transformDirection;
		public Vector3 cameraEuler;
		public Vector3 velocity;

		public float farPlaneWorldSpaceLength;

		public uint rendererCount;
		
		internal fixed float _shadowCullPlanes[6 * 4];
		internal fixed float _cameraCullPlanes[6 * 4];

		public float baseFarDistance;

		public Vector3 shadowCullCenter;
		internal fixed float layerCullDistances[kNumLayers];
		int layerCullSpherical;

		public CoreCameraValues coreCameraValues;
		public uint cameraType;
		private int projectionIsOblique;

		public static T CopyStruct<T>(ref object s1) {
			GCHandle handle = GCHandle.Alloc(s1, GCHandleType.Pinned);
			T typedStruct = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();
			return typedStruct;
		}
	}
}
