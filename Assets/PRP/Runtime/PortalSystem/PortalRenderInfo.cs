using UnityEngine;

namespace PRP.PortalSystem {
	public class PortalRenderInfo {
		public PortalRenderInfo outputPortalInfo;
		public Matrix4x4 worldToPortal;
		
		public Matrix4x4 TransformMatrix(Matrix4x4 mat) {
			return worldToPortal * mat;
		}
	}
}
