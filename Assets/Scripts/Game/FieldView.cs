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

        // 的帯・壁帯だけは発射エリアより横幅を広く使う（見た目の帯構成、GAME_RULES.md 7章とは別の演出調整）。
        public const float WideBandWidth = 9f;

        // 壁配置エリアは的帯よりさらに広く、画面端（16:9基準）まで届かせる。
        public const float WallBandWidth = FieldHeight * 16f / 9f;

        // 壁1枚の見た目サイズ（壁グリッド1セルに対する比率）。UI/WallPlacementPanelのプレビューもこれを参照し、
        // 隣接セルに置いたときに隙間ができる見た目に合わせる。
        public const float WallVisualWidthRatio = 0.5f;
        public const float WallVisualHeightRatio = 0.28f;

        // GAME_RULES.md 7章の帯構成（上端からの割合）。
        private const float TargetBandBottomFraction = 0.20f;
        private const float WallBandBottomFraction = 0.45f;
        private const float FreeBandBottomFraction = 0.70f;
        private const float LaunchBandBottomFraction = 0.90f;

        private static Sprite _sharedSquareSprite;

        private const string PhysicsSpritePath = "Field/Kenney/Physics/";
        private const string LaunchRingSpritePath = "UI/Kenney/PNG/Blue/Default/icon_outline_circle";
        private const string WallSlotSpritePath = "UI/Kenney/PNG/Blue/Default/button_square_line";
        private const string TinyFarmSpritePath = "Field/Kenney/TinyFarm/";

        // 農園演出は的・壁・発射円より必ず後ろに描画する（当たり判定には関与しない見た目のみの装飾）。
        private const int BackgroundSortingOrder = -20;
        private const int DecorationSortingOrder = -15;

        private static readonly Color InactiveLaunchColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color ActiveLaunchColor = new Color(1f, 0.65f, 0.05f, 0.95f);

        private readonly List<GameObject> _wallObjects = new List<GameObject>();
        private readonly List<GameObject> _bounceBoardObjects = new List<GameObject>();
        private readonly Dictionary<int, GameObject> _launchMarkers = new Dictionary<int, GameObject>();

        public void BuildStaticField()
        {
            BuildFarmBackground();
            BuildTargets();
            BuildWallAreaBackground();
            BuildLaunchPositions();
            BuildOutOfFieldBoundary();
        }

        // Kenney Tiny Farmの芝生タイル(tile_0105)の地色をそのまま単色地面として使う。
        // タイル自体を1ワールド単位まで拡大して並べると、タイル内の縁取りの1〜2pxが目立つ太い線として
        // 拡大されてしまう(16pxタイルはタイルマップの密な並びで見る前提のため)ので、地色の単色塗りのみにする。
        private static readonly Color FarmGrassGroundColor = new Color(78f / 255f, 151f / 255f, 76f / 255f);

        // Kenney Tiny Farmの畝タイル(tile_0049/0050)の地色。壁配置エリアだけ地面を土にして、
        // 芝エリア（的・発射円まわり）と一目で区別できるようにする。
        private static readonly Color FarmDirtGroundColor = new Color(184f / 255f, 101f / 255f, 66f / 255f);

        /// <summary>
        /// Kenney "Tiny Farm"（CC0）を使った背景演出。地色を敷き詰めたうえで、
        /// 帯の外側（壁・的・発射円の当たり判定には使われないx範囲）に木・茂み・岩を並べる。
        /// 見た目のみで当たり判定は持たせない。
        /// </summary>
        private void BuildFarmBackground()
        {
            var backgroundGo = new GameObject("FarmGrassBackground");
            backgroundGo.transform.SetParent(transform);
            backgroundGo.transform.position = Vector2.zero;

            AddVisual(backgroundGo, WideBandWidth * 2.2f, FieldHeight * 1.4f, FarmGrassGroundColor).sortingOrder = BackgroundSortingOrder;

            BuildFarmDecorations();
        }

        // 同じX座標に木を縦一列に並べると、幹やシルエットの輪郭が繋がって1本の柱のように見えてしまうため、
        // 各アイテムのXを少しずつジグザグにずらして自然な散らばりにする。
        private void BuildFarmDecorations()
        {
            // 壁配置エリア（ダート、WallBandWidth）が的帯より広いため、木・茂み・岩はその外側（芝の上）に置く。
            float marginX = WallBandWidth / 2f + 0.3f;

            CreateDecoration("tile_0078", new Vector2(-marginX - 0.1f, 2.6f), 0.7f);
            CreateDecoration("tile_0089", new Vector2(-marginX + 0.4f, 0.3f), 0.6f);
            CreateDecoration("tile_0078", new Vector2(-marginX - 0.2f, -1.8f), 0.7f);

            CreateDecoration("tile_0083", new Vector2(marginX + 0.1f, 2.6f), 0.7f);
            CreateDecoration("tile_0089", new Vector2(marginX - 0.4f, 0.3f), 0.6f);
            CreateDecoration("tile_0083", new Vector2(marginX + 0.2f, -1.8f), 0.7f);
        }

        private void CreateDecoration(string tileName, Vector2 position, float size)
        {
            var go = new GameObject($"FarmDecoration_{tileName}");
            go.transform.SetParent(transform);
            go.transform.position = position;

            Sprite sprite = Resources.Load<Sprite>(TinyFarmSpritePath + tileName);
            AddVisual(go, size, size, Color.white, sprite).sortingOrder = DecorationSortingOrder;
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

            CreateTarget(TargetZoneId.TopLeftCorner, new Vector2(-WideBandWidth * 0.32f, bandCenterY + 0.3f), cornerRadius, Color.white);
            CreateTarget(TargetZoneId.TopRightCorner, new Vector2(WideBandWidth * 0.32f, bandCenterY + 0.3f), cornerRadius, Color.white);
            CreateTarget(TargetZoneId.Center, new Vector2(0f, bandCenterY - 0.3f), centerRadius, Color.white);

            BuildBonusTargets();
        }

        // 見た目のにぎやかし・得点バリエーション用に追加した10個の小さい的の相対座標（【暫定】得点はGameConfig.BonusZoneScore）。
        // グリッド状に並べると角の的・中央の的と重なる、または詰まって見えるため、
        // 他の的およびボーナス的同士が重ならないよう間隔を空けつつ手動でばらけさせた配置にする。
        private static readonly Vector2[] BonusOffsets =
        {
            new Vector2(-4.0f, 0.40f),
            new Vector2(-3.6f, -0.50f),
            new Vector2(-2.0f, 0.55f),
            new Vector2(-1.6f, -0.55f),
            new Vector2(-0.8f, 0.10f),
            new Vector2(0.9f, 0.15f),
            new Vector2(1.7f, -0.58f),
            new Vector2(2.1f, 0.58f),
            new Vector2(3.7f, -0.45f),
            new Vector2(4.1f, 0.35f),
        };

        /// <summary>
        /// 見た目のにぎやかし・得点バリエーション用に追加した10個の小さい的（【暫定】得点はGameConfig.BonusZoneScore）。
        /// 的帯（GAME_RULES.md 7章）の中で、角・中央の的と重ならないようまばらに配置する。
        /// </summary>
        private void BuildBonusTargets()
        {
            float radius = FieldWidth * GameConfig.BonusZoneRadiusRatio;
            float baseY = FractionToWorldY(TargetBandBottomFraction / 2f);

            foreach (Vector2 offset in BonusOffsets)
            {
                CreateTarget(TargetZoneId.Bonus, new Vector2(offset.x, baseY + offset.y), radius, new Color(1f, 0.55f, 0.15f));
            }
        }

        private void CreateTarget(TargetZoneId zoneId, Vector2 position, float radius, Color tint)
        {
            var go = new GameObject($"Target_{zoneId}_{position.x:0.00}_{position.y:0.00}");
            go.transform.SetParent(transform);
            go.transform.position = position;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;

            var marker = go.AddComponent<TargetZoneMarker>();
            marker.ZoneId = zoneId;
            marker.BaseRadius = radius;

            Sprite sprite = zoneId switch
            {
                TargetZoneId.Center => Resources.Load<Sprite>(PhysicsSpritePath + "coinSilver"),
                TargetZoneId.Bonus => Resources.Load<Sprite>(PhysicsSpritePath + "coinSilver"),
                _ => Resources.Load<Sprite>(PhysicsSpritePath + "starGold"),
            };
            AddVisual(go, radius * 2.4f, radius * 2.4f, tint, sprite);
        }

        /// <summary>
        /// 壁配置エリア（GAME_RULES.md 7章の帯）の地面を土にして、芝エリアと一目で区別できるようにする。
        /// そのうえで、実際に壁が置かれる範囲（1枚分の幅・高さ）だけを枠線で示す。セル全体を塗るのではなく
        /// 壁の実寸に合わせることで、選択中の当たり判定エリア全体を覆う網掛け（廃止済み）とは違う、
        /// 「ここに壁が置かれる」という最小限の目印にする。
        /// </summary>
        private void BuildWallAreaBackground()
        {
            var groundGo = new GameObject("WallAreaGround");
            groundGo.transform.SetParent(transform);
            groundGo.transform.position = new Vector2(0f, (FractionToWorldY(TargetBandBottomFraction) + FractionToWorldY(WallBandBottomFraction)) / 2f);

            AddVisual(groundGo, WallBandWidth, WallBandHeight(), FarmDirtGroundColor).sortingOrder = BackgroundSortingOrder + 1;

            Vector2 cellSize = GetWallCellSize();
            float slotWidth = cellSize.x * WallVisualWidthRatio;
            float slotHeight = cellSize.y * WallVisualHeightRatio;
            Sprite slotSprite = Resources.Load<Sprite>(WallSlotSpritePath);

            for (int i = 0; i < GameConfig.WallGridCellCount; i++)
            {
                Vector2 center = GetWallCellCenter(i);

                var go = new GameObject($"WallSlot_{i}");
                go.transform.SetParent(transform);
                go.transform.position = center;

                AddVisual(go, slotWidth, slotHeight, new Color(1f, 1f, 1f, 0.4f), slotSprite).sortingOrder = BackgroundSortingOrder + 2;
            }
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

                // リング見た目を当たり判定より小さめにして、隣同士の間隔が空いて見えるようにする。
                Sprite ringSprite = Resources.Load<Sprite>(LaunchRingSpritePath);
                AddVisual(go, radius * 1.6f, radius * 1.6f, InactiveLaunchColor, ringSprite);
                _launchMarkers[position] = go;
            }
        }

        /// <summary>
        /// サイコロで決まった発射ポジションだけを目立つ色にする（GAME_RULES.md 6章）。
        /// どこから撃てばいいか一目でわかるように、他の5つは薄い表示のままにする。
        /// </summary>
        public void HighlightLaunchPosition(int? activePosition)
        {
            foreach (KeyValuePair<int, GameObject> entry in _launchMarkers)
            {
                bool isActive = activePosition.HasValue && entry.Key == activePosition.Value;
                entry.Value.GetComponentInChildren<SpriteRenderer>().color = isActive ? ActiveLaunchColor : InactiveLaunchColor;
            }
        }

        private void BuildOutOfFieldBoundary()
        {
            var go = new GameObject("OutOfFieldBoundary");
            go.transform.SetParent(transform);
            go.transform.position = Vector2.zero;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            float boundaryWidth = Mathf.Max(FieldWidth, Mathf.Max(WideBandWidth, WallBandWidth));
            collider.size = new Vector2(boundaryWidth * 1.1f, FieldHeight * 1.1f);

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

            float cellWidth = WallBandWidth / GameConfig.WallGridColumns;
            float cellHeight = WallBandHeight() / GameConfig.WallGridRows;
            float wallWidth = cellWidth * WallVisualWidthRatio;
            float wallHeight = cellHeight * WallVisualHeightRatio;

            foreach (WallPlacement wall in walls)
            {
                Vector2 pos = WallCellToWorld(wall.CellIndex);

                var go = new GameObject($"Wall_{wall.CellIndex}");
                go.transform.SetParent(transform);
                go.transform.position = pos;

                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(wallWidth, wallHeight);
                collider.sharedMaterial = PhysicsMaterials.Bouncy;

                var marker = go.AddComponent<WallMarker>();
                marker.CellIndex = wall.CellIndex;
                marker.IsDefaultWall = wall.IsDefaultWall;

                Sprite sprite = wall.IsDefaultWall ? LoadDefaultWallSprite() : LoadDisposableWallSprite();
                AddVisual(go, wallWidth, wallHeight, Color.white, sprite);

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

            float size = FieldWidth * GameConfig.BounceBoardSizeRatio;

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
                Sprite sprite = Resources.Load<Sprite>(PhysicsSpritePath + "elementGlass008");
                AddVisual(go, size, size, Color.white, sprite);

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

        /// <summary>壁グリッドの各セル中心のワールド座標（WallPlacementPanelのUIをフィールドに正しく重ねるために公開）。</summary>
        public Vector2 GetWallCellCenter(int cellIndex)
        {
            return WallCellToWorld(cellIndex);
        }

        /// <summary>壁グリッド1セル分のワールドサイズ（幅・高さ）。</summary>
        public Vector2 GetWallCellSize()
        {
            float cellWidth = WallBandWidth / GameConfig.WallGridColumns;
            float cellHeight = WallBandHeight() / GameConfig.WallGridRows;
            return new Vector2(cellWidth, cellHeight);
        }

        /// <summary>常設壁の見た目スプライト。WallPlacementPanelのプレビューも同じものを使い、見た目を一致させる。</summary>
        public static Sprite LoadDefaultWallSprite()
        {
            return Resources.Load<Sprite>(PhysicsSpritePath + "elementStone018");
        }

        /// <summary>使い捨て壁カードの見た目スプライト。WallPlacementPanelのプレビューも同じものを使い、見た目を一致させる。</summary>
        public static Sprite LoadDisposableWallSprite()
        {
            return Resources.Load<Sprite>(PhysicsSpritePath + "elementWood018");
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

            float cellWidth = WallBandWidth / columns;
            float cellHeight = WallBandHeight() / GameConfig.WallGridRows;
            float bandTopY = FractionToWorldY(TargetBandBottomFraction);

            float x = -WallBandWidth / 2f + cellWidth * (col + 0.5f);
            float y = bandTopY - cellHeight * (row + 0.5f);
            return new Vector2(x, y);
        }

        private static Vector2 NormalizedToWorld(Vec2 normalized)
        {
            float x = -FieldWidth / 2f + normalized.X * FieldWidth;
            float y = FractionToWorldY(normalized.Y);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 見た目（SpriteRenderer）を子オブジェクトとして追加し、指定したwidth x height（ワールド単位）で
        /// 実際に見える大きさになるようにスケールする。
        /// Kenneyスプライトはpixels per unitやピクセルサイズ次第でネイティブサイズが1x1ワールド単位ではないため、
        /// 親（go）自身のlocalScaleを直接width/heightにしてしまうと、goに付いたCollider2Dのサイズ・半径まで
        /// 意図せず一緒にスケールされてしまう（例: 壁の当たり判定が見た目とズレて隣の壁と重なる）。
        /// 見た目のスケールを子オブジェクトに閉じ込めることで、goの当たり判定はワールド単位の値をそのまま使える。
        /// </summary>
        private static SpriteRenderer AddVisual(GameObject go, float width, float height, Color color, Sprite sprite = null)
        {
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);

            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetSharedSquareSprite();
            renderer.color = color;

            Vector2 nativeSize = renderer.sprite.bounds.size;
            visualGo.transform.localScale = new Vector3(width / nativeSize.x, height / nativeSize.y, 1f);
            return renderer;
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
