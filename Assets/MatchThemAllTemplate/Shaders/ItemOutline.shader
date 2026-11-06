// Selection / hint outline for items (URP).
// Drawn as an extra material pass on the item's renderer (see Item.Select): the mesh is inflated
// along its normals by _Thickness (object space) and only its back faces are drawn in _Color, so a
// rim shows around the silhouette of the normally rendered item.
Shader "MatchThemAll/Item Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.92, 0.02, 1)
        _Thickness ("Thickness", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Thickness;
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

        Varyings InflatedVertex(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 inflated = input.positionOS.xyz + SafeNormalize(input.normalOS) * _Thickness;
            output.positionCS = TransformObjectToHClip(inflated);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex InflatedVertex
            #pragma fragment Frag
            #pragma multi_compile_instancing

            half4 Frag(Varyings input) : SV_Target { return half4(_Color.rgb, 1); }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Front
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex InflatedVertex
            #pragma fragment Frag
            #pragma multi_compile_instancing

            half Frag(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }
    }
}
