using System.Collections.Generic;

namespace ET.Client
{
    public partial class TaskData : Object
    {
        public Dictionary<int, int> TaskStates = new Dictionary<int, int>();
        public Dictionary<int, long> TaskProgresses = new Dictionary<int, long>();
    }
}
