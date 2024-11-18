
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"
	
	float g_flRotationSpeed < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	
	PixelInput MainVs( VertexInput v )
	{
		float rotAngle = g_flRotationSpeed * g_flTime;

		float c = cos(rotAngle);
		float s = sin(rotAngle);

		float tBob = 3 * (sin(2 * rotAngle) + cos(2 * rotAngle));

		float4x4 animMatrix = float4x4(
			c, -s, 0, 0,
			s, c, 0, 0,
			0, 0, 1, tBob,
			0, 0, 0, 1
		);
		float3x3 rotMatrix = (float3x3)animMatrix;

		//v.vNormalOs.xyz = float3(c,s,0);

		v.vPositionOs = mul(animMatrix, float4(v.vPositionOs, 1)).xyz;
		//v.vNormalOs.xyz = normalize(mul(rotMatrix, v.vNormalOs.xyz));

		PixelInput i = ProcessVertex( v );
		i = FinalizeVertex(i);

		// weirdly only works when rotating it after ProcessVertex, manually might need to call VS_DecodeObjectSpaceNormalAndTangent...
		i.vNormalWs.xyz = normalize(mul(rotMatrix, i.vNormalWs.xyz));

		return i;
	}
}

PS
{
	#include "common/pixel.hlsl"

	float rimStrength < UiGroup( ",0/,0/0" ); Default1( 3 ); Range1( 0, 10 ); >;


	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 goldColor = float3( 1, 0.53333, 0 );
		
		Material m = Material::From( i, 0, float4(0,0,1, 1), 0 );

		float3 normal = i.vNormalWs;
		float3 worldPosition = i.vPositionWithOffsetWs + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 lightResult = 0;
		float rawLight = 0;

		for ( uint index = 0; index < DynamicLight::Count( i.vPositionSs ); index++ )
		{
			Light light = DynamicLight::From( i.vPositionSs, worldPosition, index );

			float NoL = smoothstep(-0.05, 0, dot(light.Direction, normal));
			float shadowTerm = step(0.4, light.Visibility);
			float atten = smoothstep(0.19, 0.2, light.Attenuation);

			rawLight += light.Color * NoL * shadowTerm * atten;
			lightResult += light.Color * max(0.5, NoL * shadowTerm) * atten;
		}

		// Rim light
		float3 V = g_vCameraDirWs;
		float fresnelRes = 1-dot(normal, normalize(V));
		fresnelRes = (1 - smoothstep(0.49, 0.5, 0.35 * fresnelRes)) * rawLight;

		lightResult += fresnelRes * rimStrength;

		// Sample texture
		lightResult *= goldColor;

		return float4(lightResult, 1);
	}
}
