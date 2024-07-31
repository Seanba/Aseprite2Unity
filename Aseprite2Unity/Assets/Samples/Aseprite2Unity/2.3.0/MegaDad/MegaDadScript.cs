using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Examples.MegaDad
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

        [Tooltip("BoxCollider2D components on this game object will be represent the terrain colliders")]
        public GameObject m_TerrainProvider;

        [Tooltip("BoxCollider2D components on this game object will be represent the ladder colliders")]
        public GameObject m_LaddersProvider;

        // Gravity/Acceleration is pixels-per-second-squared
        private const float Gravity_pps2 = 360.0f;

        // Velocity is in pixels-per-second
        private const float JumpBoost_pps = 196.0f;
        private const float ClimbingSpeed_pps = 64.0f;
        private const float HorizontalSpeed_pps = 82.0f;

        private Vector2 m_CurrentVelocity_pps;

        private Animator m_Animator;
        private SpriteRenderer m_SpriteRenderer;
        private AudioSource m_AudioSource;
        private BoxCollider2D m_BoxCollider2D;

        private PhysicalState m_PhysicalState;

        // Input state that we gather every frame
        private int m_InputX;
        private int m_InputY;
        private bool m_InputJump;

        private readonly List<Rect> m_TerrainRects = new List<Rect>();
        private readonly List<Rect> m_LadderRects = new List<Rect>();

        private Rect PlayerCollisionRect => new Rect(m_BoxCollider2D.bounds.min, m_BoxCollider2D.bounds.size);

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

            m_BoxCollider2D = GetComponent<BoxCollider2D>();
            Assert.IsNotNull(m_BoxCollider2D);

            // Get our terrain rectangles
            if (m_TerrainProvider != null)
            {
                foreach (var terrainBox in m_TerrainProvider.GetComponentsInChildren<BoxCollider2D>())
                {
                    Vector2 pos = terrainBox.bounds.min;
                    Vector2 size = terrainBox.bounds.size;
                    m_TerrainRects.Add(new Rect(pos, size));
                }
            }

            // Get our ladder rectangles
            if (m_LaddersProvider != null)
            {
                foreach (var ladderBox in m_LaddersProvider.GetComponentsInChildren<BoxCollider2D>())
                {
                    Vector2 pos = (Vector2)ladderBox.gameObject.transform.position + ladderBox.offset;
                    Vector2 size = ladderBox.size;
                    m_LadderRects.Add(new Rect(pos, size));
                }
            }

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

        private void OnDrawGizmosSelected()
        {
            foreach (var rect in m_TerrainRects)
            {
                DrawGizmoRect(rect, Color.red);
            }

            Gizmos.color = new Color(1.0f, 1.0f, 0, 0.25f);
            foreach (var rect in m_LadderRects)
            {
                DrawGizmoRect(rect, Color.white);
            }

            if (m_BoxCollider2D != null)
            {
                DrawGizmoRect(PlayerCollisionRect, Color.yellow);
            }
        }

        private void DrawGizmoRect(Rect rect, Color color)
        {
            Color alpha = new Color(color.a, color.g, color.b, color.a * 0.5f);
            Gizmos.color = alpha;
            Gizmos.DrawCube(rect.center, rect.size);

            Gizmos.color = color;
            var p0 = rect.position;
            var p1 = p0 + (Vector2.up * rect.size.y);
            var p2 = p1 + (Vector2.right * rect.size.x);
            var p3 = p2 + (Vector2.down * rect.size.y);
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);
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
            // fixit - plan for this state
            // can move side-to-side
            // can "fall"


            MoveHorizontally();

            // fixit - can move side to side without changing state
            // fixit - can jump

            // We can jump while on the ground
            if (m_InputJump)
            {
                ChangePhysicalState(PhysicalState.InAir);

                // We get a boost up when we jump
                m_CurrentVelocity_pps.y = JumpBoost_pps;
                return;
            }

            // fixit - can climb ladders (check m_InputY)
        }

        private void UpdateInAir()
        {
            // Our velocity changes each frame due to gravity while we're in the air
            m_CurrentVelocity_pps.y -= Gravity_pps2 * Time.deltaTime;

            // How much are we trying to fall this frame?
            float dy = m_CurrentVelocity_pps.y * Time.deltaTime;

            // fixit - if grounded
            //m_CurrentVelocity_pps.y = 0;
            //ChangePhysicalState(PhysicalState.OnGround);


            // fixit - we also move sideways
            MoveHorizontally();

            // fixit - we can also grab a ladder
        }

        private void UpdateOnLadder()
        {
            if (m_InputY != 0)
            {
                // Move up or down the ladder, keeping within the ground and top planes
                float dy = m_InputY * ClimbingSpeed_pps * Time.deltaTime;
                var pos = gameObject.transform.position;

                // fixit - move up/down
                // fixit - collide with above
                // fixit - if we touch ground (moving down) then we're on ground ChangePhysicalState(PhysicalState.OnGround);
            }

            // We can jump off the ladder
            if (m_InputJump)
            {
                m_CurrentVelocity_pps.y = 0;
                ChangePhysicalState(PhysicalState.InAir);
                return;
            }
        }

        private void MoveHorizontally()
        {
            if (m_InputX != 0)
            {
                // fixit - boxes
            }
        }

    }
}
