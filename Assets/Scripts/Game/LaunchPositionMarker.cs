using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>実行時生成された発射ポジション円（1〜6）のマーカー。</summary>
    public class LaunchPositionMarker : MonoBehaviour
    {
        public int Position; // 1..GameConfig.LaunchPositionCount
        public float BaseRadius;
    }
}
