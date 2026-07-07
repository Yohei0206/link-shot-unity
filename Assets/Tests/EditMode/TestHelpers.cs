using System.Collections.Generic;
using LinkShot.Core;

namespace LinkShot.Core.Tests
{
    /// <summary>テスト用のGameState構築ヘルパー。デッキ検証はここでは行わず、指定したカードIDをそのまま手札にする。</summary>
    internal static class TestHelpers
    {
        /// <summary>seed固定のRngを使い、指定した2カードだけを手札に持つ最小構成のGameStateを作る（単発フェーズ検証用）。</summary>
        public static GameState NewState(string player0Card, string player1Card, int seed = 1, int firstAttacker = 0)
        {
            var state = new GameState(new List<string> { player0Card }, new List<string> { player1Card }, firstAttacker, new Rng(seed));
            PhaseMachine.Dispatch(state, new SetCardAction(0, player0Card));
            PhaseMachine.Dispatch(state, new SetCardAction(1, player1Card));
            return state;
        }

        /// <summary>壁選択フェーズを既定値（常設壁のみセル0、使い捨てなし）で進める。</summary>
        public static void PlaceDefaultWallOnly(GameState state, int defaultCell = 0)
        {
            PhaseMachine.Dispatch(state, new PlaceWallsAction(defaultCell, new List<int>()));
        }
    }
}
