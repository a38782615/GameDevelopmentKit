using System.Runtime.InteropServices;

namespace ET
{
    public static class EntityHelper
    {
        public static int Zone(this Entity entity)
        {
            return entity.IScene.Fiber.Zone;
        }

        public static Scene Scene(this Entity entity)
        {
            return entity.IScene as Scene;
        }
        
        public static T Scene<T>(this Entity entity) where T: class, IScene 
        {
            return entity.IScene as T;
        }
        
        public static Scene Root(this Entity entity)
        {
            return entity.IScene.Fiber.Root;
        }

        public static Fiber Fiber(this Entity entity)
        {
            return entity.IScene.Fiber;
        }

        public static T GetOrAddComponent<T>(this Entity entity, bool isFromPool = false) where T : Entity, IAwake, new()
        {
            var ret = entity.GetComponent<T>();
            if (ret == null)
            {
                ret = entity.AddComponent<T>(isFromPool);
            }
            return ret;
        }

        public static T GetOrAddComponent<T,A>(this Entity entity, A a,bool isFromPool = false) where T : Entity, IAwake<A>, new()
        {
            var ret = entity.GetComponent<T>();
            if (ret == null)
            {
                ret = entity.AddComponent<T,A>(a, isFromPool);
            }
            return ret;
        }

        public static T GetOrAddComponent<T, A, B>(this Entity entity, A a, B b,bool isFromPool = false) where T : Entity, IAwake<A,B>, new()
        {
            var ret = entity.GetComponent<T>();
            if (ret == null)
            {
                ret = entity.AddComponent<T, A, B>(a,b, isFromPool);
            }
            return ret;
        }

        public static T GetOrAddComponent<T, A, B, C>(this Entity entity, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>, new()
        {
            var ret = entity.GetComponent<T>();
            if (ret == null)
            {
                ret = entity.AddComponent<T, A, B, C>(a, b, c, isFromPool);
            }
            return ret;
        }


        public static T GetOrAddChild<T, A>(this Entity entity,long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>, new()
        {
            var ret = entity.GetChild<T>(id);
            if (ret == null)
            {
                ret = entity.AddChildWithId<T, A>(id, a, isFromPool);
            }
            return ret;
        }

        public static T GetOrAddChild<T, A, B>(this Entity entity, long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>, new()
        {
            var ret = entity.GetChild<T>(id);
            if (ret == null)
            {
                ret = entity.AddChildWithId<T, A, B>(id, a, b, isFromPool);
            }
            return ret;
        }

        public static T GetOrAddChild<T, A, B, C>(this Entity entity, long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>, new()
        {
            var ret = entity.GetChild<T>(id);
            if (ret == null)
            {
                ret = entity.AddChildWithId<T, A, B, C>(id, a, b, c, isFromPool);
            }
            return ret;
        }
    }
}