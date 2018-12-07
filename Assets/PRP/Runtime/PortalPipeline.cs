using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using PRP.PortalSystem;

namespace PRP {
	public class PortalPipeline : RenderPipeline {

		private struct PortalContext {
			public ScriptableRenderContext renderContext;
			public Camera camera;
			public Matrix4x4 worldToCamera;
			public Vector3 virtualCameraPos;
		}

		private CullResults cull;
		private DrawRendererFlags drawFlags;
		private CommandBuffer buffer = new CommandBuffer { name = "Render Camera" };
		private Material errorMaterial;
		private Material stencilMarkerMaterial;
		private Material stencilUnmarkerMaterial;
		private Material stencilMarkFiller;
		private Material stencilUnmarkFiller;

		private Mesh fullScreenQuad;

		private const int MAX_VISIBLE_LIGHTS = 16;

		private static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
		private static int visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
		private static int visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
		private static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");

		Vector4[] visibleLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightDirectionsOrPositions = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightAttenuations = new Vector4[MAX_VISIBLE_LIGHTS];
		Vector4[] visibleLightSpotDirections = new Vector4[MAX_VISIBLE_LIGHTS];


		public PortalPipeline(bool dynamicBatching, bool instancing) {
			//Debug.Log("Instanciating Portal Pipeline");
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader")) { hideFlags = HideFlags.HideAndDontSave };
			stencilMarkerMaterial = new Material(Shader.Find("Hidden/PRP/StencilMarker")) { hideFlags = HideFlags.HideAndDontSave };
			stencilUnmarkerMaterial = new Material(Shader.Find("Hidden/PRP/StencilUnmarker")) { hideFlags = HideFlags.HideAndDontSave };
			stencilMarkFiller = new Material(Shader.Find("Hidden/PRP/StencilMarkFiller")) { hideFlags = HideFlags.HideAndDontSave };
			stencilUnmarkFiller = new Material(Shader.Find("Hidden/PRP/StencilUnmarkFiller")) { hideFlags = HideFlags.HideAndDontSave };

			fullScreenQuad = new Mesh();
			fullScreenQuad.vertices = new Vector3[] {
				new Vector3(-1, 1, 0),
				new Vector3(1, 1, 0),
				new Vector3(1, -1, 0),
				new Vector3(-1, -1, 0)
			};
			fullScreenQuad.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);

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
			buffer.ClearRenderTarget((clearFlags & CameraClearFlags.Depth) != 0, (clearFlags & CameraClearFlags.Color) != 0, camera.backgroundColor);
			buffer.BeginSample("Render Portals");
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();

			// Portal Context
			PortalContext basePortalContext = new PortalContext() { renderContext = renderContext, camera = camera };
			PortalViewingCamera portalCamera = camera.GetComponent<PortalViewingCamera>();
			if (portalCamera == null) return;

			basePortalContext.worldToCamera = camera.worldToCameraMatrix;
			basePortalContext.virtualCameraPos = camera.transform.position;
			
			Portal[] basePortals = portalCamera.viewablePortals; // tmp
			foreach (Portal p in basePortals) {
				p.renderer = p.transform.GetComponentInChildren<MeshRenderer>();
				p.Synchronize();
			}
			RenderBaseLayer(basePortalContext, basePortals);

			buffer.BeginSample("Layer 1");
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			foreach (Portal lay1Portal in basePortals) {

				// Update Context
				PortalContext lay1PortalContext = new PortalContext() { renderContext = renderContext, camera = camera };
				lay1PortalContext.worldToCamera = lay1Portal.TransformMatrix(basePortalContext.worldToCamera);
				lay1PortalContext.virtualCameraPos = lay1Portal.TransformPosition(basePortalContext.virtualCameraPos);

				Portal[] lay2Visible = basePortals; // tmp
				RenderPortalLayer(basePortalContext, lay1PortalContext, lay1Portal, lay2Visible);

				buffer.BeginSample("Layer 2");
				renderContext.ExecuteCommandBuffer(buffer);
				buffer.Clear();
				foreach (Portal lay2Portal in lay2Visible) {
					if (lay2Portal == lay1Portal.outputPortal) continue;

					// Update Context
					PortalContext lay2PortalContext = new PortalContext() { renderContext = renderContext, camera = camera };
					lay2PortalContext.worldToCamera = lay1Portal.TransformMatrix(lay1PortalContext.worldToCamera);
					lay2PortalContext.virtualCameraPos = lay1Portal.TransformPosition(lay1PortalContext.virtualCameraPos);

					Portal[] lay3Visible = basePortals; // tmp
					RenderPortalLayer(lay1PortalContext, lay2PortalContext, lay2Portal, lay3Visible);

					buffer.BeginSample("Layer 3");
					renderContext.ExecuteCommandBuffer(buffer);
					buffer.Clear();
					foreach (Portal lay3Portal in lay3Visible) {
						if (lay3Portal == lay2Portal.outputPortal) continue;

						// Update Context
						PortalContext lay3PortalContext = new PortalContext() { renderContext = renderContext, camera = camera };
						lay3PortalContext.worldToCamera = lay1Portal.TransformMatrix(lay2PortalContext.worldToCamera);
						lay3PortalContext.virtualCameraPos = lay1Portal.TransformPosition(lay2PortalContext.virtualCameraPos);

						RenderPortalLayer(lay2PortalContext, lay3PortalContext, lay3Portal, new Portal[0]); // tmp
					}
					buffer.EndSample("Layer 3");
					renderContext.ExecuteCommandBuffer(buffer);
					buffer.Clear();
				}
				buffer.EndSample("Layer 2");
				renderContext.ExecuteCommandBuffer(buffer);
				buffer.Clear();
			}
			buffer.EndSample("Layer 1");
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();

