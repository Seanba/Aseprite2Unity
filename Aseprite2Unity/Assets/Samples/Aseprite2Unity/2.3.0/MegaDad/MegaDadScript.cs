using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Samples.MegaDad
{
    // Simple script that animates our MegaDad sprite
    public class MegaDadScript : MonoBehaviour
    {
        private enum PhysicalState
        {
            Invalid,
            OnGround,
            InAir,
            OnLadder,
        }

        public AudioClip m_AudioLadder1;
        public AudioClip m_AudioLadder2;

        // Gravity/Acceleration is pixels-per-second-squared
        private const float Gravity_pps2 = 360.0f;

        // Velocity is in pixels-per-second
        private const float JumpBoost_pps = 196.0f;
        private const float ClimbingSpeed_pps = 64.0f;
        private const float HorizontalSpeed_pps = 82.0f;

        // Because of gravity our vertical speed changes over time
        private float m_CurrentVerticalSpeed;

        private Animator m_Animator;
        private SpriteRenderer m_SpriteRenderer;
        private AudioSource m_AudioSource;
        private BoxPhysics m_PlayerBoxPhysics;

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

        private void Start()
        {
            m_Animator = GetComponent<Animator>();
            Assert.IsNotNull(m_Animator);

            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            Assert.IsNotNull(m_SpriteRenderer);

            m_AudioSource = GetComponent<AudioSource>();
            Assert.IsNotNull(m_AudioSource);

            m_PlayerBoxPhysics = GetComponent<BoxPhysics>();
            Assert.IsNotNull(m_PlayerBoxPhysics);

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
                m_CurrentVerticalSpeed = 0;
                m_Animator.SetTrigger("IsOnGround");
            }
            else if (state == PhysicalState.InAir)
            {
                m_Animator.SetTrigger("IsInAir");
            }
            else if (state == PhysicalState.OnLadder)
            {
                m_CurrentVerticalSpeed = 0;
                m_Animator.SetTrigger("IsOnLadder");
            }
            else
            {
                Debug.LogErrorFormat("Unhandled or invalid physical state: {0}", state);
            }
        }

        private void UpdateOnGround()
        {
            // Is there ground ground still beneath us? If we can move a little bit down then the answer is no.
            if (m_PlayerBoxPhysics.CanMove(0, -0.25f))
            {
                // Start falling
                m_CurrentVerticalSpeed = 0;
                ChangePhysicalState(PhysicalState.InAir);
                return;
            }

            // Are we moving left/right?
            MoveHorizontally();

            // We can jump while on the ground
            if (m_InputJump)
            {
                ChangePhysicalState(PhysicalState.InAir);

                // We get a boost up when we jump
                m_CurrentVerticalSpeed = JumpBoost_pps;
                return;
            }

            if (CanClimbLadder())
            {
                ChangePhysicalState(PhysicalState.OnLadder);
                return;
            }
        }

        private void UpdateInAir()
        {
            // Our velocity changes each frame due to gravity while we're in the air
            m_CurrentVerticalSpeed -= Gravity_pps2 * Time.deltaTime;

            // How much are we trying to fall this frame?
            float dy = m_CurrentVerticalSpeed * Time.deltaTime;

            if (!m_PlayerBoxPhysics.AttemptMove(0, dy))
            {
                // We hit something either moving up (bumping head, so start falling) or moving down (touching ground)
                m_CurrentVerticalSpeed = 0;

                if (dy < 0)
                {
                    // We couldn't move down the whole distance so we must be touching ground
                    ChangePhysicalState(PhysicalState.OnGround);
                    return;
                }
            }

            MoveHorizontally();

            if (CanClimbLadder())
            {
                ChangePhysicalState(PhysicalState.OnLadder);
            }
        }

        private void UpdateOnLadder()
        {
            if (m_InputY != 0)
            {
                // Move up or down the ladder, keeping within the ground and top planes
                float dy = m_InputY * ClimbingSpeed_pps * Time.deltaTime;

                if (!m_PlayerBoxPhysics.AttemptMove(0, dy))
                {
                    // If we bumped our head that's okay
                    // But if we touched ground then change our state
                    if (dy < 0)
                    {
                        ChangePhysicalState(PhysicalState.OnGround);
                        return;
                    }
                }
            }

            // We can jump off the ladder
            if (m_InputJump)
            {
                m_CurrentVerticalSpeed = 0;
                ChangePhysicalState(PhysicalState.InAir);
                return;
            }
        }

        private void MoveHorizontally()
        {
            if (m_InputX != 0)
            {
                float dx = m_InputX * Time.deltaTime * HorizontalSpeed_pps;
                m_PlayerBoxPhysics.AttemptMove(dx, 0);
            }
        }

        private bool CanClimbLadder()
        {
            if (m_InputY > 0)
            {
                return m_PlayerBoxPhysics.AttemptSnapToLadder();
            }

            return false;
        }
    }
}
