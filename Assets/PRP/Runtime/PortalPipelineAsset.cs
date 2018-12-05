using UnityEngine.Experimental.Rendering;
using UnityEngine;

namespace PRP {
	[CreateAssetMenu(menuName = "Rendering/PortalPipelineAsset", order = 20)]
	public class PortalPipelineAsset : RenderPipelineAsset {

		[SerializeField]
		private bool dynamicBatching = false;
		[SerializeField]
		private bool instancing = true;

		protected override IRenderPipeline InternalCreatePipeline() {
			return new PortalPipeline(dynamicBatching, instancing);
		}

	}
}
