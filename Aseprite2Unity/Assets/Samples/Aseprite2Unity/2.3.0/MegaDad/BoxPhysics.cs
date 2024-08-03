using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Samples.MegaDad
{
    // A quick-and-dirty collision system that uses boxes/rectangles for collision detection
    // The built-in physics with Unity doesn't work well (IMHO) with NES-style games
    public class BoxPhysics : MonoBehaviour
    {
        [Tooltip("BoxCollider2D components on this game object will be represent the terrain colliders")]
        public GameObject m_TerrainProvider;

        [Tooltip("BoxCollider2D components on this game object will be represent the ladder colliders")]
        public GameObject m_LaddersProvider;

        private BoxCollider2D m_PlayerBoxCollider2D;

        private readonly List<Rect> m_TerrainRects = new List<Rect>();
        private readonly List<Rect> m_LadderRects = new List<Rect>();

        private Rect PlayerCollisionRect => new Rect(m_PlayerBoxCollider2D.bounds.min, m_PlayerBoxCollider2D.bounds.size);

        private void Start()
        {
            m_PlayerBoxCollider2D = GetComponent<BoxCollider2D>();
            Assert.IsNotNull(m_PlayerBoxCollider2D, "BoxPhysics requires a BoxCollider2D on this game object");

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
                    Vector2 pos = ladderBox.bounds.min;
                    Vector2 size = ladderBox.bounds.size;
                    m_LadderRects.Add(new Rect(pos, size));
                }
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

            if (m_PlayerBoxCollider2D != null)
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

    }
}
