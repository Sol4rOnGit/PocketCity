Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        _WaterColour ("Water Colour", Color) = (0.1, 0.5, 0.8, 0.7)
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _WaveHeight ("Wave Height", Float) = 0.2
        _WaveFrequency ("Wave Frequency", Float) = 0.5
        _WaveScale ("Wave Scale", Float) = 10.0
    }

    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalPipeline" 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes{
                float4 positionOS : POSITION;
            };

            struct Varyings{
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColour;
                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz); 

                float2 scaledUV = positionWS.xz / _WaveScale;

                float wave = sin((scaledUV.x + scaledUV.y) * _WaveFrequency + (_Time.y * _WaveSpeed));
                positionWS.y += wave * _WaveHeight;

                OUT.positionHCS = TransformWorldToHClip(positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _WaterColour;
            }

            ENDHLSL
        }
    }
}
