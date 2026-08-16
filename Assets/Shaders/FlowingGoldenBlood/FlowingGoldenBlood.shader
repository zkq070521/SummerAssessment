// FlowingGoldenBlood.shader
// 流动的金血：内部亮金、边缘深金营造深浅，液态流动 + 金色星光。
// 用法：创建 Material 并指定本 Shader，在 Inspector 中可调所有颜色与参数。
//       想要更强的“光芒”请开启 URP Post-processing 的 Bloom，并调高 _Brightness / _SparkleAmount。
Shader "Custom/FlowingGoldenBlood"
{
    Properties
    {
        // ===== 颜色（Inspector 可调）=====
        _BaseColor ("主体金色（亮部）", Color) = (1.00, 0.74, 0.22, 1)
        _MidColor  ("流动中间色", Color) = (0.82, 0.50, 0.10, 1)
        _DeepColor ("边缘深金色（暗部）", Color) = (0.42, 0.20, 0.03, 1)

        // ===== 流动 =====
        _FlowSpeed   ("流动速度", Range(0, 3)) = 0.6
        _FlowScale   ("流动纹理密度", Range(1, 40)) = 10
        _FlowDistort ("流动扭曲", Range(0, 1)) = 0.35

        // ===== 边缘深浅 =====
        _RimPower    ("边缘范围（越小越宽）", Range(0.1, 8)) = 2.5
        _RimStrength ("边缘深色强度", Range(0, 1)) = 0.9

        // ===== 发光 =====
        _Brightness ("整体亮度", Range(0, 6)) = 1.4
        _Alpha      ("整体透明度", Range(0, 1)) = 1.0

        // ===== 星光（星星点点的金色光芒）=====
        _SparkleColor   ("星光颜色", Color) = (1.00, 0.95, 0.65, 1)
        _SparkleDensity ("星光密度", Range(1, 128)) = 28
        _SparkleSize    ("星光大小", Range(0, 0.4)) = 0.12
        _SparkleSpeed   ("星光闪烁速度", Range(0, 6)) = 1.2
        _SparkleAmount  ("星光强度", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        // 深色边缘需要“能压暗”的混合方式，故用 Alpha 混合而非 Additive；
        // 若只要纯发光、不要深浅，可改为：Blend One One
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MidColor;
                float4 _DeepColor;
                float  _FlowSpeed;
                float  _FlowScale;
                float  _FlowDistort;
                float  _RimPower;
                float  _RimStrength;
                float  _Brightness;
                float  _Alpha;
                float4 _SparkleColor;
                float  _SparkleDensity;
                float  _SparkleSize;
                float  _SparkleSpeed;
                float  _SparkleAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            // ---------- 哈希 / 值噪声 / FBM ----------
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // 平滑插值

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += amp * ValueNoise(p);
                    p = p * 2.03 + float2(7.1, 3.7);
                    amp *= 0.5;
                }
                return v;
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // ---------- 星光：网格哈希 + 随机游走 + 快闪 ----------
            float Sparkle(float2 uv, float time)
            {
                float2 g = uv * _SparkleDensity;
                float2 id = floor(g);
                float2 f = frac(g);

                float sparkle = 0.0;
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = id + float2(x, y);
                        float2 rnd = Hash22(cell);

                        // 星光在细胞内的位置随时间游走
                        float2 pos = 0.5 + 0.45 * sin(time * _SparkleSpeed * (0.5 + rnd) + rnd * 6.28318);
                        float d = length(float2(x, y) + pos - f);

                        // 闪烁：随机相位，pow 出“快闪即灭”的星星感
                        float twinkle = sin(time * _SparkleSpeed * (1.0 + rnd.x * 3.0) + rnd.y * 6.28318);
                        twinkle = pow(max(0.0, twinkle), 6.0);

                        // 中心亮、向边缘衰减
                        float falloff = 1.0 - smoothstep(0.0, max(_SparkleSize, 0.0001), d);
                        sparkle = max(sparkle, twinkle * falloff);
                    }
                }
                return sparkle;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float ndv = saturate(dot(normalWS, normalize(viewDirWS)));

                // ---- 流动：domain warp 让噪声沿时间滚动，模拟液态金属 ----
                float t = _Time.y * _FlowSpeed;
                float2 flowUV = input.uv * _FlowScale;

                float warp  = Fbm(flowUV + float2(t * 0.5, t * 0.2));
                float flow1 = Fbm(flowUV + float2(t, 0.0) + warp * _FlowDistort);
                float flow2 = Fbm(flowUV - float2(t * 0.7, t * 0.35) + warp * _FlowDistort);
                float flow  = saturate(flow1 * 0.65 + flow2 * 0.35);

                // ---- 颜色：深金(暗) -> 中间色 -> 亮金(亮)，由 flow 控制明暗层次 ----
                float3 col = lerp(_DeepColor.rgb, _MidColor.rgb, flow);
                col = lerp(col, _BaseColor.rgb, smoothstep(0.55, 1.0, flow));

                // ---- 边缘：越靠近轮廓，越压暗到深金色 ----
                float rim = pow(1.0 - ndv, _RimPower) * _RimStrength;
                col = lerp(col, _DeepColor.rgb, rim);

                // ---- 星光 ----
                float sparkle = Sparkle(input.uv, _Time.y);
                col += _SparkleColor.rgb * sparkle * _SparkleAmount;

                // ---- 发光强度 ----
                col *= _Brightness;

                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}
