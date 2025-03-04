﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    public readonly struct RpcInfo
    {
        public readonly IRequest Request;
        public readonly AutoResetUniTaskCompletionSource<IResponse> Tcs;

        public RpcInfo(IRequest request)
        {
            this.Request = request;
            this.Tcs = AutoResetUniTaskCompletionSource<IResponse>.Create();
        }
    }
    
    [EntitySystemOf(typeof(Session))]
    [FriendOf(typeof(Session))]
    public static partial class SessionSystem
    {
        [EntitySystem]
        private static void Awake(this Session self, AService aService)
        {
            self.AService = aService;
            long timeNow = TimeInfo.Instance.ClientNow();
            self.LastRecvTime = timeNow;
            self.LastSendTime = timeNow;

            self.requestCallbacks.Clear();
            
            Log.Info($"session create: zone: {self.Zone()} id: {self.Id} {timeNow} ");
        }
        
        [EntitySystem]
        private static void Destroy(this Session self)
        {
            self.AService.Remove(self.Id, self.Error);
            
            foreach (RpcInfo responseCallback in self.requestCallbacks.Values.ToArray())
            {
                responseCallback.Tcs.TrySetException(new RpcException(self.Error, $"session dispose: {self.Id} {self.RemoteAddress}"));
            }

            Log.Info($"session dispose: {self.RemoteAddress} id: {self.Id} ErrorCode: {self.Error}, please see ErrorCode.cs! {TimeInfo.Instance.ClientNow()}");
            
            self.requestCallbacks.Clear();
        }
        
        public static void OnResponse(this Session self, IResponse response)
        {
            if (!self.requestCallbacks.Remove(response.RpcId, out var action))
            {
                return;
            }
            action.Tcs.TrySetResult(response);
        }
        
        public static UniTask<IResponse> Call(this Session self, IRequest request, CancellationToken token)
        {
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new RpcInfo(request);
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;

            self.Send(request);
            
            void CancelAction()
            {
                if (!self.requestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                {
                    return;
                }

                self.requestCallbacks.Remove(rpcId);
                Type responseType = OpcodeType.Instance.GetResponseType(action.Request.GetType());
                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.Error = ErrorCore.ERR_Cancel;
            }
            
            return rpcInfo.Tcs.Task.AttachCancellation(token, CancelAction);
        }

        public static async UniTask<IResponse> Call(this Session self, IRequest request, int time = 0)
        {
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request);
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;
            self.Send(request);

            if (time > 0)
            {
                async UniTaskVoid Timeout()
                {
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(time);
                    if (!self.requestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                    {
                        return;
                    }

                    if (!self.requestCallbacks.Remove(rpcId))
                    {
                        return;
                    }
                    
                    action.Tcs.TrySetException(new Exception($"session call timeout: {request} {time}"));
                }
                
                Timeout().Forget();
            }

            return await rpcInfo.Tcs.Task;
        }

        public static void Send(this Session self, IMessage message)
        {
            self.Send(default, message);
        }
        
        public static void Send(this Session self, ActorId actorId, IMessage message)
        {
            self.LastSendTime = TimeInfo.Instance.ClientNow();
            LogMsg.Instance.Debug(self.Fiber(), message);

            (ushort opcode, MemoryBuffer memoryBuffer) = MessageSerializeHelper.ToMemoryBuffer(self.AService, actorId, message);
            self.AService.Send(self.Id, memoryBuffer);
        }
    }

    [ChildOf]
    public sealed class Session: Entity, IAwake<AService>, IDestroy
    {
        public AService AService { get; set; }
        
        public int RpcId
        {
            get;
            set;
        }

        public readonly Dictionary<int, RpcInfo> requestCallbacks = new();
        
        public long LastRecvTime
        {
            get;
            set;
        }

        public long LastSendTime
        {
            get;
            set;
        }

        public int Error
        {
            get;
            set;
        }

        public IPEndPoint RemoteAddress
        {
            get;
            set;
        }
    }
}