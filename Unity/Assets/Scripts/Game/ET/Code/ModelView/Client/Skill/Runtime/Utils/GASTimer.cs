namespace ET.Client
{
    /// <summary>
    /// 计时器工具类 - 用于技能和效果的时间管理
    /// </summary>
    [EnableClass]
    public class GASTimer
    {
        /// <summary>
        /// 目标时间
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 已经过的时间
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// 剩余时间
        /// </summary>
        public float RemainingTime => Duration - ElapsedTime;

        /// <summary>
        /// 进度 (0-1)
        /// </summary>
        public float Progress => Duration > 0 ? ElapsedTime / Duration : 1f;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => ElapsedTime >= Duration;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="autoStart">是否自动开始</param>
        public GASTimer(float duration, bool autoStart = true)
        {
            Duration = duration;
            ElapsedTime = 0f;
            IsRunning = autoStart;
            IsPaused = false;
        }

        /// <summary>
        /// 更新计时器
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        /// <returns>是否刚刚完成</returns>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning || IsPaused || IsCompleted)
                return false;

            float previousTime = ElapsedTime;
            ElapsedTime += deltaTime;

            // 检查是否刚刚完成
            if (previousTime < Duration && ElapsedTime >= Duration)
            {
                ElapsedTime = Duration;
                IsRunning = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
        }

        /// <summary>
        /// 暂停计时
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// 恢复计时
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        /// <param name="newDuration">新的持续时间（可选）</param>
        public void Reset(float? newDuration = null)
        {
            if (newDuration.HasValue)
            {
                Duration = newDuration.Value;
            }
            ElapsedTime = 0f;
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// 重置并开始
        /// </summary>
        /// <param name="newDuration">新的持续时间（可选）</param>
        public void Restart(float? newDuration = null)
        {
            Reset(newDuration);
            Start();
        }

        /// <summary>
        /// 设置持续时间（会重置进度）
        /// </summary>
        public void SetDuration(float duration)
        {
            Duration = duration;
            if (ElapsedTime > Duration)
            {
                ElapsedTime = Duration;
            }
        }

        /// <summary>
        /// 刷新持续时间（重置已过时间）
        /// </summary>
        public void RefreshDuration()
        {
            ElapsedTime = 0f;
            if (!IsRunning)
            {
                IsRunning = true;
            }
        }
    }


}
