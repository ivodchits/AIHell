using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AIHell.Core.Data;

[RequireComponent(typeof(ImagePipeline))]
public class ImagePostProcessor : MonoBehaviour
{
    private Material postProcessMaterial;
    private Dictionary<string, Shader> effectShaders;
    private ImagePipeline pipeline;

    [System.Serializable]
    public class PostProcessEffect
    {
        public string name;
        public float intensity;
        public float duration;
        public AnimationCurve intensityCurve;
        public bool isActive;
    }

    private void Awake()
    {
        pipeline = GetComponent<ImagePipeline>();
    }

    private void OnEnable()
    {
        InitializeShaders();
    }

    private void OnDisable()
    {
        CleanupResources();
    }

    private void InitializeShaders()
    {
        try
        {
            effectShaders = new Dictionary<string, Shader>();

            // Create and validate shaders
            CreateShader("paranoia", ShaderImplementations.ParanoiaShader);
            CreateShader("temporal", ShaderImplementations.TemporalShader);
            CreateShader("reality", ShaderImplementations.RealityBreakShader);
            
            // Initialize base post-process material
            if (effectShaders.TryGetValue("paranoia", out Shader shader))
            {
                postProcessMaterial = new Material(shader);
            }
            else
            {
                Debug.LogError("Failed to initialize base post-process material");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing shaders: {ex.Message}");
        }
    }

    private void CreateShader(string name, string shaderSource)
    {
        try
        {
            var shader = ShaderUtil.CreateShaderAsset(shaderSource);
            if (shader != null)
            {
                effectShaders[name] = shader;
            }
            else
            {
                Debug.LogError($"Failed to create shader: {name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating shader {name}: {ex.Message}");
        }
    }

    private void CleanupResources()
    {
        if (postProcessMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(postProcessMaterial);
            else
                DestroyImmediate(postProcessMaterial);
        }

        foreach (var shader in effectShaders.Values)
        {
            if (shader != null && !shader.name.StartsWith("Hidden/"))
            {
                if (Application.isPlaying)
                    Destroy(shader);
                else
                    DestroyImmediate(shader);
            }
        }
        
        effectShaders.Clear();
    }

    public void ApplyPsychologicalEffect(Texture2D sourceTexture, PostProcessEffect effect)
    {
        if (postProcessMaterial == null || sourceTexture == null)
            return;

        // Create render texture for processing
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.ARGB32
        );

        // Apply effect based on type
        switch (effect.name.ToLower())
        {
            case "paranoia":
                ApplyParanoiaEffect(sourceTexture, renderTexture, effect);
                break;
            case "temporal":
                ApplyTemporalEffect(sourceTexture, renderTexture, effect);
                break;
            case "reality":
                ApplyRealityBreakEffect(sourceTexture, renderTexture, effect);
                break;
            default:
                ApplyDefaultEffect(sourceTexture, renderTexture, effect);
                break;
        }

        // Convert back to Texture2D
        Texture2D resultTexture = ConvertRenderTextureToTexture2D(renderTexture);

        // Cleanup
        RenderTexture.ReleaseTemporary(renderTexture);

        // Update source texture with processed result
        Graphics.CopyTexture(resultTexture, sourceTexture);
        Destroy(resultTexture);
    }

    private void ApplyParanoiaEffect(Texture2D source, RenderTexture destination, PostProcessEffect effect)
    {
        if (effectShaders.TryGetValue("paranoia", out Shader shader))
        {
            Material material = new Material(shader);
            
            // Set shader parameters
            material.SetTexture("_MainTex", source);
            material.SetFloat("_Intensity", effect.intensity);
            material.SetFloat("_DistortionAmount", effect.intensity * 0.2f);
            material.SetFloat("_PulseSpeed", 1.0f + effect.intensity);
            
            // Apply effect
            Graphics.Blit(source, destination, material);
            Destroy(material);
        }
    }

    private void ApplyTemporalEffect(Texture2D source, RenderTexture destination, PostProcessEffect effect)
    {
        if (effectShaders.TryGetValue("temporal", out Shader shader))
        {
            Material material = new Material(shader);
            
            // Set shader parameters
            material.SetTexture("_MainTex", source);
            material.SetFloat("_TimeDistortion", effect.intensity);
            material.SetFloat("_WaveAmplitude", effect.intensity * 0.1f);
            material.SetFloat("_WaveFrequency", 2.0f + effect.intensity);
            
            // Apply effect
            Graphics.Blit(source, destination, material);
            Destroy(material);
        }
    }

    private void ApplyRealityBreakEffect(Texture2D source, RenderTexture destination, PostProcessEffect effect)
    {
        if (effectShaders.TryGetValue("reality", out Shader shader))
        {
            Material material = new Material(shader);
            
            // Set shader parameters
            material.SetTexture("_MainTex", source);
            material.SetFloat("_RealityBreak", effect.intensity);
            material.SetFloat("_GlitchAmount", effect.intensity * 0.3f);
            material.SetFloat("_ChromaticAberration", effect.intensity * 0.02f);
            
            // Apply effect
            Graphics.Blit(source, destination, material);
            Destroy(material);
        }
    }

    private void ApplyDefaultEffect(Texture2D source, RenderTexture destination, PostProcessEffect effect)
    {
        // Set base psychological effect parameters
        postProcessMaterial.SetTexture("_MainTex", source);
        postProcessMaterial.SetFloat("_EffectIntensity", effect.intensity);
        
        // Apply effect
        Graphics.Blit(source, destination, postProcessMaterial);
    }

    private Texture2D ConvertRenderTextureToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    public void ApplyPsychologicalDistortion(Texture2D texture, PlayerAnalysisProfile profile)
    {
        var effect = new PostProcessEffect
        {
            name = DetermineEffectType(profile),
            intensity = CalculateEffectIntensity(profile),
            duration = 1.0f,
            intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };

        ApplyPsychologicalEffect(texture, effect);
    }

    private string DetermineEffectType(PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f)
            return "paranoia";
        if (profile.ObsessionLevel > 0.7f)
            return "reality";
        return "temporal";
    }

