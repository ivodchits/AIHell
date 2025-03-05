Shader "UI/Vignette"
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
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Calculate vignette effect with added rectangularity factor
                float vignette = smoothVignette(IN.texcoord, _VignetteRoundness, _VignetteSmoothness, 
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