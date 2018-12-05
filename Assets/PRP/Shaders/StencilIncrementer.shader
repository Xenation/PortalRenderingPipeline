Shader "PRP/StencilIncrementer" {
	Properties {
		
	}
	SubShader {
		Pass {
			Stencil {
				Ref 1
				Comp Always
				Pass IncrSat
			}
		}
	}
}
