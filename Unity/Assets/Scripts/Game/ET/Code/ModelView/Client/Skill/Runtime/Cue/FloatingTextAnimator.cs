using UnityEngine;
using TMPro;

namespace ET.Client
{
    /// <summary>
    /// 飘字动画组件 - 带随机弧度的弹出效果
    /// </summary>
    public class FloatingTextAnimator : MonoBehaviour
    {
        private float _duration;
        private float _elapsedTime;
        private Vector2 _velocity;
        private float _gravity;
        private RectTransform _rectTransform;
        private TextMeshProUGUI _textComponent;
        private Vector2 _startPosition;
        private float _startScale;

        public void Initialize(float duration, float horizontalRandomRange, float popForce, float gravity)
        {
            _duration = duration;
            _gravity = gravity;
            _elapsedTime = 0f;

            _rectTransform = GetComponent<RectTransform>();
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                _textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }

            _startPosition = _rectTransform.anchoredPosition;

            // 随机水平方向和初始速度
            float randomAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            float randomHorizontal = Random.Range(-horizontalRandomRange, horizontalRandomRange);

            // 初始速度：向上 + 随机水平偏移
            _velocity = new Vector2(
                randomHorizontal + Mathf.Sin(randomAngle) * popForce * 0.3f,
                popForce + Random.Range(-20f, 20f)
            );

            // 初始放大效果
            _startScale = 1f;
            transform.localScale = Vector3.one * _startScale;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            float progress = _elapsedTime / _duration;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // 应用重力
            _velocity.y -= _gravity * Time.deltaTime;

            // 更新位置
            _rectTransform.anchoredPosition += _velocity * Time.deltaTime;

            // 缩放动画：开始时放大，然后恢复正常
            float scaleProgress = Mathf.Clamp01(progress * 4f); // 前25%完成缩放
            float currentScale = Mathf.Lerp(_startScale, 1f, scaleProgress);
            transform.localScale = Vector3.one * currentScale;

            // 淡出效果（后40%开始淡出）
            if (progress > 0.6f)
            {
                float fadeProgress = (progress - 0.6f) / 0.4f;
                float alpha = 1f - fadeProgress;

                if (_textComponent != null)
                {
                    var color = _textComponent.color;
                    color.a = alpha;
                    _textComponent.color = color;
                }
            }
        }
    }
}
