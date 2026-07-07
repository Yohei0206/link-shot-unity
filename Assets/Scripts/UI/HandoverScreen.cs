using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// デバイス受け渡し画面（ARCHITECTURE.md 5章: ローカル対戦の「伏せてセット」を秘匿する）。
    /// カード選択の前後で表示し、次のプレイヤーに端末を渡してからタップさせる。
    /// </summary>
    public class HandoverScreen : MonoBehaviour
    {
        private Text _messageText;
        private Button _continueButton;
        private Action _onContinue;

        private void Awake()
        {
            var background = UITheme.CreateImage(transform, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.98f));
            UITheme.Stretch(background.rectTransform);

            _messageText = UITheme.CreateText(transform, "Message", string.Empty, 48, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_messageText.rectTransform, new Vector2(0, 120), new Vector2(900, 300));

            _continueButton = UITheme.CreateButton(transform, "ContinueButton", "タップして続ける", HandleContinueClicked);
            UITheme.SetRect(_continueButton.GetComponent<RectTransform>(), new Vector2(0, -160), new Vector2(500, 140));

            gameObject.SetActive(false);
        }

        public void Show(string message, Action onContinue)
        {
            _messageText.text = message;
            _onContinue = onContinue;
            gameObject.SetActive(true);
        }

        private void HandleContinueClicked()
        {
            gameObject.SetActive(false);
            _onContinue?.Invoke();
        }
    }
}
