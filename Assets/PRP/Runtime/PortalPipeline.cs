using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using PRP.PortalSystem;
using System.Collections.Generic;

namespace PRP {
	public class PortalPipeline : RenderPipeline {

		private struct PortalContext {
			public ScriptableRenderContext renderContext;
			public Camera camera;
			public VirtualPortalCamera virtualCamera;

			public PortalContext CopyWith(Matrix4x4 nWorldToCamera, Vector3 nPosition, Portal nOutput) {
				PortalContext portalContext = new PortalContext() { renderContext = renderContext, camera = camera, virtualCamera = virtualCamera }; // TODO switch virtual camera to struct to avoid garbage
				portalContext.virtualCamera.position = nPosition;
				portalContext.virtualCamera.outputPortal = nOutput;
				portalContext.virtualCamera.worldToCamera = nWorldToCamera;
				return portalContext;
			}
		}

		private int maxPortalDepth;
		private bool debugCameras;

		private CullResults cull;
		private DrawRendererFlags drawFlags;
		private CommandBuffer buffer = new CommandBuffer { name = "Render Camera" };
		private Material errorMaterial;
		private Material stencilIncreaseMaterial;
		private Material stencilDecreaseMaterial;
		private Material stencilIncreaseFiller;
		private Material stencilDecreaseFiller;
		private Material depthOnly;

		private Mesh fullScreenQuad = new Mesh() {
			vertices = new Vector3[] {
				new Vector3(-1, 1, 0),
				new Vector3(1, 1, 0),
				new Vector3(1, -1, 0),
				new Vector3(-1, -1, 0)
			}
		};
		private PRPDebugger debugger;

		private const int MAX_VISIBLE_LIGHTS = 16;

		private static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
		private static int visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
		private static int visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
		private static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");

		Vector4[] visibleLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightDirectionsOrPositions = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightAttenuations = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightSpotDirections = new Vector4[MAX_VISIBLE_LIGHTS];


		public PortalPipeline(bool dynamicBatching, bool instancing, int depth, bool debugCameras) {
			maxPortalDepth = depth;

			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader")) { hideFlags = HideFlags.HideAndDontSave };
			stencilIncreaseMaterial = new Material(Shader.Find("Hidden/PRP/StencilIncrease")) { hideFlags = HideFlags.HideAndDontSave };
			stencilDecreaseMaterial = new Material(Shader.Find("Hidden/PRP/StencilDecrease")) { hideFlags = HideFlags.HideAndDontSave };
			stencilIncreaseFiller = new Material(Shader.Find("Hidden/PRP/StencilIncreaseFiller")) { hideFlags = HideFlags.HideAndDontSave };
			stencilDecreaseFiller = new Material(Shader.Find("Hidden/PRP/StencilDecreaseFiller")) { hideFlags = HideFlags.HideAndDontSave };
			depthOnly = new Material(Shader.Find("Hidden/PRP/DepthOnly")) { hideFlags = HideFlags.HideAndDontSave };

			fullScreenQuad.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);

			this.debugCameras = debugCameras;
			if (debugCameras) {
				GameObject debObj = new GameObject("PRP Debugger (Not Saved)");
				debObj.hideFlags = HideFlags.DontSave;
				debugger = debObj.AddComponent<PRPDebugger>();
			}

			if (dynamicBatching) {
				drawFlags = DrawRendererFlags.EnableDynamicBatching;
			}
			if (instancing) {
				drawFlags |= DrawRendererFlags.EnableInstancing;
			}
		}

		public override void Dispose() {
			base.Dispose();
			buffer.Release();

			if (debugCameras) {
				if (Application.isPlaying) {
					Object.Destroy(debugger.gameObject);
				} else {
					Object.DestroyImmediate(debugger.gameObject);
				}
			}
		}

		public override void Render(ScriptableRenderContext renderContext, Camera[] cameras) {
			base.Render(renderContext, cameras);

			for (int i = 0; i < cameras.Length; i++) {
				if (cameras[i].GetComponent<PortalViewingCamera>() == null) {
					Render(renderContext, cameras[i]);
				} else {
					RenderPortals(renderContext, cameras[i]);
				}
			}
		}

		public void Render(ScriptableRenderContext renderContext, Camera camera) {
			// Culling
			ScriptableCullingParameters cullingParameters;
			if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
				return;
			}
#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView) {
				ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			}
