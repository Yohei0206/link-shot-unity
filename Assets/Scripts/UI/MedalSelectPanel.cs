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
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 700), new Vector2(1000, 120));

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
            string rarity = medal.Rarity switch
            {
                Rarity.Legendary => "LEGEND",
                Rarity.Rare => "RARE",
                _ => "COMMON",
            };

            return $"{rarity}\n{medal.Element}\n{medal.Effect}";
        }
    }
}
