using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (1) 準備フェーズ: 手札のメダルをボタン一覧で表示し、1枚選ばせる（GAME_RULES.md 3章）。
    /// 選択後はHandoverScreenで秘匿してから相手に手番を渡す想定（MatchDirectorが制御）。
    /// 属性（Element）は色分け＋カタカナ表記で見分けやすくする。
    /// </summary>
    public class MedalSelectPanel : MonoBehaviour
    {
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

        public void Show(int player, IReadOnlyList<string> handMedalIds, Action<string> onSelected)
        {
            _titleText.text = $"プレイヤー{player + 1}: メダルを1枚選んでください";

            foreach (GameObject old in _buttons)
            {
                Destroy(old);
            }

            _buttons.Clear();

            int count = handMedalIds.Count;
            const float spacing = 220f;
            float startX = -(count - 1) * spacing / 2f;

            for (int i = 0; i < count; i++)
            {
                string medalId = handMedalIds[i];
                Medal medal = MedalCatalog.Get(medalId);

                Button button = UITheme.CreateButton(_buttonContainer, $"Medal_{medalId}", BuildLabel(medal), () => onSelected?.Invoke(medalId));
                UITheme.SetRect(button.GetComponent<RectTransform>(), new Vector2(startX + i * spacing, 0), new Vector2(180, 260));
                button.GetComponent<Image>().color = ElementColor(medal.Element);

                if (medal.Rarity == Rarity.Legendary)
                {
                    Image rankIcon = UITheme.CreateImage(button.transform, "RankIcon", UITheme.LoadGoldRank(0), Color.white);
                    UITheme.SetRect(rankIcon.rectTransform, new Vector2(0, 95), new Vector2(56, 56));
                }

                _buttons.Add(button.gameObject);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string BuildLabel(Medal medal)
        {
            return $"{RarityKatakana(medal.Rarity)}\n{ElementKatakana(medal.Element)}\n{EffectJapanese(medal.Effect)}";
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

        private static string ElementKatakana(Element element)
        {
            return element switch
            {
                Element.ALPHA => "アルファ",
                Element.BETA => "ベータ",
                Element.GAMMA => "ガンマ",
                _ => element.ToString(),
            };
        }

        private static string RarityKatakana(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Legendary => "レジェンド",
                Rarity.Rare => "レア",
                _ => "コモン",
            };
        }

        /// <summary>効果名の日本語表記（MEDALS.md 3-4章の効果名に準拠）。</summary>
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
