﻿#ifndef PRP_UNLIT_INCLUDED
#define PRP_UNLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/common.hlsl"

CBUFFER_START(UnityPerFrame)
	float4x4 unity_ObjectToWorld;
CBUFFER_END
CBUFFER_START(UnityPerDraw)
	float4x4 unity_MatrixVP;
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(PerInstance)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)


struct VertexInput {
	float4 pos : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput {
	float4 clipPos : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput UnlitPassVertex(VertexInput input) {
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.clipPos = mul(unity_MatrixVP, mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0)));
	return output;
}

float4 UnlitPassFragment(VertexOutput input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);
	return UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
}

#endif // PRP_UNLIT_INCLUDED