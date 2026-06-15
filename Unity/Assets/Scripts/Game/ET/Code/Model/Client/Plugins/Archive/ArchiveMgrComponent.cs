namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class ArchiveMgrComponent : Entity, IAwake, IDestroy
    {
        public string CurrentArchiveName;
        public EntityRef<ArchiveComponent> CurrentArchive;
    }
}
