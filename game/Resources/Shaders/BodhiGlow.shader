// Purpose: soft round dots of light or snow, for particles (falling snowflakes, the puff of snow
// when Bodhi lands, the lantern's halo). Each dot is a little square facing the camera; this
// shader turns it into a soft round blob, fading to nothing at its edge, in the particle's own
// colour. No texture is needed: the roundness is worked out from where the pixel sits on the
// square. Two blend modes: "soft" (covers what's behind, for snow) and "glow" (adds light, for
// halos), chosen by the two blend numbers below, which WorldLook sets in code.
Shader "Bodhi/Glow"
{
    Properties
    {
        _Softness ("Edge softness", Range(0.5, 4)) = 1.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SourceBlend ("Source blend", Float) = 5       // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DestinationBlend ("Destination blend", Float) = 10  // OneMinusSrcAlpha
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend [_SourceBlend] [_DestinationBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex glow_vertex
            #pragma fragment glow_fragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Softness;
                float _SourceBlend;
                float _DestinationBlend;
            CBUFFER_END

            struct MeshCorner
            {
                float4 position : POSITION;
                half4 colour : COLOR;
                float2 square : TEXCOORD0;  // 0 to 1 across the dot's square.
            };

            struct ToPixel
            {
                float4 screen_position : SV_POSITION;
                half4 colour : COLOR;
                float2 square : TEXCOORD0;
                half fog : TEXCOORD1;
            };

            // Places the dot's corners on screen.
            ToPixel glow_vertex(MeshCorner corner)
            {
                ToPixel result;
                result.screen_position = TransformObjectToHClip(corner.position.xyz);
                result.colour = corner.colour;
                result.square = corner.square;
                result.fog = ComputeFogFactor(result.screen_position.z);
                return result;
            }

            // Bright in the middle of the square, fading smoothly to nothing at the rim.
            half4 glow_fragment(ToPixel pixel) : SV_Target
            {
                float from_middle = length(pixel.square * 2.0 - 1.0);
                half strength = pow(saturate(1.0 - from_middle), _Softness);
                half4 colour = pixel.colour;
                colour.a *= strength;
                colour.rgb = MixFog(colour.rgb, pixel.fog);
                // In "glow" mode (adding light) the alpha has to scale the colour itself.
                colour.rgb *= _DestinationBlend == 1 ? colour.a : 1;
                return colour;
            }
            ENDHLSL
        }
    }
}
