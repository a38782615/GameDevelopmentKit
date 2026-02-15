using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// Cue资源加载器接口
    /// 项目需要实现此接口以支持自定义资源加载
    /// </summary>
    public interface ICueResourceLoader
    {
        GameObject LoadParticle(string path);
    }
}
