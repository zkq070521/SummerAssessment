Shader "Custom/StarRailToon"
{
    Properties
    {
        // ========== 基础颜色 ==========
        _BaseColor ("主颜色", Color) = (1, 1, 1, 1)
        _ShadowColor ("阴影颜色", Color) = (0.4, 0.4, 0.5, 1)
        _ShadowThreshold ("阴影硬边阈值", Range(0, 0.8)) = 0.3
        
        // ========== 高光 ==========
        _SpecularColor ("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularSize ("高光大小", Range(0, 1)) = 0.3
        _SpecularThreshold ("高光硬边阈值", Range(0, 1)) = 0.5
        
        // ========== 边缘光 ==========
        _RimColor ("边缘光颜色", Color) = (0.6, 0.8, 1, 1)
        _RimPower ("边缘光强度", Range(0, 5)) = 2.0
        
        // ========== 纹理（多张贴图） ==========
        _MainTex ("① 主颜色贴图", 2D) = "white" {}
        _RampTex ("② 渐变贴图（控制阴影颜色）", 2D) = "white" {}
        _NormalMap ("③ 法线贴图", 2D) = "bump" {}
        _SpecularMask ("④ 高光遮罩（白=反光，黑=不反光）", 2D) = "white" {}
        _EmissionMap ("⑤ 发光贴图", 2D) = "black" {}
        _EmissionColor ("发光颜色", Color) = (0, 0, 0, 1)
        
        // ========== 纹理开关（方便调试） ==========
        [Toggle] _UseRamp ("使用渐变贴图", Float) = 1
        [Toggle] _UseNormal ("使用法线贴图", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // ========== 包含URP库 ==========
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            // ========== 声明属性 ==========
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float4 _SpecularColor;
                float _SpecularSize;
                float _SpecularThreshold;
                float4 _RimColor;
                float _RimPower;
                float4 _MainTex_ST;
                float4 _EmissionColor;
                float _UseRamp;
                float _UseNormal;
            CBUFFER_END
            
            // ========== 声明所有贴图 ==========
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            TEXTURE2D(_SpecularMask);
            SAMPLER(sampler_SpecularMask);
            
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            
            // ========== 顶点输入输出 ==========
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;    // 法线贴图需要切线
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;   // 切线空间传给片元
                float3 bitangentWS : TEXCOORD3; // 副切线
                float3 positionWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
            };
            
            // ========== 顶点着色器 ==========
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 位置转换
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // 法线转换
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // 切线转换（用于法线贴图）
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                
                // 位置转换
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // 视角方向
                float3 viewDir = GetWorldSpaceViewDir(output.positionWS);
                output.viewDirWS = normalize(viewDir);
                
                // UV传递
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }
            
            // ========== 片元着色器 ==========
            half4 frag(Varyings input) : SV_Target
            {
                // ----- 1. 采样主颜色贴图 -----
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // ----- 2. 采样法线贴图（可选） -----
                float3 normal = normalize(input.normalWS);
                if (_UseNormal > 0.5)
                {
                    float3 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv).rgb;
                    normalMap = normalMap * 2.0 - 1.0; // 从0~1映射到-1~1
                    
                    // 构建TBN矩阵，把法线从切线空间转到世界空间
                    float3 T = normalize(input.tangentWS);
                    float3 B = normalize(input.bitangentWS);
                    float3 N = normalize(input.normalWS);
                    float3x3 TBN = float3x3(T, B, N);
                    
                    normal = normalize(mul(normalMap, TBN));
                }
                
                float3 viewDir = normalize(input.viewDirWS);
                
                // ----- 3. 主光源 -----
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;
                float atten = mainLight.shadowAttenuation;
                
                // ----- 4. 漫反射：半兰伯特 + 渐变贴图 -----
                float NdotL = dot(normal, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5;
                
                // 硬边阈值裁切
                float shadowStep = step(_ShadowThreshold, halfLambert);
                shadowStep = min(shadowStep, atten);
                
                float3 diffuse;
                if (_UseRamp > 0.5)
                {
                    // ===== 使用渐变贴图控制阴影颜色 =====
                    // 用halfLambert作为UV的横坐标，采样Ramp贴图
                    float rampUV = halfLambert;
                    // 用shadowStep把暗部压到0~阈值之间
                    float3 rampColor = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampUV, 0.5)).rgb;
                    diffuse = rampColor * _BaseColor.rgb * texColor.rgb * lightColor;
                    // 用shadowStep控制明暗，但Ramp贴图已经包含了过渡信息
                    diffuse *= lerp(0.8, 1.0, shadowStep);
                }
                else
                {
                    // ===== 不使用渐变贴图，用纯色阴影 =====
                    float3 lightPart = _BaseColor.rgb * texColor.rgb * lightColor;
                    float3 shadowPart = _ShadowColor.rgb * texColor.rgb * 0.5;
                    diffuse = lerp(shadowPart, lightPart, shadowStep);
                }
                
                // ----- 5. 高光 + 遮罩 -----
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = dot(normal, halfDir);
                
                // 采样高光遮罩（白色=允许高光，黑色=禁止高光）
                float specMask = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, input.uv).r;
                specMask = lerp(0.1, 1.0, specMask); // 黑色区域也给一点高光，避免死黑
                
                float specIntensity = smoothstep(_SpecularThreshold, _SpecularThreshold + _SpecularSize, NdotH);
                specIntensity *= shadowStep * specMask;
                float3 specular = specIntensity * _SpecularColor.rgb * lightColor;
                
                // ----- 6. 边缘光 -----
                float fresnel = 1.0 - dot(normal, viewDir);
                fresnel = pow(fresnel, _RimPower);
                float3 rim = fresnel * _RimColor.rgb * lightColor;
                
                // ----- 7. 自发光（发光贴图） -----
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                emission *= _EmissionColor.rgb;
                
                // ----- 8. 环境光 -----
                float3 ambient = SampleSH(normal) * 0.3;
                
                // ----- 9. 合成最终颜色 -----
                float3 finalColor = diffuse + specular + rim + ambient + emission;
                
                return half4(finalColor, 1.0);
            }
            
            ENDHLSL
        }
        
        // ========== 阴影投射Pass ==========
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_shadowcaster
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}