using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// 常時表示のスコア/ラウンド/フェーズ表示と、得点解決フェーズの結果オーバーレイ（GAME_RULES.md 3章の(6)(11)）。
    /// 詳細な履歴ログ（HistoryLog）はPhase1の後続タスクとして未実装。
    /// </summary>
    public class HudPanel : MonoBehaviour
    {
        private Text _statusText;
        private GameObject _resultOverlay;
        private Text _resultText;
        private Action _onContinue;

        private void Awake()
        {
            _statusText = UITheme.CreateText(transform, "Status", string.Empty, 26, Color.white, TextAnchor.UpperCenter);
            UITheme.SetRect(_statusText.rectTransform, new Vector2(0, 515), new Vector2(1600, 60));

            var overlayGo = new GameObject("ResultOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(transform, false);
            UITheme.Stretch((RectTransform)overlayGo.transform);
            _resultOverlay = overlayGo;

            Image dim = UITheme.CreateImage(overlayGo.transform, "Dim", null, new Color(0f, 0f, 0f, 0.6f));
            UITheme.Stretch(dim.rectTransform);

            _resultText = UITheme.CreateText(overlayGo.transform, "ResultText", string.Empty, 40, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_resultText.rectTransform, new Vector2(0, 100), new Vector2(900, 320));

            Button continueButton = UITheme.CreateButton(overlayGo.transform, "ContinueButton", "次へ", HandleContinue);
            UITheme.SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0, -180), new Vector2(400, 120));

            _resultOverlay.SetActive(false);
        }

        public void UpdateStatus(int round, int roundCount, int score0, int score1, string phaseLabel)
        {
            _statusText.text = $"ラウンド {round}/{roundCount}   P1:{score0}  -  P2:{score1}\n{phaseLabel}";
        }

        public void ShowResult(string message, Action onContinue)
        {
            _resultText.text = message;
            _onContinue = onContinue;
            _resultOverlay.SetActive(true);
        }

        private void HandleContinue()
        {
            _resultOverlay.SetActive(false);
            _onContinue?.Invoke();
        }
    }
}
