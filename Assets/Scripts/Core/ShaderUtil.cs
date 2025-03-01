using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ShaderUtil
{
    public static Shader CreateShaderAsset(string shaderSource)
    {
        if (string.IsNullOrEmpty(shaderSource))
            throw new ArgumentNullException(nameof(shaderSource));

        try
        {
            #if UNITY_EDITOR
                // In editor, create actual shader asset
                Shader shader = new Shader();
                string tempPath = "Assets/Temp/GeneratedShader.shader";
                System.IO.File.WriteAllText(tempPath, shaderSource);
                AssetDatabase.ImportAsset(tempPath);
                shader = AssetDatabase.LoadAssetAtPath<Shader>(tempPath);
                AssetDatabase.DeleteAsset(tempPath);
                return shader;
            #else
                // At runtime, create shader from source
                return new Material(shaderSource).shader;
            #endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating shader: {ex.Message}");
            return null;
        }
    }

    public static bool ValidateShader(Shader shader)
    {
        if (shader == null)
            return false;

        try
        {
            // Create temporary material to test shader
            var testMaterial = new Material(shader);
            bool isValid = testMaterial.shader.isSupported;
            UnityEngine.Object.DestroyImmediate(testMaterial);
            return isValid;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error validating shader: {ex.Message}");
            return false;
        }
    }

    public static bool HasErrors(Shader shader)
    {
        if (shader == null)
            return true;

        #if UNITY_EDITOR
            return !UnityEditor.ShaderUtil.ShaderHasError(shader);
        #else
            return !shader.isSupported;
        #endif
    }
}