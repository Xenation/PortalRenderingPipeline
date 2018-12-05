using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace PRP {
	public class PortalPipeline : RenderPipeline {

		private CullResults cull;
		private DrawRendererFlags drawFlags;
		private CommandBuffer buffer = new CommandBuffer { name = "Render Camera" };
		private Material errorMaterial;

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
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader")) { hideFlags = HideFlags.HideAndDontSave };
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
				Render(renderContext, cameras[i]);
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
			ConfigureLights();

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

		private void ConfigureLights() {
			Vector4 v;
			for (int i = 0; i < cull.visibleLights.Count && i < MAX_VISIBLE_LIGHTS; i++) {
				VisibleLight light = cull.visibleLights[i];
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

			if (cull.visibleLights.Count > MAX_VISIBLE_LIGHTS) {
				int[] lightIndices = cull.GetLightIndexMap();
				for (int i = MAX_VISIBLE_LIGHTS; i < cull.visibleLights.Count; i++) {
					lightIndices[i] = -1;
				}
				cull.SetLightIndexMap(lightIndices);
			}
		}

	}
}
