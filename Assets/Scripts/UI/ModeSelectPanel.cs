using System;
using LinkShot.AI;
using UnityEngine;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// 対戦モード選択画面（ROADMAP.md Phase 2）。「2人で対戦」「CPU対戦（弱/強）」から選ぶ。
    /// ゲームループの最初に一度だけ表示し、以後の「もう一度遊ぶ」では再選択させない。
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        private Action<int?, CpuDifficulty> _onChosen;

        private void Awake()
        {
            Image background = UITheme.CreateImage(transform, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.98f));
            UITheme.Stretch(background.rectTransform);

            Text title = UITheme.CreateText(transform, "Title", "モード選択", 48, Color.white, TextAnchor.MiddleCenter);
            UITheme.SetRect(title.rectTransform, new Vector2(0, 220), new Vector2(900, 100));

            Button localButton = UITheme.CreateButton(transform, "LocalButton", "2人で対戦", () => Choose(null, CpuDifficulty.Weak));
            UITheme.SetRect(localButton.GetComponent<RectTransform>(), new Vector2(0, 60), new Vector2(600, 130));

            Button cpuWeakButton = UITheme.CreateButton(transform, "CpuWeakButton", "CPU対戦：弱", () => Choose(1, CpuDifficulty.Weak));
            UITheme.SetRect(cpuWeakButton.GetComponent<RectTransform>(), new Vector2(0, -100), new Vector2(600, 130));

            Button cpuStrongButton = UITheme.CreateButton(transform, "CpuStrongButton", "CPU対戦：強", () => Choose(1, CpuDifficulty.Strong));
            UITheme.SetRect(cpuStrongButton.GetComponent<RectTransform>(), new Vector2(0, -260), new Vector2(600, 130));

            gameObject.SetActive(false);
        }

        public void Show(Action<int?, CpuDifficulty> onChosen)
        {
            _onChosen = onChosen;
            gameObject.SetActive(true);
        }

        private void Choose(int? cpuPlayerIndex, CpuDifficulty difficulty)
        {
            gameObject.SetActive(false);
            _onChosen?.Invoke(cpuPlayerIndex, difficulty);
        }
    }
}
