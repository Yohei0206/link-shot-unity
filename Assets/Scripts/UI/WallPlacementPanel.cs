using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (2)(7) 壁選択フェーズ: 防御側が常設壁1個(必須)＋使い捨て壁カード0枚以上を5x2グリッドに配置する（GAME_RULES.md 7章）。
    /// 壁配置は公開情報のため、HandoverScreenでの秘匿は不要（GAME_RULES.md 3章補足）。
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

            BuildGrid();

            _confirmButton = UITheme.CreateButton(transform, "ConfirmButton", "この配置で確定", HandleConfirm);
            UITheme.SetRect(_confirmButton.GetComponent<RectTransform>(), new Vector2(0, -650), new Vector2(500, 140));

            gameObject.SetActive(false);
        }

        private void BuildGrid()
        {
            const float cellSize = 160f;
            const float spacing = 24f;
            int columns = GameConfig.WallGridColumns;
            int rows = GameConfig.WallGridRows;

            float totalWidth = columns * cellSize + (columns - 1) * spacing;
            float totalHeight = rows * cellSize + (rows - 1) * spacing;

            for (int i = 0; i < GameConfig.WallGridCellCount; i++)
            {
                int cellIndex = i;
                int row = i / columns;
                int col = i % columns;

                float x = -totalWidth / 2f + cellSize / 2f + col * (cellSize + spacing);
                float y = totalHeight / 2f - cellSize / 2f - row * (cellSize + spacing);

                Image cellImage = UITheme.CreateImage(transform, $"Cell_{i}", UITheme.LoadButtonSprite("button_square_flat"), EmptyColor);
                UITheme.SetRect(cellImage.rectTransform, new Vector2(x, y + 150f), new Vector2(cellSize, cellSize));

                var button = cellImage.gameObject.AddComponent<Button>();
                button.targetGraphic = cellImage;
                button.onClick.AddListener(() => HandleCellClicked(cellIndex));

                _cellImages[i] = cellImage;
            }
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
