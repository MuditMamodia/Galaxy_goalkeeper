#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor-only utility that guarantees the UI/LogoShineSweep shader is in
// Project Settings → Graphics → Always Included Shaders. Without this entry,
// Unity's build pipeline strips the shader from the player (because the only
// reference to it is a runtime Shader.Find string, which the build scanner
// cannot see). The shader works fine in the Editor but is missing in builds —
// which is exactly the symptom of "shine works in Editor, gone in build".
//
// Runs automatically on assembly reload via InitializeOnLoad, so opening the
// project once is enough — the change is persisted into GraphicsSettings.asset
// and the shader will be in every build from then on.
[InitializeOnLoad]
public static class LogoShineSweepShaderInclude
{
    private const string ShaderName = "UI/LogoShineSweep";

    static LogoShineSweepShaderInclude()
    {
        // Defer one frame so AssetDatabase is ready (Shader.Find can return null
        // mid-import otherwise).
        EditorApplication.delayCall += EnsureIncluded;
    }

    [MenuItem("Tools/Galaxy Goalkeeper/Force Re-add LogoShineSweep Shader To Always Included")]
    private static void ManualReAdd()
    {
        EnsureIncluded();
    }

    private static void EnsureIncluded()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            // Shader asset not in project yet — typical during the first import of the
            // .shader file. The next assembly reload will re-fire this and find it.
            return;
        }

        // GraphicsSettings is a backed asset. Editing it requires going through its
        // SerializedObject because Unity's GraphicsSettings C# API doesn't expose
        // AlwaysIncludedShaders for write.
        UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (settings == null || settings.Length == 0) return;
        SerializedObject graphicsSettings = new SerializedObject(settings[0]);
        SerializedProperty alwaysIncluded = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncluded == null || !alwaysIncluded.isArray) return;

        // Bail if already present (don't add a duplicate entry every reload).
        for (int i = 0; i < alwaysIncluded.arraySize; i++)
        {
            SerializedProperty slot = alwaysIncluded.GetArrayElementAtIndex(i);
            if (slot.objectReferenceValue == shader) return;
        }

        // Append the shader to the end of the list. SerializedProperty array insertion
        // is the only reliable way; setting `objectReferenceValue` on a fresh slot writes
        // the asset reference into GraphicsSettings.asset, which gets serialized to disk
        // the next time Unity flushes ProjectSettings.
        int newIdx = alwaysIncluded.arraySize;
        alwaysIncluded.InsertArrayElementAtIndex(newIdx);
        alwaysIncluded.GetArrayElementAtIndex(newIdx).objectReferenceValue = shader;
        graphicsSettings.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        Debug.Log("[LogoShineSweepShaderInclude] Added '" + ShaderName + "' to Always Included Shaders. " +
                  "It will now be packaged into every build of this project.");
    }
}
#endif
