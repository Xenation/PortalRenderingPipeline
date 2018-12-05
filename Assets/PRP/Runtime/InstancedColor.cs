using UnityEngine;

namespace PRP {
	[RequireComponent(typeof(MeshRenderer))]
	public class InstancedColor : MonoBehaviour {

		[SerializeField]
		private Color color = Color.white;
		public Color instancedColor {
			get {
				return color;
			}
			set {
				color = value;
				UpdateColor();
			}
		}

		private MeshRenderer meshRenderer;
		private MaterialPropertyBlock propertyBlock;

		private int colorPropId;

		private void Awake() {
			Init();
			UpdateColor();
		}

		private void OnValidate() {
			if (meshRenderer == null || propertyBlock == null) {
				Init();
			}
			UpdateColor();
		}

		private void Init() {
			meshRenderer = GetComponent<MeshRenderer>();
			propertyBlock = new MaterialPropertyBlock();
			colorPropId = Shader.PropertyToID("_Color");
		}

		private void UpdateColor() {
			propertyBlock.SetColor(colorPropId, color);
			meshRenderer.SetPropertyBlock(propertyBlock);
		}

	}
}
