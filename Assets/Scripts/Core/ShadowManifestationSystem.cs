using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

namespace AIHell.Core
{
    public static class UIEffects
    {
        public enum EffectType
        {
            Paranoia,
            RealityDistortion,
            VoidManifestation,
            ShadowConvergence
        }

        public class Effect
        {
            public EffectType type;
            public float intensity;
            public float duration;
        }
    }

    [RequireComponent(typeof(StyleTransferProcessor))]
    [RequireComponent(typeof(EventProcessor))]
    public class ShadowManifestationSystem : MonoBehaviour
    {
        private StyleTransferProcessor styleProcessor;
        private EventProcessor eventProcessor;
        private LLMManager llmManager;
        private List<ManifestationProfile> exampleManifestations;
        private Dictionary<string, ManifestationProfile> manifestationProfiles = new Dictionary<string, ManifestationProfile>();

        [System.Serializable]
        public class ManifestationProfile
        {
            public string id;
            public string type;
            public string description;
            public string interpretationPrompt;
            public float intensity;
            public float duration;
            public string styleId;
            public string[] psychologicalTriggers;
            public bool isActive;
            public float lastTriggerTime;
            public AnimationCurve manifestationCurve;
        }

        private async void Awake()
        {
            styleProcessor = GetComponent<StyleTransferProcessor>();
            eventProcessor = GetComponent<EventProcessor>();
            llmManager = GameManager.Instance.LLMManager;
            await InitializeSystem();
        }

        private async Task InitializeSystem()
        {
            // Initialize example manifestations for LLM reference
            InitializeExampleManifestations();
            
            // Generate initial set of dynamic manifestations
            await GenerateManifestations();
        }

        private void InitializeExampleManifestations()
        {
            exampleManifestations = new List<ManifestationProfile>
            {
                new ManifestationProfile {
                    id = "inner_darkness_example",
                    type = "psychological_horror",
                    description = "The shadows in the room begin to pulse with your heartbeat",
                    interpretationPrompt = "Describe how inner fears manifest as physical shadows",
                    psychologicalTriggers = new[] { "fear", "anxiety", "dread" }
                },
                new ManifestationProfile {
                    id = "reality_breakdown_example",
                    type = "surreal_horror",
                    description = "The walls breathe with impossible geometry",
                    interpretationPrompt = "Detail how reality warps under psychological pressure",
                    psychologicalTriggers = new[] { "distortion", "unreality", "confusion" }
                }
                // More examples for LLM reference
            };
        }

        private async Task GenerateManifestations()
        {
            string prompt = "Using these example manifestations as reference, generate new unique psychological horror manifestations:\n\n";
            
            foreach (var example in exampleManifestations)
            {
                prompt += $"Example Manifestation:\n" +
                         $"Type: {example.type}\n" +
                         $"Description: {example.description}\n" +
                         $"Interpretation: {example.interpretationPrompt}\n" +
                         $"Triggers: {string.Join(", ", example.psychologicalTriggers)}\n\n";
            }

            string response = await llmManager.GenerateResponse(prompt);
            await ProcessGeneratedManifestations(response);
        }

        private async Task ProcessGeneratedManifestations(string llmResponse)
        {
            try
            {
                var lines = llmResponse.Split('\n');
                ManifestationProfile currentProfile = null;

                foreach (var line in lines)
                {
                    if (line.StartsWith("Type:"))
                    {
                        if (currentProfile != null)
                        {
                            manifestationProfiles[currentProfile.id] = currentProfile;
                        }
                        currentProfile = new ManifestationProfile
                        {
                            id = $"manifest_{Guid.NewGuid():N}",
                            type = line.Replace("Type:", "").Trim(),
                            manifestationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                            isActive = false,
                            lastTriggerTime = 0f
                        };
                    }
                    else if (currentProfile != null)
                    {
                        if (line.StartsWith("Description:"))
                            currentProfile.description = line.Replace("Description:", "").Trim();
                        else if (line.StartsWith("Interpretation:"))
                            currentProfile.interpretationPrompt = line.Replace("Interpretation:", "").Trim();
                        else if (line.StartsWith("Triggers:"))
                            currentProfile.psychologicalTriggers = line.Replace("Triggers:", "").Split(',').Select(t => t.Trim()).ToArray();
                    }
                }

                if (currentProfile != null)
                {
                    manifestationProfiles[currentProfile.id] = currentProfile;
                }

                await ValidateManifestations();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing manifestations: {ex.Message}");
                InitializeFallbackManifestations();
            }
        }

