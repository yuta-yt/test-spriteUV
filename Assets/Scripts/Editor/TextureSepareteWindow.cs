using UnityEngine;
using UnityEditor;

namespace SpriteUVTest.Editor.Pipeline
{

using CompressQuality = TextureConverter.DDSCompressQuality;

public sealed class TextureSeparaterWindow : EditorWindow
{
    static string StoredSrcDirectory;
    [SerializeField] string _srcDirectoryPath;

    static string StoredOutDirectory;
    [SerializeField] string _outDirectoryPath;
    [SerializeField] string _srcDirectoryInfo;

    const string DefaultNVTTPath = "C:\\Program Files\\NVIDIA Corporation\\NVIDIA Texture Tools Exporter\\nvtt_export.exe";
    [SerializeField] string _nvttPath;

    [SerializeField] CompressQuality _quality = CompressQuality.Normal;
    [SerializeField] bool _yFlip = true;
    [SerializeField] bool _useCuda = true;
    [SerializeField] bool _deleteTmp = true;

    GUIStyle _boldLabel;

    [MenuItem("Window/SeparateTexturePipeline")]
    static void Open()
    {
        var window = GetWindow<TextureSeparaterWindow>("Separate Texture Pipeline");
        window.minSize = new Vector2(480, 375);
    }

    public void OnGUI()
    {
        EditorGUILayout.Space(5);
        DrawPathSettings();

#if UNITY_EDITOR_WIN
        EditorGUILayout.Space(20);
        DrawNVTTSettings();
#endif

        EditorGUILayout.Space(20);
        DrawProcessButton();

        EditorGUILayout.Space(5);
        DrawProceedingInfo();

        if(GUI.changed)
        {
            CheckDirectoryPath();
        }
    }

    void DrawPathSettings()
    {
        EditorGUILayout.LabelField("Path Settings", _boldLabel);

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Source Sequence Directory" + _srcDirectoryInfo);

        using (new EditorGUILayout.HorizontalScope())
        {
            _srcDirectoryPath = EditorGUILayout.TextField(_srcDirectoryPath);

            if(GUILayout.Button("Select", GUILayout.Width(80)))
            {
                _srcDirectoryPath = EditorUtility.OpenFolderPanel("Open",  Application.dataPath, string.Empty);
                CheckDirectoryPath();
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Save Directory");

        using (new EditorGUILayout.HorizontalScope())
        {
            _outDirectoryPath = EditorGUILayout.TextField(_outDirectoryPath);

            if(GUILayout.Button("Select", GUILayout.Width(80)))
            {
                _outDirectoryPath = EditorUtility.OpenFolderPanel("Open",  Application.dataPath, string.Empty);
            }
        }

        EditorGUI.indentLevel--;
    }

    void DrawNVTTSettings()
    {
        EditorGUILayout.LabelField("NVIDIA Texture Tools Exporter Settings", _boldLabel);

        EditorGUI.indentLevel++;

        if(!System.IO.File.Exists(DefaultNVTTPath))
        {
            EditorGUILayout.LabelField("Application Path");
            using (new EditorGUILayout.HorizontalScope())
            {
                _nvttPath = EditorGUILayout.TextField(_nvttPath);

                if(GUILayout.Button("Select", GUILayout.Width(80)))
                {
                    _nvttPath = EditorUtility.OpenFilePanel("Open",  Application.dataPath, string.Empty);
                }
            }
        }
        else
        {
            _nvttPath = DefaultNVTTPath;
        }

        EditorGUILayout.Space(2);
        _quality = (CompressQuality)EditorGUILayout.EnumPopup("Compress Quality", _quality, GUILayout.Width(250));

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Flip Vertically", GUILayout.Width(134));
            _yFlip = EditorGUILayout.Toggle(_yFlip);
        }

        using(new EditorGUI.DisabledGroupScope((int)_quality >= 2))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Use CUDA", GUILayout.Width(134));
                _useCuda = EditorGUILayout.Toggle(_useCuda);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Delete Temp Files", GUILayout.Width(134));
            _deleteTmp = EditorGUILayout.Toggle(_deleteTmp);
        }

        EditorGUI.indentLevel--;
    }

    void DrawProcessButton()
    {
        if(GUILayout.Button("Proceed"))
        {

#if UNITY_EDITOR_WIN
            if(!System.IO.File.Exists(_nvttPath))
            {
                Debug.LogError("Error : NVIDIA Texture Tools Exporter is Not Found");
                return;
            }
#endif

            TextureConverter.Process(_srcDirectoryPath, _outDirectoryPath, _nvttPath, new TextureConverter.NVTTSetting
            {
                quality = _quality,
                useCuda = _useCuda,
                yFlip = _yFlip,
                deleteTmp = _deleteTmp
            });
        }

        EditorGUILayout.Space(2);
        using(new EditorGUI.DisabledGroupScope(!TextureConverter.IsProceeding))
        {
            if(GUILayout.Button("Stop"))
            {
                TextureConverter.StopProcess();
            }
        }
    }

    void DrawProceedingInfo()
    {
        var rect = GUILayoutUtility.GetRect(position.width, System.IO.File.Exists(DefaultNVTTPath)? 80 : 38);
        rect.height += position.size.y - 400;

        using (new GUI.GroupScope(rect, GUI.skin.box))
        {
            rect.x = 5;
            rect.y = 0;
            rect.height = EditorGUIUtility.singleLineHeight * 1.5f;

            if(TextureConverter.IsOptProcess)
            {
                GUI.Label(rect, $"3/3 Proceeding Asset Optimize.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
            }
            else if(TextureConverter.IsDdsProcess)
            {
                GUI.Label(rect, $"2/3 Proceeding dds Convert on NVIDIA Texture Tools Exporter.... ");
            }
            else if(TextureConverter.IsProceeding)
            {
                GUI.Label(rect, $"1/3 Proceeding Separate.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
            }
        }
    }

    public void OnEnable()
    {
        _boldLabel = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState(){textColor = Color.grey * 1.5f}
        };
        
        _srcDirectoryPath = StoredSrcDirectory ?? _srcDirectoryPath;
        _outDirectoryPath = StoredOutDirectory ?? _outDirectoryPath;

        CheckDirectoryPath();
    } 

    public void OnDisable()
    {
        StoredSrcDirectory = _srcDirectoryPath;
        StoredOutDirectory = _outDirectoryPath;

        TextureConverter.StopProcess();
    }

    void CheckDirectoryPath()
    {
        if(!System.IO.Directory.Exists(_srcDirectoryPath))
        {
            _srcDirectoryInfo = "";
        }
        else if(!_srcDirectoryPath.Contains("Resources"))
        {
            _srcDirectoryInfo = "      Error : Need to select a directory in Resources folder";
        }
        else
        {
            var files = System.IO.Directory.GetFiles(_srcDirectoryPath);

            // Exclude .meta files
            _srcDirectoryInfo = "      " + (files.Length/2).ToString() + " Files";
        }
    }
}

}