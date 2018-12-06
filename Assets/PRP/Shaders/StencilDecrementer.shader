Shader "PRP/StencilDecrementer" {
	Properties {
		
	}
	SubShader {
		Pass {
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Stencil {
				Ref 1
				Comp Always
				Pass DecrSat
			}

			CGPROGRAM
			#pragma target 3.5

			#pragma vertex vert
			#pragma fragment frag
			
			struct VertexIntput {
				float4 vertex : POSITION;
			};

			struct VertexOutput {
				float4 clipPos : SV_POSITION;
			};

			VertexOutput vert(VertexIntput i) {
				VertexOutput o;
				o.clipPos = UnityObjectToClipPos(i.vertex);
				return o;
			}

			fixed4 frag(VertexOutput i) : SV_TARGET {
				return fixed4(0, 1, 0, 0.25);
			}

			ENDCG
		}
	}
}
