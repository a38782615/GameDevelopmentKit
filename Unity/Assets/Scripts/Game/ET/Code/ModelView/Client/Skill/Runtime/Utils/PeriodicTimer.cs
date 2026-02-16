namespace ET.Client
{
    /// <summary>
    /// 周期计时器 - 用于周期性效果
    /// </summary>
    [EnableClass]
    public class PeriodicTimer
    {
        /// <summary>
        /// 周期时间
        /// </summary>
        public float Period { get; private set; }

        /// <summary>
        /// 当前周期已过时间
        /// </summary>
        public float CurrentPeriodTime { get; private set; }

        /// <summary>
        /// 已触发次数
        /// </summary>
        public int TriggerCount { get; private set; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="period">周期时间</param>
        /// <param name="executeOnStart">是否在开始时立即触发一次</param>
        public PeriodicTimer(float period, bool executeOnStart = false)
        {
            Period = period;
            CurrentPeriodTime = executeOnStart ? period : 0f; // 如果立即执行，设置为周期时间以触发
            TriggerCount = 0;
            IsRunning = true;
        }

        /// <summary>
        /// 更新计时器
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        /// <returns>本帧触发次数</returns>
        public int Tick(float deltaTime)
        {
            if (!IsRunning || Period <= 0)
                return 0;

            CurrentPeriodTime += deltaTime;

            int triggers = 0;
            while (CurrentPeriodTime >= Period)
            {
                CurrentPeriodTime -= Period;
                TriggerCount++;
                triggers++;
            }

            return triggers;
        }

        /// <summary>
        /// 重置周期计时
        /// </summary>
        public void ResetPeriod()
        {
            CurrentPeriodTime = 0f;
        }

        /// <summary>
        /// 设置新的周期
        /// </summary>
        public void SetPeriod(float period)
        {
            Period = period;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            IsRunning = true;
        }

        /// <summary>
        /// 完全重置
        /// </summary>
        public void Reset(float? newPeriod = null)
        {
            if (newPeriod.HasValue)
            {
                Period = newPeriod.Value;
            }
            CurrentPeriodTime = 0f;
            TriggerCount = 0;
            IsRunning = false;
        }
    }
}
