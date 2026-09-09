Shader "Custom/DaoGuang_Glow"
{
    Properties
    {
        _CenterColor ("中心颜色", Color) = (1, 1, 1, 1)
        _GlowColor   ("外发光颜色", Color) = (0.2, 0.6, 1, 1)
        _CenterWidth ("中心宽度", Range(0.01, 1)) = 0.2
        _GlowPower   ("发光衰减", Range(0.1, 10)) = 2.0
        _GlowStrength("发光强度", Range(0, 5)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // 加法混合：颜色叠加，产生发光
        Blend One One
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

            fixed4 _CenterColor;
            fixed4 _GlowColor;
            float _CenterWidth;
            float _GlowPower;
            float _GlowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // uv.y 以 0.5 为中心向上下两侧渐变：0 = 中心，1 = 边缘
                float dist = abs(i.uv.y - 0.5) * 2.0;

                // 中心亮核：中心宽度内保持纯中心色，向外平滑衰减到 0
                float core = 1.0 - smoothstep(0.0, _CenterWidth, dist);

                // 外发光：从中心向边缘按指数衰减
                float glow = pow(saturate(1.0 - dist), _GlowPower);

                // 中心色 + 外发光色叠加，再乘发光强度
                fixed3 rgb = (_CenterColor.rgb * core + _GlowColor.rgb * glow) * _GlowStrength;

                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
