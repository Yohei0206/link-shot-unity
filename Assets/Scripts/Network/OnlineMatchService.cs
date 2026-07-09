using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Network
{
    /// <summary>
    /// オンライン同期対戦のルーム作成/参加とアクション同期(ARCHITECTURE.md 4章)。
    /// ポーリング方式(WebSocket/Realtimeは使わない)。Game層(MatchDirector)から駆動される想定で、
    /// このクラス自体はMonoBehaviourではない。
    /// </summary>
    public class OnlineMatchService
    {
        private const string RoomCodeChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // 0/O, 1/I/Lなど紛らわしい文字を除外
        private const int RoomCodeLength = 6;

        private readonly SupabaseRestClient _client;
        private readonly System.Random _random = new System.Random();

        public string MatchId { get; private set; }
        public string RoomCode { get; private set; }

        /// <summary>0=部屋を作った側(先にサインインした側)、1=参加した側。</summary>
        public int LocalPlayerIndex { get; private set; } = -1;

        private int _lastSeenSequence;

        public OnlineMatchService(SupabaseConfig config)
        {
            _client = new SupabaseRestClient(config);
        }

        public IEnumerator SignIn(Action<bool, string> onComplete)
        {
            return _client.SignInAnonymously(onComplete);
        }

        /// <summary>ルームコードを発行して部屋を作成し、自分をplayer0として登録する。</summary>
        public IEnumerator CreateRoom(Action<bool, string, string> onComplete)
        {
            string roomCode = GenerateRoomCode();
            string body = $"{{\"room_code\":\"{roomCode}\",\"player0_id\":\"{_client.UserId}\"}}";

            bool done = false;
            bool ok = false;
            string error = null;

            yield return _client.Post("matches", body, (success, response) =>
            {
                done = true;
                ok = success;
                if (success)
                {
                    MatchRow[] rows = JsonArrayUtility.FromJsonArray<MatchRow>(response);
                    MatchId = rows[0].id;
                    RoomCode = roomCode;
                    LocalPlayerIndex = 0;
                }
                else
                {
                    error = response;
                }
            });

            while (!done)
            {
                yield return null;
            }

            onComplete?.Invoke(ok, ok ? roomCode : null, error);
        }

        /// <summary>ルームコードで待機中の部屋を見つけ、自分をplayer1として参加する。</summary>
        public IEnumerator JoinRoom(string roomCode, Action<bool, string> onComplete)
        {
            bool found = false;
            MatchRow match = null;
            string error = null;

            yield return _client.Get($"matches?room_code=eq.{roomCode}&status=eq.waiting&select=*", (success, response) =>
            {
                if (!success)
                {
                    error = response;
                    return;
                }

                MatchRow[] rows = JsonArrayUtility.FromJsonArray<MatchRow>(response);
                if (rows.Length == 0)
                {
                    error = "部屋が見つからないか、既に対戦が始まっています";
                    return;
                }

                match = rows[0];
                found = true;
            });

            if (!found)
            {
                onComplete?.Invoke(false, error);
                yield break;
            }

            string patchBody = $"{{\"player1_id\":\"{_client.UserId}\",\"status\":\"active\"}}";
            bool patched = false;
            string patchError = null;

            yield return _client.Patch($"matches?id=eq.{match.id}", patchBody, (success, response) =>
            {
                patched = success;
                if (!success)
                {
                    patchError = response;
                }
            });

            if (!patched)
            {
                onComplete?.Invoke(false, patchError);
                yield break;
            }

            MatchId = match.id;
            RoomCode = roomCode;
            LocalPlayerIndex = 1;
            onComplete?.Invoke(true, null);
        }

        /// <summary>確定したGameActionをmatch_actionsへ追記する。</summary>
        public IEnumerator PushAction(GameAction action, Action<bool, string> onComplete)
        {
            return PushRaw(action.GetType().Name, NetworkActionCodec.Encode(action), onComplete);
        }

        /// <summary>
        /// デッキ選択の同期用。デッキ選択はGameState作成前(PhaseMachineの外)で起きるため、
        /// 通常のGameActionではなく専用のaction_type("DeckSelected")として送る。
        /// </summary>
        public IEnumerator PushDeckSelection(IReadOnlyList<string> cardIds, Action<bool, string> onComplete)
        {
            var payload = new NetworkActionCodec.Payload { actionType = "DeckSelected", deckCardIds = cardIds.ToArray() };
            return PushRaw("DeckSelected", JsonUtility.ToJson(payload), onComplete);
        }

        /// <summary>GameAction化されていない同期用データ(例: 発射ポジション確定通知)をmatch_actionsへ追記する。</summary>
        public IEnumerator PushRaw(string actionType, string payloadJson, Action<bool, string> onComplete)
        {
            int sequence = _lastSeenSequence + 1;
            string body = $"{{\"match_id\":\"{MatchId}\",\"sequence\":{sequence},\"action_type\":\"{actionType}\",\"payload\":{payloadJson}}}";

            bool done = false;
            bool ok = false;
            string error = null;

            yield return _client.Post("match_actions", body, (success, response) =>
            {
                done = true;
                ok = success;
                if (success)
                {
                    _lastSeenSequence = sequence;
                }
                else
                {
                    error = response;
                }
            });

            while (!done)
            {
                yield return null;
            }

            onComplete?.Invoke(ok, error);
        }

        /// <summary>
        /// 前回ポーリング以降に追加された行を、sequence昇順で取得する(生の行を返す。
        /// "DeckSelected"かGameActionかはMatchDirector側でaction_typeを見て判断する)。
        /// </summary>
        public IEnumerator PollNewActions(Action<bool, List<MatchActionRow>, string> onComplete)
        {
            string path = $"match_actions?match_id=eq.{MatchId}&sequence=gt.{_lastSeenSequence}&order=sequence.asc&select=*";

            bool done = false;
            bool ok = false;
            string error = null;
            var result = new List<MatchActionRow>();

            yield return _client.Get(path, (success, response) =>
            {
                done = true;
                ok = success;
                if (!success)
                {
                    error = response;
                    return;
                }

                MatchActionRow[] rows = JsonArrayUtility.FromJsonArray<MatchActionRow>(response);
                foreach (MatchActionRow row in rows.OrderBy(r => r.sequence))
                {
                    result.Add(row);
                    _lastSeenSequence = row.sequence;
                }
            });

            while (!done)
            {
                yield return null;
            }

            onComplete?.Invoke(ok, result, error);
        }

        /// <summary>部屋を作った側が、相手が参加してstatus='active'になるまで待つ。</summary>
        public IEnumerator WaitForOpponent(Action<bool, string> onComplete)
        {
            while (true)
            {
                bool done = false;
                bool active = false;
                string error = null;

                yield return _client.Get($"matches?id=eq.{MatchId}&select=status", (success, response) =>
                {
                    done = true;
                    if (!success)
                    {
                        error = response;
                        return;
                    }

                    MatchRow[] rows = JsonArrayUtility.FromJsonArray<MatchRow>(response);
                    active = rows.Length > 0 && rows[0].status == "active";
                });

                while (!done)
                {
                    yield return null;
                }

                if (error != null)
                {
                    onComplete?.Invoke(false, error);
                    yield break;
                }

                if (active)
                {
                    onComplete?.Invoke(true, null);
                    yield break;
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

        private string GenerateRoomCode()
        {
            var chars = new char[RoomCodeLength];
            for (int i = 0; i < RoomCodeLength; i++)
            {
                chars[i] = RoomCodeChars[_random.Next(RoomCodeChars.Length)];
            }

            return new string(chars);
        }
    }
}
