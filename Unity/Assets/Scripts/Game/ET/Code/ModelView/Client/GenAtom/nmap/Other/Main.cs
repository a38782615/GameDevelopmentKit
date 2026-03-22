using UnityEngine;
using ET;
using Unity.Mathematics;

[EnableClass]
public class MainTest : MonoBehaviour
{
    BiomeMap _biomeMap;
    const int _textureScale = 10;

    //    void Update()
    //    {
    //        if (_map != null && _map.SelectedCenter != null)
    //        {
    //            _selector.transform.localPosition = new Vector3(_map.SelectedCenter.point.x, _map.SelectedCenter.point.y, 1);
    //        }
    //    }
    public GameObject go;
    void Awake()
    {
        uint seed = 1;
        _biomeMap = new BiomeMap(new float2(50, 50));
        _biomeMap.Init(1);
        Unity.Mathematics.Random r = new Unity.Mathematics.Random(seed);
        NoisyEdges noisyEdge = new NoisyEdges(ref r);
        noisyEdge.BuildNoisyEdges(_biomeMap);

        var mat = go.GetComponent<Renderer>().material;
        new MapTexture(_textureScale).AttachTexture(mat, _biomeMap, noisyEdge);
    }
}