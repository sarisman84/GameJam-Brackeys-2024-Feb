using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEditor.Timeline.TimelinePlaybackControls;

[RequireComponent(typeof(CharacterController), typeof(WeaponHolder), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 1.0f;
    public float acceleration = 1.0f;
    public float decceleration = 1.0f;
    [Space()]
    public InputActionAsset inputActions;

    private CharacterController characterController;
    private WeaponHolder weaponHolder;

    private Camera cam;
    private CinemachineVirtualCamera liveCam;
    private Vector2 currentDir;
    private Vector2 inputDir;
    private bool usingWeapon;
    private Plane plane = new Plane(Vector3.up, 0);
    private Health healthComponent;

    private UIManager.UIView lastKnownView;



    void Start()
    {
        GameplayManager.RegisterPlayer(this);
        cam = Camera.main;
        liveCam = cam.GetLiveCamera();


        Health.onDeathEvent += PlayerDeath;
        WeaponHolder.AddWeapon("gun");

        characterController = GetComponent<CharacterController>();
        inputActions.FindAction("move").performed += OnMovePerformed;
        inputActions.FindAction("move").canceled += OnMoveCanceled;
        inputActions.FindAction("fire").performed += OnFirePerformed;
        inputActions.FindAction("fire").canceled += OnFireCanceled;
        inputActions.FindAction("aim").performed += OnAimPerformed;
        inputActions.FindAction("weapon_select").performed += OnWeaponSelectPerformed;
        inputActions.FindAction("weapon_select").canceled += OnWeaponSelectCanceled;
    }

    private void OnWeaponSelectCanceled(InputAction.CallbackContext context)
    {
        lastKnownView = UIManager.CurrentView;
        UIManager.SetCurrentViewTo(UIManager.UIView.WeaponSelect);
        var weaponSelectView = UIManager.GetView<WeaponSelectView>(UIManager.UIView.WeaponSelect);
        weaponSelectView.PopulateWeaponWheel(WeaponHolder.GetWeaponInventory());
    }

    private void OnWeaponSelectPerformed(InputAction.CallbackContext context)
    {
        UIManager.SetCurrentViewTo(lastKnownView);
    }

    private void PlayerDeath(GameObject self)
    {
        SetActive(false);
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        Vector3 screenPos = context.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(screenPos);

        if (plane.Raycast(ray, out var distance))
        {
            var worldPos = ray.GetPoint(distance);
            var dir = worldPos - transform.position;
            WeaponHolder.SetAimingDir(dir);
        }


    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        usingWeapon = false;
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        usingWeapon = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        var dir = context.ReadValue<Vector2>();
        inputDir = dir;

    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        inputDir = Vector2.zero;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (GameplayManager.IsPaused)
        {
            return;
        }

        currentDir = Vector2.Lerp(currentDir, inputDir, inputDir.magnitude < currentDir.magnitude ? decceleration : acceleration);
        var motion = new Vector3(currentDir.x * movementSpeed, 0, currentDir.y * movementSpeed);
        var forwardDir = liveCam.transform.up;
        forwardDir.y = 0;
        var rightDir = liveCam.transform.right;
        rightDir.y = 0;

        var cameraAlignedMotion = forwardDir * motion.z + rightDir * motion.x;
        characterController.Move(cameraAlignedMotion * Time.deltaTime);

        if (usingWeapon)
        {
            weaponHolder.FireWeapon();
        }
    }

    public void SetActive(bool newState)
    {
        gameObject.SetActive(newState);
    }

    public void SetPosition(Vector3 position)
    {
        if (!characterController)
            characterController = GetComponent<CharacterController>();

        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }

    public Health Health
    {
        get
        {
            if (!healthComponent)
                healthComponent = gameObject.GetComponent<Health>();
            return healthComponent;
        }
    }

    public WeaponHolder WeaponHolder
    {
        get
        {
            if (!weaponHolder)
                weaponHolder = GetComponent<WeaponHolder>();
            return weaponHolder;
        }
    }
}
