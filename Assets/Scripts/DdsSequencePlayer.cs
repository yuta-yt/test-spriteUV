using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteUVTest.Runtime
{

public sealed class DdsSequencePlayer : MonoBehaviour
{
    [SerializeField] string _highBitSequence = "";
    [SerializeField] string _lowBitSequence = "";
    [SerializeField] MeshRenderer _targetRenderer;
    [SerializeField] string _highUVProperty = "";
    [SerializeField] string _lowUVProperty = "";

    [SerializeField] Vector2Int _size = new Vector2Int(2048, 1024);

    [SerializeField] int _frameRate = 30;
    int _numFrames = int.MaxValue;

    float _duration = 0;
    public float Duration => _duration;

    float _time = 0f;
    public float currentTime
    {
        get => _time;
        set
        {
            _prevIndexTime = _indexTime;
            _time = value;

            if(_indexTime >= _numFrames - 1)
            {
                if(_loop)
                    _time = 0;
                else
                {
                    _time = Mathf.Min(value, _duration);
                    Pause();
                }
            }
        }
    }

    [SerializeField] float _speed = 1.0f;
    public float Speed
    {
        get => _speed;
        set
        {
            _speed = Mathf.Max(value, 0f);
        }
    }

    int _indexTime => Mathf.FloorToInt(_time * _frameRate);
    int _prevIndexTime = 0;

    Texture2D _highTexture;
    Texture2D _lowTexture;

    [SerializeField] bool _playOnAwake = false;
    [SerializeField] bool _loop = false;

    public bool IsPlaying { get; private set; } = false;

    string[] _highFrames;
    string[] _lowFrames;

    void Start()
    {
        currentTime = 0;
        if(_playOnAwake) IsPlaying = true;

        _highTexture = new Texture2D(_size.x, _size.y, TextureFormat.BC5, false, true);
        _lowTexture = new Texture2D(_size.x, _size.y, TextureFormat.DXT5, false, true);

        _highFrames = Directory.GetFiles(Application.streamingAssetsPath + _highBitSequence)
                                .Where(x => Path.GetExtension(x) == ".ddsasset").ToArray();

        _lowFrames = Directory.GetFiles(Application.streamingAssetsPath + _lowBitSequence)
                                .Where(x => Path.GetExtension(x) == ".ddsasset").ToArray();

        _numFrames = _highFrames.Length;
        _duration = _numFrames / (float)_frameRate;

        _targetRenderer.material.SetTexture(_highUVProperty, _highTexture);
        _targetRenderer.material.SetTexture(_lowUVProperty , _lowTexture);
    }

    void Update()
    {
        if(IsPlaying) 
        {
            if(_prevIndexTime != _indexTime) LoadCurrentFrame();

            _prevIndexTime = _indexTime;
            currentTime += Time.deltaTime * Speed;
        }
    }

    public void Play()
    {
        if(_indexTime >= _numFrames - 1)
            currentTime = 0;

        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        currentTime = 0;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Resume()
    {
        currentTime = 0;
    }

    void LoadCurrentFrame()
    {
        byte[] bytesHigh = File.ReadAllBytes(_highFrames[_indexTime]);
        byte[] bytesLow = File.ReadAllBytes(_lowFrames[_indexTime]);

        _highTexture.LoadRawTextureData(bytesHigh);
        _highTexture.Apply();

        _lowTexture.LoadRawTextureData(bytesLow);
        _lowTexture.Apply();
    }
}

}