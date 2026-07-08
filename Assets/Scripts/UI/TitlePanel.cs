using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// タイトル画面。ゲームループの先頭(初回起動時・試合終了後の両方)で表示し、
    /// タップでモード選択画面へ進む(MatchDirectorが制御)。
    /// </summary>
    public class TitlePanel : MonoBehaviour
    {
        private Action _onStart;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0.08f, 0.1f, 0.16f, 1f));
            UITheme.Stretch(background.rectTransform);

            Text title = UITheme.CreateText(transform, "Title", "Link-Shot", 96, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(title.rectTransform, new Vector2(0, 120), new Vector2(1400, 200));

            Text subtitle = UITheme.CreateText(transform, "Subtitle", "交代制フリーキック対戦", 32, new Color(0.8f, 0.85f, 0.95f), TextAnchor.MiddleCenter);
            UITheme.SetRect(subtitle.rectTransform, new Vector2(0, 10), new Vector2(1200, 80));

            Button startButton = UITheme.CreateButton(transform, "StartButton", "タップして始める", HandleStartClicked);
            UITheme.SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0, -220), new Vector2(500, 130));

            gameObject.SetActive(false);
        }

        public void Show(Action onStart)
        {
            _onStart = onStart;
            gameObject.SetActive(true);
        }

        private void HandleStartClicked()
        {
            gameObject.SetActive(false);
            _onStart?.Invoke();
        }
    }
}
