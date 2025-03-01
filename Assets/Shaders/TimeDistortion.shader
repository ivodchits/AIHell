Shader "Hidden/TimeDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TimeDistortion ("Time Distortion", Float) = 1
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 2
        _TimeEchoStrength ("Time Echo Strength", Float) = 0.5
        _EchoCount ("Echo Count", Range(1, 5)) = 3
        _TimeslipIntensity ("Timeslip Intensity", Float) = 0.3
        _TemporalNoiseScale ("Temporal Noise Scale", Float) = 20
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
            float _TimeDistortion;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _TimeEchoStrength;
            int _EchoCount;
            float _TimeslipIntensity;
            float _TemporalNoiseScale;

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float temporalNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float temporalFbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * temporalNoise(p * frequency);
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
                float time = _Time.y;
                
                // Temporal wave distortion
                float2 timeWaveUV = uv;
                float timeWave = sin(time * _WaveFrequency);
                
                timeWaveUV.x += sin(uv.y * 6.28318 + time) * _WaveAmplitude * timeWave * _TimeDistortion;
                timeWaveUV.y += cos(uv.x * 6.28318 + time) * _WaveAmplitude * timeWave * _TimeDistortion;
                
                // Time echo effect
                fixed4 col = fixed4(0,0,0,1);
                for(int j = 0; j < _EchoCount; j++)
                {
                    float echoTime = time - j * 0.1;
                    float2 echoUV = timeWaveUV;
                    
                    // Add temporal noise to each echo
                    float temporalDistortion = temporalFbm(echoUV * _TemporalNoiseScale + echoTime);
                    echoUV += temporalDistortion * _TimeslipIntensity * _TimeDistortion;
                    
                    // Sample with temporal offset
                    fixed4 echo = tex2D(_MainTex, echoUV);
                    float echoStrength = _TimeEchoStrength * (1.0 - float(j) / float(_EchoCount));
                    col += echo * echoStrength;
                }
                
                // Normalize the color
                col /= _EchoCount;
                
                // Add time-based color manipulation
                float timeFactor = sin(time) * 0.5 + 0.5;
                col = lerp(col, col * float4(1.2, 0.8, 0.8, 1), _TimeDistortion * timeFactor);
                
                // Add temporal artifacts
                float2 artifactUV = uv * _TemporalNoiseScale + time;
                float artifact = temporalFbm(artifactUV);
                float artifactMask = step(0.7, artifact) * _TimeDistortion * 0.3;
                col.rgb = lerp(col.rgb, 1 - col.rgb, artifactMask);
                
                // Time slice effect
                float timeSlice = floor(uv.y * 20) / 20;
                float sliceOffset = hash(float2(timeSlice, floor(time * 5))) * _TimeslipIntensity * _TimeDistortion;
                col.rgb = lerp(col.rgb, tex2D(_MainTex, uv + float2(sliceOffset, 0)).rgb, _TimeDistortion * 0.2);
                
                // Temporal vignette
                float2 vignetteUV = (i.screenPos.xy / i.screenPos.w - 0.5) * 2.0;
                float vignette = 1.0 - dot(vignetteUV, vignetteUV) * 0.3;
                float temporalVignette = lerp(1.0, vignette, _TimeDistortion * sin(time * 2) * 0.5 + 0.5);
                col.rgb *= temporalVignette;
                
                return col;
            }
            ENDCG
        }
    }
}