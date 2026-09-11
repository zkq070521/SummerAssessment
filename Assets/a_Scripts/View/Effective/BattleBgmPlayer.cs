using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗背景音乐播放器 — 挂载在 Battle1 场景的常驻物体上。
    /// 进入战斗场景即循环播放背景音乐；退出战斗场景（单场景模式切换导致本场景卸载）时，
    /// 本组件随物体一起销毁，AudioSource 自动停止，无需额外事件监听。
    /// </summary>
    public class BattleBgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _bgmClip;          // 循环播放的战斗背景音乐
        [SerializeField] private AudioSource _audioSource;    // 可空，Awake 自动补

        private void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;   // 2D 音乐，不受距离 / 朝向影响
        }

        private void Start()
        {
            if (_bgmClip == null)
            {
                Debug.LogWarning("[BattleBgmPlayer] 未配置 _bgmClip，无背景音乐播放");
                return;
            }

            _audioSource.clip = _bgmClip;
            _audioSource.Play();
        }
    }
}
