using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SpriteUVTest.Editor 
{

// https://github.com/keijiro/KlakSpout/blob/main/Packages/jp.keijiro.klak.spout/Editor/MaterialPropertySelector.cs
static class MaterialPropertySelector
{
    #region Public method

    // Material property dropdown list
    public static void DropdownList
      (SerializedProperty rendererProperty,
       SerializedProperty materialPropertyHigh,
       SerializedProperty materialPropertyLow)
    {
        var shader = GetShaderFromRenderer(rendererProperty);

        // Abandon the current value if there is no shader assignment.
        if (shader == null)
        {
            materialPropertyHigh.stringValue = "";
            materialPropertyLow.stringValue = "";
            return;
        }

        var names = CachePropertyNames(shader);

        // Abandon the current value if there is no option.
        if (names.Length == 0)
        {
            materialPropertyHigh.stringValue = "";
            materialPropertyLow.stringValue = "";
            return;
        }

        // Dropdown GUI
        var index = System.Array.IndexOf(names, materialPropertyHigh.stringValue);
        var newIndex = EditorGUILayout.Popup("High UV Property", index, names);
        if (index != newIndex) materialPropertyHigh.stringValue = names[newIndex];

        index = System.Array.IndexOf(names, materialPropertyLow.stringValue);
        newIndex = EditorGUILayout.Popup("Low UV Property", index, names);
        if (index != newIndex) materialPropertyLow.stringValue = names[newIndex];
    }

    #endregion

    #region Utility function

    // Shader retrieval function
    static Shader GetShaderFromRenderer(SerializedProperty property)
    {
        var renderer = property.objectReferenceValue as Renderer;
        if (renderer == null) return null;

        var material = renderer.sharedMaterial;
        if (material == null) return null;

        return material.shader;
    }

    #endregion

    #region Property name cache

    static Shader _cachedShader;
    static string[] _cachedPropertyNames;

    static bool IsPropertyTexture(Shader shader, int index)
      => ShaderUtil.GetPropertyType(shader, index) ==
         ShaderUtil.ShaderPropertyType.TexEnv;

    static string[] CachePropertyNames(Shader shader)
    {
        if (shader == _cachedShader) return _cachedPropertyNames;

        var names =
          Enumerable.Range(0, ShaderUtil.GetPropertyCount(shader))
          .Where(i => IsPropertyTexture(shader, i))
          .Select(i => ShaderUtil.GetPropertyName(shader, i));

        _cachedShader = shader;
        _cachedPropertyNames = names.ToArray();

        return _cachedPropertyNames;
    }

    #endregion
}

}