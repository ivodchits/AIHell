Shader "Hidden/EmotionalComposition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Psychological Intensity", Float) = 1
        _EmotionalTint ("Emotional Tint", Color) = (1,1,1,1)
        _DistortionOffset ("Distortion Offset", Vector) = (0,0,0,0)
        _ChromaticAberration ("Chromatic Aberration", Float) = 0.02
        _GrainIntensity ("Grain Intensity", Float) = 0.1
        _VignetteIntensity ("Vignette Intensity", Float) = 0.4
        _Time ("Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            float4 _EmotionalTint;
            float2 _DistortionOffset;
            float _ChromaticAberration;
            float _GrainIntensity;
            float _VignetteIntensity;
            float _Time;

            // Noise functions for psychological effects
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float2 c00 = i;
                float2 c10 = i + float2(1, 0);
                float2 c01 = i + float2(0, 1);
                float2 c11 = i + float2(1, 1);

                float n00 = hash(c00);
                float n10 = hash(c10);
                float n01 = hash(c01);
                float n11 = hash(c11);

                float nx0 = lerp(n00, n10, f.x);
                float nx1 = lerp(n01, n11, f.x);
                return lerp(nx0, nx1, f.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Psychological distortion
                float2 distortionUV = uv;
                float distortTime = _Time * (1.0 + _Intensity * 0.5);
                
                // Add emotional wavering
                float emotionalWave = sin(distortTime * 2.0 + uv.y * 10.0) * 0.01 * _Intensity;
                distortionUV.x += emotionalWave;
                
                // Add psychological noise distortion
                float noiseDistortion = fbm(uv * 5.0 + distortTime) * 0.02 * _Intensity;
                distortionUV += noiseDistortion;
                
                // Apply main distortion
                distortionUV += _DistortionOffset * _Intensity;

                // Sample with chromatic aberration
                float2 aberration = _ChromaticAberration * normalize(uv - 0.5) * _Intensity;
                fixed4 col;
                col.r = tex2D(_MainTex, distortionUV + aberration).r;
                col.g = tex2D(_MainTex, distortionUV).g;
                col.b = tex2D(_MainTex, distortionUV - aberration).b;
                col.a = 1;

                // Apply emotional tinting
                col.rgb = lerp(col.rgb, col.rgb * _EmotionalTint.rgb, _Intensity * 0.5);

                // Add psychological grain
                float2 grainUV = i.screenPos.xy / i.screenPos.w;
                float grain = noise(grainUV * 500.0 + _Time * 50.0);
                col.rgb += (grain - 0.5) * _GrainIntensity * _Intensity;

                // Add emotional vignette
                float2 vignetteUV = (uv - 0.5) * 2.0;
                float vignette = 1.0 - dot(vignetteUV, vignetteUV);
                vignette = saturate(vignette * (1.0 + sin(_Time * 2.0) * 0.1 * _Intensity));
                col.rgb *= lerp(1.0, vignette, _VignetteIntensity * _Intensity);

                // Add psychological edge enhancement
                float2 edgeOffset = float2(0.001, 0.001) * _Intensity;
                float edge = 0.0;
                edge += abs(tex2D(_MainTex, uv + edgeOffset).r - tex2D(_MainTex, uv - edgeOffset).r);
                edge += abs(tex2D(_MainTex, uv + edgeOffset.yx).r - tex2D(_MainTex, uv - edgeOffset.yx).r);
                col.rgb = lerp(col.rgb, col.rgb * (1.0 - edge * 2.0), _Intensity * 0.3);

                // Add subtle pulsing based on psychological intensity
                float pulse = sin(_Time * 3.0) * 0.5 + 0.5;
                col.rgb *= 1.0 + pulse * _Intensity * 0.1;

                // Add emotional color shifting
                float shift = sin(_Time * 2.0) * 0.1 * _Intensity;
                col.r += shift;
                col.b -= shift * 0.5;

                return col;
            }
            ENDCG
        }
    }
}