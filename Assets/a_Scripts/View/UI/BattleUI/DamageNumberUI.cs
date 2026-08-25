using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 单个伤害数字实例 — 世界空间 Canvas 上的浮动伤害文字
    ///
    /// 生命周期：从对象池取出 → Show() 播放上浮动画 → 动画结束自动回收
    /// 预制体需包含：Canvas（WorldSpace）、TMP_Text（主数字）、TMP_Text（暴击标签，可选）
    /// </summary>
    public class DamageNumberUI : MonoBehaviour
    {
        // ── 序列化字段 ──

        [Header("TMP 引用")]
        [SerializeField] private TMP_Text _damageText;
        [SerializeField] private TMP_Text _critLabel;

        [Header("动画参数")]
        [SerializeField] private float _floatDistance = 1.8f;          // 上浮高度（世界单位）
        [SerializeField] private float _duration = 0.9f;               // 动画总时长
        [SerializeField] private float _fadeStartDelay = 0.35f;        // 延迟后开始渐隐
        [SerializeField] private Ease _floatEase = Ease.OutQuad;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;

        [Header("字号")]
        [SerializeField] private float _normalFontSize = 4f;          // 普通伤害字号
        [SerializeField] private float _critFontSize = 8f;            // 暴击伤害字号

        // ── 标签常量 ──

        /// <summary>暴击标签文字</summary>
        public const string LabelCrit = "暴击";
        /// <summary>真实伤害标签文字</summary>
        public const string LabelTrueDamage = "真伤";

        private static readonly Color LabelCritColor = new Color(1f, 0.85f, 0.2f, 1f);       // 金黄
        private static readonly Color LabelTrueDamageColor = new Color(0.9f, 0.95f, 1f, 1f); // 冷白

        /// <summary>根据标签返回颜色（未知标签用白色）</summary>
        private static Color GetLabelColor(string label)
        {
            switch (label)
            {
                case LabelCrit: return LabelCritColor;
                case LabelTrueDamage: return LabelTrueDamageColor;
                default: return Color.white;
            }
        }

        // ── 内部状态 ──

        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Tween _moveTween;
        private Tween _fadeTween;
        private Tween _scaleTween;
        private System.Action<DamageNumberUI> _onComplete;

        private const float SCALE_POP_DURATION = 0.2f;                 // 弹入缩放的时长

        // ── 初始化 ──

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();

            if (_damageText == null)
                _damageText = GetComponentInChildren<TMP_Text>();

            // 确保初始不可见
            _canvas.enabled = false;

            // 诊断日志：确认预制体原始缩放
            Debug.Log($"[DamageNumberUI] Awake — localScale: {transform.localScale}, canvas.renderMode: {_canvas.renderMode}");
        }

        /// <summary>
        /// 播放伤害数字动画
        /// </summary>
        /// <param name="damage">伤害数值</param>
        /// <param name="color">数字颜色（元素色 / 暴击橙 / 真伤白）</param>
        /// <param name="label">标签文字（"暴击"/"真伤"，空则无标签）</param>
        /// <param name="worldPosition">目标头顶世界坐标</param>
        /// <param name="onComplete">动画结束回调，用于回收到对象池</param>
        public void Show(int damage, Color color, string label, Vector3 worldPosition, System.Action<DamageNumberUI> onComplete)
        {
            _onComplete = onComplete;

            // 0. 从对象池取出时激活（池中实例为 SetActive(false) 状态）
            gameObject.SetActive(true);

            // 1. 设置数字（Alpha 强制为 1，防止 DOFade 残留）
            //    预制体中 DamageText 处于未激活状态，必须先激活才能渲染
            _damageText.gameObject.SetActive(true);
            _damageText.text = damage.ToString();
            _damageText.color = new Color(color.r, color.g, color.b, 1f);
            _damageText.alpha = 1f;

            // 2. 标签（"暴击"/"真伤" 等；无标签则隐藏）
            bool emphasized = label == LabelCrit;
            if (_critLabel != null)
            {
                bool hasLabel = !string.IsNullOrEmpty(label);
                _critLabel.gameObject.SetActive(hasLabel);
                if (hasLabel)
                {
                    _critLabel.text = label;
                    _critLabel.color = GetLabelColor(label);
                }
            }

            // 3. 暴击时字体更大更粗
            _damageText.fontSize = emphasized ? _critFontSize : _normalFontSize;
            _damageText.fontStyle = emphasized ? FontStyles.Bold : FontStyles.Normal;

            // 4. 定位到世界坐标 + 随机横向偏移
            float randomOffsetX = Random.Range(-0.4f, 0.4f);
            transform.position = worldPosition + new Vector3(randomOffsetX, 0f, 0f);

            // 5. 启用显示
            _canvas.enabled = true;

            // 6. 播放动画序列
            PlayAnimation();
        }

        /// <summary>
        /// DOTween 动画：弹入 → 上浮 + 渐隐 → 回收
        /// </summary>
        private void PlayAnimation()
        {
            // 终止之前可能残留的动画
            KillAllTweens();

            // 获取目标缩放：优先用预制体的原始 localScale，兜底用 0.01（世界空间 Canvas 典型值）
            Vector3 targetScale = transform.localScale;
            if (targetScale == Vector3.zero || targetScale.magnitude < 0.0001f)
            {
                targetScale = new Vector3(0.01f, 0.01f, 0.01f);
                Debug.LogWarning($"[DamageNumberUI] localScale 为 0，使用兜底值 {targetScale}");
            }

            Debug.Log($"[DamageNumberUI] PlayAnimation — targetScale: {targetScale}");

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * _floatDistance;

            // 弹入缩放：0 → targetScale
            _scaleTween = transform.DOScale(targetScale, SCALE_POP_DURATION)
                .From(Vector3.zero)
                .SetEase(_scaleEase);

            // 上浮
            _moveTween = transform.DOMove(endPos, _duration)
                .SetEase(_floatEase);

            // 渐隐（延迟启动，保证数字先清晰显示再开始淡出）
            _fadeTween = _damageText.DOFade(0f, _duration - _fadeStartDelay)
                .SetDelay(_fadeStartDelay)
                .SetEase(Ease.InQuad);

            if (_critLabel != null && _critLabel.gameObject.activeSelf)
            {
                _critLabel.DOFade(0f, _duration - _fadeStartDelay)
                    .SetDelay(_fadeStartDelay)
                    .SetEase(Ease.InQuad);
            }

            // 动画全部完成后回收
            _moveTween.OnComplete(() =>
            {
                _canvas.enabled = false;
                gameObject.SetActive(false);
                _onComplete?.Invoke(this);
            });
        }

        /// <summary>终止所有动画，在回收前调用</summary>
        private void KillAllTweens()
        {
            _moveTween?.Kill();
            _fadeTween?.Kill();
            _scaleTween?.Kill();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }
    }
}
