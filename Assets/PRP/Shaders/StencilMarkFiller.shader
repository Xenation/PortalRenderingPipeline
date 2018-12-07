Shader "Hidden/PRP/StencilMarkFiller" {
	Properties {
		
	}
	SubShader {
		Pass {
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Stencil { // Always writes 1 at most significant bit
				Ref 128
				WriteMask 128
				Comp Always
				Pass Replace
				ZFail Replace
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
				o.clipPos = i.vertex;
				return o;
			}

			fixed4 frag(VertexOutput i) : SV_TARGET {
				return fixed4(0, 0, 0, 0);
			}

			ENDCG
		}
	}
}
