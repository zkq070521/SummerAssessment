Shader "Custom/Skybox_CartoonSunset"
{
    Properties
    {
        // ========== 四层硬边渐变色（卡通风格） ==========
        _Color1 ("① 顶部颜色", Color) = (0.05, 0.05, 0.2, 1)    // 深蓝紫
        _Color2 ("② 中上颜色", Color) = (0.4, 0.1, 0.5, 1)       // 紫红
        _Color3 ("③ 中下颜色", Color) = (1.0, 0.5, 0.1, 1)       // 亮橙
        _Color4 ("④ 底部颜色", Color) = (0.8, 0.2, 0.1, 1)       // 暖红
        
        // ========== 分层位置控制 ==========
        _Height1 ("分层1位置", Range(0, 1)) = 0.75
        _Height2 ("分层2位置", Range(0, 1)) = 0.45
        _Height3 ("分层3位置", Range(0, 1)) = 0.15
        
        // ========== 太阳 ==========
        _SunColor ("太阳颜色", Color) = (1.0, 0.9, 0.5, 1)
        _SunSize ("太阳大小", Range(0.01, 0.3)) = 0.12
        _SunHeight ("太阳高度", Range(-0.5, 0.8)) = 0.25
        
        // ========== 卡通云 ==========
        _CloudColor ("云颜色", Color) = (1.0, 0.85, 0.7, 1)
        _CloudDensity ("云密度", Range(0, 0.5)) = 0.2
        _CloudSpeed ("云速度", Range(0, 0.1)) = 0.03
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
                float4 _Color1, _Color2, _Color3, _Color4;
                float _Height1, _Height2, _Height3;
                float4 _SunColor;
                float _SunSize, _SunHeight;
                float4 _CloudColor;
                float _CloudDensity, _CloudSpeed;
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
            
            // ===== 简单噪声（用于云） =====
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x),
                           lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x),
                           f.y);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float height = dir.y * 0.5 + 0.5; // 映射到0~1
                
                // ============================================================
                // 1. 硬边分层渐变（干净、卡通）
                // ============================================================
                float3 skyColor;
                if (height > _Height1)
                    skyColor = _Color1.rgb;
                else if (height > _Height2)
                    skyColor = _Color2.rgb;
                else if (height > _Height3)
                    skyColor = _Color3.rgb;
                else
                    skyColor = _Color4.rgb;
                
                // 层与层之间加一点平滑过渡（只有一点点，保持卡通感）
                float blend = 0.1;
                float h = height;
                float3 color1 = lerp(_Color2.rgb, _Color1.rgb, saturate((h - _Height1) / blend + 0.5));
                float3 color2 = lerp(_Color3.rgb, _Color2.rgb, saturate((h - _Height2) / blend + 0.5));
                float3 color3 = lerp(_Color4.rgb, _Color3.rgb, saturate((h - _Height3) / blend + 0.5));
                
                if (h > _Height1) skyColor = color1;
                else if (h > _Height2) skyColor = color2;
                else if (h > _Height3) skyColor = color3;
                else skyColor = _Color4.rgb;
                
                // ============================================================
                // 2. 太阳（干净、硬边、发光）
                // ============================================================
                float sunAngle = _SunHeight * 3.14159 * 0.5;
                float3 sunDir = float3(0, sin(sunAngle), cos(sunAngle));
                float sunDot = dot(dir, sunDir);
                
                // 太阳主体（硬边）
                float sun = step(1.0 - _SunSize, sunDot);
                // 外发光（柔和一点）
                float glow = smoothstep(1.0 - _SunSize * 3.0, 1.0 - _SunSize, sunDot) * 0.4;
                
                float3 sunColor = _SunColor.rgb * (sun + glow);
                
                // ============================================================
                // 3. 卡通云（干净、低密度、不遮挡天空）
                // ============================================================
                float2 cloudUV = dir.xz * 1.5 + _Time.y * _CloudSpeed;
                float cloud = noise(cloudUV) * noise(cloudUV * 2.3 + 1.7);
                cloud = saturate(cloud * 1.2 - 0.3); // 压缩灰度范围
                
                // 只在地平线附近出现云
                float cloudMask = 1.0 - abs(dir.y) * 0.8;
                cloud = cloud * cloudMask * _CloudDensity * 4.0;
                
                // 硬边云（卡通风格）
                float cloudStep = step(0.3, cloud);
                float3 cloudColor = _CloudColor.rgb * cloudStep * 0.6;
                
                // ============================================================
                // 4. 合成
                // ============================================================
                // 天空颜色直接作为底色，云叠加在上面（不是混合，是叠加）
                float3 finalColor = skyColor + sunColor + cloudColor;
                
                // 增加饱和度，让颜色更鲜艳
                finalColor = saturate(finalColor);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}