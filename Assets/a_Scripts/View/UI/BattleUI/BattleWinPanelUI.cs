using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗胜利横幅动画 — 挂载在 BattleCanva（Canvas）上，Inspector 中拖入 WinPanel。
    ///
    /// WinPanel 下有两个子物体（Star / Text）：
    ///   1. 战斗开始时两者都隐藏；
    ///   2. 敌人总血量归零（总血条扣到 0，OnEnemyHpDepleted 触发）1 秒后，
    ///      第一个子物体显示，并在 0.8 秒内高度平滑收缩为 0 后消失；
    ///   3. 高度归零的同时，第二个子物体高度从 0 平滑展开到自然高度（约 210）。
    ///
    /// 通过 BattleEventCenter 事件与逻辑层解耦；高度动画基于 RectTransform.sizeDelta.y
    /// （两个子物体均为中心锚点，sizeDelta.y 即高度）。使用 DOTween 平滑缓动。
    /// </summary>
    public class BattleWinPanelUI : MonoBehaviour
    {
        /// <summary>敌人血量归零后到开始播放胜利动画的延迟（秒）</summary>
        private const float WIN_SHOW_DELAY = 1f;

        /// <summary>第一个子物体（Star）高度收缩到 0 的时长（秒）</summary>
        private const float COLLAPSE_DURATION = 0.8f;

        /// <summary>第二个子物体（Text）高度展开的时长（秒）。规格未指定，取与收缩一致</summary>
        private const float EXPAND_DURATION = 0.8f;

        [Header("胜利面板")]
        [SerializeField] private RectTransform _winPanel;   // 拖入 WinPanel

        [Header("缓动")]
        [SerializeField] private Ease _collapseEase = Ease.OutQuad;
        [SerializeField] private Ease _expandEase = Ease.OutQuad;

        private RectTransform _firstChild;    // WinPanel 第一个子物体（Star）
        private RectTransform _secondChild;   // WinPanel 第二个子物体（Text）
        private Vector2 _firstChildOriginalSize;
        private Vector2 _secondChildOriginalSize;

        private void Awake()
        {
            if (_winPanel == null)
            {
                Debug.LogWarning("[BattleWinPanelUI] _winPanel 未赋值，请在 Inspector 中拖入 WinPanel");
                return;
            }

            if (_winPanel.childCount < 2)
            {
                Debug.LogWarning("[BattleWinPanelUI] WinPanel 子物体不足 2 个，无法播放胜利动画");
                return;
            }

            _firstChild = _winPanel.GetChild(0) as RectTransform;
            _secondChild = _winPanel.GetChild(1) as RectTransform;
            _firstChildOriginalSize = _firstChild.sizeDelta;
            _secondChildOriginalSize = _secondChild.sizeDelta;

            ResetToHidden();
        }

        private void OnEnable()
        {
            BattleEventCenter.OnBattleStart += HandleBattleStart;
            BattleEventCenter.OnEnemyHpDepleted += HandleEnemyHpDepleted;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleStart -= HandleBattleStart;
            BattleEventCenter.OnEnemyHpDepleted -= HandleEnemyHpDepleted;

            if (_firstChild != null) _firstChild.DOKill();
            if (_secondChild != null) _secondChild.DOKill();
        }

        private void HandleBattleStart()
        {
            ResetToHidden();
        }

        private void HandleEnemyHpDepleted()
        {
            if (_firstChild == null || _secondChild == null)
                return;

            StartCoroutine(PlayWinAnimation());
        }

        /// <summary>隐藏两个子物体并复位尺寸，同时终止尚未完成的动画</summary>
        private void ResetToHidden()
        {
            if (_firstChild != null)
            {
                _firstChild.DOKill();
                _firstChild.sizeDelta = _firstChildOriginalSize;
                _firstChild.gameObject.SetActive(false);
            }

            if (_secondChild != null)
            {
                _secondChild.DOKill();
                _secondChild.sizeDelta = _secondChildOriginalSize;
                _secondChild.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 胜利动画：等待 WIN_SHOW_DELAY 秒后显示第一个子物体，
        /// 令其高度在 COLLAPSE_DURATION 秒内平滑收缩为 0；归零时展开第二个子物体。
        /// </summary>
        private IEnumerator PlayWinAnimation()
        {
            yield return new WaitForSeconds(WIN_SHOW_DELAY);

            _firstChild.gameObject.SetActive(true);
            _firstChild.sizeDelta = _firstChildOriginalSize;
            _firstChild
                .DOSizeDelta(new Vector2(_firstChildOriginalSize.x, 0f), COLLAPSE_DURATION)
                .SetEase(_collapseEase)
                .OnComplete(ShowSecondChild);
        }

        /// <summary>第一个子物体高度归零后：隐藏它，并让第二个子物体高度从 0 展开到自然高度（约 210）</summary>
        private void ShowSecondChild()
        {
            _firstChild.gameObject.SetActive(false);

            _secondChild.gameObject.SetActive(true);
            _secondChild.sizeDelta = new Vector2(_secondChildOriginalSize.x, 0f);
            _secondChild
                .DOSizeDelta(new Vector2(_secondChildOriginalSize.x, _secondChildOriginalSize.y), EXPAND_DURATION)
                .SetEase(_expandEase);
        }
    }
}
