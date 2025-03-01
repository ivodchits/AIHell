Shader "Hidden/RealityBreak"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RealityBreak ("Reality Break", Float) = 1
        _GlitchAmount ("Glitch Amount", Float) = 0.3
        _ChromaticAberration ("Chromatic Aberration", Float) = 0.02
        _WarpIntensity ("Reality Warp", Float) = 0.5
        _FractureScale ("Fracture Scale", Float) = 10
        _VoidColor ("Void Color", Color) = (0,0,0,1)
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
            float _RealityBreak;
            float _GlitchAmount;
            float _ChromaticAberration;
            float _WarpIntensity;
            float _FractureScale;
            float4 _VoidColor;

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            float voronoi(float2 uv)
            {
                float2 fl = floor(uv);
                float2 fr = frac(uv);
                float res = 8.0;
                
                for(int j = -1; j <= 1; j++)
                {
                    for(int i = -1; i <= 1; i++)
                    {
                        float2 p = float2(i,j);
                        float2 o = random(fl + p);
                        o = 0.5 + 0.5 * sin(_Time.y + 6.2831 * o);
                        float2 r = p - fr + o;
                        float d = dot(r,r);
                        res = min(res, d);
                    }
                }
                return sqrt(res);
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
                
                // Reality fracturing
                float fractureTime = floor(_Time.y * 20) * 0.5;
                float2 fracturePos = float2(random(float2(fractureTime, 9)), 
                                         random(float2(fractureTime, 5)));
                float fractureStrength = random(fracturePos);
                
                // Reality warping
                float2 warpUV = uv;
                float warpTime = _Time.y * 0.5;
                float2 warpCenter = float2(0.5 + sin(warpTime) * 0.2, 
                                         0.5 + cos(warpTime * 0.7) * 0.2);
                float2 warpVector = (warpUV - warpCenter) * 2.0;
                float warpDist = length(warpVector);
                float warpFactor = sin(warpDist * 10.0 - _Time.y * 2.0) * 0.1;
                warpUV += normalize(warpVector) * warpFactor * _WarpIntensity * _RealityBreak;
                
                // Voronoi fracturing
                float voronoiNoise = voronoi(uv * _FractureScale + _Time.y);
                float2 fractureOffset = float2(voronoiNoise * 0.02, voronoiNoise * 0.02) * _RealityBreak;
                warpUV += fractureOffset;
                
                // Glitch effect
                if (fractureStrength < _GlitchAmount * _RealityBreak)
                {
                    float glitchLine = floor(uv.y * 10) / 10;
                    float glitchOffset = (random(float2(glitchLine, fractureTime)) - 0.5) * 0.1;
                    warpUV.x += glitchOffset * _RealityBreak;
                }
                
                // Sample with chromatic aberration
                fixed4 col;
                col.r = tex2D(_MainTex, warpUV + float2(_ChromaticAberration, 0) * _RealityBreak).r;
                col.g = tex2D(_MainTex, warpUV).g;
                col.b = tex2D(_MainTex, warpUV - float2(_ChromaticAberration, 0) * _RealityBreak).b;
                col.a = 1;
                
                // Reality void effect
                float voidMask = smoothstep(0.4, 0.6, voronoiNoise);
                col = lerp(col, _VoidColor, voidMask * _RealityBreak * 0.5);
                
                // Edge distortion
                float2 edgeUV = i.screenPos.xy / i.screenPos.w;
                float edgeDistortion = length(edgeUV - 0.5) * 2.0;
                col.rgb *= 1.0 - (edgeDistortion * _RealityBreak * 0.3);
                
                // Pulse effect
                float pulse = sin(_Time.y * 2.0 + uv.y * 10.0) * 0.5 + 0.5;
                col = lerp(col, col * float4(1.2, 0.8, 1.2, 1), pulse * _RealityBreak * 0.2);
                
                // Add subtle film grain
                float grain = random(uv + _Time.y) * 0.1;
                col.rgb += grain * _RealityBreak;
                
                return col;
            }
            ENDCG
        }
    }
}