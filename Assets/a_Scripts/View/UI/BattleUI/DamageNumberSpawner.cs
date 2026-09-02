using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 伤害数字生成器 — 订阅 OnDamageDealt 事件，在目标头顶生成浮动伤害数字
    ///
    /// 挂载在 BattleManager 所在的 GameObject 上。
    /// 通过对象池管理 DamageNumberUI 实例，支持 AOE 同时命中多目标。
    ///
    /// 连击规则：把一次伤害拆成多段依次跳出（各段之和 = 实际伤害，仅视觉拆分）
    /// 颜色规则：普通按 ElementType 元素色，暴击用橙色，真伤用白色
    /// 标签规则：暴击段显示「暴击」，真伤段显示「真伤」
    /// </summary>
    public class DamageNumberSpawner : MonoBehaviour
    {
        // ── 序列化字段 ──

        [Header("预制体")]
        [SerializeField] private DamageNumberUI _damageNumberPrefab;

        [Header("对象池")]
        [SerializeField] private int _poolSize = 15;          // 预初始化数量
        [SerializeField] private int _maxPoolSize = 30;       // 池上限（安全兜底）

        [Header("生成偏移")]
        [SerializeField] private float _heightOffset = 1.2f;  // 目标头顶偏移（世界单位）
        [SerializeField] private float _appearDelay = 1.5f;   // 命中后延迟出现，等待摄像机切到受击镜头（对齐 BattleCameraController 攻击起手时长）

        [Header("连击效果")]
        [SerializeField] private int _minHits = 3;                       // 每次攻击最少跳出几段数字
        [SerializeField] private int _maxHits = 5;                       // 最多跳出几段数字
        [SerializeField] private float _hitStaggerDelay = 0.08f;         // 相邻两段数字的间隔（秒）
        [SerializeField] private float _horizontalSpread = 0.8f;         // 数字横向散布范围
        [SerializeField] [Range(0f, 1f)] private float _trueDamageChance = 0.35f; // 出现「真伤」段的概率

        // ── 对象池 ──

        private readonly Queue<DamageNumberUI> _pool = new Queue<DamageNumberUI>();
        private int _activeCount = 0;

        // ── 元素颜色表 ──

        private static readonly Dictionary<ElementType, Color> ElementColors = new Dictionary<ElementType, Color>
        {
            { ElementType.Physical,  new Color(0.75f, 0.75f, 0.78f) },  // 银白灰
            { ElementType.Fire,      new Color(1.00f, 0.42f, 0.29f) },  // 橙红
            { ElementType.Ice,       new Color(0.41f, 0.82f, 0.91f) },  // 冰蓝
            { ElementType.Lightning, new Color(0.70f, 0.53f, 1.00f) },  // 紫电
            { ElementType.Wind,      new Color(0.36f, 0.86f, 0.58f) },  // 翠绿
            { ElementType.Quantum,   new Color(0.88f, 0.25f, 0.98f) },  // 品紫
            { ElementType.Imaginary, new Color(1.00f, 0.84f, 0.25f) },  // 金黄
        };

        private static readonly Color DefaultColor = Color.white;

        // 特殊数字颜色
        private static readonly Color CritColor = new Color(1f, 0.62f, 0.15f);   // 暴击橙
        private static readonly Color TrueDamageColor = Color.white;              // 真伤白

        // ── 摄像机缓存 ──
        private Camera _cachedCamera;

        // ── 生命周期 ──

        private void Awake()
        {
            // 预初始化对象池
            if (_damageNumberPrefab != null)
            {
                for (int i = 0; i < _poolSize; i++)
                {
                    DamageNumberUI instance = CreateNewInstance();
                    _pool.Enqueue(instance);
                }
                Debug.Log($"[DamageNumberSpawner] 对象池已初始化 {_poolSize} 个实例");
            }
            else
            {
                Debug.LogWarning("[DamageNumberSpawner] _damageNumberPrefab 未赋值，请在 Inspector 中拖入 DamageNumber 预制体");
            }
        }

        private void Start()
        {
            // 缓存战斗摄像机：取场景中 CinemachineBrain 的输出摄像机。
            // 不能依赖 Camera.main —— 战斗相机由 CinemachineBrain 驱动，
            // Camera.main 可能为空或指向错误相机，导致伤害数字的
            // Canvas.worldCamera 与朝向设置错误、数字不朝镜头。
            CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
            Camera cam = brain != null ? brain.OutputCamera : null;
            if (cam == null && brain != null)
                cam = brain.GetComponent<Camera>(); // OutputCamera 为空时，从 Brain 所在对象取 Camera
            _cachedCamera = cam != null ? cam : Camera.main;
        }

        private void OnEnable()
        {
            BattleEventCenter.OnDamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnDamageDealt -= OnDamageDealt;
        }

        // ── 事件回调 ──

        /// <summary>
        /// 收到伤害事件 → 启动多段数字连击（模拟「崩坏：星穹铁道」式跳字爽感）
        /// </summary>
        private void OnDamageDealt(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
        {
            if (_damageNumberPrefab == null)
            {
                Debug.LogWarning("[DamageNumberSpawner] _damageNumberPrefab 未赋值，跳过");
                return;
            }

            if (target == null || string.IsNullOrEmpty(target.heroID))
                return;

            StartCoroutine(SpawnDamageCascade(source, target, damage, isCritical));
        }

        /// <summary>
        /// 把一次伤害拆成 N 段依次跳出，营造多段连击的爽感。
        /// 各段之和 = 实际伤害，仅视觉拆分，不影响数值平衡。
        /// </summary>
        private IEnumerator SpawnDamageCascade(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
        {
            // 等摄像机从攻击镜头切到受击镜头再跳数字，避免攻击起手阶段数字提前出现
            yield return new WaitForSeconds(_appearDelay);

            // 目标世界坐标只查一次
            if (!BattleManager.Instance.TryGetEntityTransform(target.heroID, out Transform targetTransform))
            {
                Debug.LogWarning($"[DamageNumberSpawner] 未找到目标 {target.heroName} (heroID={target.heroID}) 的 Transform");
                yield break;
            }

            Vector3 basePos = targetTransform.position + Vector3.up * _heightOffset;
            Color elementColor = GetColorByElement(source?.element ?? ElementType.Physical);

            // 拆段：每段 ≥ 1，总和 = 实际伤害
            int hitCount = Mathf.Clamp(Random.Range(_minHits, _maxHits + 1), 1, damage);
            int[] segments = SplitDamage(damage, hitCount);

            // 随机挑一段作为「真伤」（优先选较小的后续段，主段保持元素/暴击色）
            int trueDamageIndex = -1;
            if (Random.value < _trueDamageChance && segments.Length > 0)
                trueDamageIndex = segments.Length == 1 ? 0 : Random.Range(1, segments.Length);

            for (int i = 0; i < segments.Length; i++)
            {
                Color color = elementColor;
                string label = string.Empty;

                if (i == trueDamageIndex)
                {
                    color = TrueDamageColor;
                    label = DamageNumberUI.LabelTrueDamage;
                }
                else if (isCritical)
                {
                    color = CritColor;
                    label = DamageNumberUI.LabelCrit;
                }

                Vector3 pos = basePos
                    + Vector3.right * Random.Range(-_horizontalSpread, _horizontalSpread)
                    + Vector3.up * Random.Range(0f, 0.5f);

                SpawnNumber(segments[i], color, label, pos);

                // 相邻段之间留出一点间隔，形成逐个跳出的节奏
                if (i < segments.Length - 1)
                    yield return new WaitForSeconds(_hitStaggerDelay);
            }
        }

        /// <summary>从池中取出一个实例，把世界坐标投影成屏幕坐标后播放</summary>
        private void SpawnNumber(int value, Color color, string label, Vector3 worldPos)
        {
            if (_cachedCamera == null)
            {
                Debug.LogWarning("[DamageNumberSpawner] _cachedCamera 为 null，无法投影屏幕坐标");
                return;
            }

            // 世界坐标 → 屏幕坐标（屏幕空间数字，像 2D 一样固定在屏幕上）
            Vector3 screenPos = _cachedCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f)
                return; // 目标在摄像机后方，跳过

            DamageNumberUI instance = GetFromPool();

            // 确保数字渲染在其他 UI 之上
            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 100;

            instance.Show(value, color, label, screenPos, ReturnToPool);
        }

        /// <summary>
        /// 把 damage 拆成 segments 段，每段 ≥ 1、总和 = damage，大数字排前面。
        /// </summary>
        private int[] SplitDamage(int damage, int segments)
        {
            int n = Mathf.Max(1, Mathf.Min(segments, damage));
            int[] parts = new int[n];

            int remaining = damage;
            for (int i = 0; i < n - 1; i++)
            {
                // 每段至少 1，并为后续每段各预留至少 1
                int maxTake = remaining - (n - 1 - i);
                int take = Random.Range(1, maxTake + 1);
                parts[i] = take;
                remaining -= take;
            }
            parts[n - 1] = remaining;

            // 大数字在前，视觉上更有冲击力
            System.Array.Sort(parts);
            System.Array.Reverse(parts);
            return parts;
        }

        // ── 对象池 ──

        /// <summary>从池中取出一个实例；池空时动态扩容</summary>
        private DamageNumberUI GetFromPool()
        {
            if (_pool.Count > 0)
            {
                _activeCount++;
                return _pool.Dequeue();
            }

            // 动态扩容：创建新实例直接使用（不放入池，由 ReturnToPool 回收）
            if (_activeCount < _maxPoolSize)
            {
                _activeCount++;
                return CreateNewInstance();
            }

            // 池耗尽，强制创建一个（不加入池，用完即弃）
            Debug.LogWarning("[DamageNumberSpawner] 对象池耗尽，创建临时实例");
            return Instantiate(_damageNumberPrefab, transform);
        }

        /// <summary>回收实例到对象池</summary>
        private void ReturnToPool(DamageNumberUI instance)
        {
            if (instance == null) return;

            instance.transform.SetParent(transform);
            _pool.Enqueue(instance);
            _activeCount--;
        }

        /// <summary>创建一个新实例（不放入池，由调用方决定入池还是直接使用）</summary>
        private DamageNumberUI CreateNewInstance()
        {
            DamageNumberUI instance = Instantiate(_damageNumberPrefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }

        // ── 颜色查询 ──

        /// <summary>根据元素类型返回对应颜色</summary>
        public static Color GetColorByElement(ElementType element)
        {
            if (ElementColors.TryGetValue(element, out Color color))
                return color;
            return DefaultColor;
        }
    }
}
