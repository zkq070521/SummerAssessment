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
        [SerializeField] private float _floatDistance = 80f;          // 上浮高度（屏幕像素）
        [SerializeField] private float _duration = 1f;                // 动画总时长
        [SerializeField] private float _fadeStartDelay = 0.35f;       // 延迟后开始渐隐
        [SerializeField] private Ease _floatEase = Ease.OutQuad;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;

        [Header("字号")]
        [SerializeField] private float _normalFontSize = 40f;         // 普通伤害字号（像素）
        [SerializeField] private float _critFontSize = 56f;           // 暴击伤害字号（像素）

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
        private RectTransform _rectTransform;   // 根节点 Canvas 的 RectTransform
        private RectTransform _contentRect;     // 数字文本的 RectTransform（定位与动画目标）
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

            // 屏幕空间渲染：数字固定在屏幕像素坐标，像 2D 一样始终正面、不被 3D 物体遮挡
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transform.localScale = Vector3.one;

            // 定位与动画目标 = 数字文本
            if (_damageText != null)
                _contentRect = _damageText.rectTransform;

            // 暴击/真伤标签挂到数字文本下，跟随数字一起定位
            if (_critLabel != null && _damageText != null)
            {
                _critLabel.rectTransform.SetParent(_damageText.rectTransform, false);
                _critLabel.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            }

            // 确保初始不可见
            _canvas.enabled = false;
        }

        /// <summary>
        /// 播放伤害数字动画
        /// </summary>
        /// <param name="damage">伤害数值</param>
        /// <param name="color">数字颜色（元素色 / 暴击橙 / 真伤白）</param>
        /// <param name="label">标签文字（"暴击"/"真伤"，空则无标签）</param>
        /// <param name="worldPosition">目标头顶世界坐标</param>
        /// <param name="onComplete">动画结束回调，用于回收到对象池</param>
        public void Show(int damage, Color color, string label, Vector3 screenPosition, System.Action<DamageNumberUI> onComplete)
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

            // 4. 屏幕坐标 → Canvas 本地坐标（Overlay Canvas 铺满屏幕，pivot 在屏幕中心）
            //    加随机横向像素偏移，模拟多段连击的散布
            float randomOffsetX = Random.Range(-20f, 20f);
            if (_rectTransform != null && _contentRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _rectTransform, screenPosition, null, out localPoint))
                {
                    _contentRect.localPosition = new Vector3(localPoint.x + randomOffsetX, localPoint.y, 0f);
                }
            }

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

            Vector3 startPos = _contentRect.localPosition;
            Vector3 endPos = startPos + Vector3.up * _floatDistance;  // _floatDistance 为屏幕像素

            // 弹入缩放：0 → 1（屏幕空间 scale 为 1）
            _scaleTween = _contentRect.DOScale(Vector3.one, SCALE_POP_DURATION)
                .From(Vector3.zero)
                .SetEase(_scaleEase);

            // 上浮（屏幕像素）
            _moveTween = _contentRect.DOLocalMove(endPos, _duration)
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
