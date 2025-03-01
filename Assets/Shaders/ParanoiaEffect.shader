Shader "Hidden/ParanoiaEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Effect Intensity", Float) = 1
        _DistortionAmount ("Distortion Amount", Float) = 0.2
        _PulseSpeed ("Pulse Speed", Float) = 1
        _NoiseScale ("Noise Scale", Float) = 10
        _EdgeIntensity ("Edge Intensity", Float) = 0.5
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
            float _Intensity;
            float _DistortionAmount;
            float _PulseSpeed;
            float _NoiseScale;
            float _EdgeIntensity;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 ip = floor(p);
                float2 u = frac(p);
                u = u*u*(3.0-2.0*u);
                
                float res = lerp(
                    lerp(rand(ip), rand(ip+float2(1.0,0.0)), u.x),
                    lerp(rand(ip+float2(0.0,1.0)), rand(ip+float2(1.0,1.0)), u.x),
                    u.y);
                return res*res;
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
                
                // Time-based distortion
                float time = _Time.y * _PulseSpeed;
                float pulse = sin(time) * 0.5 + 0.5;
                
                // Add noise-based distortion
                float2 noiseUV = uv * _NoiseScale + float2(time * 0.5, time * 0.3);
                float noiseValue = noise(noiseUV);
                
                // Apply paranoid distortion
                uv.x += sin(uv.y * 10 + time) * _DistortionAmount * pulse * _Intensity * noiseValue;
                uv.y += cos(uv.x * 10 + time) * _DistortionAmount * pulse * _Intensity * noiseValue;
                
                // Sample main texture
                fixed4 col = tex2D(_MainTex, uv);
                
                // Add edge detection
                float2 edgeOffset = float2(0.001, 0.001) * _EdgeIntensity;
                float edge = 0;
                edge += abs(tex2D(_MainTex, uv + edgeOffset).r - tex2D(_MainTex, uv - edgeOffset).r);
                edge += abs(tex2D(_MainTex, uv + edgeOffset.yx).r - tex2D(_MainTex, uv - edgeOffset.yx).r);
                
                // Apply edge effect
                col.rgb = lerp(col.rgb, col.rgb * (1-edge), _Intensity * pulse);
                
                // Add subtle color shift
                float shift = sin(time * 2) * 0.01 * _Intensity;
                col.r = tex2D(_MainTex, uv + float2(shift, 0)).r;
                col.b = tex2D(_MainTex, uv - float2(shift, 0)).b;
                
                // Add vignette effect
                float2 vignetteUV = (i.screenPos.xy / i.screenPos.w) * 2.0 - 1.0;
                float vignette = 1.0 - dot(vignetteUV, vignetteUV) * 0.15;
                col.rgb *= vignette;
                
                return col;
            }
            ENDCG
        }
    }
}