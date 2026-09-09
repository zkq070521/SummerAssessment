Shader "Custom/Trail_NoiseFade"
{
    Properties
    {
        _MainTex ("主贴图", 2D) = "white" {}
        _Color ("主颜色", Color) = (1, 1, 1, 1)

        _NoiseTex ("噪波贴图", 2D) = "white" {}
        _NoiseTiling ("噪波密度", Range(0.1, 20)) = 2.0
        _NoiseAmount ("噪波强度", Range(0, 2)) = 0.5

        _FadeAmount ("尾部透明量", Range(0, 1)) = 0.5
        _FadeSoftness ("边缘柔化", Range(0.01, 0.5)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // 标准 Alpha 透明混合
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            sampler2D _NoiseTex;
            float _NoiseTiling;
            float _NoiseAmount;
            float _FadeAmount;
            float _FadeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // 噪波：采样灰度噪波，用来扰动尾部渐隐边界，让边缘不规则
                float noise = tex2D(_NoiseTex, i.uv * _NoiseTiling).r;

                // uv.x：0 = 头部（不透明），1 = 尾部（透明）
                // 溶解量 = 沿长度方向 + 噪波扰动
                float dissolve = i.uv.x + (noise - 0.5) * _NoiseAmount;

                // 超过阈值的地方变透明，边缘用 smoothstep 柔化
                float alpha = 1.0 - smoothstep(_FadeAmount - _FadeSoftness, _FadeAmount + _FadeSoftness, dissolve);
                col.a *= saturate(alpha);

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
