using System;
using System.IO;
using UnityEngine;

public class DdsLoader : MonoBehaviour
{
    [SerializeField] string _pathHigh = "rtHigh.dds";
    [SerializeField] string _pathLow = "rtLow.dds";
    [SerializeField] MeshRenderer _renderer;

    [SerializeField] bool _readHigh;
    [SerializeField] bool _readLow;
    [SerializeField] bool _readHalf;

    Texture2D textureHigh;
    Texture2D textureLow;

    const int DDS_HEADER_SIZE = 148;

    const int WIDTH = 2048;
    const int HEIGHT = 1024;

    const int MaxIndex = 899;
    float index = 0;
    float prevIndex = 0;

    string Index => Mathf.FloorToInt(this.index).ToString("D5");
    [SerializeField] float _fps = 30;

    void Start()
    {
        textureHigh = new Texture2D(WIDTH, HEIGHT, TextureFormat.BC5, false, true);
        textureLow = new Texture2D(WIDTH, HEIGHT, TextureFormat.BC7, false, true);
    }

    void Update()
    {
        if(_readHigh && prevIndex != index)
        {
            string path = _pathHigh.Split('_')[0] + "_" + Index + ".ddsasset";
            string pathHigh = Application.streamingAssetsPath + "/" + path;

            byte[] bytesHigh = File.ReadAllBytes(pathHigh);
        
            textureHigh.LoadRawTextureData(bytesHigh);
            textureHigh.Apply();

            _renderer.material.SetTexture("_HighUV", textureHigh);
        }

        if(_readLow && prevIndex != index)
        {
            string path = _pathLow.Split('_')[0] + "_" + Index + ".ddsasset";
            string pathLow = Application.streamingAssetsPath + "/" + path;

            byte[] bytesLow = File.ReadAllBytes(pathLow);
        
            textureLow.LoadRawTextureData(bytesLow);
            textureLow.Apply();

            _renderer.material.SetTexture("_LowUV" , textureLow);
        }

        if(_readHigh && _readLow) 
        {
            prevIndex = index;
            index = (index + Time.deltaTime * _fps) % (MaxIndex + 1);
        }
    }

}
