Shader "Custom/BE_DaoGuang"
{
    Properties
    {
        _MainTex ("剑光渐变图", 2D) = "white" {}
        _Tiling ("填充长度", Vector) = (1, 1, 0, 0)
        _Offset ("偏移", Vector) = (0, 0, 0, 0)
        _Color ("主颜色 (蓝色)", Color) = (0.1, 0.6, 1, 1)
        _CoreColor ("核心炽白色", Color) = (1, 1, 1, 1)
        _Brightness ("发光强度", Range(0, 5)) = 2.0
        _CoreThreshold ("核心阈值", Range(0.01, 1)) = 0.8
        _EdgeSoftness ("边缘柔化强度", Range(0.01, 0.5)) = 0.15
        
        // ===== 新增：透明度控制 =====
        _AlphaScale ("透明度 (0=全透, 1=原样, 2=更不透明)", Range(0, 3)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // 叠加混合模式：颜色相加，但透明度由我们控制
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

            sampler2D _MainTex;
            float4 _Tiling;
            float4 _Offset;
            float4 _Color;
            float4 _CoreColor;
            float _Brightness;
            float _CoreThreshold;
            float _EdgeSoftness;
            float _AlphaScale;  // 新增

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy + _Offset.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样贴图
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // UV边缘柔化
                float2 edgeDist = abs(i.uv - 0.5) * 2.0;
                float maxEdgeDist = max(edgeDist.x, edgeDist.y);
                float edgeSoft = 1.0 - smoothstep(0.0, 1.0 + _EdgeSoftness, maxEdgeDist);
                
                // 贴图亮度作为遮罩
                float mask = col.r * edgeSoft;
                
                // 核心色混合
                float coreFactor = smoothstep(_CoreThreshold, 1.0, mask);
                float4 finalColor = lerp(_Color, _CoreColor, coreFactor);
                
                // 颜色输出
                finalColor.rgb *= _Brightness * col.rgb * edgeSoft;
                
                // ===== 关键修改：透明度 = mask × 亮度 × _AlphaScale =====
                // _AlphaScale = 1 时保持原样，>1 更不透明，<1 更透明
                finalColor.a = mask * _Brightness * _AlphaScale;
                
                // 钳制Alpha在0-1之间，防止溢出
                finalColor.a = saturate(finalColor.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
}