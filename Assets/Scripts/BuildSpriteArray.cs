using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteUVTest.Runtime
{

public class BuildSpriteArray : MonoBehaviour
{
    [SerializeField] MeshRenderer _renderer;
    [SerializeField] List<Texture2D> _sprites;

    void Start()
    {
        var array = new Texture2DArray(_sprites[0].width,
                                                _sprites[0].height,
                                                _sprites.Count,
                                                UnityEngine.Experimental.Rendering.DefaultFormat.LDR,
                                                UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

        for (int i = 0; i < _sprites.Count; i++)
            array.SetPixels(_sprites[i].GetPixels(), i);

        array.Apply();

        _renderer.material.SetTexture("_SpriteArray", array);
    }
}

}