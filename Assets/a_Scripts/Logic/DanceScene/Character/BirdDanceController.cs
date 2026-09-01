using UnityEngine;

/// <summary>
/// 知更鸟舞蹈控制器（Dance 场景专用）。
/// 按 A 左、按 D 右，后按的键优先（按住 A 再按 D 切到右，反之亦然），全部松开回到待机。
/// 动画切换由 Bird.controller 的状态机过渡驱动（isLeft / isRight 两个 bool 参数）。
/// 本控制器只负责写入参数，不处理 WASD 移动 —— 鸟模型无移动脚本，Dance 场景不加载玩家角色。
/// </summary>
[RequireComponent(typeof(Animator))]
public class BirdDanceController : MonoBehaviour
{
    /// <summary>舞蹈朝向：待机 / 左 / 右</summary>
    private enum DanceDirection
    {
        Idle,
        Left,
        Right,
    }

    // 动画参数 hash（与 Bird.controller 的 Parameters 中 isLeft / isRight 一致）
    private static readonly int IsLeftHash = Animator.StringToHash("isLeft");
    private static readonly int IsRightHash = Animator.StringToHash("isRight");

    private Animator _animator;
    private DanceDirection _direction = DanceDirection.Idle;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_animator == null) return;

        UpdateDirection();
        _animator.SetBool(IsLeftHash, _direction == DanceDirection.Left);
        _animator.SetBool(IsRightHash, _direction == DanceDirection.Right);
    }

    /// <summary>按「后按优先」解析当前朝向：新按下的键覆盖，当前键松开则回退到另一键或待机。</summary>
    private void UpdateDirection()
    {
        bool leftHeld = Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.D);

        // 新按下的键覆盖当前方向（后按优先，左右可直接互切）
        if (Input.GetKeyDown(KeyCode.A))
            _direction = DanceDirection.Left;
        else if (Input.GetKeyDown(KeyCode.D))
            _direction = DanceDirection.Right;

        // 当前方向的键已松开 → 回退到仍按住的另一键，否则回待机
        if (_direction == DanceDirection.Left && !leftHeld)
            _direction = rightHeld ? DanceDirection.Right : DanceDirection.Idle;
        else if (_direction == DanceDirection.Right && !rightHeld)
            _direction = leftHeld ? DanceDirection.Left : DanceDirection.Idle;

        // 兜底：Idle 下若已有键按住（如启用时键已按下），直接进入对应方向
        if (_direction == DanceDirection.Idle)
            _direction = leftHeld ? DanceDirection.Left
                       : rightHeld ? DanceDirection.Right
                       : DanceDirection.Idle;
    }
}