#endif
			CullResults.Cull(ref cullingParameters, renderContext, ref cull);
			ConfigureLights(cull);

			// Cam setup
			renderContext.SetupCameraProperties(camera);

			// Clear
			CameraClearFlags clearFlags = camera.clearFlags;
			buffer.ClearRenderTarget((clearFlags & CameraClearFlags.Depth) != 0, (clearFlags & CameraClearFlags.Color) != 0, camera.backgroundColor);
			buffer.BeginSample("Render Camera");
			buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
			buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
			buffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations);
			buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections);
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			
			// Draw Opaque
			DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit")) { flags = drawFlags, rendererConfiguration = RendererConfiguration.PerObjectLightIndices8};
			drawSettings.sorting.flags = SortFlags.CommonOpaque;
			FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);
			filterSettings.renderQueueRange = RenderQueueRange.opaque;
			renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

			// Draw Skybox
			renderContext.DrawSkybox(camera);

			// Draw Transparent
			drawSettings.sorting.flags = SortFlags.CommonTransparent;
			filterSettings.renderQueueRange = RenderQueueRange.transparent;
			renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// Draw Default
			DrawDefaultPipeline(renderContext, camera);
#endif

			// Submit
			buffer.EndSample("Render Camera");
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			renderContext.Submit();
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		private void DrawDefaultPipeline(ScriptableRenderContext renderContext, Camera camera) {
			DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("ForwardBase"));
			drawSettings.SetShaderPassName(1, new ShaderPassName("PrepassBase"));
			drawSettings.SetShaderPassName(2, new ShaderPassName("Always"));
			drawSettings.SetShaderPassName(3, new ShaderPassName("Vertex"));
			drawSettings.SetShaderPassName(4, new ShaderPassName("VertexLMRGBM"));
			drawSettings.SetShaderPassName(5, new ShaderPassName("VertexLM"));
			drawSettings.SetOverrideMaterial(errorMaterial, 0);
			FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);
			renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
		}
