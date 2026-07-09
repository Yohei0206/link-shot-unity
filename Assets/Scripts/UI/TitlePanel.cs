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

            Text title = UITheme.CreateText(transform, "Title", "LINK-SHOT", 110, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UITheme.SetRect(title.rectTransform, new Vector2(0, 60), new Vector2(1500, 220));

            // ロゴ画像素材が無いため、太字＋アウトライン＋ドロップシャドウでロゴ風に見せる。
            Outline outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.15f, 0f, 1f);
            outline.effectDistance = new Vector2(4, -4);

            Shadow shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(6, -8);

            Button startButton = UITheme.CreateButton(transform, "StartButton", "タップして始める", HandleStartClicked);
            UITheme.SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0, -180), new Vector2(500, 130));

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
