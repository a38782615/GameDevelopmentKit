using System;
using System.Reflection;
using System.Threading.Tasks;
using Bright.Serialization;
using Cysharp.Threading.Tasks;
using SimpleJSON;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Extension;
using UnityGameFramework.Runtime;

namespace Game
{
    public enum TablesLoadType : byte
    {
        Undefined = 0,
        Bytes,
        Json,
    }
    
    public partial class TablesComponent : GameFrameworkComponent
    {
        /// <summary>
        /// Luban加载类型
        /// </summary>
        [ShowInInspector, ReadOnly]
        public TablesLoadType LoadType { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            TablesMemory.Clear();
        }

        private void OnDestroy()
        {
            TablesMemory.Clear();
        }

        /// <summary>
        /// 加载所有的配置表Table
        /// </summary>
        /// <returns>是否是</returns>
        public async UniTask LoadAllAsync()
        {
            Type tablesType = this.GetType();
            MethodInfo loadMethodInfo = tablesType.GetMethod("LoadAsync");
            Type loaderReturnType = loadMethodInfo.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
            if (loaderReturnType == typeof (Task<ByteBuf>))
            {
                LoadType = TablesLoadType.Bytes;
                async Task<ByteBuf> LoadByteBuf(string file)
                {
                    TextAsset textAsset = await GameEntry.Resource.LoadAssetAsync<TextAsset>(AssetUtility.GetLubanAsset(file, false));
                    return new ByteBuf(textAsset.bytes);
                }
                Func<string, Task<ByteBuf>> func = LoadByteBuf;
                await (Task)loadMethodInfo.Invoke(this, new object[] { func });
            }
            else
            {
                LoadType = TablesLoadType.Json;
                async Task<JSONNode> LoadJson(string file)
                {
                    TextAsset textAsset = await GameEntry.Resource.LoadAssetAsync<TextAsset>(AssetUtility.GetLubanAsset(file, true));
                    return JSON.Parse(textAsset.text);
                }
                Func<string, Task<JSONNode>> func = LoadJson;
                await (Task)loadMethodInfo.Invoke(this, new object[] { func });
            }
        }
    }
}