using System;
using System.Collections.Generic;
using LinkShot.Core;
using LinkShot.Game;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (2)(7) 壁選択フェーズ: 防御側が常設壁1個(必須)＋使い捨て壁カード0枚以上を5x2グリッドに配置する（GAME_RULES.md 7章）。
    /// 壁配置は公開情報のため、HandoverScreenでの秘匿は不要（GAME_RULES.md 3章補足）。
    /// モーダル背景は出さず、盤面（FieldView）の壁配置エリアに直接タップ判定を重ねる。
    /// タップ判定は掴みやすいようセル全体を使うが、見た目（プレビュー）はFieldViewで実際に生成される
    /// 薄い壁と同じ幅・高さにしておき、隣接セルに置いたときに隙間が見えるようにする。
    /// </summary>
    public class WallPlacementPanel : MonoBehaviour
    {
        private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color HitAreaColor = new Color(1f, 1f, 1f, 0.18f);

        private Text _titleText;
        private Text _remainingText;
        private Button _confirmButton;
        private readonly Image[] _cellIndicators = new Image[GameConfig.WallGridCellCount];
        private readonly Image[] _cellHitAreas = new Image[GameConfig.WallGridCellCount];

        private FieldView _fieldView;
        private Camera _camera;

        private int? _defaultCell;
        private readonly HashSet<int> _disposableCells = new HashSet<int>();
        private int _remainingDisposable;
        private Action<int, IReadOnlyList<int>> _onConfirm;

        private void Awake()
        {
            Image instructionBacking = UITheme.CreateImage(transform, "InstructionBacking", null, new Color(0f, 0f, 0f, 0.5f));
            UITheme.SetRect(instructionBacking.rectTransform, new Vector2(0, 430), new Vector2(1920, 130));

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 32, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 455), new Vector2(1400, 60));

            _remainingText = UITheme.CreateText(transform, "Remaining", string.Empty, 24, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_remainingText.rectTransform, new Vector2(0, 405), new Vector2(1400, 50));

            _confirmButton = UITheme.CreateButton(transform, "ConfirmButton", "この配置で確定", HandleConfirm);
            UITheme.SetRect(_confirmButton.GetComponent<RectTransform>(), new Vector2(0, -460), new Vector2(500, 130));

            gameObject.SetActive(false);
        }

        /// <summary>MatchDirectorから一度だけ呼び出し、フィールドの実座標を使ってグリッドを構築する。</summary>
        public void Configure(FieldView fieldView, Camera camera)
        {
            _fieldView = fieldView;
            _camera = camera;
            BuildGrid();
        }

        private void BuildGrid()
        {
            var rectTransform = (RectTransform)transform;
            Vector2 cellSizeWorld = _fieldView.GetWallCellSize();

            for (int i = 0; i < GameConfig.WallGridCellCount; i++)
            {
                int cellIndex = i;
                Vector2 centerWorld = _fieldView.GetWallCellCenter(i);
                Vector2 centerLocal = WorldToCanvasLocal(centerWorld, rectTransform);

                // タップ判定はセル全体を使う（掴みやすさ優先）。見た目はほぼ透明。
                Vector2 hitSize = ProjectWorldSize(centerWorld, cellSizeWorld * 0.95f, rectTransform);
                Image hitArea = UITheme.CreateImage(transform, $"CellHit_{i}", null, HitAreaColor);
                UITheme.SetRect(hitArea.rectTransform, centerLocal, hitSize);

                var button = hitArea.gameObject.AddComponent<Button>();
                button.targetGraphic = hitArea;
                button.onClick.AddListener(() => HandleCellClicked(cellIndex));

                // プレビューはFieldViewが実際に生成する壁と同じスプライト・幅・高さを使い、
                // 選択中の見た目と配置後の見た目が一致するようにする（未選択時は透明）。
                Vector2 previewSizeWorld = new Vector2(cellSizeWorld.x * FieldView.WallVisualWidthRatio, cellSizeWorld.y * FieldView.WallVisualHeightRatio);
                Vector2 previewSize = ProjectWorldSize(centerWorld, previewSizeWorld, rectTransform);
                Image indicator = UITheme.CreateImage(hitArea.transform, "Indicator", FieldView.LoadDefaultWallSprite(), EmptyColor);
                UITheme.SetRect(indicator.rectTransform, Vector2.zero, previewSize);

                _cellIndicators[i] = indicator;
                _cellHitAreas[i] = hitArea;
            }
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

        public void Show(int defenderPlayer, int remainingDisposableCards, Action<int, IReadOnlyList<int>> onConfirm)
        {
            _titleText.text = $"プレイヤー{defenderPlayer + 1}（防御側）: 壁を配置してください";
            _remainingDisposable = remainingDisposableCards;
            _defaultCell = null;
            _disposableCells.Clear();
            _onConfirm = onConfirm;
            RefreshVisuals();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleCellClicked(int cell)
        {
            if (_defaultCell == cell)
            {
                _defaultCell = null;
            }
            else if (_disposableCells.Contains(cell))
            {
                _disposableCells.Remove(cell);
            }
            else if (_defaultCell == null)
            {
                _defaultCell = cell;
            }
            else if (_disposableCells.Count < _remainingDisposable)
            {
                _disposableCells.Add(cell);
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _cellIndicators.Length; i++)
            {
                Image indicator = _cellIndicators[i];
                bool selected = _defaultCell == i || _disposableCells.Contains(i);

                if (_defaultCell == i)
                {
                    indicator.sprite = FieldView.LoadDefaultWallSprite();
                    indicator.color = Color.white;
                }
                else if (_disposableCells.Contains(i))
                {
                    indicator.sprite = FieldView.LoadDisposableWallSprite();
                    indicator.color = Color.white;
                }
                else
                {
                    indicator.color = EmptyColor;
                }

                // 選択中はタップ判定の半透明背景を消し、実際の壁と同じ見た目（色味）になるようにする。
                _cellHitAreas[i].color = selected ? EmptyColor : HitAreaColor;
            }

            _remainingText.text = $"使い捨て壁カード残り: {_remainingDisposable - _disposableCells.Count} / {_remainingDisposable}";
            _confirmButton.interactable = _defaultCell != null;
        }

        private void HandleConfirm()
        {
            if (_defaultCell == null)
            {
                return;
            }

            int defaultCell = _defaultCell.Value;
            var disposable = new List<int>(_disposableCells);
            gameObject.SetActive(false);
            _onConfirm?.Invoke(defaultCell, disposable);
        }
    }
}
