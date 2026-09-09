Shader "Custom/FluorescentFlower"
{
    Properties
    {
        _BaseColor ("花朵颜色", Color) = (0.15, 0.4, 1, 1)
        _GlowColor ("荧光颜色", Color) = (0.35, 0.85, 1, 1)
        _Emission  ("荧光强度", Range(0, 10)) = 3.0
        _RimStrength ("边缘荧光强度", Range(0, 10)) = 2.0
        _RimPower  ("边缘荧光锐度", Range(0.1, 10)) = 3.0
        _Opacity   ("不透明度", Range(0, 1)) = 0.8

        _SwayStrength ("摇曳幅度", Range(0, 0.5)) = 0.05
        _SwaySpeed    ("摇曳速度", Range(0, 10)) = 2.0
        _SwayFrequency("摇曳频率", Range(0, 10)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // 半透明花瓣
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            fixed4 _BaseColor;
            fixed4 _GlowColor;
            float _Emission;
            float _RimStrength;
            float _RimPower;
            float _Opacity;
            float _SwayStrength;
            float _SwaySpeed;
            float _SwayFrequency;

            v2f vert (appdata v)
            {
                // 风摇曳（顶点动画）：根部固定，越高摆动越大，X/Z 两方向相位错开更自然
                float heightFactor = saturate(v.vertex.y);
                float phase = _Time.y * _SwaySpeed + v.vertex.x * _SwayFrequency + v.vertex.z * _SwayFrequency * 0.6;
                v.vertex.x += sin(phase) * _SwayStrength * heightFactor;
                v.vertex.z += cos(phase * 0.8) * _SwayStrength * 0.5 * heightFactor;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 视线方向（从表面指向摄像机）
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                // 基础花朵颜色
                fixed3 rgb = _BaseColor.rgb;

                // 荧光自发光（整体均匀加亮）
                rgb += _GlowColor.rgb * _Emission;

                // 边缘荧光（Fresnel：越侧视越亮，形成发光的轮廓）
                float ndv = saturate(dot(normalize(i.worldNormal), viewDir));
                float rim = pow(1.0 - ndv, _RimPower);
                rgb += _GlowColor.rgb * rim * _RimStrength;

                return fixed4(rgb, _Opacity);
            }
            ENDCG
        }
    }
    FallBack Off
}
