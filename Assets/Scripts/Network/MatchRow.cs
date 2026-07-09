using System;

namespace LinkShot.Network
{
    /// <summary>supabase/schema.sqlの`matches`テーブル1行に対応するDTO。</summary>
    [Serializable]
    public class MatchRow
    {
        public string id;
        public string room_code;
        public string status;
        public string player0_id;
        public string player1_id;
        public int first_attacker_player;
        public int rng_seed;
        public int round;
        public int shot_index;
        public string phase;
        public int winner;
        public string created_at;
        public string updated_at;
    }

    /// <summary>supabase/schema.sqlの`match_actions`テーブル1行に対応するDTO。</summary>
    [Serializable]
    public class MatchActionRow
    {
        public string id;
        public string match_id;
        public int sequence;
        public string action_type;
        public NetworkActionCodec.Payload payload;
        public string created_at;
    }
}
