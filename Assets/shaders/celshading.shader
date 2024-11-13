// Ideally you wouldn't need half these includes for an unlit shader
// But it's stupiod

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#include "common/shared.hlsl"
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

	// Create a Texture2D with required annotation, but now without any macros. Does the same thing as CreateTexture2DWithoutSampler.
	CreateInputTexture2D( SonicTexture, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	Texture2D baseColor < Channel( RGB, Box( SonicTexture ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >; 

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::From( i );

		float3 normal = m.Normal;
		float3 lightResult = 0;

		for ( uint index = 0; index < DynamicLight::Count( m.ScreenPosition ); index++ )
		{
			Light light = DynamicLight::From( m.ScreenPosition, m.WorldPosition, index );

			float NoL = step(0, dot(light.Direction, normal));
			float shadowTerm = step(0.4, light.Visibility);
			lightResult += light.Color * max(0.5, NoL * shadowTerm * light.Attenuation);
		}

		// Sample texture
		lightResult *= baseColor.Sample(g_sAniso, i.vTextureCoords.xy);

		return float4(lightResult, 1);
	}
}
