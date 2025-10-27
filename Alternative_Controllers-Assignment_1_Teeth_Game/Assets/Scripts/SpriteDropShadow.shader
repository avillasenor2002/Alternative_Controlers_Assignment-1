Shader "Custom/2D/SpriteDropShadow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset ("Shadow Offset", Vector) = (0.05, -0.05, 0, 0)
        _ShadowSoftness ("Shadow Softness", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteShadowPass"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                
                // Shadow sample (offset UV)
                fixed2 shadowUV = i.uv + _ShadowOffset.xy;
                fixed4 shadowTex = tex2D(_MainTex, shadowUV);

                // Apply softness by sampling around offset
                fixed4 softShadow = shadowTex;
                softShadow.a *= smoothstep(0.0, _ShadowSoftness, shadowTex.a);

                // Combine shadow color
                fixed4 shadow = _ShadowColor * softShadow.a;

                // Draw shadow first, then sprite on top
                fixed4 result = mainTex;
                result.rgb = (shadow.rgb * shadow.a * (1 - mainTex.a)) + result.rgb;
                result.a = saturate(mainTex.a + shadow.a * 0.5);

                return result * i.color;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
