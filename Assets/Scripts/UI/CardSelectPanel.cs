using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (1) 準備フェーズ: 手札のカードをボタン一覧で表示し、1枚選ばせる（GAME_RULES.md 3章）。
    /// 選択後はHandoverScreenで秘匿してから相手に手番を渡す想定（MatchDirectorが制御）。
    /// 属性（Element）は色分け＋アイコン（火・水・草）で見分けやすくする。
    /// </summary>
    public class CardSelectPanel : MonoBehaviour
    {
        private const string CardPanelSpritePath = "UI/Kenney/FantasyBorders/panel-007";
        private const string ElementIconPath = "UI/Elements/icon_element_";

        private Text _titleText;
        private Transform _buttonContainer;
        private readonly List<GameObject> _buttons = new List<GameObject>();

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0f, 0f, 0f, 0.75f));
            UITheme.Stretch(background.rectTransform);

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 44, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 400), new Vector2(1400, 100));

            var containerGo = new GameObject("Buttons", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            _buttonContainer = containerGo.transform;
            UITheme.SetRect((RectTransform)_buttonContainer, Vector2.zero, new Vector2(1000, 800));

            gameObject.SetActive(false);
        }

        public void Show(int player, IReadOnlyList<string> handCardIds, Action<string> onSelected)
        {
            _titleText.text = $"プレイヤー{player + 1}: カードを1枚選んでください";

            foreach (GameObject old in _buttons)
            {
                Destroy(old);
            }

            _buttons.Clear();

            int count = handCardIds.Count;
            const float spacing = 220f;
            float startX = -(count - 1) * spacing / 2f;

            for (int i = 0; i < count; i++)
            {
                string cardId = handCardIds[i];
                Card card = CardCatalog.Get(cardId);

                GameObject cardGo = BuildCard(cardId, card, () => onSelected?.Invoke(cardId));
                UITheme.SetRect(cardGo.GetComponent<RectTransform>(), new Vector2(startX + i * spacing, 0), new Vector2(180, 260));

                _buttons.Add(cardGo);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// カード1枚のUI（Kenney Fantasy UI Bordersの枚パネル＋属性アイコン＋レアリティ★＋効果名）を組み立てる。
        /// </summary>
        private GameObject BuildCard(string cardId, Card card, UnityAction onClick)
        {
            Image background = UITheme.CreateImage(_buttonContainer, $"Card_{cardId}", Resources.Load<Sprite>(CardPanelSpritePath), ElementColor(card.Element));
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);

            Image icon = UITheme.CreateImage(background.transform, "ElementIcon", ElementIcon(card.Element), Color.white);
            UITheme.SetRect(icon.rectTransform, new Vector2(0, 75), new Vector2(70, 70));

            Text starsText = UITheme.CreateText(background.transform, "Stars", RarityStars(card.Rarity), 26, new Color(0.85f, 0.65f, 0.05f), TextAnchor.MiddleCenter);
            UITheme.SetRect(starsText.rectTransform, new Vector2(0, 20), new Vector2(170, 40));

            Text label = UITheme.CreateText(background.transform, "Label", EffectJapanese(card.Effect), 28, Color.black, TextAnchor.MiddleCenter);
            UITheme.SetRect(label.rectTransform, new Vector2(0, -75), new Vector2(170, 100));

            return background.gameObject;
        }

        /// <summary>属性ごとの色分け（三すくみを見分けやすくする。CLAUDE.md用語集: ALPHA/BETA/GAMMA）。</summary>
        private static Color ElementColor(Element element)
        {
            return element switch
            {
                Element.ALPHA => new Color(0.85f, 0.3f, 0.3f),
                Element.BETA => new Color(0.3f, 0.5f, 0.9f),
                Element.GAMMA => new Color(0.35f, 0.75f, 0.4f),
                _ => Color.white,
            };
        }

        /// <summary>属性を表す自作アイコン（火=ALPHA/水=BETA/草=GAMMA。ElementColorの配色と対応させている）。</summary>
        private static Sprite ElementIcon(Element element)
        {
            return element switch
            {
                Element.ALPHA => Resources.Load<Sprite>(ElementIconPath + "fire"),
                Element.BETA => Resources.Load<Sprite>(ElementIconPath + "water"),
                Element.GAMMA => Resources.Load<Sprite>(ElementIconPath + "leaf"),
                _ => null,
            };
        }

        /// <summary>レアリティを星の数で表現する（文字ラベルではなく★の個数で見分ける）。</summary>
        private static string RarityStars(Rarity rarity)
        {
            int count = rarity switch
            {
                Rarity.Legendary => 3,
                Rarity.Rare => 2,
                _ => 1,
            };
            return new string('★', count);
        }

        /// <summary>効果名の日本語表記（CARDS.md 3-4章の効果名に準拠）。</summary>
        private static string EffectJapanese(EffectId effect)
        {
            return effect switch
            {
                EffectId.WallRemove => "壁除去",
                EffectId.BounceBoard => "バウンド板",
                EffectId.RangeBoost => "範囲拡張",
                EffectId.WallShift => "壁移動",
                EffectId.GhostBall => "すり抜け",
                EffectId.WideGate => "的の拡大",
                EffectId.Reroll => "振り直し",
                EffectId.CurveShot => "軌道操作",
                EffectId.PowerShot => "初速アップ",
                EffectId.ScoreDouble => "得点2倍",
                EffectId.SafetyNet => "保険",
                EffectId.MiniBall => "ボール縮小",
                EffectId.DoubleShot => "2連射",
                EffectId.PositionChoice => "出目選択",
                EffectId.WallReturn => "壁カード強奪",
                _ => effect.ToString(),
            };
        }
    }
}
