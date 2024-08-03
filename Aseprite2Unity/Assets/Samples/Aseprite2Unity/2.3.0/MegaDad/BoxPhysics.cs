using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Samples.MegaDad
{
    // A quick-and-dirty collision system that uses boxes/rectangles for collision detection
    // The built-in physics with Unity doesn't work well (IMHO) with NES-style games
    public class BoxPhysics : MonoBehaviour
    {
        // We have to avoid in the smallest of overlaps when resting one object next to another
        private const float CollisionBias = 1.0f / 256.0f;

        [Tooltip("BoxCollider2D components on this game object will be represent the terrain colliders")]
        public GameObject m_TerrainProvider;

        [Tooltip("BoxCollider2D components on this game object will be represent the ladder colliders")]
        public GameObject m_LaddersProvider;

        private BoxCollider2D m_PlayerBoxCollider2D;

        private readonly List<Rect> m_TerrainRects = new List<Rect>();
        private readonly List<Rect> m_LadderRects = new List<Rect>();

        private Rect PlayerCollisionBox => new Rect(m_PlayerBoxCollider2D.bounds.min, m_PlayerBoxCollider2D.bounds.size);

        public bool CanMove(float dx, float dy)
        {
            // Test that moving in the dx and dy directions will not make us collide with terrain
            var playerBox = PlayerCollisionBox;
            
            if (dx > 0)
            {
                playerBox.xMax += dx;
            }
            else if (dx < 0)
            {
                playerBox.xMin += dx;
            }

            if (dy > 0)
            {
                playerBox.yMax += dy;
            }
            else if (dy < 0)
            {
                playerBox.yMin += dy;
            }

            foreach (var terrainBox in m_TerrainRects)
            {
                if (terrainBox.Overlaps(playerBox))
                {
                    return false;
                }
            }

            // We won't collide with terrain
            return true;
        }

        // Returns false if we couldn't move the whole distance given
        public bool AttemptMove(float dx, float dy)
        {
            bool moved = true;

            if (dx < 0)
            {
                moved = moved && MoveLeft(dx);
            }
            else if (dx > 0)
            {
                moved = moved && MoveRight(dx);
            }

            if (dy < 0)
            {
                moved = moved && MoveDown(dy);
            }
            else if (dy > 0)
            {
                moved = moved && MoveUp(dy);
            }

            return moved;
        }

        // Returns true if our position changed to the center of a nearby ladder
        public bool AttemptSnapToLadder()
        {
            Vector2 ourPosition = PlayerCollisionBox.center;

            foreach (var ladderBox in m_LadderRects)
            {
                if (ladderBox.Contains(ourPosition))
                {
                    Vector2 translate;
                    translate.x = ladderBox.center.x - ourPosition.x;
                    translate.y = 0;
                    TranslatePlayer(translate);
                    return true;
                }
            }

            return false;
        }

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
            foreach (var box in m_TerrainRects)
            {
                DrawGizmoRect(box, Color.red);
            }

            Gizmos.color = new Color(1.0f, 1.0f, 0, 0.25f);
            foreach (var box in m_LadderRects)
            {
                DrawGizmoRect(box, Color.white);
            }

            if (m_PlayerBoxCollider2D != null)
            {
                DrawGizmoRect(PlayerCollisionBox, Color.yellow);
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

        // Given a colliion rectangle we want to reposition the player
        private void RepositionPlayer(Rect newPlayerBox)
        {
            Rect oldPlayerBox = PlayerCollisionBox;

            // Moving the player around should never change their collision width or height
            Assert.IsTrue(Mathf.Approximately(oldPlayerBox.width, newPlayerBox.width));
            Assert.IsTrue(Mathf.Approximately(oldPlayerBox.height, newPlayerBox.height));

            TranslatePlayer(newPlayerBox.position - oldPlayerBox.position);
        }

        private void TranslatePlayer(Vector2 dv)
        {
            m_PlayerBoxCollider2D.gameObject.transform.Translate(dv);

            // We need to sync the collider bounds every time we move the player since we are basing collision testing off of collider bounds
            Physics2D.SyncTransforms();
        }

        private bool MoveRight(float dx)
        {
            Assert.IsTrue(dx > 0);

            bool fullyMoved = true;

            var oldPlayerRect = PlayerCollisionBox;
            var playerRect = oldPlayerRect;
            var lockedWidth = playerRect.width;

            // Do we overlap with terrain rectangles as we move to the right?
            // And if so what xMax position do we need to be to stay just to the left?
            playerRect.xMax += dx;

            foreach (var terrainBox in m_TerrainRects)
            {
                if (terrainBox.Overlaps(playerRect))
                {
                    playerRect.xMax = terrainBox.xMin - CollisionBias;
                    playerRect.xMin = playerRect.xMax - lockedWidth;
                    fullyMoved = false;
                }
            }

            if (playerRect != oldPlayerRect)
            {
                // Make sure our width remains the same even though we changed our xMax
                playerRect.xMin = playerRect.xMax - lockedWidth;
                RepositionPlayer(playerRect);
            }

            return fullyMoved;
        }

        private bool MoveLeft(float dx)
        {
            Assert.IsTrue(dx < 0);

            bool fullyMoved = true;

            var oldPlayerRect = PlayerCollisionBox;
            var playerRect = oldPlayerRect;
            var lockedWidth = playerRect.width;

            // Do we overlap with terrain rectangles as we move to the left?
            // And if so what xMin position do we need to be to stay just to the right?
            playerRect.xMin += dx;

            foreach (var terrainBox in m_TerrainRects)
            {
                if (terrainBox.Overlaps(playerRect))
                {
                    playerRect.xMin = terrainBox.xMax + CollisionBias;
                    playerRect.xMax = playerRect.xMin + lockedWidth;
                    fullyMoved = false;
                }
            }

            if (playerRect != oldPlayerRect)
            {
                // Make sure our width remains the same even though we changed our xMin
                playerRect.xMax = playerRect.xMin + lockedWidth;
                RepositionPlayer(playerRect);
            }

            return fullyMoved;
        }

        private bool MoveUp(float dy)
        {
            Assert.IsTrue(dy > 0);

            bool fullyMoved = true;

            var oldPlayerRect = PlayerCollisionBox;
            var playerRect = oldPlayerRect;
            var lockedHeight = playerRect.height;

            // Do we overlap with terrain rectangles as we move up?
            // And if so what yMax position do we need to remain just below?
            playerRect.yMax += dy;

            foreach (var terrainBox in m_TerrainRects)
            {
                if (terrainBox.Overlaps(playerRect))
                {
                    playerRect.yMax = terrainBox.yMin - CollisionBias;
                    playerRect.yMin = playerRect.yMax - lockedHeight;
                    fullyMoved = false;
                }
            }

            if (playerRect != oldPlayerRect)
            {
                // Make sure our height remains the same even though we changed our yMax
                playerRect.yMin = playerRect.yMax - lockedHeight;
                RepositionPlayer(playerRect);
            }

            return fullyMoved;
        }

        private bool MoveDown(float dy)
        {
            Assert.IsTrue(dy < 0);

            bool fullyMoved = true;

            var oldPlayerRect = PlayerCollisionBox;
            var playerRect = oldPlayerRect;
            var lockedHeight = playerRect.height;

            // Do we overlap with terrain rectangles as we move down?
            // And if so what yMin position do we need to be on top?
            playerRect.yMin += dy;

            foreach (var terrainBox in m_TerrainRects)
            {
                if (terrainBox.Overlaps(playerRect))
                {
                    playerRect.yMin = terrainBox.yMax + CollisionBias;
                    playerRect.yMax = playerRect.yMin + lockedHeight;
                    fullyMoved = false;
                }
            }

            if (playerRect != oldPlayerRect)
            {
                // Make sure our height remains the same even though we changed our yMin
                playerRect.yMax = playerRect.yMin + lockedHeight;
                RepositionPlayer(playerRect);
            }

            return fullyMoved;
        }
    }
}
