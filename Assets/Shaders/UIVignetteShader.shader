Shader "UI/TVShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Vignette)]
        _VignetteColor ("Vignette Color", Color) = (0,0,0,1)
        _VignetteIntensity ("Vignette Intensity", Range(0,1)) = 0.5
        _VignetteRoundness ("Vignette Roundness", Range(0,2)) = 0.25  // Default to more rectangular
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01,3)) = 1.5
        [Toggle] _InnerGlow ("Inner Glow Effect", Float) = 0
        _RectangularityFactor ("Rectangularity", Range(0,5)) = 2.0    // New property to control rectangular shape

        [Header(TV Noise)]
        [Toggle] _EnableNoise ("Enable Noise Effect", Float) = 1
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.1
        _NoiseScale ("Noise Scale", Range(1,100)) = 50
        _NoiseSpeed ("Noise Speed", Range(0,10)) = 5
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.1
        _ScanlineCount ("Scanline Count", Range(1,100)) = 50
        _ScanlineSpeed ("Scanline Speed", Range(0,10)) = 2

        [Header(Pixelization)]
        [Toggle] _EnablePixelization ("Enable Pixelization", Float) = 1
        _PixelSize ("Pixel Size", Range(1,100)) = 8
        [Toggle] _SnapToPixel ("Snap To Pixel Grid", Float) = 1
        _ColorReduction ("Color Depth Reduction", Range(0,1)) = 0.5

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _VignetteColor;
            float _VignetteIntensity;
            float _VignetteRoundness;
            float _VignetteSmoothness;
            float _InnerGlow;
            float _RectangularityFactor;
            
            // TV Noise properties
            float _EnableNoise;
            float _NoiseIntensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            
            // Pixelization properties
            float _EnablePixelization;
            float _PixelSize;
            float _SnapToPixel;
            float _ColorReduction;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            // Hash function for noise generation
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.13);
                p3 += dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Random noise function
            float noise(float2 uv, float time)
            {
                float2 i = floor(uv * _NoiseScale);
                float2 f = frac(uv * _NoiseScale);
                
                // Add time to get movement
                i += time * _NoiseSpeed;
                
                // Four corner hash values
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                // Cubic Hermite interpolation
                f = f * f * (3.0 - 2.0 * f);
                
                // Bilinear interpolation for smoother noise
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Scanline effect
            float scanlines(float2 uv, float time)
            {
                float scanline = sin(uv.y * _ScanlineCount + time * _ScanlineSpeed);
                return (0.5 + 0.5 * scanline) * _ScanlineIntensity;
            }

            // Apply color depth reduction (fewer colors for retro look)
            float3 reduceColorDepth(float3 color, float reduction)
            {
                if (reduction <= 0) return color;
                
                // Calculate number of color steps (lower = fewer colors)
                float steps = lerp(256, 4, reduction);
                
                // Quantize the color
                return floor(color * steps) / steps;
            }
            
            // Pixelate UV coordinates
            float2 pixelateUV(float2 uv)
            {
                if (_EnablePixelization <= 0.5) return uv;
                
                float2 dimensions = float2(
                    _ScreenParams.x / _PixelSize, 
                    _ScreenParams.y / _PixelSize
                );
                
                if (_SnapToPixel > 0.5)
                {
                    // Snap to pixel grid
                    return floor(uv * dimensions) / dimensions;
                }
                else
                {
                    // Just pixelate without snapping
                    return (floor(uv * dimensions) + 0.5) / dimensions;
                }
            }

            float smoothVignette(float2 uv, float roundness, float smoothness, float intensity, float rectFactor) 
            {
                // Get distance from center (0.5, 0.5)
                float2 center = uv - 0.5;
                float2 absCenter = abs(center) * 2.0; // Scale to 0-1 range from center
                
                // Create rectangular vignette with rounded corners
                float rect = pow(
                    pow(absCenter.x, rectFactor) + 
                    pow(absCenter.y, rectFactor),
                    1.0 / rectFactor
                );
                
                // Create circular vignette
                float circ = length(absCenter);
                
                // Blend between rectangular and circular based on roundness
                // Lower roundness values = more rectangular
                float shape = lerp(rect, circ, roundness);
                
                // Apply smoothness and intensity
                float vignetteValue = pow(shape, smoothness);
                
                // Convert to 0-1 range where 1 = no effect, 0 = full vignette
                return saturate(1.0 - vignetteValue * intensity);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Apply pixelization effect to UV coordinates if enabled
                float2 pixelUV = pixelateUV(IN.texcoord);
                
                // Sample texture with pixelized UVs
                half4 color = (tex2D(_MainTex, pixelUV) + _TextureSampleAdd) * IN.color;

                // Apply TV noise effect if enabled
                if (_EnableNoise > 0.5) {
                    // Generate white noise
                    float staticNoise = noise(pixelUV, _Time.y);
                    
                    // Generate scanlines
                    float scan = scanlines(pixelUV, _Time.y);
                    
                    // Apply noise to the color
                    color.rgb = lerp(color.rgb, float3(staticNoise, staticNoise, staticNoise), _NoiseIntensity);
                    
                    // Apply scanlines - darker where scanlines are present
                    color.rgb -= float3(scan, scan, scan);
                }
                
                // Apply color depth reduction for retro look
                if (_EnablePixelization > 0.5) {
                    color.rgb = reduceColorDepth(color.rgb, _ColorReduction);
                }
                
                // Calculate vignette effect with added rectangularity factor
                float vignette = smoothVignette(pixelUV, _VignetteRoundness, _VignetteSmoothness, 
                                             _VignetteIntensity, _RectangularityFactor);
                
                // Apply vignette
                if (_InnerGlow > 0.5) {
                    // Inner glow mode - vignette is brightest at edges
                    color.rgb = lerp(color.rgb, _VignetteColor.rgb, 1.0 - vignette);
                } else {
                    // Standard vignette - darkest at edges
                    color.rgb = lerp(_VignetteColor.rgb, color.rgb, vignette);
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}