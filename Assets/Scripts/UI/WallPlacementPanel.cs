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
    /// グリッドのボタンはFieldView側の壁セル座標をカメラ経由でCanvas座標に投影して配置し、
    /// 実際に壁が生成される位置・サイズとズレないようにする。
    /// </summary>
    public class WallPlacementPanel : MonoBehaviour
    {
        private static readonly Color EmptyColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color DefaultWallColor = new Color(0.45f, 0.25f, 0.1f);
        private static readonly Color DisposableWallColor = new Color(0.85f, 0.55f, 0.2f);

        private Text _titleText;
        private Text _remainingText;
        private Button _confirmButton;
        private readonly Image[] _cellImages = new Image[GameConfig.WallGridCellCount];

        private FieldView _fieldView;
        private Camera _camera;

        private int? _defaultCell;
        private readonly HashSet<int> _disposableCells = new HashSet<int>();
        private int _remainingDisposable;
        private Action<int, IReadOnlyList<int>> _onConfirm;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0f, 0f, 0f, 0.75f));
            UITheme.Stretch(background.rectTransform);

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 40, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 700), new Vector2(1000, 100));

            _remainingText = UITheme.CreateText(transform, "Remaining", string.Empty, 32, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_remainingText.rectTransform, new Vector2(0, 600), new Vector2(1000, 60));

            _confirmButton = UITheme.CreateButton(transform, "ConfirmButton", "この配置で確定", HandleConfirm);
            UITheme.SetRect(_confirmButton.GetComponent<RectTransform>(), new Vector2(0, -650), new Vector2(500, 140));

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
                Vector2 leftLocal = WorldToCanvasLocal(centerWorld + new Vector2(-cellSizeWorld.x / 2f, 0f), rectTransform);
                Vector2 rightLocal = WorldToCanvasLocal(centerWorld + new Vector2(cellSizeWorld.x / 2f, 0f), rectTransform);
                Vector2 topLocal = WorldToCanvasLocal(centerWorld + new Vector2(0f, cellSizeWorld.y / 2f), rectTransform);
                Vector2 bottomLocal = WorldToCanvasLocal(centerWorld + new Vector2(0f, -cellSizeWorld.y / 2f), rectTransform);

                float width = Mathf.Abs(rightLocal.x - leftLocal.x) * 0.92f;
                float height = Mathf.Abs(topLocal.y - bottomLocal.y) * 0.92f;

                Image cellImage = UITheme.CreateImage(transform, $"Cell_{i}", UITheme.LoadButtonSprite("button_square_flat"), EmptyColor);
                UITheme.SetRect(cellImage.rectTransform, centerLocal, new Vector2(width, height));

                var button = cellImage.gameObject.AddComponent<Button>();
                button.targetGraphic = cellImage;
                button.onClick.AddListener(() => HandleCellClicked(cellIndex));

                _cellImages[i] = cellImage;
            }
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
            for (int i = 0; i < _cellImages.Length; i++)
            {
                if (_defaultCell == i)
                {
                    _cellImages[i].color = DefaultWallColor;
                }
                else if (_disposableCells.Contains(i))
                {
                    _cellImages[i].color = DisposableWallColor;
                }
                else
                {
                    _cellImages[i].color = EmptyColor;
                }
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
