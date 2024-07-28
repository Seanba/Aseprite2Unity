using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Examples.MegaDad
{
    // Simple script that animates our MegaDad sprite
    public class MegaDadScript : MonoBehaviour
    {
        public AudioClip m_AudioLadder1;
        public AudioClip m_AudioLadder2;

        // Gravity/Acceleration is pixels-per-second-squared
        private const float Gravity_pps2 = 360.0f;
        private const float GroundPlane = 0.0f;
        private const float TopPlane = 56.0f;

        // Velocity is in pixels-per-second
        private const float JumpBoost_pps = 196.0f;
        private const float ClimbingSpeed_pps = 64.0f;

        private Vector2 m_CurrentVelocity_pps;

        private Animator m_Animator;
        private SpriteRenderer m_SpriteRenderer;
        private AudioSource m_AudioSource;

        public enum PhysicalState
        {
            Invalid,
            OnGround,
            InAir,
            OnLadder,
        }

        private PhysicalState m_PhysicalState;

        // Input state that we gather every frame
        private int m_InputX;
        private int m_InputY;
        private bool m_InputJump;

        // Animation event - called from animation clip
        public void DoClimb1()
        {
            if (m_AudioLadder1 != null)
            {
                m_AudioSource.PlayOneShot(m_AudioLadder1);
            }
        }

        // Animation event - called from animation clip
        public void DoClimb2()
        {
            if (m_AudioLadder2 != null)
            {
                m_AudioSource.PlayOneShot(m_AudioLadder2);
            }
        }

        private void Awake()
        {
            m_Animator = GetComponentInChildren<Animator>();
            Assert.IsNotNull(m_Animator);

            m_SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(m_SpriteRenderer);

            m_AudioSource = GetComponentInChildren<AudioSource>();
            Assert.IsNotNull(m_AudioSource);

            ChangePhysicalState(PhysicalState.OnGround);
        }

        private void Update()
        {
            // Convert x and y axis inputs from float to [-1, 0, +1]
            m_InputX = Mathf.FloorToInt(Input.GetAxisRaw("Horizontal"));
            m_InputY = Mathf.FloorToInt(Input.GetAxisRaw("Vertical"));
            m_InputJump = Input.GetButtonDown("Jump");

            // Flip our sprite if needed
            if (m_InputX < 0)
            {
                m_SpriteRenderer.flipX = true;
            }
            else if (m_InputX > 0)
            {
                m_SpriteRenderer.flipX = false;
            }

            // Update animator
            m_Animator.SetInteger("Velocity_x", m_InputX);
            m_Animator.SetInteger("Velocity_y", m_InputY);

            switch (m_PhysicalState)
            {
                case PhysicalState.OnGround:
                    UpdateOnGround();
                    break;

                case PhysicalState.InAir:
                    UpdateInAir();
                    break;

                case PhysicalState.OnLadder:
                    UpdateOnLadder();
                    break;
            }
        }

        private void ChangePhysicalState(PhysicalState state)
        {
            if (state == m_PhysicalState)
            {
                return;
            }

            m_PhysicalState = state;

            if (state == PhysicalState.OnGround)
            {
                m_Animator.SetTrigger("IsOnGround");
            }
            else if (state == PhysicalState.InAir)
            {
                m_Animator.SetTrigger("IsInAir");
            }
            else if (state == PhysicalState.OnLadder)
            {
                m_Animator.SetTrigger("IsOnLadder");
            }
            else
            {
                Debug.LogErrorFormat("Unhandled or invalid physical state: {0}", state);
            }
        }

        private void UpdateOnGround()
        {
            // We can jump while on the ground
            if (m_InputJump)
            {
                ChangePhysicalState(PhysicalState.InAir);

                // We get a boost up when we jump
                m_CurrentVelocity_pps.y = JumpBoost_pps;
                return;
            }

            if (m_InputY > 0)
            {
                // Climb onto a ladder
                ChangePhysicalState(PhysicalState.OnLadder);
            }
        }

        private void UpdateInAir()
        {
            // Our velocity changes each frame due to gravity while we're in the air
            m_CurrentVelocity_pps.y -= Gravity_pps2 * Time.deltaTime;

            // How much are we moving this frame?
            float dy = m_CurrentVelocity_pps.y * Time.deltaTime;
            var pos = gameObject.transform.position;

            // Are we going to land back on the "ground"
            if (pos.y + dy < GroundPlane)
            {
                gameObject.transform.position = new Vector3(pos.x, GroundPlane, pos.z);

                // We've hit the ground and we're done
                ChangePhysicalState(PhysicalState.OnGround);
            }
            else
            {
                gameObject.transform.Translate(0, dy, 0);
            }
        }

        private void UpdateOnLadder()
        {
            if (m_InputY != 0)
            {
                // Move up or down the ladder, keeping within the ground and top planes
                float dy = m_InputY * ClimbingSpeed_pps * Time.deltaTime;
                var pos = gameObject.transform.position;

                if (pos.y + dy > TopPlane)
                {
                    gameObject.transform.position = new Vector3(pos.x, TopPlane, pos.z);
                }
                else if (pos.y + dy < GroundPlane)
                {
                    gameObject.transform.position = new Vector3(pos.x, GroundPlane, pos.z);

                    // We're on the ground and need to transition to another physical state
                    ChangePhysicalState(PhysicalState.OnGround);
                    return;
                }
                else
                {
                    gameObject.transform.Translate(0, dy, 0);
                }
            }

            // We can jump off the ladder
            if (m_InputJump)
            {
                m_CurrentVelocity_pps.y = 0;
                ChangePhysicalState(PhysicalState.InAir);
                return;
            }
        }
    }
}
