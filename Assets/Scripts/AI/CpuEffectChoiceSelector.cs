using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// 対象選択が必要なカード効果(WALL_REMOVE/WALL_SHIFT/BOUNCE_BOARD/WIDE_GATE)に対する
    /// CPUの選択(GAME_RULES.md 9章)。有効な範囲内でランダムに選ぶだけの簡単な実装。
    /// </summary>
    public static class CpuEffectChoiceSelector
    {
        // BOUNCE_BOARDの設置位置は正規化フィールド座標(0..1, y=0が上端)。
        // 実際の有効範囲はGame層のFieldViewの帯構成に依存するため、
        // どの帯構成でも安全に収まる中央寄りの範囲だけを使う。
        private const float BounceXMin = 0.3f;
        private const float BounceXMax = 0.7f;
        private const float BounceYMin = 0.25f;
        private const float BounceYMax = 0.65f;

        public static EffectChoice Choose(GameState state, EffectId effectId, Rng rng)
        {
            switch (effectId)
            {
                case EffectId.WallRemove:
                    return ChooseWallRemove(state, rng);
                case EffectId.WallShift:
                    return ChooseWallShift(state, rng);
                case EffectId.BounceBoard:
                    return ChooseBounceBoard(rng);
                case EffectId.WideGate:
                    return ChooseWideGate(rng);
                default:
                    return default;
            }
        }

        private static EffectChoice ChooseWallRemove(GameState state, Rng rng)
        {
            IReadOnlyList<WallPlacement> walls = state.Field.DefenderWalls;
            if (walls.Count == 0)
            {
                return default;
            }

            int cell = walls[rng.NextInt(0, walls.Count)].CellIndex;
            return new EffectChoice { WallTargetCellIndex = cell };
        }

        private static EffectChoice ChooseWallShift(GameState state, Rng rng)
        {
            IReadOnlyList<WallPlacement> walls = state.Field.DefenderWalls;
            if (walls.Count == 0)
            {
                return default;
            }

            int fromCell = walls[rng.NextInt(0, walls.Count)].CellIndex;
            var occupied = new HashSet<int>(walls.Select(w => w.CellIndex));

            int toCell = fromCell;
            int attempts = 0;
            int maxAttempts = GameConfig.WallGridCellCount * 2;
            while (attempts < maxAttempts)
            {
                int candidate = rng.NextInt(0, GameConfig.WallGridCellCount);
                if (!occupied.Contains(candidate))
                {
                    toCell = candidate;
                    break;
                }

                attempts++;
            }

            return new EffectChoice { WallTargetCellIndex = fromCell, WallDestinationCellIndex = toCell };
        }

        private static EffectChoice ChooseBounceBoard(Rng rng)
        {
            float x = BounceXMin + (float)rng.NextFloat01() * (BounceXMax - BounceXMin);
            float y = BounceYMin + (float)rng.NextFloat01() * (BounceYMax - BounceYMin);
            return new EffectChoice { BouncePosition = new Vec2(x, y) };
        }

        private static EffectChoice ChooseWideGate(Rng rng)
        {
            var zones = new[] { TargetZoneId.Score500, TargetZoneId.Score300, TargetZoneId.Score100 };
            return new EffectChoice { WideGateZone = zones[rng.NextInt(0, zones.Length)] };
        }
    }
}
