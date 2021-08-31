using System.IO;
using System.Linq;
using UnityEngine;

namespace SpriteUVTest.Runtime
{

public sealed class DdsSequencePlayer : MonoBehaviour
{
    #region Serialize field
    [SerializeField] string _highBitSequence = "";
    [SerializeField] string _lowBitSequence = "";

    [SerializeField] MeshRenderer _targetRenderer;
    [SerializeField] string _highUVProperty = "";
    [SerializeField] string _lowUVProperty = "";

    [SerializeField] Vector2Int _size = new Vector2Int(2048, 1024);
    [SerializeField] int _frameRate = 30;

    [SerializeField] float _speed = 1.0f;
    public bool _playOnAwake = false;
    public bool _loop = false;
    #endregion

    #region Private field
    int _numFrames = int.MaxValue;
    float _duration = 0;
    float _time = 0f;
    int _fps = 0;

    int _indexTime => Mathf.FloorToInt(_time * _fps);
    int _prevIndexTime = 0;

    Texture2D _highTexture;
    Texture2D _lowTexture;

    string[] _highFrames;
    string[] _lowFrames;
    #endregion // Private field

    #region Public accessable property
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

            if(_prevIndexTime != _indexTime) LoadCurrentFrame();
        }
    }

    public float Speed
    {
        get => _speed;
        set
        {
            _speed = Mathf.Max(value, 0f);
        }
    }

    public bool IsPlaying { get; private set; } = false;
    public float Duration => _duration;
    #endregion // Punlic accesible property

    #region MonoBehabiour method implemetation
    void Start()
    {
        InitSequence();
        if(_playOnAwake) IsPlaying = true;
    }

    void Update()
    {
        if(IsPlaying) 
        {
            currentTime += Time.deltaTime * Speed;
        } 

        _targetRenderer.sharedMaterial.SetTexture(_highUVProperty, _highTexture);
        _targetRenderer.sharedMaterial.SetTexture(_lowUVProperty , _lowTexture);
    }
    #endregion // MonoBehabiour method implemetation

    #region  Private method
    void LoadCurrentFrame()
    {
        byte[] bytesHigh = File.ReadAllBytes(_highFrames[_indexTime]);
        byte[] bytesLow = File.ReadAllBytes(_lowFrames[_indexTime]);

        _highTexture.LoadRawTextureData(bytesHigh);
        _highTexture.Apply();

        _lowTexture.LoadRawTextureData(bytesLow);
        _lowTexture.Apply();
    }

    void InitSequence()
    {
        _highTexture = new Texture2D(_size.x, _size.y, TextureFormat.BC5, false, true);
        _lowTexture = new Texture2D(_size.x, _size.y, TextureFormat.DXT5, false, true);

        _highFrames = Directory.GetFiles(Application.streamingAssetsPath + _highBitSequence)
                                .Where(x => Path.GetExtension(x) == ".ddsasset").ToArray();

        _lowFrames = Directory.GetFiles(Application.streamingAssetsPath + _lowBitSequence)
                                .Where(x => Path.GetExtension(x) == ".ddsasset").ToArray();

        _numFrames = Mathf.Min(_highFrames.Length, _lowFrames.Length);
        _duration = _numFrames / (float)_frameRate;
        _fps = _frameRate;

        _targetRenderer.sharedMaterial.SetTexture(_highUVProperty, _highTexture);
        _targetRenderer.sharedMaterial.SetTexture(_lowUVProperty , _lowTexture);

        currentTime = 0;
        LoadCurrentFrame();
    }
    #endregion // Private method

    #region Public API method 
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

    public void OpenSequenceFromDirectory(string highBitPath, string lowBitPath, int width, int height, int frameRate)
    {
        _highBitSequence = highBitPath;
        _lowBitSequence = lowBitPath;
        _size.x = width;
        _size.y = height;
        _frameRate = frameRate;

        InitSequence();
    }
    #endregion // Public API Method
}

}