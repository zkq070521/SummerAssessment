using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 回合行动管理器 — 纯逻辑类，以 AV（Action Value）系统驱动行动顺序
    ///
    /// 公式：AV = 10000 / speed，值越小越先行动
    /// 时间推进：所有单位减去当前最小 AV，行动者 AV 重置后重新排队
    ///
    /// 不继承 MonoBehaviour，由 BattleManager 构造并驱动
    /// </summary>
    public class TurnManager
    {
        private const float BASE_AV = 10000f;

        /// <summary>实体 → 当前行动值</summary>
        private readonly Dictionary<BattleEntityData, float> _avMap = new Dictionary<BattleEntityData, float>();

        /// <summary>
        /// 用参战单位列表初始化
        /// </summary>
        public TurnManager(IReadOnlyList<BattleEntityData> units)
        {
            if (units == null) return;

            foreach (BattleEntityData entity in units)
            {
                if (entity == null) continue;
                AddEntityInternal(entity);
            }
        }

        /// <summary>
        /// 获取下一个行动者
        /// 1. 找到 AV 最小的单位
        /// 2. 所有单位 AV 减去该最小值（时间流逝）
        /// 3. 重置行动单位的 AV，重新排队
        /// </summary>
        /// <returns>下一个行动单位；无存活单位时返回 null</returns>
        public BattleEntityData GetNextActor()
        {
            if (_avMap.Count == 0) return null;

            // 找 AV 最小的单位
            BattleEntityData nextActor = null;
            float minAV = float.MaxValue;

            foreach (KeyValuePair<BattleEntityData, float> kv in _avMap)
            {
                if (kv.Value < minAV)
                {
                    minAV = kv.Value;
                    nextActor = kv.Key;
                }
            }

            if (nextActor == null) return null;

            // 时间流逝：所有单位 AV 减去最小值
            var allUnits = new List<BattleEntityData>(_avMap.Keys);
            foreach (BattleEntityData entity in allUnits)
            {
                _avMap[entity] -= minAV;
            }

            // 重置行动单位的 AV
            float speed = nextActor.speed;
            _avMap[nextActor] = speed > 0 ? BASE_AV / speed : float.MaxValue;

            return nextActor;
        }

        /// <summary>
        /// 移除单位（死亡时调用）
        /// </summary>
        public void RemoveUnit(BattleEntityData entity)
        {
            if (entity == null) return;
            if (_avMap.ContainsKey(entity))
            {
                _avMap.Remove(entity);
            }
        }

        /// <summary>
        /// 返回按当前 AV 升序排列的单位列表（供 UI 行动条显示）
        /// </summary>
        public IReadOnlyList<BattleEntityData> GetTurnOrder()
        {
            List<BattleEntityData> sorted = new List<BattleEntityData>(_avMap.Keys);
            sorted.Sort((a, b) => _avMap[a].CompareTo(_avMap[b]));
            return sorted;
        }

        private void AddEntityInternal(BattleEntityData entity)
        {
            float speed = entity.speed;
            float av = speed > 0 ? BASE_AV / speed : float.MaxValue;
            _avMap[entity] = av;
        }
    }
}
