using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FollowBehaviour : MonoBehaviour
{
    public float followRadius = 10.0f;
    public float minFollowRadius = 2.0f;
    public float movementSpeed = 10.0f;

    private CharacterController characterController;
    private Vector3 movementDirection;

    public Vector3 MovementDirection { set { movementDirection = value; } }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    private void Update()
    {
        characterController.Move(movementDirection * movementSpeed * Time.deltaTime);
    }


    public void ResetPosition()
    {
        characterController.enabled = false;
        transform.localPosition = Vector3.zero;
        characterController.enabled = true;
    }
}
