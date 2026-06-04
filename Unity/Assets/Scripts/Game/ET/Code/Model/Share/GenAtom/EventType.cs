using Unity.Mathematics;

namespace ET.Client
{
    public struct SceneChangeStart
    {
    }

    public struct SceneChangeFinish
    {
    }

    public struct SceneChangeBeforeLoadUnit
    {
    }

    public struct AfterCreateClientScene
    {
    }

    public struct AfterCreateCurrentScene
    {
    }

    public struct AppStartInitFinish
    {
    }

    public struct LoginFinish
    {
    }

    public struct EnterMapFinish
    {
    }

    public struct AfterUnitCreate
    {
        public Unit Unit;
    }

    public struct GoScene
    {
        public int SceneId;
    }

    public struct FightInputScreenClick
    {
        public float2 ScreenPosition;
    }
}
