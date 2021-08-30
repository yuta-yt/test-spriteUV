using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace SpriteUVTest.Editor.Pipeline
{

public static class TextureConverter
{
    public static int Count { get; private set; }
    public static int CompleteCount { get; private set; }

    public static bool IsDdsProcess { get; private set; }
    public static bool IsOptProcess { get; private set; }
    public static bool IsProceeding { get; private set; }

    static TextureSeparaterWindow _window;

    static EditorCoroutine PipelineCoroutine = null;
    static EditorCoroutine SeparateCoroutine = null;
    static EditorCoroutine ConvertCoroutine = null;
    static EditorCoroutine OptimizeCoroutine = null;

    static System.Diagnostics.Process _process = null;
    static string _batchPath = null;

    public enum DDSCompressQuality
    {
        Fastest = 0,
        Normal,
        Production,
        Highest
    }

    public struct NVTTSetting
    {
        public DDSCompressQuality quality;
        public bool yFlip;
        public bool useCuda;
        public bool deleteTmp;
    }

    // Entry Point
    public static void Process(string path, string outPath, string nvttPath, NVTTSetting setting)
    {
        if(PipelineCoroutine != null && IsProceeding)
        {
            Debug.LogError("Error : Proceeding Previous Process");
            return;
        }

        if(!Directory.Exists(path))
        {
            Debug.LogError("Error : Directory Not Found");
            return;
        }

        if(!path.Contains("Resources"))
        {
            Debug.LogError("Error : Directory Not in Resources");
            return;
        }

        _window = TextureSeparaterWindow.GetWindow<TextureSeparaterWindow>();

        PipelineCoroutine = 
            _window.StartCoroutine(Pipeline(path, outPath, nvttPath, setting));
    }

    static IEnumerator Pipeline(string path, string outPath, string nvttPath, NVTTSetting setting)
    {
        IsProceeding = true;

        Directory.CreateDirectory(outPath + "/High/png");
        Directory.CreateDirectory(outPath + "/Low/png");
        
        Directory.CreateDirectory(outPath + "/High/dds");
        Directory.CreateDirectory(outPath + "/Low/dds");

        string[] files = Directory.GetFiles(path)
                                .Where(x => Path.GetExtension(x) == ".png").ToArray();

        Count = files.Length;

        SeparateCoroutine = _window.StartCoroutine(SeparateTextureSequence(files, outPath));
        yield return SeparateCoroutine;

#if UNITY_EDITOR_WIN
        IsDdsProcess = true;
        _window.Repaint();

        ConvertCoroutine = _window.StartCoroutine(ConvertTextureSequence(outPath, nvttPath, setting));
        yield return ConvertCoroutine;

        IsDdsProcess = false;
        IsOptProcess = true;

        OptimizeCoroutine = _window.StartCoroutine(OptimizeDdsSequence(outPath));
        yield return OptimizeCoroutine;

        IsOptProcess = false;

        // Delete tmp files
        if(setting.deleteTmp)
        {
            Directory.Delete(outPath + "/High/png", true);
            Directory.Delete(outPath + "/Low/png" , true);

            Directory.Delete(outPath + "/High/dds", true);
            Directory.Delete(outPath + "/Low/dds" , true);

            // Try delete metadata file, if output path is in Unity Asset folder.
            try
            {
                File.Delete(outPath + "/High/png.meta");
                File.Delete(outPath + "/Low/png.meta");
                File.Delete(outPath + "/High/dds.meta");
                File.Delete(outPath + "/Low/dds.meta");
            }
            finally
            {
                
            }
        }
#endif

        IsProceeding = false;

        _window.Repaint();
    }

    static IEnumerator SeparateTextureSequence(string[] paths, string outPath)
    {
        var i = 0;
        foreach(var path in paths)
        {
            string relPath = path.Substring(path.IndexOf("Resources") + 10).Split('.')[0];

            // Texture2D.LoadImage(byte[]) is forces the format to be RGBA32(8bpc)...
            var texture = Resources.Load<Texture2D>(relPath);
            BlitSeparateShader(texture, path, outPath);

            CompleteCount++;
            _window.Repaint();

            i++;
            if(i % 5 == 0) yield return null;
        }

        _window.Repaint();
        yield return null;

        Count = 0;
        CompleteCount = 0;
    }

    static IEnumerator ConvertTextureSequence(string outPath, string nvttPath, NVTTSetting setting)
    {
        string[] highBitFiles = Directory.GetFiles(outPath + "/High/png");
        string[] lowBitFiles = Directory.GetFiles(outPath + "/Low/png");

        var info = new System.Diagnostics.ProcessStartInfo();
        info.FileName = nvttPath;
        info.UseShellExecute = true;

        string batchArgs = "";

        // High bit is compressed using bc5.
        // Its advantageous for compressing geometric information such as normals.
        // Mipmap option is disabled by default to avoid performance degradation due to increased file size.
        foreach(var f in highBitFiles)
        {
            if(Path.GetExtension(f) != ".png") continue;

            string fileName = Path.GetFileNameWithoutExtension(f);

            batchArgs += @"""" + f  + @"""" + " /f bc5" + " /q " + (int)(setting.quality) + " /no-mips /dx10";
            if(!setting.useCuda) batchArgs += " /no-cuda";
            if(setting.yFlip) batchArgs += " /save-flip-y";
            batchArgs += @" /o """ + outPath + "/High/dds/" + fileName + @".dds""";

            batchArgs += "\n";
        }
        
        // Low bit is compressed using bc3.
        // bc5 should be used, but we needed an additional channel,
        // to store the Texture2DArray index.
        foreach(var f in lowBitFiles)
        {
            if(Path.GetExtension(f) != ".png") continue;

            string fileName = Path.GetFileNameWithoutExtension(f);

            batchArgs += @"""" + f  + @"""" + " /f bc3" + " /q " + (int)(setting.quality) + " /no-mips /dx10";
            if(!setting.useCuda) batchArgs += " /no-cuda";
            if(setting.yFlip) batchArgs += " /save-flip-y";
            batchArgs += @" /o """ + outPath + "/Low/dds/" + fileName + @".dds""";

            batchArgs += "\n";
        }

        // Create simple text file include batch command arguments
        // https://forums.developer.nvidia.com/t/texture-tools-exporter-standalone-batch-scripting-command-line/145541
        _batchPath = outPath + "/batch.nvdds";
        File.WriteAllText(_batchPath, batchArgs);

        info.Arguments = "/b " + _batchPath;
        _process = System.Diagnostics.Process.Start(info);

        while(true)
        {
            if(_process.HasExited) break;
            yield return null;
        }

        // Avoid IOException
        yield return new EditorWaitForSeconds(.5f);

        try
        {
            File.Delete(_batchPath);
        }
        finally
        {
            // Finalize
            _batchPath = null;
            _process = null;
        }
    }

    static IEnumerator OptimizeDdsSequence(string outPath)
    {
        string[] highDdsFiles = Directory.GetFiles(outPath + "/High/dds");
        string[] lowDdsFiles = Directory.GetFiles(outPath + "/Low/dds");

        // Filtering .dds file
        highDdsFiles = highDdsFiles.Where(x => Path.GetExtension(x) == ".dds").ToArray();
        lowDdsFiles = lowDdsFiles.Where(x => Path.GetExtension(x) == ".dds").ToArray();

        Count = highDdsFiles.Length;

        const int DDS_HEADER_SIZE = 148;

        for(int i = 0; i < highDdsFiles.Length; i++)
        {
            // High bit dds
            byte[] bytesHigh = File.ReadAllBytes(highDdsFiles[i]);
    
            // Copy to buffer exclude dds header
            byte[] dxtBytesHigh = new byte[bytesHigh.Length - DDS_HEADER_SIZE];
            System.Buffer.BlockCopy(bytesHigh, DDS_HEADER_SIZE, dxtBytesHigh, 0, bytesHigh.Length - DDS_HEADER_SIZE);
    
            string fileName = Path.GetFileNameWithoutExtension(highDdsFiles[i]) + ".ddsasset";
            File.WriteAllBytes(outPath + "/High/" + fileName, dxtBytesHigh);

            // Low bit dds
            byte[] bytesLow = File.ReadAllBytes(lowDdsFiles[i]);

            byte[] dxtBytesLow = new byte[bytesLow.Length - DDS_HEADER_SIZE];
            System.Buffer.BlockCopy(bytesLow, DDS_HEADER_SIZE, dxtBytesLow, 0, bytesLow.Length - DDS_HEADER_SIZE);

            fileName = Path.GetFileNameWithoutExtension(lowDdsFiles[i]) + ".ddsasset";
            File.WriteAllBytes(outPath + "/Low/" + fileName, dxtBytesLow);

            CompleteCount++;
            _window.Repaint();

            if(i % 10 == 0) yield return null;
        }

        _window.Repaint();
        yield return null;

        Count = 0;
        CompleteCount = 0;
    }

    static void BlitSeparateShader(Texture2D src, string path, string outPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);

        var mat = new Material(Shader.Find("Hidden/BitSplit"));
        var rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGBHalf);

        // Apply shader at pass 0
        Graphics.Blit(src, rt, mat, 0);
        RenderTexture.active = rt;

        // Copy RenderTexture to Texture2D
        var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0,0,rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        System.IO.File.WriteAllBytes(outPath + "/High/png/" + fileName + ".png", tex.EncodeToPNG());

        // Apply shader at pass 1
        Graphics.Blit(src, rt, mat, 1);
        RenderTexture.active = rt;

        // Copy RenderTexture to Texture2D
        tex.ReadPixels(new Rect(0,0,rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        System.IO.File.WriteAllBytes(outPath + "/Low/png/" + fileName + ".png", tex.EncodeToPNG());

        GameObject.DestroyImmediate(rt);
    }

    public static void StopProcess()
    {
        if(PipelineCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(PipelineCoroutine);

        if(SeparateCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(SeparateCoroutine);

        if(ConvertCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(ConvertCoroutine);

        if(OptimizeCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(OptimizeCoroutine);

        PipelineCoroutine = SeparateCoroutine = ConvertCoroutine = OptimizeCoroutine = null;

        _process?.Kill();
        _process = null;

        // Delete batch arg file
        Task.Run(async () =>
        {
            // Avoid IOException
            await Task.Delay(500);

            try
            {
                if(_batchPath != null) File.Delete(_batchPath);
            }
            finally
            {
                _batchPath = null;
            }
        });

        IsProceeding = false;
        IsDdsProcess = false;
        IsOptProcess = false;

        Count = 0;
        CompleteCount = 0;

        _window?.Repaint();
    }
}

}