    private float CalculateEffectIntensity(PlayerAnalysisProfile profile)
    {
        return Mathf.Max(
            profile.FearLevel,
            profile.ObsessionLevel,
            profile.AggressionLevel
        );
    }

    public void CreateTransitionEffect(Texture2D sourceTexture, string transitionType, float duration)
    {
        var effect = new PostProcessEffect
        {
            name = transitionType,
            intensity = 1.0f,
            duration = duration,
            intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
            isActive = true
        };

        StartCoroutine(AnimateTransition(sourceTexture, effect));
    }

    private System.Collections.IEnumerator AnimateTransition(Texture2D texture, PostProcessEffect effect)
    {
        float elapsed = 0f;
        
        while (elapsed < effect.duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / effect.duration;
            
            effect.intensity = effect.intensityCurve.Evaluate(t);
            ApplyPsychologicalEffect(texture, effect);
            
            yield return null;
        }
    }

    // Shader implementations
    private static class ShaderImplementations
    {
        public static string ParanoiaShader = @"
Shader ""Hidden/ParanoiaEffect""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Intensity (""Effect Intensity"", Float) = 1
        _DistortionAmount (""Distortion Amount"", Float) = 0.2
        _PulseSpeed (""Pulse Speed"", Float) = 1
    }

    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Intensity;
            float _DistortionAmount;
            float _PulseSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Add pulsing distortion
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                uv.x += sin(uv.y * 10 + _Time.y) * _DistortionAmount * pulse * _Intensity;
                uv.y += cos(uv.x * 10 + _Time.y) * _DistortionAmount * pulse * _Intensity;
                
                // Sample texture with distorted UVs
                fixed4 col = tex2D(_MainTex, uv);
                
                // Add color shifting
                float shift = sin(_Time.y * 2) * 0.01 * _Intensity;
                col.r = tex2D(_MainTex, uv + float2(shift, 0)).r;
                col.b = tex2D(_MainTex, uv - float2(shift, 0)).b;
                
                return col;
            }
            ENDCG
        }
    }
}";

        public static string TemporalShader = @"
Shader ""Hidden/TemporalDistortion""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _TimeDistortion (""Time Distortion"", Float) = 1
        _WaveAmplitude (""Wave Amplitude"", Float) = 0.1
        _WaveFrequency (""Wave Frequency"", Float) = 2
    }

    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _TimeDistortion;
            float _WaveAmplitude;
            float _WaveFrequency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Add temporal wave distortion
                float timeWave = sin(_Time.y * _WaveFrequency);
                uv.x += sin(uv.y * 6.28 + _Time.y) * _WaveAmplitude * timeWave * _TimeDistortion;
                uv.y += cos(uv.x * 6.28 + _Time.y) * _WaveAmplitude * timeWave * _TimeDistortion;
                
                // Add time-based color manipulation
                fixed4 col = tex2D(_MainTex, uv);
                float timeFactor = sin(_Time.y) * 0.5 + 0.5;
                col = lerp(col, col * float4(1.2, 0.8, 0.8, 1), _TimeDistortion * timeFactor);
                
                return col;
            }
            ENDCG
        }
    }
}";

        public static string RealityBreakShader = @"
Shader ""Hidden/RealityBreak""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _RealityBreak (""Reality Break"", Float) = 1
        _GlitchAmount (""Glitch Amount"", Float) = 0.3
        _ChromaticAberration (""Chromatic Aberration"", Float) = 0.02
    }

    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _RealityBreak;
            float _GlitchAmount;
            float _ChromaticAberration;

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Reality breaking distortion
                float glitchTime = floor(_Time.y * 20) * 0.5;
                float2 glitchPos = float2(random(float2(glitchTime, 9)), random(float2(glitchTime, 5)));
                float glitchStrength = random(glitchPos);
                
                if (glitchStrength < _GlitchAmount * _RealityBreak)
                {
                    uv.y = frac(uv.y + glitchPos.x * 0.1);
                }
                
                // Chromatic aberration
                fixed4 col;
                col.r = tex2D(_MainTex, uv + float2(_ChromaticAberration, 0) * _RealityBreak).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(_ChromaticAberration, 0) * _RealityBreak).b;
                col.a = 1;
                
                // Reality break intensity
                float breakEffect = sin(_Time.y * 2 + uv.y * 10) * 0.5 + 0.5;
                col = lerp(col, col * float4(1.2, 0.8, 1.2, 1), breakEffect * _RealityBreak * 0.3);
                
                return col;
            }
            ENDCG
        }
    }
}";
    }
}