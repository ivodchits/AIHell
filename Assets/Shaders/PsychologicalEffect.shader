Shader "Hidden/PsychologicalEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EffectIntensity ("Effect Intensity", Float) = 1
        _UneaseFactor ("Unease Factor", Float) = 0.5
        _ShadowIntensity ("Shadow Intensity", Float) = 0.3
        _PulseRate ("Pulse Rate", Float) = 1
        _DistortionScale ("Distortion Scale", Float) = 10
        _SubtleNoiseScale ("Subtle Noise Scale", Float) = 30
        _ShadowColor ("Shadow Color", Color) = (0.1,0.05,0.15,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
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
            float _EffectIntensity;
            float _UneaseFactor;
            float _ShadowIntensity;
            float _PulseRate;
            float _DistortionScale;
            float _SubtleNoiseScale;
            float4 _ShadowColor;

            // Perlin noise function
            float2 hash2(float2 p)
            {
                p = float2(dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float perlinNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float2 u = f*f*(3.0-2.0*f);

                return lerp(lerp(dot(hash2(i + float2(0.0,0.0)), f - float2(0.0,0.0)), 
                               dot(hash2(i + float2(1.0,0.0)), f - float2(1.0,0.0)), u.x),
                          lerp(dot(hash2(i + float2(0.0,1.0)), f - float2(0.0,1.0)), 
                               dot(hash2(i + float2(1.0,1.0)), f - float2(1.0,1.0)), u.x), u.y);
            }

            // Fractal Brownian Motion
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 5; i++)
                {
                    value += amplitude * perlinNoise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Subtle psychological warping
                float time = _Time.y * _PulseRate;
                float warpNoise = fbm(uv * _DistortionScale + time * 0.1);
                float2 warpUV = uv + warpNoise * _UneaseFactor * _EffectIntensity * 0.02;
                
                // Creeping shadow effect
                float2 shadowUV = i.screenPos.xy / i.screenPos.w;
                float shadowNoise = fbm(shadowUV * _SubtleNoiseScale + time * 0.05);
                float shadowMask = smoothstep(0.3, 0.7, shadowNoise);
                
                // Sample main texture with warping
                fixed4 col = tex2D(_MainTex, warpUV);
                
                // Apply subtle color manipulation
                float subtleNoise = fbm(uv * _SubtleNoiseScale + time * 0.2) * 0.1;
                col.rgb = lerp(col.rgb, col.rgb * (1.0 + subtleNoise), _EffectIntensity * 0.3);
                
                // Add creeping shadows
                float shadowEffect = shadowMask * _ShadowIntensity * _EffectIntensity;
                col.rgb = lerp(col.rgb, _ShadowColor.rgb, shadowEffect);
                
                // Subtle pulsing vignette
                float2 vignetteUV = (shadowUV - 0.5) * 2.0;
                float vignette = 1.0 - dot(vignetteUV, vignetteUV) * 0.2;
                float vignetteIntensity = (sin(time) * 0.5 + 0.5) * _EffectIntensity * 0.2;
                col.rgb *= lerp(1.0, vignette, vignetteIntensity);
                
                // Add subtle color bleeding
                float2 bleedOffset = float2(
                    sin(uv.y * 10.0 + time) * 0.002,
                    cos(uv.x * 10.0 + time) * 0.002
                ) * _EffectIntensity;
                
                col.r += tex2D(_MainTex, warpUV + bleedOffset).r * 0.1 * _EffectIntensity;
                col.b += tex2D(_MainTex, warpUV - bleedOffset).b * 0.1 * _EffectIntensity;
                
                // Subtle noise grain
                float grain = frac(sin(dot(uv + time, float2(12.9898, 78.233))) * 43758.5453);
                col.rgb += (grain - 0.5) * 0.05 * _EffectIntensity;
                
                // Edge darkening
                float edge = 1.0 - length(vignetteUV) * 0.5;
                col.rgb *= lerp(1.0, edge, _EffectIntensity * 0.3);
                
                return col;
            }
            ENDCG
        }
    }
}