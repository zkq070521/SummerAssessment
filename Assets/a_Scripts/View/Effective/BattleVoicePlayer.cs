using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗语音播放器 — 在战斗关键节点播放对应角色的 HeroData 语音资源。
    ///
    /// 事件 → 语音映射：
    ///   OnBattleStart（战斗开始）   → battleStartVoice（玩家全员）
    ///   OnUnitAttack（单攻）        → skillVoice（攻击方）
    ///   OnAoeAttack（群攻/终结技）  → ultimateVoice（攻击方）
    ///   OnUnitHit（受击）           → hitVoice（受击方）
    ///   OnUnitDeath（阵亡）         → dieVoice（阵亡方）
    ///
    /// 语音随 BattleEntityData 从 HeroData 拷贝（见 BattleManager.CreatePlayerTeam）。
    /// 挂载在 Battle1 场景的常驻物体上（如 BattleManager），需一个 AudioSource（可自动补）。
    /// </summary>
    public class BattleVoicePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;   // 播放语音的 AudioSource（可空，Awake 自动补）

        private void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;   // 2D 语音，不受距离衰减
        }

        private void OnEnable()
        {
            BattleEventCenter.OnBattleStart += HandleBattleStart;
            BattleEventCenter.OnUnitAttack += HandleUnitAttack;
            BattleEventCenter.OnAoeAttack += HandleAoeAttack;
            BattleEventCenter.OnUnitHit += HandleUnitHit;
            BattleEventCenter.OnUnitDeath += HandleUnitDeath;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleStart -= HandleBattleStart;
            BattleEventCenter.OnUnitAttack -= HandleUnitAttack;
            BattleEventCenter.OnAoeAttack -= HandleAoeAttack;
            BattleEventCenter.OnUnitHit -= HandleUnitHit;
            BattleEventCenter.OnUnitDeath -= HandleUnitDeath;
        }

        private void HandleBattleStart()
        {
            if (BattleManager.Instance == null) return;

            foreach (BattleEntityData entity in BattleManager.Instance.PlayerTeam)
            {
                if (entity != null)
                    Play(entity.battleStartVoice);
            }
        }

        private void HandleUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (source != null)
                Play(source.skillVoice);
        }

        private void HandleAoeAttack(BattleEntityData source, IReadOnlyList<BattleEntityData> targets)
        {
            if (source != null)
                Play(source.ultimateVoice);
        }

        private void HandleUnitHit(BattleEntityData target)
        {
            if (target != null)
                Play(target.hitVoice);
        }

        private void HandleUnitDeath(BattleEntityData entity)
        {
            if (entity != null)
                Play(entity.dieVoice);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }
    }
}
