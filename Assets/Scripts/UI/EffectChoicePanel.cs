using System;
using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;
using LinkShot.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (4)(9) カード効果解決フェーズ: 対象選択が必要な4効果（WALL_REMOVE/WALL_SHIFT/BOUNCE_BOARD/WIDE_GATE）の
    /// 選択UIをまとめて提供する。選ぶのは常に攻撃側（CARDS.md 4章）。
    /// フィールド上のワールド座標をキャンバスへ投影する仕組みはWallPlacementPanelと同じ考え方。
    /// </summary>
    public class EffectChoicePanel : MonoBehaviour
    {
        private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color HitAreaColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color SelectedColor = new Color(0.2f, 0.9f, 0.4f, 0.7f);

        private Text _titleText;
        private FieldView _fieldView;
        private Camera _camera;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Awake()
        {
            Image instructionBacking = UITheme.CreateImage(transform, "InstructionBacking", null, new Color(0f, 0f, 0f, 0.5f));
            UITheme.SetRect(instructionBacking.rectTransform, new Vector2(0, 430), new Vector2(1920, 130));

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 30, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 430), new Vector2(1700, 100));

            gameObject.SetActive(false);
        }

        /// <summary>MatchDirectorから一度だけ呼び出す。</summary>
        public void Configure(FieldView fieldView, Camera camera)
        {
            _fieldView = fieldView;
            _camera = camera;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BeginShow(string title)
        {
            foreach (GameObject go in _spawned)
            {
                Destroy(go);
            }

            _spawned.Clear();
            _titleText.text = title;
            gameObject.SetActive(true);
        }

        /// <summary>WALL_REMOVE: 防御側の壁を1枚選ばせる。選んだ瞬間に確定する。</summary>
        public void ShowWallRemove(IReadOnlyList<WallPlacement> walls, Action<int> onChosen)
        {
            BeginShow("壁除去: 除去する壁を選んでください");

            foreach (WallPlacement wall in walls)
            {
                int cellIndex = wall.CellIndex;
                Button button = CreateWallCellButton(cellIndex, HitAreaColor, () =>
                {
                    Hide();
                    onChosen(cellIndex);
                });
                _spawned.Add(button.gameObject);
            }
        }

        /// <summary>WALL_SHIFT: 移動元の壁→移動先の空きマスの順に選ばせる。</summary>
        public void ShowWallShift(IReadOnlyList<WallPlacement> walls, Action<int, int> onChosen)
        {
            BeginShow("壁移動: 移動させる壁を選んでください");

            var occupiedCells = new HashSet<int>(walls.Select(w => w.CellIndex));

            foreach (WallPlacement wall in walls)
            {
                int fromCell = wall.CellIndex;
                Button button = CreateWallCellButton(fromCell, HitAreaColor, () =>
                {
                    ShowWallShiftDestination(fromCell, occupiedCells, onChosen);
                });
                _spawned.Add(button.gameObject);
            }
        }

        private void ShowWallShiftDestination(int fromCell, HashSet<int> occupiedCells, Action<int, int> onChosen)
        {
            BeginShow("壁移動: 移動先の空きマスを選んでください");

            for (int cellIndex = 0; cellIndex < GameConfig.WallGridCellCount; cellIndex++)
            {
                if (occupiedCells.Contains(cellIndex))
                {
                    continue;
                }

                int destinationCell = cellIndex;
                Button button = CreateWallCellButton(destinationCell, HitAreaColor, () =>
                {
                    Hide();
                    onChosen(fromCell, destinationCell);
                });
                _spawned.Add(button.gameObject);
            }
        }

        /// <summary>WIDE_GATE: 拡大する得点階層を1つ選ばせる。</summary>
        public void ShowWideGate(Action<TargetZoneId> onChosen)
        {
            BeginShow("的の拡大: 拡大する得点階層を選んでください");

            var containerGo = new GameObject("Buttons", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            UITheme.SetRect((RectTransform)containerGo.transform, Vector2.zero, new Vector2(1200, 200));
            _spawned.Add(containerGo);

            (TargetZoneId zone, string label)[] options =
            {
                (TargetZoneId.Score500, "500点"),
                (TargetZoneId.Score300, "300点"),
                (TargetZoneId.Score100, "100点"),
            };

            const float spacing = 260f;
            float startX = -(options.Length - 1) * spacing / 2f;

            for (int i = 0; i < options.Length; i++)
            {
                TargetZoneId zone = options[i].zone;
                Button button = UITheme.CreateButton(containerGo.transform, $"Zone_{zone}", options[i].label, () =>
                {
                    Hide();
                    onChosen(zone);
                });
                UITheme.SetRect(button.GetComponent<RectTransform>(), new Vector2(startX + i * spacing, 0), new Vector2(220, 140));
            }
        }

        /// <summary>BOUNCE_BOARD: フィールド上の任意位置をクリックして設置位置を決める（確定ボタンあり）。</summary>
        public void ShowBounceBoard(Action<Vec2> onChosen)
        {
            BeginShow("バウンド板: 設置する位置をタップしてください");

            var rectTransform = (RectTransform)transform;
            (Vector2 min, Vector2 max) bounds = _fieldView.GetBouncePlacementWorldBounds();
            Vector2 centerWorld = (bounds.min + bounds.max) / 2f;
            Vector2 sizeWorld = bounds.max - bounds.min;

            Vector2 centerLocal = WorldToCanvasLocal(centerWorld, rectTransform);
            Vector2 areaSize = ProjectWorldSize(centerWorld, sizeWorld, rectTransform);

            Image area = UITheme.CreateImage(transform, "PlacementArea", null, HitAreaColor);
            UITheme.SetRect(area.rectTransform, centerLocal, areaSize);
            _spawned.Add(area.gameObject);

            Image marker = UITheme.CreateImage(area.transform, "Marker", null, SelectedColor);
            Vector2 markerSize = ProjectWorldSize(centerWorld, new Vector2(0.4f, 0.4f), rectTransform);
            UITheme.SetRect(marker.rectTransform, Vector2.zero, markerSize);
            marker.gameObject.SetActive(false);

            Button confirmButton = UITheme.CreateButton(transform, "ConfirmButton", "この位置で確定", () => { });
            UITheme.SetRect(confirmButton.GetComponent<RectTransform>(), new Vector2(0, -460), new Vector2(500, 130));
            confirmButton.interactable = false;
            _spawned.Add(confirmButton.gameObject);

            Vector2? chosenWorld = null;

            ClickCapture capture = area.gameObject.AddComponent<ClickCapture>();
            capture.OnClick = eventData =>
            {
                Vector3 screenPoint = eventData.position;
                Vector2 world = _camera.ScreenToWorldPoint(screenPoint);
                world.x = Mathf.Clamp(world.x, bounds.min.x, bounds.max.x);
                world.y = Mathf.Clamp(world.y, bounds.min.y, bounds.max.y);
                chosenWorld = world;

                Vector2 localPoint = WorldToCanvasLocal(world, rectTransform) - centerLocal;
                marker.rectTransform.anchoredPosition = localPoint;
                marker.gameObject.SetActive(true);
                confirmButton.interactable = true;
            };

            confirmButton.onClick.AddListener(() =>
            {
                if (chosenWorld == null)
                {
                    return;
                }

                Hide();
                onChosen(FieldView.WorldToNormalized(chosenWorld.Value));
            });
        }

        private Button CreateWallCellButton(int cellIndex, Color color, Action onClick)
        {
            var rectTransform = (RectTransform)transform;
            Vector2 centerWorld = _fieldView.GetWallCellCenter(cellIndex);
            Vector2 sizeWorld = _fieldView.GetWallCellSize() * 0.9f;

            Vector2 centerLocal = WorldToCanvasLocal(centerWorld, rectTransform);
            Vector2 size = ProjectWorldSize(centerWorld, sizeWorld, rectTransform);

            Image image = UITheme.CreateImage(transform, $"CellHit_{cellIndex}", null, color);
            UITheme.SetRect(image.rectTransform, centerLocal, size);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        private Vector2 ProjectWorldSize(Vector2 centerWorld, Vector2 sizeWorld, RectTransform rectTransform)
        {
            Vector2 leftLocal = WorldToCanvasLocal(centerWorld + new Vector2(-sizeWorld.x / 2f, 0f), rectTransform);
            Vector2 rightLocal = WorldToCanvasLocal(centerWorld + new Vector2(sizeWorld.x / 2f, 0f), rectTransform);
            Vector2 topLocal = WorldToCanvasLocal(centerWorld + new Vector2(0f, sizeWorld.y / 2f), rectTransform);
            Vector2 bottomLocal = WorldToCanvasLocal(centerWorld + new Vector2(0f, -sizeWorld.y / 2f), rectTransform);

            return new Vector2(Mathf.Abs(rightLocal.x - leftLocal.x), Mathf.Abs(topLocal.y - bottomLocal.y));
        }

        private Vector2 WorldToCanvasLocal(Vector2 worldPos, RectTransform rectTransform)
        {
            Vector3 screenPoint = _camera.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        /// <summary>クリック位置（スクリーン座標）を取得するための小さなヘルパー。UnityのButtonはonClickに引数を渡せないため。</summary>
        private class ClickCapture : MonoBehaviour, IPointerClickHandler
        {
            public Action<PointerEventData> OnClick;

            public void OnPointerClick(PointerEventData eventData)
            {
                OnClick?.Invoke(eventData);
            }
        }
    }
}
