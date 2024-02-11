using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Timeline.TimelinePlaybackControls;

[RequireComponent(typeof(CharacterController), typeof(WeaponHolder))]
public class PlayerController : MonoBehaviour, IDamageable
{
    public int health = 10;
    public float movementSpeed = 1.0f;
    public float acceleration = 1.0f;
    public float decceleration = 1.0f;
    [Space()]
    public InputActionAsset inputActions;

    private CharacterController characterController;
    private WeaponHolder weaponHolder;

    private Camera cam;
    private Vector2 currentDir;
    private Vector2 inputDir;
    private bool usingWeapon;
    private Plane plane = new Plane(Vector3.up, 0);
    private int currentHealth;
    void Start()
    {
        currentHealth = health;
        cam = Camera.main;
        weaponHolder = GetComponent<WeaponHolder>();
        weaponHolder.AddWeapon("gun");

        characterController = GetComponent<CharacterController>();
        inputActions.FindAction("move").performed += OnMovePerformed;
        inputActions.FindAction("move").canceled += OnMoveCanceled;
        inputActions.FindAction("fire").performed += OnFirePerformed;
        inputActions.FindAction("fire").canceled += OnFireCanceled;
        inputActions.FindAction("aim").performed += OnAimPerformed;

        StartCoroutine(TryFiringWeapon());
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        Vector3 screenPos = context.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(screenPos);

        if (plane.Raycast(ray, out var distance))
        {
            var worldPos = ray.GetPoint(distance);
            var dir = worldPos - transform.position;
            weaponHolder.SetAimingDir(dir);
        }


    }

    private IEnumerator TryFiringWeapon()
    {
        while (true)
        {
            if (usingWeapon)
            {
                yield return weaponHolder.FireWeapon(0);
            }
            else
            {
                yield return null;
            }
        }


    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {

        usingWeapon = false;
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Firing!");
        usingWeapon = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        inputDir = context.ReadValue<Vector2>();

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
        currentDir = Vector2.Lerp(currentDir, inputDir, inputDir.magnitude < currentDir.magnitude ? decceleration : acceleration);
        var motion = new Vector3(currentDir.x * movementSpeed, 0, currentDir.y * movementSpeed);
        characterController.Move(motion * Time.deltaTime);
    }

    public void OnDamageTaken(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Debug.Log("You died!");
        }
    }
}
