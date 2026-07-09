using System;
using LinkShot.AI;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// 対戦モード選択画面（ROADMAP.md Phase 2/3）。「2人で対戦」「CPU対戦（弱/強）」「オンライン対戦」から選ぶ。
    /// ゲームループの最初に一度だけ表示し、以後の「もう一度遊ぶ」では再選択させない。
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        private Action<int?, CpuDifficulty> _onLocalOrCpuChosen;
        private Action _onOnlineChosen;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.98f));
            UITheme.Stretch(background.rectTransform);

            Text title = UITheme.CreateText(transform, "Title", "モード選択", 48, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(title.rectTransform, new Vector2(0, 250), new Vector2(900, 100));

            Button localButton = UITheme.CreateButton(transform, "LocalButton", "2人で対戦", () => ChooseLocalOrCpu(null, CpuDifficulty.Weak));
            UITheme.SetRect(localButton.GetComponent<RectTransform>(), new Vector2(0, 100), new Vector2(600, 130));

            Button cpuWeakButton = UITheme.CreateButton(transform, "CpuWeakButton", "CPU対戦：弱", () => ChooseLocalOrCpu(1, CpuDifficulty.Weak));
            UITheme.SetRect(cpuWeakButton.GetComponent<RectTransform>(), new Vector2(0, -40), new Vector2(600, 130));

            Button cpuStrongButton = UITheme.CreateButton(transform, "CpuStrongButton", "CPU対戦：強", () => ChooseLocalOrCpu(1, CpuDifficulty.Strong));
            UITheme.SetRect(cpuStrongButton.GetComponent<RectTransform>(), new Vector2(0, -180), new Vector2(600, 130));

            Button onlineButton = UITheme.CreateButton(transform, "OnlineButton", "オンライン対戦", ChooseOnline);
            UITheme.SetRect(onlineButton.GetComponent<RectTransform>(), new Vector2(0, -320), new Vector2(600, 130));

            gameObject.SetActive(false);
        }

        public void Show(Action<int?, CpuDifficulty> onLocalOrCpuChosen, Action onOnlineChosen)
        {
            _onLocalOrCpuChosen = onLocalOrCpuChosen;
            _onOnlineChosen = onOnlineChosen;
            gameObject.SetActive(true);
        }

        private void ChooseLocalOrCpu(int? cpuPlayerIndex, CpuDifficulty difficulty)
        {
            gameObject.SetActive(false);
            _onLocalOrCpuChosen?.Invoke(cpuPlayerIndex, difficulty);
        }

        private void ChooseOnline()
        {
            gameObject.SetActive(false);
            _onOnlineChosen?.Invoke();
        }
    }
}
