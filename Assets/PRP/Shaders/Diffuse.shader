Shader "PRP/Diffuse" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_AmbientLight ("Ambient Light", Color) = (0.1, 0.1, 0.1, 1)
	}
	SubShader {
		Tags {
			
		}

		Pass {
			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling

			#pragma vertex DiffusePassVertex
			#pragma fragment DiffusePassFragment

			#include "../ShaderLibrary/Diffuse.hlsl"

			ENDHLSL
		}

		Pass {
			Tags {
				"LightMode" = "PRP"
			}
			Stencil {
				Ref 128
				ReadMask 128
				Comp Equal
			}

			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling

			#pragma vertex DiffusePassVertex
			#pragma fragment DiffusePassFragment

			#include "../ShaderLibrary/Diffuse.hlsl"

			ENDHLSL
		}
	}
}
