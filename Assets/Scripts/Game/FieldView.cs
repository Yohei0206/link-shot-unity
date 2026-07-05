using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// フィールド要素（的・壁グリッド・発射円・バウンド板）の実行時生成（ARCHITECTURE.md 3.4章）。
    /// シーンへのデータ埋め込みはせず、すべてGameConfigの数値から都度組み立てる。
    /// </summary>
    public class FieldView : MonoBehaviour
    {
        public const float FieldWidth = 6f;
        public const float FieldHeight = 8f;

        // GAME_RULES.md 7章の帯構成（上端からの割合）。
        private const float TargetBandBottomFraction = 0.20f;
        private const float WallBandBottomFraction = 0.45f;
        private const float FreeBandBottomFraction = 0.70f;
        private const float LaunchBandBottomFraction = 0.90f;

        private static Sprite _sharedSquareSprite;

        private readonly List<GameObject> _wallObjects = new List<GameObject>();
        private readonly List<GameObject> _bounceBoardObjects = new List<GameObject>();
        private readonly Dictionary<int, GameObject> _launchMarkers = new Dictionary<int, GameObject>();

        public void BuildStaticField()
        {
            BuildTargets();
            BuildLaunchPositions();
            BuildOutOfFieldBoundary();
        }

        private static float FractionToWorldY(float fraction)
        {
            return FieldHeight / 2f - fraction * FieldHeight;
        }

        private void BuildTargets()
        {
            float cornerRadius = FieldWidth * GameConfig.CornerZoneRadiusRatio;
            float centerRadius = FieldWidth * GameConfig.CenterZoneRadiusRatio;
            float bandCenterY = FractionToWorldY(TargetBandBottomFraction / 2f);

            CreateTarget(TargetZoneId.TopLeftCorner, new Vector2(-FieldWidth * 0.32f, bandCenterY + 0.3f), cornerRadius);
            CreateTarget(TargetZoneId.TopRightCorner, new Vector2(FieldWidth * 0.32f, bandCenterY + 0.3f), cornerRadius);
            CreateTarget(TargetZoneId.Center, new Vector2(0f, bandCenterY - 0.3f), centerRadius);
        }

        private void CreateTarget(TargetZoneId zoneId, Vector2 position, float radius)
        {
            var go = new GameObject($"Target_{zoneId}");
            go.transform.SetParent(transform);
            go.transform.position = position;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;

            var marker = go.AddComponent<TargetZoneMarker>();
            marker.ZoneId = zoneId;
            marker.BaseRadius = radius;

            Color color = zoneId == TargetZoneId.Center ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.8f, 0.1f);
            AddVisual(go, radius * 2f, radius * 2f, color);
        }

        private void BuildLaunchPositions()
        {
            float radius = FieldWidth * GameConfig.LaunchCircleRadiusRatio;
            float y = FractionToWorldY((FreeBandBottomFraction + LaunchBandBottomFraction) / 2f);
            float slotWidth = FieldWidth / GameConfig.LaunchPositionCount;

            for (int i = 0; i < GameConfig.LaunchPositionCount; i++)
            {
                int position = i + 1;
                float x = -FieldWidth / 2f + slotWidth * (i + 0.5f);

                var go = new GameObject($"LaunchPosition_{position}");
                go.transform.SetParent(transform);
                go.transform.position = new Vector2(x, y);

                var collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = radius;

                var marker = go.AddComponent<LaunchPositionMarker>();
                marker.Position = position;
                marker.BaseRadius = radius;

                AddVisual(go, radius * 2f, radius * 2f, new Color(0.6f, 0.6f, 0.6f, 0.5f));
                _launchMarkers[position] = go;
            }
        }

        private void BuildOutOfFieldBoundary()
        {
            var go = new GameObject("OutOfFieldBoundary");
            go.transform.SetParent(transform);
            go.transform.position = Vector2.zero;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(FieldWidth * 1.1f, FieldHeight * 1.1f);

            go.AddComponent<OutOfFieldMarker>();
        }

        /// <summary>防御側の壁配置（常設壁＋使い捨て壁）を反映する。ショットごとに全入れ替えする。</summary>
        public void ApplyWalls(IReadOnlyList<WallPlacement> walls)
        {
            foreach (GameObject go in _wallObjects)
            {
                Destroy(go);
            }

            _wallObjects.Clear();

            float cellWidth = FieldWidth / GameConfig.WallGridColumns;
            float cellHeight = WallBandHeight() / GameConfig.WallGridRows;

            foreach (WallPlacement wall in walls)
            {
                Vector2 pos = WallCellToWorld(wall.CellIndex);

                var go = new GameObject($"Wall_{wall.CellIndex}");
                go.transform.SetParent(transform);
                go.transform.position = pos;

                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(cellWidth * 0.8f, cellHeight * 0.8f);
                collider.sharedMaterial = PhysicsMaterials.Bouncy;

                var marker = go.AddComponent<WallMarker>();
                marker.CellIndex = wall.CellIndex;
                marker.IsDefaultWall = wall.IsDefaultWall;

                Color color = wall.IsDefaultWall ? new Color(0.45f, 0.25f, 0.1f) : new Color(0.75f, 0.5f, 0.2f);
                AddVisual(go, cellWidth * 0.8f, cellHeight * 0.8f, color);

                _wallObjects.Add(go);
            }
        }

        /// <summary>攻撃側が設置したバウンド板を反映する（BOUNCE_BOARD効果）。</summary>
        public void ApplyBounceBoards(IReadOnlyList<BouncePlacement> boards)
        {
            foreach (GameObject go in _bounceBoardObjects)
            {
                Destroy(go);
            }

            _bounceBoardObjects.Clear();

            float size = FieldWidth / GameConfig.WallGridColumns * 0.8f;

            foreach (BouncePlacement board in boards)
            {
                Vector2 pos = NormalizedToWorld(board.Position);

                var go = new GameObject("BounceBoard");
                go.transform.SetParent(transform);
                go.transform.position = pos;

                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(size, size);
                collider.sharedMaterial = PhysicsMaterials.Bouncy;

                go.AddComponent<BounceBoardMarker>();
                AddVisual(go, size, size, new Color(0.2f, 0.9f, 0.5f));

                _bounceBoardObjects.Add(go);
            }
        }

        public Vector2 GetLaunchPositionWorld(int position)
        {
            return _launchMarkers[position].transform.position;
        }

        public float GetLaunchRadiusWorld(bool boosted)
        {
            float radius = FieldWidth * GameConfig.LaunchCircleRadiusRatio;
            return boosted ? radius * GameConfig.RangeBoostRadiusMultiplier : radius;
        }

        private static float WallBandHeight()
        {
            return FractionToWorldY(TargetBandBottomFraction) - FractionToWorldY(WallBandBottomFraction);
        }

        private static Vector2 WallCellToWorld(int cellIndex)
        {
            int columns = GameConfig.WallGridColumns;
            int row = cellIndex / columns;
            int col = cellIndex % columns;

            float cellWidth = FieldWidth / columns;
            float cellHeight = WallBandHeight() / GameConfig.WallGridRows;
            float bandTopY = FractionToWorldY(TargetBandBottomFraction);

            float x = -FieldWidth / 2f + cellWidth * (col + 0.5f);
            float y = bandTopY - cellHeight * (row + 0.5f);
            return new Vector2(x, y);
        }

        private static Vector2 NormalizedToWorld(Vec2 normalized)
        {
            float x = -FieldWidth / 2f + normalized.X * FieldWidth;
            float y = FractionToWorldY(normalized.Y);
            return new Vector2(x, y);
        }

        private static void AddVisual(GameObject go, float width, float height, Color color)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSharedSquareSprite();
            renderer.color = color;
            go.transform.localScale = new Vector3(width, height, 1f);
        }

        /// <summary>他のGame/層クラス（Ball生成等）からも使う共有の1x1白プレースホルダースプライト。</summary>
        public static Sprite GetSharedSquareSprite()
        {
            if (_sharedSquareSprite != null)
            {
                return _sharedSquareSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _sharedSquareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _sharedSquareSprite;
        }
    }
}
