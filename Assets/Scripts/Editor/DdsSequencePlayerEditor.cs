using UnityEngine;
using UnityEditor;

namespace SpriteUVTest.Editor
{

[CustomEditor(typeof(SpriteUVTest.Runtime.DdsSequencePlayer))]
sealed class DdsSequencePlayerEditor : UnityEditor.Editor
{
    SerializedProperty _highSeqDirectory;
    SerializedProperty _lowSeqDirectory;

    SerializedProperty _targetRenderer;
    SerializedProperty _highUVProperty;
    SerializedProperty _lowUVProperty;

    SerializedProperty _size;
    SerializedProperty _frameRate;

    SerializedProperty _playOnAwake;
    SerializedProperty _loop;
    SerializedProperty _speed;

    Runtime.DdsSequencePlayer _player;

    void OnEnable()
    {
        var finder = new PropertyFinder(serializedObject);

        _highSeqDirectory = finder["_highBitSequence"];
        _lowSeqDirectory = finder["_lowBitSequence"];

        _targetRenderer = finder["_targetRenderer"];
        _highUVProperty = finder["_highUVProperty"];
        _lowUVProperty = finder["_lowUVProperty"];

        _size = finder["_size"];
        _frameRate = finder["_frameRate"];

        _playOnAwake = finder["_playOnAwake"];
        _loop = finder["_loop"];
        _speed = finder["_speed"];

        _player = serializedObject.targetObject as Runtime.DdsSequencePlayer;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Sequence Directory
        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.DelayedTextField(_highSeqDirectory);
            if(GUILayout.Button("Select", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Open",  Application.streamingAssetsPath, string.Empty);

                if(!path.Contains("StreamingAssets"))
                {
                    _highSeqDirectory.stringValue = "Place the sequence in StreamingAssets!";
                }
                else
                {
                    _highSeqDirectory.stringValue = path.Substring(path.IndexOf("StreamingAssets") + "StreamingAssets".Length);
                }
            }
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.DelayedTextField(_lowSeqDirectory);
            if(GUILayout.Button("Select", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Open",  Application.streamingAssetsPath, string.Empty);
                if(!path.Contains("StreamingAssets"))
                {
                    _lowSeqDirectory.stringValue = "Place the sequence in StreamingAssets!";
                }
                else
                {
                    _lowSeqDirectory.stringValue = path.Substring(path.IndexOf("StreamingAssets") + "StreamingAssets".Length);
                }
            }
        }

        EditorGUILayout.Space(10);

        EditorGUI.BeginChangeCheck();
        // var size = EditorGUILayout.Vector2IntField("Frame Size", _size.vector2IntValue);
        _size.vector2IntValue = EditorGUILayout.Vector2IntField("Frame Size", _size.vector2IntValue);
        var setSize = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(_frameRate, new GUIContent("Frame Rate"));

        EditorGUILayout.Space(10);

        if(Application.isPlaying)
        {
            if(GUILayout.Button("Load", GUILayout.Width(60)))
            {
                _player.OpenSequenceFromDirectory(_highSeqDirectory.stringValue,
                                                    _lowSeqDirectory.stringValue,
                                                    _size.vector2IntValue.x,
                                                    _size.vector2IntValue.y,
                                                    _frameRate.intValue);
            }
            EditorGUILayout.Space(10);
        }

        // Target texture/renderer
        EditorGUILayout.PropertyField(_targetRenderer);

        EditorGUI.indentLevel++;

        if (_targetRenderer.hasMultipleDifferentValues)
        {
            // Multiple renderers selected: Show a simple text field.
            EditorGUILayout.PropertyField(_highUVProperty, new GUIContent("HighUVProperty"));
            EditorGUILayout.PropertyField(_lowUVProperty, new GUIContent("LowUVProperty"));
        }
        else if (_targetRenderer.objectReferenceValue != null)
        {
            // Single renderer: Show the material property selection dropdown.
            MaterialPropertySelector.DropdownList(_targetRenderer, _highUVProperty, _lowUVProperty);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        EditorGUILayout.PropertyField(_playOnAwake, new GUIContent("Play On Awake"));
        EditorGUILayout.PropertyField(_loop, new GUIContent("Loop"));
        _speed.floatValue = Mathf.Max(EditorGUILayout.Slider("Play Speed", _speed.floatValue, .01f, 5f), 0f);

        serializedObject.ApplyModifiedProperties();
    }
}

// Utilities for finding serialized properties
struct PropertyFinder
{
    SerializedObject _so;

    public PropertyFinder(SerializedObject so)
      => _so = so;

    public SerializedProperty this[string name]
      => _so.FindProperty(name);
}

}