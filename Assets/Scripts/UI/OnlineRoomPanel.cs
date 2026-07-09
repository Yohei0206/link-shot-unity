using System;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// オンライン対戦のルーム作成/参加画面(ROADMAP.md Phase 3)。
    /// このパネル自体は通信を行わない(見た目とボタン操作の通知のみ)。実際のSupabase通信は
    /// MatchDirectorが行い、結果に応じてShowWaitingForOpponent/ShowStatus/Hideを呼び分ける。
    /// </summary>
    public class OnlineRoomPanel : MonoBehaviour
    {
        private Action _onCreateRoomClicked;
        private Action<string> _onJoinRoomClicked;

        private GameObject _createJoinGroup;
        private InputField _roomCodeInput;
        private Text _statusText;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.98f));
            UITheme.Stretch(background.rectTransform);

            Text title = UITheme.CreateText(transform, "Title", "オンライン対戦", 48, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(title.rectTransform, new Vector2(0, 280), new Vector2(900, 100));

            var groupGo = new GameObject("CreateJoinGroup", typeof(RectTransform));
            groupGo.transform.SetParent(transform, false);
            UITheme.Stretch((RectTransform)groupGo.transform);
            _createJoinGroup = groupGo;

            Button createButton = UITheme.CreateButton(groupGo.transform, "CreateButton", "部屋を作る", HandleCreateClicked);
            UITheme.SetRect(createButton.GetComponent<RectTransform>(), new Vector2(0, 120), new Vector2(600, 130));

            Text joinLabel = UITheme.CreateText(groupGo.transform, "JoinLabel", "ルームコードを入力して参加", 28, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(joinLabel.rectTransform, new Vector2(0, -60), new Vector2(600, 60));

            _roomCodeInput = CreateInputField(groupGo.transform, new Vector2(0, -140));

            Button joinButton = UITheme.CreateButton(groupGo.transform, "JoinButton", "参加する", HandleJoinClicked);
            UITheme.SetRect(joinButton.GetComponent<RectTransform>(), new Vector2(0, -260), new Vector2(600, 130));

            _statusText = UITheme.CreateText(transform, "Status", string.Empty, 30, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(_statusText.rectTransform, new Vector2(0, 0), new Vector2(1000, 300));

            gameObject.SetActive(false);
        }

        private static InputField CreateInputField(Transform parent, Vector2 position)
        {
            Image background = UITheme.CreateImage(parent, "RoomCodeInput", null, Color.white);
            UITheme.SetRect(background.rectTransform, position, new Vector2(400, 90));

            Text text = UITheme.CreateText(background.transform, "Text", string.Empty, 36, Color.black, TextAnchor.MiddleCenter);
            UITheme.Stretch(text.rectTransform);

            var input = background.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.characterLimit = 6;
            input.characterValidation = InputField.CharacterValidation.Alphanumeric;
            return input;
        }

        /// <summary>onCreateRoomClicked/onJoinRoomClickedは、各ボタンが押されたことをMatchDirectorへ知らせるだけ。</summary>
        public void Show(Action onCreateRoomClicked, Action<string> onJoinRoomClicked)
        {
            _onCreateRoomClicked = onCreateRoomClicked;
            _onJoinRoomClicked = onJoinRoomClicked;
            _roomCodeInput.text = string.Empty;
            _statusText.text = string.Empty;
            _createJoinGroup.SetActive(true);
            gameObject.SetActive(true);
        }

        public void ShowStatus(string message)
        {
            _statusText.text = message;
        }

        /// <summary>部屋作成後、相手の参加を待っている間の表示に切り替える。</summary>
        public void ShowWaitingForOpponent(string roomCode)
        {
            _createJoinGroup.SetActive(false);
            _statusText.text = $"ルームコード: {roomCode}\n\nこのコードを相手に伝えてください\n参加を待っています...";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleCreateClicked()
        {
            _onCreateRoomClicked?.Invoke();
        }

        private void HandleJoinClicked()
        {
            string code = (_roomCodeInput.text ?? string.Empty).Trim().ToUpperInvariant();
            _onJoinRoomClicked?.Invoke(code);
        }
    }
}
