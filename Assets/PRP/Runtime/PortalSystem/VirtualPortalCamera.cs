﻿using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PRP.PortalSystem {
	public struct VirtualPortalCamera {

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct CoreVirtualCameraValues {
			public int filterMode;
			public uint cullingMask;
			public int guid;
			public int renderImmediateObjects;
		};

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct VirtualCameraProperties {
			public const int kNumLayers = 32;

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
			public Vector3 cameraEuler; // In radians
			public Vector3 velocity;

			public float farPlaneWorldSpaceLength;

			public uint rendererCount;

			internal fixed float _shadowCullPlanes[6 * 4];
			internal fixed float _cameraCullPlanes[6 * 4];

			public float baseFarDistance;

			public Vector3 shadowCullCenter;
			internal fixed float layerCullDistances[kNumLayers];
			int layerCullSpherical;

			public CoreVirtualCameraValues coreCameraValues;
			public uint cameraType;
			private int projectionIsOblique;
		}


		public Portal outputPortal;
		private Camera referenceCamera;
		public Vector3 position;
		public VirtualCameraProperties properties;

		public Plane[] frustrumPlanes;
		// Corners in camera space
		public Vector3 outputPortalTopLeft;
		public Vector3 outputPortalBotLeft;
		public Vector3 outputPortalTopRight;
		public Vector3 outputPortalBotRight;
		public bool narrowedFrustum;
		
		public Matrix4x4 worldToCamera {
			get {
				return properties.worldToCamera;
			}
			set {
				properties.worldToCamera = value;
				UpdateMatricesFromWTC();
			}
		}
		public Matrix4x4 projectionMatrix {
			get {
				return properties.implicitProjection;
			}
			set {
				properties.implicitProjection = value;
			}
		}

		public VirtualPortalCamera(Camera camera, Portal outPortal) {
			referenceCamera = camera;
			position = Vector3.zero;
			properties = new VirtualCameraProperties();
			frustrumPlanes = new Plane[6];
			outputPortal = outPortal;
			narrowedFrustum = false;
			outputPortalTopLeft = Vector3.zero;
			outputPortalBotLeft = Vector3.zero;
			outputPortalTopRight = Vector3.zero;
			outputPortalBotRight = Vector3.zero;
			SetReferenceCamera(camera);
		}

		private void UpdateMatricesFromWTC() {
			properties.cameraToWorld = properties.worldToCamera.inverse;
			properties.cameraWorldToClip = properties.actualWorldToClip = properties.implicitProjection * properties.worldToCamera;
			properties.cameraClipToWorld = properties.cameraToWorld * properties.implicitProjection.inverse;
			unsafe {
				// 0: left  1: right  2: down  3: up  4: near  5: far
				GeometryUtility.CalculateFrustumPlanes(properties.cameraWorldToClip, frustrumPlanes);
				if (outputPortal != null) {
					if (!narrowedFrustum) {
						PRPMath.OrganizeCorners(ref properties.worldToCamera, ref properties.cameraWorldToClip, outputPortal.corners, outputPortal.middle, ref outputPortalBotLeft, ref outputPortalTopLeft, ref outputPortalTopRight, ref outputPortalBotRight);
						narrowedFrustum = true;
						PRPDebugger.debugPositions.Add(position + Vector3.up * 2f);
					}
					PRPMath.FrustumPlanesFromOrganizedCorners(ref properties.cameraToWorld, position, ref frustrumPlanes, outputPortalBotLeft, outputPortalTopLeft, outputPortalTopRight, outputPortalBotRight);
					PRPDebugger.debugPositions.Add(position + Vector3.up * 1f);
				}
				PRPDebugger.AddDebugCullingFrustum(frustrumPlanes, new Color(.5f, .5f, 0f, 1f));
				for (int i = 0; i < 6; i++) {
					properties._cameraCullPlanes[i * 4] = frustrumPlanes[i].normal.x;
					properties._cameraCullPlanes[i * 4 + 1] = frustrumPlanes[i].normal.y;
					properties._cameraCullPlanes[i * 4 + 2] = frustrumPlanes[i].normal.z;
					properties._cameraCullPlanes[i * 4 + 3] = frustrumPlanes[i].distance;
				}
				for (int i = 0; i < 6 * 4; i++) { // TODO is it usefull?
					properties._shadowCullPlanes[i] = properties._cameraCullPlanes[i];
				}
			}
		}

		public void SetReferenceCamera(Camera camera) {
			referenceCamera = camera;
			position = camera.transform.position;
			Matrix4x4 inverseProjection = camera.projectionMatrix.inverse;
			properties = new VirtualCameraProperties();
			properties.actualWorldToClip = camera.projectionMatrix * camera.worldToCameraMatrix;
			properties.baseFarDistance = camera.farClipPlane; // TODO check correct
			properties.cameraAspect = camera.aspect;
			properties.cameraClipToWorld = camera.cameraToWorldMatrix * inverseProjection;
			properties.cameraEuler = camera.transform.rotation.eulerAngles * Mathf.Deg2Rad;
			properties.cameraFar = camera.farClipPlane;
			properties.cameraNear = camera.nearClipPlane;
			properties.cameraToWorld = camera.cameraToWorldMatrix; // TODO use inverse of virtual worldToCamera
			properties.cameraType = (uint) camera.cameraType;
			properties.cameraWorldToClip = camera.projectionMatrix * camera.worldToCameraMatrix;
			properties.coreCameraValues = new CoreVirtualCameraValues() { cullingMask = (uint) camera.cullingMask, filterMode = (int) FilterMode.Point, guid = camera.GetInstanceID(), renderImmediateObjects = 0 }; // TODO what is it?
			properties.farPlaneWorldSpaceLength = camera.farClipPlane; // TODO what is it?
			properties.implicitProjection = camera.projectionMatrix;
			//properties.layerCullDistances // TODO what is it?
			unsafe {
				for (int i = 0; i < VirtualCameraProperties.kNumLayers; i++) {
					properties.layerCullDistances[i] = camera.farClipPlane;
				}
			}
			properties.projectionFar = camera.farClipPlane; // TODO check correct
			properties.projectionNear = camera.nearClipPlane; // TODO check correct
			properties.rendererCount = 0; // TODO what is it?
			properties.right = camera.transform.right;
			properties.screenRect = camera.pixelRect;
			properties.shadowCullCenter = position; // TODO check correct
			properties.stereoWorldToClipLeft = Matrix4x4.identity; // Should not be used
			properties.stereoWorldToClipRight = Matrix4x4.identity;
			properties.transformDirection = camera.transform.forward; // TODO what is it?
			properties.up = camera.transform.up;
			properties.velocity = Vector3.zero; // Should not be used
			properties.viewDir = camera.transform.forward;
			properties.worldToCamera = camera.worldToCameraMatrix;
			unsafe {
				GeometryUtility.CalculateFrustumPlanes(properties.actualWorldToClip, frustrumPlanes);
				for (int i = 0; i < 6; i++) {
					properties._cameraCullPlanes[i * 4] = frustrumPlanes[i].normal.x;
					properties._cameraCullPlanes[i * 4 + 1] = frustrumPlanes[i].normal.y;
					properties._cameraCullPlanes[i * 4 + 2] = frustrumPlanes[i].normal.z;
					properties._cameraCullPlanes[i * 4 + 3] = frustrumPlanes[i].distance;
				}
				for (int i = 0; i < 6 * 4; i++) { // TODO is it usefull?
					properties._shadowCullPlanes[i] = properties._cameraCullPlanes[i];
				}
			}
		}

		public CameraProperties GetCameraProperties() {
			object objProps = properties;
			return CopyStruct<CameraProperties>(ref objProps);
		}

		private static T CopyStruct<T>(ref object s1) {
			GCHandle handle = GCHandle.Alloc(s1, GCHandleType.Pinned);
			T typedStruct = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();
			return typedStruct;
		}

	}
}
