using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 伤害数字生成器 — 订阅 OnDamageDealt 事件，在目标头顶生成浮动伤害数字
    ///
    /// 挂载在 BattleManager 所在的 GameObject 上。
    /// 通过对象池管理 DamageNumberUI 实例，支持 AOE 同时命中多目标。
    ///
    /// 颜色规则：按攻击方的 ElementType 区分元素色
    /// 暴击规则：isCritical = true 时数字放大 + 显示「暴击！」标签
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
            // 缓存摄像机引用（避免 Camera.main 的每帧查找开销）
            _cachedCamera = Camera.main;
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
        /// 收到伤害事件 → 生成伤害数字
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

            // 获取目标世界坐标
            if (!BattleManager.Instance.TryGetEntityTransform(target.heroID, out Transform targetTransform))
            {
                Debug.LogWarning($"[DamageNumberSpawner] 未找到目标 {target.heroName} (heroID={target.heroID}) 的 Transform");
                return;
            }

            Vector3 worldPos = targetTransform.position + Vector3.up * _heightOffset;
            Color color = GetColorByElement(source?.element ?? ElementType.Physical);

            DamageNumberUI instance = GetFromPool();

            // 生成前先面向摄像机 + 设置 Canvas 渲染摄像机
            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100; // 确保渲染在所有 3D 物体前面
                if (_cachedCamera != null)
                {
                    canvas.worldCamera = _cachedCamera;
                    // WorldSpace Canvas 正面为 -Z：令 forward 与摄像机同向，正面即朝向镜头（文字正常不镜像）
                    instance.transform.forward = _cachedCamera.transform.forward;
                }
                else
                {
                    Debug.LogWarning("[DamageNumberSpawner] _cachedCamera 为 null！Canvas 无法获得摄像机引用");
                }
            }

            // 检查 TMP 字体是否赋值
            TMP_Text tmpText = instance.GetComponentInChildren<TMP_Text>();
            if (tmpText != null && tmpText.font == null)
            {
                Debug.LogError("[DamageNumberSpawner] TMP_Text.font 为 null！请在预制体上给 DamageText 赋值 SDF 字体！");
            }

            instance.Show(damage, color, isCritical, worldPos, ReturnToPool);
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
