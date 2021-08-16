using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeparateBit : MonoBehaviour
{
    [SerializeField] Texture _srcTexture;

    [SerializeField] Shader _shader;

    void Start()
    {
        var mat = new Material(_shader);
        var rt = new RenderTexture(_srcTexture.width, _srcTexture.height, 0, RenderTextureFormat.ARGBHalf);

        Graphics.Blit(_srcTexture, rt, mat, 0);
        RenderTexture.active = rt;

        var tex = new Texture2D(_srcTexture.width, _srcTexture.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0,0,rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        System.IO.File.WriteAllBytes(Application.dataPath + "/Texture/SplitUV/rtHigh.png", tex.EncodeToPNG());


        Graphics.Blit(_srcTexture, rt, mat, 1);
        RenderTexture.active = rt;

        tex.ReadPixels(new Rect(0,0,rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        System.IO.File.WriteAllBytes(Application.dataPath + "/Texture/SplitUV/rtLow.png", tex.EncodeToPNG());

        Destroy(rt);
        UnityEditor.AssetDatabase.Refresh();
    }
}
