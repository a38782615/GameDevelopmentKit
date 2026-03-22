using UnityEngine;
using ET;
using Unity.Mathematics;
using UnityEngine.UI;
using System;

[EnableClass]
public class MapMain : MonoBehaviour
{
    BiomeMap _biomeMap;
    const int _textureScale = 1;
    public SpriteRenderer image;
    void Awake()
    {
        uint seed = 1;
        _biomeMap = new BiomeMap(new float2(500, 500));
        _biomeMap.Init(seed);
        NoisyEdges noisyEdge = new NoisyEdges(seed);
        noisyEdge.BuildNoisyEdges(_biomeMap);

        var texture = new MapTexture(_textureScale).GetTexture(_biomeMap, noisyEdge);
        var sp = Sprite.Create(texture, new Rect(0, 0, _biomeMap.Width * _textureScale, _biomeMap.Height * _textureScale), Vector2.zero);
        image.sprite = sp;
    }
}