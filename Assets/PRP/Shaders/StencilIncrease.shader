Shader "Hidden/PRP/StencilIncrease" {
	Properties {
		
	}
	SubShader {
		Pass {
			ZWrite Off
			Blend Zero One
			Stencil { // Always writes 1 at most significant bit
				Ref 1
				Comp Always
				Pass IncrSat
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
				return fixed4(0, 0, 0, 0);
			}

			ENDCG
		}
	}
}
