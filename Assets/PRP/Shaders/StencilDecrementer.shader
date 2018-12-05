Shader "PRP/StencilDecrementer" {
	Properties {
		
	}
	SubShader {
		Pass {
			Stencil {
				Ref 1
				Comp Always
				Pass DecrSat
			}
		}
	}
}
