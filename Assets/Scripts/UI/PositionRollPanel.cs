using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (3)(8) 発射ポジション決定フェーズ: POSITION_CHOICE（自由選択）とREROLL（振り直し確認）のUI。
    /// どちらも攻撃側だけが操作する（CARDS.md 3-4章）。通常の（効果なし）サイコロ判定にはUIを出さない。
    /// </summary>
    public class PositionRollPanel : MonoBehaviour
    {
        private Text _titleText;
        private Transform _buttonContainer;
        private readonly List<GameObject> _buttons = new List<GameObject>();

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0f, 0f, 0f, 0.6f));
            UITheme.Stretch(background.rectTransform);

            _titleText = UITheme.CreateText(transform, "Title", string.Empty, 36, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_titleText.rectTransform, new Vector2(0, 200), new Vector2(1600, 100));

            var containerGo = new GameObject("Buttons", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            _buttonContainer = containerGo.transform;
            UITheme.SetRect((RectTransform)_buttonContainer, Vector2.zero, new Vector2(1200, 300));

            gameObject.SetActive(false);
        }

        /// <summary>POSITION_CHOICE発動時: サイコロの代わりに発射ポジションを自由に選ばせる。</summary>
        public void ShowPositionChoice(Action<int> onChosen)
        {
            ClearButtons();
            _titleText.text = "POSITION_CHOICE発動: 発射ポジションを選んでください";

            const float spacing = 180f;
            float startX = -(GameConfig.LaunchPositionCount - 1) * spacing / 2f;

            for (int i = 0; i < GameConfig.LaunchPositionCount; i++)
            {
                int position = i + 1;
                Button button = UITheme.CreateButton(_buttonContainer, $"Position_{position}", position.ToString(), () =>
                {
                    Hide();
                    onChosen(position);
                });
                UITheme.SetRect(button.GetComponent<RectTransform>(), new Vector2(startX + i * spacing, 0), new Vector2(140, 140));
                _buttons.Add(button.gameObject);
            }

            gameObject.SetActive(true);
        }

        /// <summary>REROLL発動時: 出目を見せたうえで振り直すか確定するかを選ばせる。</summary>
        public void ShowReroll(int rolledPosition, Action<bool> onDecided)
        {
            ClearButtons();
            _titleText.text = $"REROLL発動: 出目は{rolledPosition}番です。振り直しますか？";

            Button rerollButton = UITheme.CreateButton(_buttonContainer, "RerollButton", "振り直す", () =>
            {
                Hide();
                onDecided(true);
            });
            UITheme.SetRect(rerollButton.GetComponent<RectTransform>(), new Vector2(-260, 0), new Vector2(400, 140));
            _buttons.Add(rerollButton.gameObject);

            Button confirmButton = UITheme.CreateButton(_buttonContainer, "ConfirmButton", "この目で確定", () =>
            {
                Hide();
                onDecided(false);
            });
            UITheme.SetRect(confirmButton.GetComponent<RectTransform>(), new Vector2(260, 0), new Vector2(400, 140));
            _buttons.Add(confirmButton.gameObject);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ClearButtons()
        {
            foreach (GameObject go in _buttons)
            {
                Destroy(go);
            }

            _buttons.Clear();
        }
    }
}
