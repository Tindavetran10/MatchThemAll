Shader "Hidden/Match_Them_All/Outline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1, 0.9, 0.1, 1)
        _Thickness ("Thickness", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Thickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _PixelizeSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 posCS    = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);

                float2 n      = normalize(normalCS.xy + float2(0.0001, 0.0001));
                float pixelSize = (_PixelizeSize > 0.0) ? _PixelizeSize : 1.0;
                float2 offset = n * ((_Thickness * pixelSize) / _ScreenParams.xy) * 2.0;
                posCS.xy     += offset * posCS.w;

                output.positionCS = posCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
