using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LinkShot.Network
{
    /// <summary>
    /// Supabase REST(PostgREST) + Auth(匿名認証)への薄いUnityWebRequestラッパー(ARCHITECTURE.md 4章)。
    /// 公式SDKには依存せず、必要なエンドポイントだけを直接叩く。
    /// </summary>
    public class SupabaseRestClient
    {
        private readonly SupabaseConfig _config;

        public string AccessToken { get; private set; }
        public string UserId { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(AccessToken);

        public SupabaseRestClient(SupabaseConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 匿名サインインを行い、以後のリクエストで使うaccess_token・ユーザーIDを保持する。
        /// Supabase側で Authentication > Providers > Anonymous Sign-Ins を有効化しておく必要がある。
        /// </summary>
        public IEnumerator SignInAnonymously(Action<bool, string> onComplete)
        {
            string url = $"{_config.ProjectUrl}/auth/v1/signup";
            using var request = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes("{}");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _config.AnonKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, $"{request.responseCode}: {request.downloadHandler?.text ?? request.error}");
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<AuthSignupResponse>(request.downloadHandler.text);
                AccessToken = response.access_token;
                UserId = response.user.id;
                onComplete?.Invoke(true, null);
            }
            catch (Exception e)
            {
                onComplete?.Invoke(false, $"parse error: {e.Message} / body={request.downloadHandler.text}");
            }
        }

        /// <summary>PostgRESTへのGET。pathはクエリ文字列込みで渡す(例: "matches?room_code=eq.ABC123")。</summary>
        public IEnumerator Get(string path, Action<bool, string> onComplete)
        {
            using var request = UnityWebRequest.Get($"{_config.ProjectUrl}/rest/v1/{path}");
            ApplyHeaders(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onComplete);
        }

        /// <summary>PostgRESTへのPOST(行の作成)。既定でPrefer: return=representationを付け、作成した行を返させる。</summary>
        public IEnumerator Post(string path, string jsonBody, Action<bool, string> onComplete)
        {
            using var request = new UnityWebRequest($"{_config.ProjectUrl}/rest/v1/{path}", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "return=representation");
            yield return request.SendWebRequest();
            HandleResponse(request, onComplete);
        }

        /// <summary>PostgRESTへのPATCH(行の更新)。pathにフィルタ条件を含める(例: "matches?id=eq.XXXX")。</summary>
        public IEnumerator Patch(string path, string jsonBody, Action<bool, string> onComplete)
        {
            using var request = new UnityWebRequest($"{_config.ProjectUrl}/rest/v1/{path}", "PATCH");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "return=representation");
            yield return request.SendWebRequest();
            HandleResponse(request, onComplete);
        }

        private void ApplyHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("apikey", _config.AnonKey);
            if (IsSignedIn)
            {
                request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
            }
        }

        private static void HandleResponse(UnityWebRequest request, Action<bool, string> onComplete)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, $"{request.responseCode}: {request.downloadHandler?.text ?? request.error}");
                return;
            }

            onComplete?.Invoke(true, request.downloadHandler.text);
        }

        [Serializable]
        private class AuthSignupResponse
        {
            public string access_token;
            public AuthUser user;
        }

        [Serializable]
        private class AuthUser
        {
            public string id;
        }
    }
}
