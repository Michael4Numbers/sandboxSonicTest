// Ideally you wouldn't need half these includes for an unlit shader
// But it's stupiod

MODES
{
	Forward();
	Depth(); 
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#include "common/shared.hlsl"
	#define CUSTOM_MATERIAL_INPUTS

	
	float fresnel(float amount, float3 normal, float3 view)
	{
		return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
	}
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

    // Input texture for cel shading
    CreateInputTexture2D( SonicTexture, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
    Texture2D baseColor < Channel( RGB, Box( SonicTexture ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >; 

    SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;

    float g_flSpeed < UiGroup( ",0/,0/0" ); Default1( 10 ); Range1( 0, 100 ); >;
    float rimStrength < UiGroup( ",0/,0/0" ); Default1( 3 ); Range1( 0, 10 ); >;

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        // Calculate spinning UVs
        float2 uvCenter = float2(0.5, 0.5);
        float2 uvOffset = i.vTextureCoords.xy - uvCenter;

        float angle = g_flSpeed * g_flTime;
        float cosA = cos(angle);
        float sinA = sin(angle);

        float2 rotatedUVs = float2(
            cosA * uvOffset.x - sinA * uvOffset.y,
            sinA * uvOffset.x + cosA * uvOffset.y
        ) + uvCenter;

        // Sample the spinning texture
        float3 textureColor = baseColor.Sample(g_sSampler0, rotatedUVs).rgb;

        // Cel shading lighting calculation
        float3 normal = i.vNormalWs;
        float3 worldPosition = i.vPositionWithOffsetWs + g_vHighPrecisionLightingOffsetWs.xyz;
        float3 lightResult = 0;
        float rawLight = 0;

        for (uint index = 0; index < DynamicLight::Count(i.vPositionSs); index++)
        {
            Light light = DynamicLight::From(i.vPositionSs, worldPosition, index);

            float NoL = step(0, dot(light.Direction, normal));
            float shadowTerm = step(0.4, light.Visibility);
            float atten = smoothstep(0.19, 0.2, light.Attenuation);

            rawLight += light.Color * NoL * shadowTerm * atten;
            lightResult += light.Color * max(0.5, NoL * shadowTerm) * atten;
        }

        // Rim lighting
        float3 V = g_vCameraDirWs;
        float fresnelRes = 1 - dot(normal, normalize(V));
        fresnelRes = (1 - smoothstep(0.49, 0.5, 0.35 * fresnelRes)) * rawLight;
        lightResult += fresnelRes * rimStrength;

        // Multiply cel shading light by texture color
        lightResult *= textureColor;

        return float4(lightResult, 1);
    }
}
