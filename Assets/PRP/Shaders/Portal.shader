Shader "PRP/Portal" {
	Properties {
		_Color("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags {
			
		}
		
        Pass {
			Stencil {
				Ref 1
				WriteMask 1
				Comp Always
				Pass Replace
			}

		}
	}
}
