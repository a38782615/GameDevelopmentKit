namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class EntityBody : Entity, IAwake, IDestroy
    {
        public const int CircleShape = 1;
        public const int RectangleShape = 2;

        public int Width;
        public int Height;
        public int Shape;
    }
}
