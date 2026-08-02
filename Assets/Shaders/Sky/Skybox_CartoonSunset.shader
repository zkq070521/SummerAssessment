Shader "Custom/Skybox_CartoonSunset"
{
    Properties
    {
        // ========== 天空分层色 ==========
        _SkyTop ("顶部颜色", Color) = (0.05, 0.02, 0.15, 1)       // 深蓝紫（夜空）
        _SkyUpper ("中上颜色", Color) = (0.35, 0.08, 0.25, 1)     // 紫罗兰
        _SkyMid ("中部颜色", Color) = (0.8, 0.25, 0.15, 1)        // 橙粉（晚霞主色）
        _SkyHorizon ("地平线颜色", Color) = (1.0, 0.6, 0.2, 1)     // 暖橙黄（地平线光）
        _SkyBottom ("底部颜色", Color) = (0.6, 0.15, 0.05, 1)     // 暗红棕（地平线下）

        // ========== 分层过渡位置 ==========
        _UpperPos ("上层过渡位置", Range(0, 1)) = 0.7
        _MidPos ("中部过渡位置", Range(0, 1)) = 0.45
        _HorizonPos ("地平线位置", Range(0, 1)) = 0.3
        _BottomPos ("底部过渡位置", Range(0, 1)) = 0.1

        // ========== 过渡平滑度 ==========
        _TransitionSmooth ("过渡平滑度", Range(0.01, 0.5)) = 0.15

        // ========== 地平线光晕 ==========
        _HorizonGlowColor ("地平线光晕颜色", Color) = (1.0, 0.7, 0.3, 1)
        _HorizonGlowWidth ("地平线光晕宽度", Range(0.01, 0.5)) = 0.12
        _HorizonGlowIntensity ("地平线光晕强度", Range(0, 2)) = 0.8

        // ========== 太阳 ==========
        [HDR] _SunColor ("太阳颜色", Color) = (1.0, 0.85, 0.3, 1)
        _SunSize ("太阳核心大小", Range(0.005, 0.2)) = 0.1
        _SunGlowSize ("太阳光晕大小", Range(0.01, 0.4)) = 0.12
        _SunGlowIntensity ("太阳光晕强度", Range(0, 2)) = 0.8
        _SunHeight ("太阳高度", Range(-0.3, 0.7)) = 0.25

        // ========== 云 ==========
        _CloudColor ("云颜色", Color) = (1.0, 0.75, 0.55, 1)
        _CloudOpacity ("云不透明度", Range(0, 1)) = 0.35
        _CloudDensity ("云密度", Range(0, 1)) = 0.4
        _CloudSpeed ("云速度", Range(0, 0.1)) = 0.02
        _CloudScale ("云缩放", Range(0.5, 5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyTop, _SkyUpper, _SkyMid, _SkyHorizon, _SkyBottom;
                float _UpperPos, _MidPos, _HorizonPos, _BottomPos;
                float _TransitionSmooth;
                float4 _HorizonGlowColor;
                float _HorizonGlowWidth, _HorizonGlowIntensity;
                float4 _SunColor;
                float _SunSize, _SunGlowSize, _SunGlowIntensity, _SunHeight;
                float4 _CloudColor;
                float _CloudOpacity, _CloudDensity, _CloudSpeed, _CloudScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 worldPos = mul(unity_ObjectToWorld, input.positionOS).xyz;
                output.direction = normalize(worldPos);
                return output;
            }

            // ── 简单 2D 噪声 ──
            float hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash2(i), hash2(i + float2(1, 0)), f.x),
                    lerp(hash2(i + float2(0, 1)), hash2(i + float2(1, 1)), f.x),
                    f.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    value += amp * noise2D(p * freq);
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return value;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float h = dir.y * 0.5 + 0.5; // [0, 1], 0=bottom, 1=top

                // ═══════════════════════════════════════════════════
                // 1. 平滑天空渐变
                // ═══════════════════════════════════════════════════
                // 用 smoothstep 在 5 个颜色之间做自然过渡
                float t_upper     = smoothstep(_UpperPos - _TransitionSmooth, _UpperPos + _TransitionSmooth, h);
                float t_mid       = smoothstep(_MidPos   - _TransitionSmooth, _MidPos   + _TransitionSmooth, h);
                float t_horizon   = smoothstep(_HorizonPos - _TransitionSmooth, _HorizonPos + _TransitionSmooth, h);
                float t_bottom    = smoothstep(_BottomPos - _TransitionSmooth, _BottomPos + _TransitionSmooth, h);

                // 逐层 lerp
                float3 layer1 = lerp(_SkyBottom.rgb, _SkyHorizon.rgb, t_bottom);
                float3 layer2 = lerp(layer1,           _SkyMid.rgb,     t_horizon);
                float3 layer3 = lerp(layer2,           _SkyUpper.rgb,   t_mid);
                float3 skyColor = lerp(layer3,          _SkyTop.rgb,     t_upper);

                // ═══════════════════════════════════════════════════
                // 2. 地平线光晕（宽而柔和的暖光带）
                // ═══════════════════════════════════════════════════
                float horizonDist = abs(h - _HorizonPos);
                float horizonGlow = exp(-horizonDist / _HorizonGlowWidth);
                horizonGlow *= _HorizonGlowIntensity;
                skyColor = lerp(skyColor, _HorizonGlowColor.rgb, horizonGlow * _HorizonGlowColor.a);

                // ═══════════════════════════════════════════════════
                // 3. 太阳
                // ═══════════════════════════════════════════════════
                float sunAngle = _SunHeight * 3.14159 * 0.5;
                float3 sunDir = normalize(float3(0, sin(sunAngle), cos(sunAngle)));
                float sunDist = 1.0 - dot(dir, sunDir); // 角距离：0=太阳中心

                // 太阳核心：清晰圆盘
                float sunCore = step(sunDist, _SunSize);

                // 太阳光晕：从核心边缘向外柔光衰减
                float sunGlow = saturate(1.0 - sunDist / _SunGlowSize);
                sunGlow = sunGlow * sunGlow * _SunGlowIntensity;

                // 合成
                float3 sunContribution = _SunColor.rgb * (sunCore + sunGlow);

                // ═══════════════════════════════════════════════════
                // 4. 云
                // ═══════════════════════════════════════════════════
                float2 cloudUV = dir.xz * _CloudScale + _Time.y * _CloudSpeed;
                float cloud = fbm(cloudUV);
                cloud = smoothstep(1.0 - _CloudDensity, 1.0, cloud);

                // 云主要分布在地平线附近
                float cloudHorizonMask = 1.0 - abs(dir.y) * 1.2;
                cloudHorizonMask = smoothstep(0.1, 0.6, cloudHorizonMask);
                cloud *= cloudHorizonMask;

                // 云被夕阳照亮（颜色偏向暖色）
                float3 cloudLit = _CloudColor.rgb * (1.0 + horizonGlow * 0.5);

                // ═══════════════════════════════════════════════════
                // 5. 合成
                // ═══════════════════════════════════════════════════
                float3 finalColor = skyColor + sunContribution;
                finalColor = lerp(finalColor, cloudLit, cloud * _CloudOpacity * _CloudColor.a);
                finalColor = saturate(finalColor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