        private void InitializeFallbackManifestations()
        {
            var fallback = new ManifestationProfile
            {
                id = "fallback_manifestation",
                type = "psychological_horror",
                description = "Shadows shift and dance at the edges of your vision",
                interpretationPrompt = "Describe how psychological pressure manifests as visual distortions",
                intensity = 0.5f,
                duration = 5f,
                psychologicalTriggers = new[] { "fear", "anxiety" },
                manifestationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
            manifestationProfiles.Clear();
            manifestationProfiles[fallback.id] = fallback;
        }

        private async Task ValidateManifestations()
        {
            foreach (var manifestation in manifestationProfiles.Values.ToList())
            {
                if (!IsValidManifestation(manifestation))
                {
                    string prompt = $"Fix this invalid manifestation:\n" +
                                  $"Type: {manifestation.type}\n" +
                                  $"Description: {manifestation.description}\n" +
                                  "Ensure it has a clear description and psychological impact.";

                    string response = await llmManager.GenerateResponse(prompt);
                    var fixedManifestation = ParseManifestationResponse(response);
                    
                    if (IsValidManifestation(fixedManifestation))
                    {
                        manifestationProfiles[manifestation.id] = fixedManifestation;
                    }
                    else
                    {
                        manifestationProfiles.Remove(manifestation.id);
                    }
                }
            }
        }

        private bool IsValidManifestation(ManifestationProfile manifestation)
        {
            return manifestation != null &&
                   !string.IsNullOrEmpty(manifestation.type) &&
                   !string.IsNullOrEmpty(manifestation.description) &&
                   manifestation.psychologicalTriggers != null &&
                   manifestation.psychologicalTriggers.Length > 0;
        }

        private ManifestationProfile ParseManifestationResponse(string response)
        {
            try
            {
                var lines = response.Split('\n');
                var profile = new ManifestationProfile
                {
                    id = $"manifest_{Guid.NewGuid():N}",
                    manifestationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                    isActive = false,
                    lastTriggerTime = 0f,
                    duration = UnityEngine.Random.Range(3f, 8f),
                    intensity = UnityEngine.Random.Range(0.4f, 0.8f)
                };

                foreach (var line in lines)
                {
                    if (line.StartsWith("Type:"))
                        profile.type = line.Replace("Type:", "").Trim();
                    else if (line.StartsWith("Description:"))
                        profile.description = line.Replace("Description:", "").Trim();
                    else if (line.StartsWith("Interpretation:"))
                        profile.interpretationPrompt = line.Replace("Interpretation:", "").Trim();
                    else if (line.StartsWith("Triggers:"))
                        profile.psychologicalTriggers = line.Replace("Triggers:", "").Split(',').Select(t => t.Trim()).ToArray();
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing manifestation response: {ex.Message}");
                return null;
            }
        }

        public async Task ProcessNewManifestations(string theme, string[] manifestations)
        {
            foreach (var manifestation in manifestations)
            {
                string prompt = $"Convert this thematic manifestation into a full psychological horror manifestation:\n" +
                               $"Theme: {theme}\n" +
                               $"Base Manifestation: {manifestation}\n" +
                               "Generate a complete manifestation profile with type, description, interpretation, and triggers.";

                string response = await llmManager.GenerateResponse(prompt);
                var profile = ParseManifestationResponse(response);
                
                if (IsValidManifestation(profile))
                {
                    manifestationProfiles[profile.id] = profile;
                }
            }
        }

        public async void ProcessPsychologicalState(PlayerAnalysisProfile profile, Room currentRoom)
        {
            // Generate unique manifestation based on current psychological state
            var manifestation = await GenerateContextualManifestation(profile, currentRoom);
            
            if (ShouldTriggerManifestation(manifestation, profile))
            {
                await TriggerManifestation(manifestation, profile, currentRoom);
            }

            UpdateActiveManifestations(profile);
        }

        private async Task<ManifestationProfile> GenerateContextualManifestation(PlayerAnalysisProfile profile, Room currentRoom)
        {
            string prompt = $"Generate a psychological manifestation based on:\n" +
                           $"Fear Level: {profile.FearLevel}\n" +
                           $"Obsession Level: {profile.ObsessionLevel}\n" +
                           $"Aggression Level: {profile.AggressionLevel}\n" +
                           $"Current Room: {currentRoom.DescriptionText}\n" +  // Changed to DescriptionText
                           $"Recent Events: {GetRecentEventContext()}\n\n" +
                           "The manifestation should be unique and personally unsettling, " +
                           "drawing from the player's current psychological state.";

            string response = await llmManager.GenerateResponse(prompt);
            return ParseManifestationResponse(response);
        }

        private string GetRecentEventContext()
        {
            var recentEvents = eventProcessor.GetRecentEvents()?.Select(e => e.description).ToList() ?? new List<string>();
            return string.join("\n", recentEvents);
        }

        private bool ShouldTriggerManifestation(ManifestationProfile manifestation, PlayerAnalysisProfile profile)
        {
            if (manifestation.isActive)
                return false;

            if (Time.time - manifestation.lastTriggerTime < manifestation.duration * 2)
                return false;

            return CalculateTriggerProbability(manifestation, profile) > Random.value;
        }

        private float CalculateTriggerProbability(ManifestationProfile manifestation, PlayerAnalysisProfile profile)
        {
            // Base probability affected by psychological state
            float baseProbability = Mathf.Max(
                profile.FearLevel,
                profile.ObsessionLevel,
                profile.AggressionLevel
            );

            // Modify based on tension
            float tensionModifier = GameManager.Instance.TensionManager.GetCurrentTension();
            
            return Mathf.Clamp01(baseProbability * tensionModifier);
        }

        private async Task TriggerManifestation(ManifestationProfile manifestation, PlayerAnalysisProfile profile, Room currentRoom)
        {
            manifestation.isActive = true;
            manifestation.lastTriggerTime = Time.time;

            // Generate unique description using LLM
            string description = await GenerateManifestationDescription(manifestation, profile);

            // Request image generation with appropriate style
            GameManager.Instance.ImageGenerator.RequestContextualImage(
                currentRoom,
                (texture) => {
                    if (texture != null)
                    {
                        StartCoroutine(ProcessManifestation(manifestation, texture, profile, description));
                    }
                }
            );
        }

        private async Task<string> GenerateManifestationDescription(ManifestationProfile manifestation, PlayerAnalysisProfile profile)
        {
            string prompt = $"Describe how this manifestation appears, considering:\n" +
                           $"Manifestation Type: {manifestation.type}\n" +
                           $"Base Description: {manifestation.description}\n" +
                           $"Player's Fear: {profile.FearLevel}\n" +
                           $"Player's Obsession: {profile.ObsessionLevel}\n" +
                           $"Player's Aggression: {profile.AggressionLevel}\n\n" +
                           "The description should be uniquely unsettling and personal to the player's state.";

            return await llmManager.GenerateResponse(prompt);
        }

        private IEnumerator ProcessManifestation(ManifestationProfile manifestation, Texture2D texture, PlayerAnalysisProfile profile, string description)
        {
            float elapsed = 0f;
            
            while (elapsed < manifestation.duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / manifestation.duration;
                float currentIntensity = manifestation.manifestationCurve.Evaluate(normalizedTime) * manifestation.intensity;

                // Apply style transfer
                styleProcessor.ApplyHorrorStyle(
                    texture,
                    manifestation.styleId,
                    currentIntensity,
                    (processedTexture) => {
                        DisplayManifestation(processedTexture, manifestation, currentIntensity);
                    }
                );

                // Update psychological state
                UpdatePsychologicalImpact(manifestation, profile, currentIntensity);

                yield return null;
            }

            manifestation.isActive = false;
        }

        private void DisplayManifestation(Texture2D texture, ManifestationProfile manifestation, float intensity)
        {
            // Apply additional visual effects based on manifestation type
            var effect = new UIEffects.Effect {
                intensity = intensity,
                duration = 0.5f
            };

            switch (manifestation.type)
            {
                case "psychological_horror":
                    effect.type = UIEffects.EffectType.Paranoia;
                    break;
                case "surreal_horror":
                    effect.type = UIEffects.EffectType.RealityDistortion;
                    break;
                case "cosmic_horror":
                    effect.type = UIEffects.EffectType.VoidManifestation;
                    break;
                case "dark_romanticism":
                    effect.type = UIEffects.EffectType.ShadowConvergence;
                    break;
            }

            // Apply the effect through UIManager
            if (GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.SendMessage("OnPsychologicalEffectApplied", effect, SendMessageOptions.DontRequireReceiver);
            }

            // Display processed texture
            GameManager.Instance.UIManager.DisplayGeneratedImage(texture);
        }

        private void UpdatePsychologicalImpact(ManifestationProfile manifestation, PlayerAnalysisProfile profile, float intensity)
        {
            // Update psychological state based on manifestation type
            switch (manifestation.type)
            {
                case "psychological_horror":
                    profile.FearLevel += intensity * 0.1f * Time.deltaTime;
                    break;
                case "surreal_horror":
                    profile.ObsessionLevel += intensity * 0.1f * Time.deltaTime;
                    break;
                case "cosmic_horror":
                    profile.FearLevel += intensity * 0.15f * Time.deltaTime;
                    profile.ObsessionLevel += intensity * 0.05f * Time.deltaTime;
                    break;
                case "dark_romanticism":
                    profile.AggressionLevel += intensity * 0.1f * Time.deltaTime;
                    break;
            }

            // Update emotional state
            GameManager.Instance.EmotionalResponseSystem.ProcessEmotionalStimulus(
                $"manifestation_{manifestation.id}",
                intensity,
                profile
            );

            // Modify tension
            GameManager.Instance.TensionManager.ModifyTension(
                intensity * 0.2f,
                $"manifestation_{manifestation.type}"
            );
        }

        private void UpdateActiveManifestations(PlayerAnalysisProfile profile)
        {
            foreach (var manifestation in manifestationProfiles.Values)
            {
                if (manifestation.isActive)
                {
                    float timeSinceTriggered = Time.time - manifestation.lastTriggerTime;
                    if (timeSinceTriggered > manifestation.duration)
                    {
                        manifestation.isActive = false;
                    }
                }
            }
        }

        public ManifestationProfile GetActiveManifestation()
        {
            foreach (var manifestation in manifestationProfiles.Values)
            {
                if (manifestation.isActive)
                {
                    return manifestation;
                }
            }
            return null;
        }

        public void ResetManifestations()
        {
            foreach (var manifestation in manifestationProfiles.Values)
            {
                manifestation.isActive = false;
                manifestation.lastTriggerTime = 0f;
            }
        }

        public async Task<bool> ShouldGenerateNewManifestations()
        {
            var gameState = GameManager.Instance.StateManager.GetCurrentState();
            string prompt = "Based on the current game state, should new manifestation types be introduced?\n" +
                           $"Current Tension: {gameState.tensionLevel}\n" +
                           $"Active Manifestations: {GetActiveManifestationCount()}\n" +
                           $"Time in Current Phase: {gameState.timeInCurrentPhase}";

            string response = await llmManager.GenerateResponse(prompt);
            return response.ToLower().Contains("yes");
        }

        private int GetActiveManifestationCount()
        {
            return GetActiveManifestations().Count;
        }

        public List<ManifestationProfile> GetActiveManifestations()
        {
            return manifestationProfiles.Values
                .Where(m => m.isActive)
                .OrderByDescending(m => m.intensity)
                .ToList();
        }

        public void UpdateManifestationIntensity(string manifestationId, float newIntensity)
        {
            if (manifestationProfiles.TryGetValue(manifestationId, out var manifestation))
            {
                manifestation.intensity = Mathf.Clamp01(newIntensity);
                
                // Update duration based on intensity
                manifestation.duration = Mathf.Lerp(3f, 8f, manifestation.intensity);
                
                // Update manifestation curve
                if (manifestation.intensity > 0.7f)
                {
                    // Create more dramatic curve for high intensity
                    manifestation.manifestationCurve = new AnimationCurve(
                        new Keyframe(0, 0, 0, 2),
                        new Keyframe(0.3f, 0.8f),
                        new Keyframe(0.7f, 0.9f),
                        new Keyframe(1, 0, -2, 0)
                    );
                }
                else
                {
                    // Gentle curve for lower intensity
                    manifestation.manifestationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
                }
            }
        }
    }
}