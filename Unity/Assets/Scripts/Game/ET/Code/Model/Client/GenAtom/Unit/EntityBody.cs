namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class EntityBody : Entity, IAwake, IDestroy
    {
        public enum ShapeType
        {
            CircleShape = 1,
            RectangleShape = 2
        }

        public float Width;
        public float Height;
        public ShapeType Shape;
    }
}
