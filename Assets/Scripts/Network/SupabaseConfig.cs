using UnityEngine;

namespace LinkShot.Network
{
    /// <summary>
    /// Supabaseプロジェクトの接続情報(ARCHITECTURE.md 4章)。
    /// AnonKeyはクライアント埋め込み前提の公開キーなのでコミットしてよいが、
    /// service_roleキー(管理者権限の秘密鍵)は絶対にここに入れないこと。
    /// </summary>
    [CreateAssetMenu(fileName = "SupabaseConfig", menuName = "LinkShot/Supabase Config")]
    public class SupabaseConfig : ScriptableObject
    {
        [Tooltip("SupabaseプロジェクトのURL(例: https://xxxxx.supabase.co)")]
        public string ProjectUrl = "";

        [Tooltip("anon / public キー。service_roleキーは絶対に入れないこと。")]
        public string AnonKey = "";

        public bool IsConfigured => !string.IsNullOrEmpty(ProjectUrl) && !string.IsNullOrEmpty(AnonKey);
    }
}
