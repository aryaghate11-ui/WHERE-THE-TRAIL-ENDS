namespace HyyderWorks.Footstepper.Demo
{
    using UnityEngine;

    public class PlayerController : MonoBehaviour
    {
        public float speed = 5f;

        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(x, 0, z).normalized;
            rb.linearVelocity = new Vector3(movement.x * speed, rb.linearVelocity.y, movement.z * speed);
        }
    }
}