#endif

		//private PortalContext portalContext;
		private void RenderPortals(ScriptableRenderContext renderContext, Camera camera) {

			// Clear
			CameraClearFlags clearFlags = camera.clearFlags;
			buffer.SetViewport(camera.pixelRect);
			buffer.ClearRenderTarget((clearFlags & CameraClearFlags.Depth) != 0, (clearFlags & CameraClearFlags.Color) != 0, camera.backgroundColor);
			//buffer.BeginSample("Render Portals");
			//renderContext.ExecuteCommandBuffer(buffer);
			//buffer.Clear();

			// Portal Context
			PortalContext basePortalContext = new PortalContext() { renderContext = renderContext, camera = camera, virtualCamera = new VirtualPortalCamera(camera, null) };
			PortalViewingCamera portalCamera = camera.GetComponent<PortalViewingCamera>();
			if (portalCamera == null) return;
			basePortalContext.virtualCamera.worldToCamera = camera.worldToCameraMatrix;
			basePortalContext.virtualCamera.position = camera.transform.position;
			if (debugCameras) {
				debugger.ClearCameras();
				PRPDebugger.ClearDebugPlanes();
				PRPDebugger.debugPositions.Clear();
				debugger.AddCamera(basePortalContext.virtualCamera);
			}
			
			List<Portal> visiblePortals = new List<Portal>();
			PortalsManager.I.GetPortalsInFrustum(basePortalContext.virtualCamera.frustrumPlanes, ref visiblePortals, null);
			foreach (Portal p in visiblePortals) {
				p.renderer = p.transform.GetComponentInChildren<MeshRenderer>();
			}

			if (visiblePortals.Count != 0 && maxPortalDepth > 1) {
				RenderLayer(0, maxPortalDepth, null, visiblePortals, basePortalContext);
			}

			RenderBaseLayer(basePortalContext, visiblePortals);
			
			// Submit
			//buffer.EndSample("Render Portals");
			//renderContext.ExecuteCommandBuffer(buffer);
			//buffer.Clear();
			renderContext.Submit();
		}

		private void RenderLayer(int depth, int maxDepth, Portal viewingPortal, List<Portal> visiblePortals, PortalContext context) {
			//buffer.BeginSample("Layer " + depth);
			//context.renderContext.ExecuteCommandBuffer(buffer);
			//buffer.Clear();
			List<Portal> nextVisible = new List<Portal>();
			foreach (Portal portal in visiblePortals) {
				if (viewingPortal != null && portal == viewingPortal.outputPortal) continue;

				// Update Context
				PortalContext layerContext = context.CopyWith(portal.TransformInverseMatrix(context.virtualCamera.worldToCamera), portal.TransformPosition(context.virtualCamera.position), portal.outputPortal);
				//Matrix4x4 lay2Proj = lay2PortalContext.camera.projectionMatrix;
				//PRPMath.SetObliqueNearPlane(ref lay2Proj, new Plane(lay2PortalContext.virtualCamera.worldToCamera.MultiplyVector(lay2Portal.portalPlane.normal), lay2PortalContext.virtualCamera.worldToCamera.MultiplyPoint3x4(lay2Portal.transform.position)));
				//lay2PortalContext.virtualCamera.projectionMatrix = lay2Proj;

				//StencilIncreaseFill(layerContext);
				//StencilDecrease(context, lay2Portal);
				buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilIncreaseFiller);
				buffer.SetViewMatrix(context.virtualCamera.worldToCamera);
				buffer.SetProjectionMatrix(context.virtualCamera.projectionMatrix);
				buffer.DrawRenderer(portal.renderer, stencilDecreaseMaterial);

				if (debugCameras) {
					debugger.AddCamera(layerContext.virtualCamera);
				}

				//Portal[] nextVisible = visiblePortals; // tmp
				PortalsManager.I.GetPortalsInFrustum(layerContext.virtualCamera.frustrumPlanes, ref nextVisible, portal.outputPortal);
				if (nextVisible.Count != 0 && depth + 1 < maxDepth) {
					RenderLayer(depth + 1, maxDepth, portal, visiblePortals, layerContext);
				} else {
					context.renderContext.ExecuteCommandBuffer(buffer);
					buffer.Clear();
				}

				buffer.SetViewport(context.camera.pixelRect); // tmp
				buffer.SetViewProjectionMatrices(layerContext.virtualCamera.worldToCamera, layerContext.virtualCamera.projectionMatrix);
				RenderPortalLayer(layerContext);

				//StencilDecreaseFill(layerContext);
				//WriteDepthOnly(context, lay2Portal);
				buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilDecreaseFiller);
				buffer.SetViewProjectionMatrices(context.virtualCamera.worldToCamera, context.virtualCamera.projectionMatrix);
				buffer.DrawRenderer(portal.renderer, depthOnly);
				context.renderContext.ExecuteCommandBuffer(buffer);
				buffer.Clear();
			}
			//buffer.EndSample("Layer " + depth);
			//context.renderContext.ExecuteCommandBuffer(buffer);
			//buffer.Clear();
		}

		private void RenderBaseLayer(PortalContext portalContext, List<Portal> firstLayerPortals) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.SetViewProjectionMatrices(portalContext.virtualCamera.worldToCamera, portalContext.virtualCamera.projectionMatrix);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			RenderPortalLayer(portalContext);
		}

		private void RenderPortalLayer(PortalContext previousPortalContext, PortalContext portalContext, Portal portal, Portal[] nextLayerPortals) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.SetViewProjectionMatrices(portalContext.virtualCamera.worldToCamera, portalContext.virtualCamera.projectionMatrix);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			RenderPortalLayer(portalContext);
		}

		private void RenderPortalLayer(PortalContext portalContext) {

			// Culling
			ScriptableCullingParameters cullingParameters = new ScriptableCullingParameters();
			if (!CullResults.GetCullingParameters(portalContext.camera, out cullingParameters)) {
				return;
			}
			//cullingParameters.cullingMatrix = portalContext.camera.projectionMatrix * portalContext.virtualCamera.worldToCamera;
			//cullingParameters.position = portalContext.virtualCamera.position;
			//cullingParameters.cameraProperties = portalContext.virtualCamera.GetCameraProperties();
			GeometryUtility.CalculateFrustumPlanes(portalContext.virtualCamera.properties.actualWorldToClip, portalContext.virtualCamera.frustrumPlanes);
			for (int i = 0; i < portalContext.virtualCamera.frustrumPlanes.Length; i++) {
				cullingParameters.SetCullingPlane(i, portalContext.virtualCamera.frustrumPlanes[i]);
			}
			if (portalContext.virtualCamera.outputPortal != null) { // Set The Near Plane to the portal's plane
				cullingParameters.SetCullingPlane(4, portalContext.virtualCamera.outputPortal.plane);
			}
			CullResults.Cull(ref cullingParameters, portalContext.renderContext, ref cull);

			// Setup Lights
			ConfigureLights(cull);
			//buffer.BeginSample("Render Portal Layer");
			buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
			buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
			buffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations);
			buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();

			// Draw Opaque
			DrawRendererSettings drawSettings = new DrawRendererSettings(portalContext.camera, new ShaderPassName("PRP")) { flags = drawFlags, rendererConfiguration = RendererConfiguration.PerObjectLightIndices8 };
			drawSettings.sorting.worldToCameraMatrix = portalContext.virtualCamera.worldToCamera;
			drawSettings.sorting.cameraPosition = portalContext.virtualCamera.position;
			drawSettings.sorting.flags = SortFlags.CommonOpaque;
			FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);
			filterSettings.renderQueueRange = RenderQueueRange.opaque;
			portalContext.renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

			// Draw Skybox
			portalContext.renderContext.DrawSkybox(portalContext.camera);

			// Draw Transparent
			drawSettings.sorting.flags = SortFlags.CommonTransparent;
			filterSettings.renderQueueRange = RenderQueueRange.transparent;
			portalContext.renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

			//buffer.EndSample("Render Portal Layer");
			//portalContext.renderContext.ExecuteCommandBuffer(buffer);
			//buffer.Clear();
		}

		private void StencilMark(PortalContext portalContext, Portal portal) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.SetViewMatrix(portalContext.virtualCamera.worldToCamera);
			buffer.SetProjectionMatrix(portalContext.virtualCamera.projectionMatrix);
			buffer.DrawRenderer(portal.renderer, stencilIncreaseMaterial);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilDecrease(PortalContext portalContext, Portal portal) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.SetViewMatrix(portalContext.virtualCamera.worldToCamera);
			buffer.SetProjectionMatrix(portalContext.virtualCamera.projectionMatrix);
			buffer.DrawRenderer(portal.renderer, stencilDecreaseMaterial);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void WriteDepthOnly(PortalContext portalContext, Portal portal) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.SetViewMatrix(portalContext.virtualCamera.worldToCamera);
			buffer.SetProjectionMatrix(portalContext.virtualCamera.projectionMatrix);
			buffer.DrawRenderer(portal.renderer, depthOnly);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilIncreaseFill(PortalContext portalContext) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilIncreaseFiller);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilDecreaseFill(PortalContext portalContext) {
			buffer.SetViewport(portalContext.camera.pixelRect); // tmp
			buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilDecreaseFiller);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void ConfigureLights(CullResults cullResults) {
			Vector4 v;
			for (int i = 0; i < cullResults.visibleLights.Count && i < MAX_VISIBLE_LIGHTS; i++) {
				VisibleLight light = cullResults.visibleLights[i];
				visibleLightColors[i] = light.finalColor;
				visibleLightAttenuations[i] = Vector4.zero;
				visibleLightAttenuations[i].w = 1f;
				switch (light.lightType) {
					case LightType.Directional:
						v = light.localToWorld.GetColumn(2);
						v.x = -v.x;
						v.y = -v.y;
						v.z = -v.z;
						visibleLightDirectionsOrPositions[i] = v;
						break;
					default:
					case LightType.Point:
						visibleLightDirectionsOrPositions[i] = light.localToWorld.GetColumn(3);
						visibleLightAttenuations[i].x = 1f / Mathf.Max(light.range * light.range, 0.00001f);
						break;
					case LightType.Spot:
						v = light.localToWorld.GetColumn(2);
						v.x = -v.x;
						v.y = -v.y;
						v.z = -v.z;
						visibleLightSpotDirections[i] = v;
						float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
						float outerCos = Mathf.Cos(outerRad);
						float outerTan = Mathf.Tan(outerRad);
						float innerCos = Mathf.Cos(Mathf.Atan((46f / 64f) * outerTan));
						float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
						visibleLightAttenuations[i].z = 1f / angleRange;
						visibleLightAttenuations[i].w = -outerCos * visibleLightAttenuations[i].z;
						goto case LightType.Point;
				}
			}

			if (cullResults.visibleLights.Count > MAX_VISIBLE_LIGHTS) {
				int[] lightIndices = cullResults.GetLightIndexMap();
				for (int i = MAX_VISIBLE_LIGHTS; i < cullResults.visibleLights.Count; i++) {
					lightIndices[i] = -1;
				}
				cullResults.SetLightIndexMap(lightIndices);
			}
		}

	}
}
