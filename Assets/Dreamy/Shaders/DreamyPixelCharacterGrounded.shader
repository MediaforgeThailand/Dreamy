Shader "Dreamy/Pixel Character Grounded"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        _GroundTint ("Feet Tint", Color) = (0.62,0.66,0.70,1)
        _GroundBlend ("Feet Blend", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _GroundTint;
            half _GroundBlend;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.texcoord) * input.color;
                half feetMask = saturate((0.34 - input.texcoord.y) * 3.2);
                color.rgb = lerp(color.rgb, color.rgb * _GroundTint.rgb, feetMask * _GroundBlend);
                return color;
            }
            ENDCG
        }
    }
}
