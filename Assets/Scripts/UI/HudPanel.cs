using System;
using LinkShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// 常時表示のフェーズ表示・両プレイヤーのカード情報ボード（左=P1/右=P2）と、
    /// 得点解決フェーズの結果オーバーレイ（GAME_RULES.md 3章の(6)(11)）。
    /// 詳細な履歴ログ（HistoryLog）はPhase1の後続タスクとして未実装。
    /// </summary>
    public class HudPanel : MonoBehaviour
    {
        private const string CardPanelSpritePath = "UI/Kenney/FantasyBorders/panel-007";
        private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0f);

        private Text _statusText;
        private PlayerInfoCard _player0Card;
        private PlayerInfoCard _player1Card;
        private GameObject _resultOverlay;
        private Text _resultText;
        private Action _onContinue;

        private class PlayerInfoCard
        {
            public Image Background;
            public Text ScoreText;
            public Image ElementIcon;
            public Text EffectNameText;
            public Text StatusText;
        }

        private void Awake()
        {
            _statusText = UITheme.CreateText(transform, "Status", string.Empty, 26, Color.white, TextAnchor.UpperCenter);
            UITheme.SetRect(_statusText.rectTransform, new Vector2(0, 515), new Vector2(1600, 60));

            _player0Card = BuildPlayerCard("P1Card", new Vector2(-800, 60));
            _player1Card = BuildPlayerCard("P2Card", new Vector2(800, 60));

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

        /// <summary>
        /// 自分側(P1)は左、相手側(P2)は右にカードUI風のボードを配置する。
        /// 属性アイコンをカード名の左に置く横並びレイアウト。
        /// </summary>
        private PlayerInfoCard BuildPlayerCard(string name, Vector2 position)
        {
            Image background = UITheme.CreateImage(transform, name, Resources.Load<Sprite>(CardPanelSpritePath), Color.white);
            UITheme.SetRect(background.rectTransform, position, new Vector2(300, 260));

            Text scoreText = UITheme.CreateText(background.transform, "Score", string.Empty, 30, Color.black, TextAnchor.MiddleCenter);
            UITheme.SetRect(scoreText.rectTransform, new Vector2(0, 90), new Vector2(280, 60));

            Image icon = UITheme.CreateImage(background.transform, "ElementIcon", null, EmptyColor);
            UITheme.SetRect(icon.rectTransform, new Vector2(-85, 0), new Vector2(64, 64));

            Text effectNameText = UITheme.CreateText(background.transform, "EffectName", string.Empty, 24, Color.black, TextAnchor.MiddleLeft);
            UITheme.SetRect(effectNameText.rectTransform, new Vector2(45, 0), new Vector2(160, 70));

            Text statusText = UITheme.CreateText(background.transform, "Status", string.Empty, 22, new Color(0.6f, 0.05f, 0.05f), TextAnchor.MiddleCenter);
            UITheme.SetRect(statusText.rectTransform, new Vector2(0, -90), new Vector2(280, 40));

            return new PlayerInfoCard
            {
                Background = background,
                ScoreText = scoreText,
                ElementIcon = icon,
                EffectNameText = effectNameText,
                StatusText = statusText,
            };
        }

        public void UpdateStatus(string phaseLabel)
        {
            _statusText.text = phaseLabel;
        }

        /// <summary>
        /// P1/P2それぞれのカード情報ボードを更新する。cardがnullの間(カード未確定)はアイコン・効果名を空にする。
        /// isCurrentAttackerがtrueのときだけ、その効果が発動しているか(有効/無効)を表示する。
        /// </summary>
        public void UpdatePlayerInfo(int player, int score, Card card, bool isCurrentAttacker, bool activated)
        {
            PlayerInfoCard target = player == 0 ? _player0Card : _player1Card;
            target.ScoreText.text = $"P{player + 1}  {score}";

            if (card == null)
            {
                target.Background.color = Color.white;
                target.ElementIcon.sprite = null;
                target.ElementIcon.color = EmptyColor;
                target.EffectNameText.text = string.Empty;
                target.StatusText.text = string.Empty;
                return;
            }

            // アイコン自体は無彩色なので、CardSelectPanelと同じく背景を属性色で塗って見分けやすくする。
            target.Background.color = CardVisuals.ElementColor(card.Element);
            target.ElementIcon.sprite = CardVisuals.ElementIcon(card.Element);
            target.ElementIcon.color = Color.white;
            target.EffectNameText.text = CardVisuals.EffectJapanese(card.Effect);
            target.StatusText.text = isCurrentAttacker ? (activated ? "(有効)" : "(無効)") : string.Empty;
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
