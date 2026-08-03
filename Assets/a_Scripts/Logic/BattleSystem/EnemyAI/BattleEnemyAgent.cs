using System.Collections;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗敌人 AI 代理 — 挂载在每个敌方战斗实体上，桥接回合制战斗与 Behavior Designer 行为树。
    ///
    /// 工作流程：
    ///   1. 监听 BattleEventCenter.OnTurnStart
    ///   2. 当轮到自身时，启用 BD 行为树进行目标选择（单帧求值）
    ///   3. 行为树完成后读取 SharedVariable TargetHeroID
    ///   4. 调用 BattleManager.ExecuteAction 执行攻击
    ///   5. 延迟后推进到下一回合
    ///
    /// BD 行为树在默认状态下禁用，仅在敌人回合短暂启用以避免持续帧更新。
    /// </summary>
    [RequireComponent(typeof(BehaviorTree))]
    public class BattleEnemyAgent : MonoBehaviour
    {
        /// <summary>行为树完成后到执行攻击的观察等待时间（秒）</summary>
        private const float PRE_ACTION_DELAY = 1.5f;

        /// <summary>BD SharedVariable 名称：选中的目标 heroID</summary>
        private const string VAR_TARGET_HERO_ID = "TargetHeroID";

        private BehaviorTree _behaviorTree;
        private BattleEntityData _myEntity;
        private bool _isActing;

        // ── 生命周期 ──

        private void Awake()
        {
            _behaviorTree = GetComponent<BehaviorTree>();
            _behaviorTree.RestartWhenComplete = false;
            _behaviorTree.DisableBehavior();
        }

        private void OnEnable()
        {
            BattleEventCenter.OnTurnStart += OnTurnStart;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnTurnStart -= OnTurnStart;
            _behaviorTree.OnBehaviorEnd -= OnDecisionComplete;
        }

        // ── 公共 API ──

        /// <summary>
        /// 初始化：绑定到指定战斗实体数据并设置 AI 行为树资产。
        /// 由 BattleManager.InitializeEnemyAgents() 调用。
        /// </summary>
        /// <param name="entity">此敌人对应的运行时 BattleEntityData</param>
        /// <param name="aiAsset">ExternalBehaviorTree 资产（可选；为 null 时使用纯 C# 回退逻辑）</param>
        public void Initialize(BattleEntityData entity, ExternalBehaviorTree aiAsset = null)
        {
            _myEntity = entity;

            if (aiAsset != null)
            {
                _behaviorTree.ExternalBehavior = aiAsset;
            }
        }

        // ── 事件处理 ──

        /// <summary>
        /// 回合开始回调：检查是否为自身回合，是则启动决策流程
        /// </summary>
        private void OnTurnStart(BattleEntityData currentActor)
        {
            if (_myEntity == null || currentActor != _myEntity) return;
            if (_isActing) return; // 防止同一回合重复触发

            _isActing = true;

            // 有 BD 行为树 → 启用行为树进行决策
            if (_behaviorTree.ExternalBehavior != null)
            {
                _behaviorTree.OnBehaviorEnd += OnDecisionComplete;
                _behaviorTree.EnableBehavior();
            }
            else
            {
                // 纯 C# 回退：无行为树时随机选择目标
                FallbackRandomAttack();
            }
        }

        /// <summary>
        /// BD 行为树完成回调：读取决策结果 → 执行攻击 → 推进回合
        /// </summary>
        private void OnDecisionComplete(Behavior behavior)
        {
            _behaviorTree.OnBehaviorEnd -= OnDecisionComplete;
            _behaviorTree.DisableBehavior();

            // 读取行为树输出的目标 heroID
            SharedVariable var = _behaviorTree.GetVariable(VAR_TARGET_HERO_ID);
            string targetHeroID = var != null ? (string)var.GetValue() : null;

            ExecuteAndAdvance(targetHeroID);
        }

        // ── 内部方法 ──

        /// <summary>
        /// 执行攻击动作并延迟推进回合
        /// </summary>
        /// <param name="targetHeroID">目标玩家 heroID；为 null 时跳过攻击但仍推进回合</param>
        private void ExecuteAndAdvance(string targetHeroID)
        {
            if (BattleManager.Instance == null)
            {
                _isActing = false;
                return;
            }

            // 解析目标并执行攻击
            BattleEntityData target = BattleManager.Instance.TryGetPlayerByHeroID(targetHeroID);
            if (target != null && target.isAlive)
            {
                BattleManager.Instance.ExecuteAction(_myEntity, target);
                Debug.Log($"[BattleEnemyAgent] {_myEntity.heroName} 攻击 → {target.heroName}");
            }
            else
            {
                Debug.LogWarning($"[BattleEnemyAgent] {_myEntity.heroName} 无法找到有效目标 (targetHeroID={targetHeroID ?? "null"})，跳过攻击");
            }

            StartCoroutine(DelayedNextTurn());
        }

        /// <summary>
        /// 无 BD 行为树时的纯 C# 回退逻辑：随机选择存活玩家攻击
        /// </summary>
        private void FallbackRandomAttack()
        {
            if (BattleManager.Instance == null)
            {
                _isActing = false;
                return;
            }

            var playerTeam = BattleManager.Instance.PlayerTeam;
            if (playerTeam != null)
            {
                // 查找存活玩家
                for (int i = 0; i < playerTeam.Count; i++)
                {
                    if (playerTeam[i] != null && playerTeam[i].isAlive)
                    {
                        string targetID = playerTeam[i].heroID;
                        ExecuteAndAdvance(targetID);
                        return;
                    }
                }
            }

            // 无存活目标，直接推进回合
            ExecuteAndAdvance(null);
        }

        /// <summary>
        /// 等待观察时间后推进到下一回合
        /// </summary>
        private IEnumerator DelayedNextTurn()
        {
            yield return new WaitForSeconds(PRE_ACTION_DELAY);
            _isActing = false;

            if (BattleManager.Instance != null)
                BattleManager.Instance.StartCoroutineWaitAndNextTurn();
        }
    }
}
