using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// (12) リザルト画面: 最終スコア・勝敗と、全ショットの履歴の振り返りを表示する（ARCHITECTURE.md 5章）。
    /// 「もう一度遊ぶ」でMatchDirectorに新しい対戦（デッキ選択から）のやり直しを要求する。
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        private Text _scoreText;
        private Transform _historyContainer;
        private readonly List<GameObject> _historyRows = new List<GameObject>();
        private Action _onRestart;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.96f));
            UITheme.Stretch(background.rectTransform);

            Text title = UITheme.CreateText(transform, "Title", "試合終了", 48, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(title.rectTransform, new Vector2(0, 490), new Vector2(1200, 80));

            _scoreText = UITheme.CreateText(transform, "Score", string.Empty, 32, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            UITheme.SetRect(_scoreText.rectTransform, new Vector2(0, 425), new Vector2(1200, 80));

            var historyGo = new GameObject("History", typeof(RectTransform));
            historyGo.transform.SetParent(transform, false);
            _historyContainer = historyGo.transform;
            UITheme.SetRect((RectTransform)_historyContainer, new Vector2(0, 60), new Vector2(1400, 640));

            Button restartButton = UITheme.CreateButton(transform, "RestartButton", "もう一度遊ぶ", HandleRestartClicked);
            UITheme.SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0, -490), new Vector2(500, 110));

            gameObject.SetActive(false);
        }

        public void Show(GameState state, Action onRestart)
        {
            _onRestart = onRestart;

            int p0 = state.Players[0].Score;
            int p1 = state.Players[1].Score;
            string resultText = state.Winner == null ? "引き分け" : $"プレイヤー{state.Winner + 1}の勝利！";
            _scoreText.text = $"P1: {p0}   -   P2: {p1}\n{resultText}";

            BuildHistory(state.History);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuildHistory(IReadOnlyList<ShotRecord> history)
        {
            foreach (GameObject go in _historyRows)
            {
                Destroy(go);
            }

            _historyRows.Clear();

            const float rowHeight = 44f;
            float startY = (history.Count - 1) * rowHeight / 2f;

            for (int i = 0; i < history.Count; i++)
            {
                ShotRecord record = history[i];
                string attackOrder = record.ShotIndex == 0 ? "先攻" : "後攻";
                string cardName = CardVisuals.EffectJapanese(CardCatalog.Get(record.AttackerCardId).Effect);
                string effectNote = record.EffectActivated ? "(発動)" : string.Empty;
                string line = $"R{record.Round} {attackOrder}  P{record.Attacker + 1}攻撃  [{cardName}]{effectNote}  {record.Outcome}  {record.Score}点";

                Text rowText = UITheme.CreateText(_historyContainer, $"Row_{i}", line, 22, Color.white, TextAnchor.MiddleCenter);
                UITheme.SetRect(rowText.rectTransform, new Vector2(0, startY - i * rowHeight), new Vector2(1380, rowHeight));
                _historyRows.Add(rowText.gameObject);
            }
        }

        private void HandleRestartClicked()
        {
            Hide();
            _onRestart?.Invoke();
        }
    }
}
