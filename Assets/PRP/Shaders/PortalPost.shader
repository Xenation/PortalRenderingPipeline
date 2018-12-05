Shader "PRP/PortalPost" {
	Properties {
		_MainTex ("MainTex", 2d) = "white" {}
	}
	SubShader {
		Tags {
			
		}
		
        Pass {
			Stencil {
				Ref 1
				WriteMask 1
				Comp Equal
			}
			CGPROGRAM

			#pragma target 3.5

			#pragma vertex PostVertex
			#pragma fragment PostFragment

			sampler2D _MainTex;

			struct VertexInput {
				float4 pos : POSITION;
				float2 uv : TEXCOORD;
			};

			struct VertexOutput {
				float4 clipPos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			VertexOutput PostVertex(VertexInput input) {
				VertexOutput output;
				output.clipPos = input.pos;
				output.uv = input.uv;
				return output;
			}

			float4 PostFragment(VertexOutput input) : SV_TARGET {
				return tex2D(_MainTex, input.uv);
			}

			ENDCG
		}
	}
}
