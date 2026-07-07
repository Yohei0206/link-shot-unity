using System;
using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// デッキ選択画面: 15種のカードプールから、レアリティ制限（CARDS.md 2章）を満たす
    /// {GameConfig.DeckSize}枚を選ばせる。選んだカードをタップすると選択/解除がトグルする。
    /// 選択数・レアリティ制限が満たされたときだけ確定ボタンが押せるようになる。
    /// </summary>
    public class DeckSelectPanel : MonoBehaviour
    {
        private const string CardPanelSpritePath = "UI/Kenney/FantasyBorders/panel-007";
        private static readonly Color DimFactor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color CheckmarkColor = new Color(1f, 0.95f, 0.3f);

        private Text _titleText;
        private Text _statusText;
        private Button _confirmButton;
        private Transform _gridContainer;

        private readonly Dictionary<string, Image> _cardBackgrounds = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> _cardCheckmarks = new Dictionary<string, Text>();
        private readonly HashSet<string> _selected = new HashSet<string>();
        private Action<IReadOnlyList<string>> _onConfirm;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0f, 0f, 0f, 0.85f));
            UITheme.Stretch(background.rectTransform);

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 40, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 490), new Vector2(1700, 80));

            _statusText = UITheme.CreateText(transform, "Status", string.Empty, 26, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            UITheme.SetRect(_statusText.rectTransform, new Vector2(0, 440), new Vector2(1700, 50));

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(transform, false);
            _gridContainer = gridGo.transform;
            UITheme.SetRect((RectTransform)_gridContainer, new Vector2(0, 30), new Vector2(1800, 700));

            _confirmButton = UITheme.CreateButton(transform, "ConfirmButton", "このデッキで決定", HandleConfirm);
            UITheme.SetRect(_confirmButton.GetComponent<RectTransform>(), new Vector2(0, -490), new Vector2(500, 110));

            BuildGrid();
            gameObject.SetActive(false);
        }

        /// <summary>15枚ぶんのカードボタンを一度だけ作る（プレイヤーごとに作り直す必要はない）。</summary>
        private void BuildGrid()
        {
            const int columns = 5;
            const float colSpacing = 190f;
            const float rowSpacing = 230f;

            IReadOnlyList<Card> allCards = CardCatalog.All;
            int rows = Mathf.CeilToInt((float)allCards.Count / columns);
            float startX = -(columns - 1) * colSpacing / 2f;
            float startY = (rows - 1) * rowSpacing / 2f;

            for (int i = 0; i < allCards.Count; i++)
            {
                Card card = allCards[i];
                int col = i % columns;
                int row = i / columns;
                Vector2 position = new Vector2(startX + col * colSpacing, startY - row * rowSpacing);

                GameObject cardGo = BuildCard(card, position);
                _cardBackgrounds[card.Id] = cardGo.GetComponent<Image>();
            }
        }

        private GameObject BuildCard(Card card, Vector2 position)
        {
            Image background = UITheme.CreateImage(_gridContainer, $"Card_{card.Id}", Resources.Load<Sprite>(CardPanelSpritePath), CardVisuals.ElementColor(card.Element));
            UITheme.SetRect(background.rectTransform, position, new Vector2(160, 210));

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => HandleCardClicked(card.Id));

            Image icon = UITheme.CreateImage(background.transform, "ElementIcon", CardVisuals.ElementIcon(card.Element), Color.white);
            UITheme.SetRect(icon.rectTransform, new Vector2(0, 55), new Vector2(56, 56));

            Text starsText = UITheme.CreateText(background.transform, "Stars", CardVisuals.RarityStars(card.Rarity), 20, new Color(0.85f, 0.65f, 0.05f), TextAnchor.MiddleCenter);
            UITheme.SetRect(starsText.rectTransform, new Vector2(0, 15), new Vector2(150, 30));

            Text label = UITheme.CreateText(background.transform, "Label", CardVisuals.EffectJapanese(card.Effect), 20, Color.black, TextAnchor.MiddleCenter);
            UITheme.SetRect(label.rectTransform, new Vector2(0, -55), new Vector2(150, 80));

            Text checkmark = UITheme.CreateText(background.transform, "Checkmark", string.Empty, 30, CheckmarkColor, TextAnchor.UpperRight);
            UITheme.SetRect(checkmark.rectTransform, new Vector2(55, 90), new Vector2(60, 40));
            _cardCheckmarks[card.Id] = checkmark;

            return background.gameObject;
        }

        public void Show(int player, Action<IReadOnlyList<string>> onConfirm)
        {
            _selected.Clear();
            _onConfirm = onConfirm;
            _titleText.text = $"プレイヤー{player + 1}: デッキを{GameConfig.DeckSize}枚選んでください";
            RefreshVisuals();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleCardClicked(string cardId)
        {
            if (_selected.Contains(cardId))
            {
                _selected.Remove(cardId);
            }
            else if (_selected.Count < GameConfig.DeckSize)
            {
                _selected.Add(cardId);
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            foreach (Card card in CardCatalog.All)
            {
                bool selected = _selected.Contains(card.Id);
                Color baseColor = CardVisuals.ElementColor(card.Element);
                _cardBackgrounds[card.Id].color = selected ? baseColor : baseColor * DimFactor;
                _cardCheckmarks[card.Id].text = selected ? "✓" : string.Empty;
            }

            bool full = _selected.Count == GameConfig.DeckSize;
            string error = null;
            bool valid = full && CardCatalog.IsValidDeck(_selected.ToList(), out error);

            _statusText.text = full
                ? (valid ? "決定できます" : error)
                : $"選択中: {_selected.Count} / {GameConfig.DeckSize}（★★★は{GameConfig.LegendaryDeckLimit}枚まで、★★は{GameConfig.RareDeckLimit}枚まで）";

            _confirmButton.interactable = valid;
        }

        private void HandleConfirm()
        {
            var deck = _selected.ToList();
            if (!CardCatalog.IsValidDeck(deck, out _))
            {
                return;
            }

            Hide();
            _onConfirm?.Invoke(deck);
        }
    }
}
