Shader "Custom/Ember_Charcoal"
{
    Properties
    {
        _TopColor    ("上颜色", Color) = (1.0, 0.9, 0.6, 1)
        _MidColor    ("中颜色", Color) = (1.0, 0.45, 0.1, 1)
        _BottomColor ("下颜色", Color) = (0.3, 0.08, 0.02, 1)

        _RimStrength ("外围发光强度", Range(0, 5)) = 2.0
        _RimPower    ("外围发光锐度", Range(0.1, 10)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite On
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

            fixed4 _TopColor;
            fixed4 _MidColor;
            fixed4 _BottomColor;
            float _RimStrength;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 上中下垂直渐变：下(0) → 中(0.5) → 上(1)，两段 smoothstep 平滑过渡
                float h = saturate(i.uv.y);
                float t1 = smoothstep(0.0, 0.5, h);
                float t2 = smoothstep(0.5, 1.0, h);
                fixed3 gradColor = lerp(_BottomColor.rgb, _MidColor.rgb, t1);
                gradColor = lerp(gradColor, _TopColor.rgb, t2);

                // 外围发光：以 uv 中心为原点，越靠外越亮
                float dist = length(i.uv - 0.5) * 2.0;   // 0 = 中心，1 = 边缘
                float rim = pow(saturate(dist), _RimPower);

                // 渐变颜色 + 外围发光（边缘加亮）
                fixed3 rgb = gradColor * (1.0 + rim * _RimStrength);

                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
