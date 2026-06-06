using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game;
using MongoDB.Bson.Serialization.Attributes;
using UnityEngine;
using UnityGameFramework.Extension;
using GameEntry = Game.GameEntry;

namespace ET
{
    public abstract class UGFEntity<T> : UGFEntity where T : AETMonoUGFEntity
    {
        [BsonIgnore]
        public T View { get; private set; }

        [BsonIgnore]
        internal override ETMonoUGFEntity UGFMono
        {
            get => base.UGFMono;
            set
            {
                if (value == null)
                {
                    base.UGFMono = null;
                    this.View = null;
                }
                else
                {
                    base.UGFMono = value;
                    this.View = value.GetComponent<T>();
                }
            }
        }
    }

    [EnableMethod]
    public abstract class UGFEntity : Entity
    {
        [BsonIgnore]
        private UnityGameFramework.Runtime.Entity m_UGFEntity;
        [BsonIgnore]
        private CancellationTokenSourcePlus m_Cts;
        [BsonIgnore]
        private int m_ShowEntityAwaitCount;
        [BsonIgnore]
        internal virtual ETMonoUGFEntity UGFMono { get; set; }
        [BsonIgnore]
        public Transform CachedTransform { get; internal set; }
        [BsonIgnore]
        public bool Available => this.m_UGFEntity != null && this.m_UGFEntity.Logic.Available;
        [BsonIgnore]
        public bool Visible
        {
            get
            {
                return this.m_UGFEntity != null && this.m_UGFEntity.Logic.Visible;
            }
            set
            {
                if (this.m_UGFEntity == null)
                {
                    Log.Warning("Entity is not shown.");
                    return;
                }
                this.m_UGFEntity.Logic.Visible = value;
            }
        }

        public override void Dispose()
        {
            bool isDisposed = this.IsDisposed;
            if (!isDisposed)
            {
                if (this.m_Cts != null)
                {
                    this.m_Cts.Cancel();
                    if (this.m_ShowEntityAwaitCount <= 0)
                    {
                        ObjectPool.Instance.Recycle(this.m_Cts);
                        this.m_Cts = null;
                    }
                }
                if (this.Available)
                {
                    GameEntry.Entity.HideEntity(this.m_UGFEntity);
                    this.m_UGFEntity = null;
                }
            }
            base.Dispose();
        }

        public async UniTask ShowEntityAsync(int entityTypeId)
        {
            CancellationTokenSourcePlus cts = this.GetOrCreateCancellationTokenSource();
            CancellationToken cancellationToken = this.MallocShowEntityToken(cts);
            try
            {
                this.m_UGFEntity = await GameEntry.Entity.ShowEntityAsync<ETMonoUGFEntity>(entityTypeId, ETMonoUGFEntityData.Create(this), cancellationToken: cancellationToken);
            }
            finally
            {
                this.FreeShowEntityToken(cts);
            }

            if(this.m_UGFEntity == null)
            {
                throw new Exception($"UGFEntity ShowEntityAsync failed! entityTypeId:'{entityTypeId}'.");
            }
        }

        public async UniTask ShowEntityAsync(string entityAssetName, string entityGroupName, int priority = 0)
        {
            CancellationTokenSourcePlus cts = this.GetOrCreateCancellationTokenSource();
            CancellationToken cancellationToken = this.MallocShowEntityToken(cts);
            try
            {
                this.m_UGFEntity = await GameEntry.Entity.ShowEntityAsync(
                    GameEntry.Entity.GenerateSerialId(),
                    typeof(ETMonoUGFEntity),
                    entityAssetName,
                    entityGroupName,
                    priority,
                    ETMonoUGFEntityData.Create(this),
                    cancellationToken: cancellationToken);
            }
            finally
            {
                this.FreeShowEntityToken(cts);
            }

            if (this.m_UGFEntity == null)
            {
                throw new Exception($"UGFEntity ShowEntityAsync failed! entityAssetName:'{entityAssetName}' entityGroupName:'{entityGroupName}'.");
            }
        }

        public async UniTask ShowUIEntityAsync(int entityTypeId)
        {
            CancellationTokenSourcePlus cts = this.GetOrCreateCancellationTokenSource();
            CancellationToken cancellationToken = this.MallocShowEntityToken(cts);
            try
            {
                this.m_UGFEntity = await GameEntry.Entity.ShowUIEntityAsync<ETMonoUGFEntity>(entityTypeId, ETMonoUGFEntityData.Create(this), cancellationToken: cancellationToken);
            }
            finally
            {
                this.FreeShowEntityToken(cts);
            }

            if(this.m_UGFEntity == null)
            {
                throw new Exception($"UGFEntity ShowUIEntityAsync failed! entityTypeId:'{entityTypeId}'.");
            }
        }

        public void SetEntityVisible(bool visible)
        {
            if (this.m_UGFEntity != null)
            {
                this.m_UGFEntity.Logic.Visible = visible;
            }
        }

        public void AttachToParent(UGFEntity parentEntity)
        {
            if (this.m_UGFEntity != null && parentEntity.m_UGFEntity != null)
            {
                GameEntry.Entity.AttachEntity(this.m_UGFEntity, parentEntity.m_UGFEntity);
            }
        }

        public void DetachFromParent()
        {
            if (this.m_UGFEntity != null)
            {
                GameEntry.Entity.DetachEntity(this.m_UGFEntity);
            }
        }

        private CancellationTokenSourcePlus GetOrCreateCancellationTokenSource()
        {
            if (this.m_Cts == null)
            {
                this.m_Cts = ObjectPool.Instance.Fetch<CancellationTokenSourcePlus>();
            }

            return this.m_Cts;
        }

        private CancellationToken MallocShowEntityToken(CancellationTokenSourcePlus cts)
        {
            this.m_ShowEntityAwaitCount++;
            return cts.MallocToken();
        }

        private void FreeShowEntityToken(CancellationTokenSourcePlus cts)
        {
            cts.FreeToken();
            this.m_ShowEntityAwaitCount--;
            if (this.IsDisposed && this.m_Cts == cts && this.m_ShowEntityAwaitCount <= 0)
            {
                ObjectPool.Instance.Recycle(cts);
                this.m_Cts = null;
            }
        }
    }
}

