namespace HyyderWorks.Footstepper.Demo
{
    using UnityEngine;

    public class AnimatedPlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float rotateSpeed = 100f;

        private CharacterController controller;
        private Animator animator;
        private float currentSpeed;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            float vertical = Input.GetAxis("Vertical"); // W/S keys
            float horizontal = Input.GetAxis("Horizontal"); // A/D keys

            // Rotate player with A/D
            transform.Rotate(0, horizontal * rotateSpeed * Time.deltaTime, 0);

            // Move forward/backward with W/S
            Vector3 movement = transform.forward * (vertical * moveSpeed);

            // Apply gravity
            movement.y = -9.81f;

            // Move the character
            controller.Move(movement * Time.deltaTime);

            // Set animation speed based on movement
            currentSpeed = Mathf.Abs(vertical);
            animator.SetFloat("Speed", currentSpeed);
        }
    }
}