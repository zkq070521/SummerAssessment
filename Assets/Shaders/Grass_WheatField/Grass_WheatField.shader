Shader "Custom/Grass_WheatField"
{
    Properties
    {
        // ========== 颜色 ==========
        _BaseColor ("主颜色", Color) = (0.6, 0.8, 0.3, 1)
        _TipColor ("麦穗颜色", Color) = (0.9, 0.8, 0.2, 1)
        _BottomColor ("底部颜色", Color) = (0.3, 0.5, 0.1, 1)
        
        // ========== 纹理 ==========
        _MainTex ("主纹理", 2D) = "white" {}
        
        // ========== 风吹参数 ==========
        _WindStrength ("🌊 风强度", Range(0, 0.5)) = 0.15
        _WindSpeed ("💨 风速", Range(0, 3)) = 1.5
        _WindFrequency ("〰️ 频率", Range(0.5, 4)) = 2.0
        
        // ========== 交互参数（角色推开） ==========
        _InteractionStrength ("👤 推开强度", Range(0, 1)) = 0.5
        _InteractionRadius ("👤 推开半径", Range(0.5, 5)) = 1.5

        // ========== Alpha Clip ==========
        _Cutoff ("✂️ 透明度裁剪阈值", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"  // 必须关批处理，否则顶点位置不对
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // ========== 属性声明 ==========
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _BottomColor;
                float4 _MainTex_ST;
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _InteractionStrength;
                float _InteractionRadius;
                float _Cutoff;
            CBUFFER_END
            
            // ========== 交互数据（从C#脚本传入） ==========
            float3 _InteractionPosition; // 角色世界坐标
            float _InteractionEnabled;   // 0或1
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;  // 顶点颜色（用于控制每棵草的高度）
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 color : TEXCOORD3; // 传递颜色到片元
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 pos = input.positionOS.xyz;
                float height = pos.y; // 麦秆高度
                
                // ============================================================
                // 1. 风吹拂动（和头发飘动一样）
                // ============================================================
                float time = _Time.y * _WindSpeed;
                
                // 用位置生成波浪，每棵草相位不同
                float wave1 = sin(time + pos.x * _WindFrequency + pos.z * _WindFrequency * 0.8);
                float wave2 = cos(time * 0.7 + pos.z * _WindFrequency * 1.2 + pos.x * 0.6);
                float windWave = (wave1 * 0.6 + wave2 * 0.4);
                
                // 越高的地方摆动越大（草尖摆动大，根部不动）
                float heightWeight = smoothstep(0, 1, height);
                
                // 沿X轴和Z轴摆动（水平方向）
                float3 windOffset = float3(
                    windWave * _WindStrength * heightWeight,
                    0,
                    windWave * _WindStrength * 0.5 * heightWeight
                );
                
                // ============================================================
                // 2. 角色交互：推开（麦秆向两侧倒伏）
                // ============================================================
                float3 worldPos = TransformObjectToWorld(pos);
                float3 interactionOffset = 0;
                
                if (_InteractionEnabled > 0.5)
                {
                    // 计算麦秆到角色的水平距离
                    float3 toGrass = worldPos - _InteractionPosition;
                    toGrass.y = 0; // 只取水平方向
                    float dist = length(toGrass);
                    
                    if (dist < _InteractionRadius)
                    {
                        // 距离越近，推开越强
                        float strength = 1.0 - dist / _InteractionRadius;
                        strength *= strength * _InteractionStrength;
                        
                        // 推开方向：从角色指向麦秆
                        float3 dir = normalize(toGrass + 0.001); // 防止零向量
                        
                        // 沿水平方向推开，高度越高推得越明显
                        float heightPush = smoothstep(0, 1, height);
                        interactionOffset = dir * strength * 0.5 * heightPush;
                        
                        // 让麦秆稍微弯下来（压低Y轴）
                        interactionOffset.y = -strength * 0.15 * heightPush;
                    }
                }
                
                // ============================================================
                // 3. 合成最终位置
                // ============================================================
                pos += windOffset + interactionOffset;
                
                // 顶点转换
                output.positionCS = TransformObjectToHClip(pos);
                output.positionWS = TransformObjectToWorld(pos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // ============================================================
                // 4. 颜色：根据高度混合
                // ============================================================
                float heightFactor = smoothstep(0, 1, height);
                output.color = lerp(_BottomColor.rgb, _TipColor.rgb, heightFactor);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // 采样纹理
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Alpha Clip：裁剪掉纹理透明区域，让麦子呈现叶片形状
                clip(texColor.a - _Cutoff);

                // 基础颜色 = 顶点颜色 × 纹理 × 主颜色
                float3 finalColor = input.color * texColor.rgb * _BaseColor.rgb;
                
                // 简单光照（让麦秆有立体感）
                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = dot(normal, mainLight.direction) * 0.5 + 0.5;
                finalColor *= lerp(0.5, 1.0, NdotL);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // ========== 阴影投射 ==========
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
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