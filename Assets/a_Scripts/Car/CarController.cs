using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("移动参数")]
    public float motorForce = 1000f;     // 引擎动力
    public float brakeForce = 3000f;     // 刹车力
    public float maxSteerAngle = 30f;    // 最大转向角度

    [Header("车轮设置")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("输入参数")]
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    [Header("输入配置")]
    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap drivingMap;
    private InputAction moveAction;
    private InputAction brakeAction;

    void Awake()
    {
        horizontalInput = 0;
        verticalInput = 0;
        isBraking = false;


        drivingMap = inputActions.FindActionMap("Driving");

        // 找到具体的动作
        moveAction = drivingMap.FindAction("Move");
        brakeAction = drivingMap.FindAction("Brake");

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        brakeAction.performed += OnBrakePerformed;
        brakeAction.canceled += OnBrakeCanceled;
    }

    void OnEnable()
    {
        // 启用输入
        drivingMap.Enable();
    }

    void OnDisable()
    {
        // 禁用输入
        drivingMap.Disable();
    }

    void OnDestroy()
    {
        // 取消注册，防止内存泄漏
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;
        brakeAction.performed -= OnBrakePerformed;
        brakeAction.canceled -= OnBrakeCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // 读取Vector2值（x:左右，y:前后）
        Vector2 input = context.ReadValue<Vector2>();
        horizontalInput = input.x;
        Debug.Log("Horizontal Input: " + horizontalInput);
        verticalInput = input.y;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        horizontalInput = 0f;
        verticalInput = 0f;
    }

    private void OnBrakePerformed(InputAction.CallbackContext context)
    {
        isBraking = true;
    }

    private void OnBrakeCanceled(InputAction.CallbackContext context)
    {
        isBraking = false;
    }




    void FixedUpdate()
    {
        // 处理车辆物理（FixedUpdate用于物理计算）
        HandleMotor();
        HandleSteering();
    }



    void HandleMotor()
    {
        // 给后轮施加动力
        rearLeftWheel.motorTorque = verticalInput * motorForce;
        rearRightWheel.motorTorque = verticalInput * motorForce;

        // 刹车处理
        if (isBraking)
        {
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
            frontLeftWheel.brakeTorque = brakeForce;
            frontRightWheel.brakeTorque = brakeForce;
        }
        else
        {
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
        }
    }

    void HandleSteering()
    {
        // 前轮转向
        float steerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;
    }
}