			// Submit
			buffer.EndSample("Render Portals");
			renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
			renderContext.Submit();
		}

		private void RenderBaseLayer(PortalContext portalContext, Portal[] firstLayerPortals) {
			StencilMarkFill(portalContext);

			foreach (Portal nPortal in firstLayerPortals) {
				StencilUnmark(portalContext, nPortal);
			}

			RenderPortalLayer(portalContext);

			StencilUnmarkFill(portalContext);
		}

		private void RenderPortalLayer(PortalContext previousPortalContext, PortalContext portalContext, Portal portal, Portal[] nextLayerPortals) {
			StencilMark(previousPortalContext, portal);

			foreach (Portal nPortal in nextLayerPortals) {
				StencilUnmark(portalContext, nPortal);
			}

			RenderPortalLayer(portalContext);

			StencilUnmark(previousPortalContext, portal);
		}

		private void RenderPortalLayer(PortalContext portalContext) {

			// Culling
			CullResults cullResults = new CullResults();
			ScriptableCullingParameters cullingParameters;
			if (!CullResults.GetCullingParameters(portalContext.camera, out cullingParameters)) {
				return;
			}
			//CameraProperties cameraProperties = new CameraProperties();
			//cameraProperties.SetCameraCullingPlane(); // Could be usefull to cull everything before the output portal
			//cullingParameters.cullingMatrix = portalContext.worldToCamera * portalContext.camera.projectionMatrix;
			//cullingParameters.position = portalContext.virtualCameraPos;
			//object cp = cullingParameters.cameraProperties;
			//HCamProp camProp = HCamProp.CopyStruct<HCamProp>(ref cp);
			//camProp.worldToCamera = portalContext.worldToCamera;
			//camProp.actualWorldToClip = portalContext.worldToCamera * portalContext.camera.projectionMatrix;
			//object objCamProp = camProp;
			//CameraProperties resCamProp = HCamProp.CopyStruct<CameraProperties>(ref objCamProp);
			//cullingParameters.cameraProperties = resCamProp;
			CullResults.Cull(ref cullingParameters, portalContext.renderContext, ref cullResults);

			// Setup Lights
			ConfigureLights(cullResults);
			buffer.BeginSample("Render Portal Layer");
			buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
			buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
			buffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations);
			buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();

			// Draw Opaque
			DrawRendererSettings drawSettings = new DrawRendererSettings(portalContext.camera, new ShaderPassName("PRP")) { flags = drawFlags, rendererConfiguration = RendererConfiguration.PerObjectLightIndices8 };
			drawSettings.sorting.worldToCameraMatrix = portalContext.worldToCamera;
			drawSettings.sorting.cameraPosition = portalContext.virtualCameraPos;
			drawSettings.sorting.flags = SortFlags.CommonOpaque;
			FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);
			filterSettings.renderQueueRange = RenderQueueRange.opaque;
			portalContext.renderContext.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

			// Draw Skybox
			portalContext.renderContext.DrawSkybox(portalContext.camera);

			// Draw Transparent
			drawSettings.sorting.flags = SortFlags.CommonTransparent;
			filterSettings.renderQueueRange = RenderQueueRange.transparent;
			portalContext.renderContext.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

			buffer.EndSample("Render Portal Layer");
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilMark(PortalContext portalContext, Portal portal) {
			buffer.SetViewport(new Rect(0, 0, portalContext.camera.pixelWidth, portalContext.camera.pixelHeight)); // tmp
			buffer.SetViewMatrix(portalContext.worldToCamera);
			buffer.SetProjectionMatrix(portalContext.camera.projectionMatrix);
			buffer.DrawRenderer(portal.renderer, stencilMarkerMaterial);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilUnmark(PortalContext portalContext, Portal portal) {
			buffer.SetViewport(new Rect(0, 0, portalContext.camera.pixelWidth, portalContext.camera.pixelHeight)); // tmp
			buffer.SetViewMatrix(portalContext.worldToCamera);
			buffer.SetProjectionMatrix(portalContext.camera.projectionMatrix);
			buffer.DrawRenderer(portal.renderer, stencilUnmarkerMaterial);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilMarkFill(PortalContext portalContext) {
			buffer.SetViewport(new Rect(0, 0, portalContext.camera.pixelWidth, portalContext.camera.pixelHeight)); // tmp
			buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilMarkFiller);
			portalContext.renderContext.ExecuteCommandBuffer(buffer);
			buffer.Clear();
		}

		private void StencilUnmarkFill(PortalContext portalContext) {
			buffer.SetViewport(new Rect(0, 0, portalContext.camera.pixelWidth, portalContext.camera.pixelHeight)); // tmp
			buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, stencilUnmarkFiller);
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
