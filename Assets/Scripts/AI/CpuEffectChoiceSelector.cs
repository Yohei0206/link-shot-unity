using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// 対象選択が必要なカード効果(WALL_REMOVE/WALL_SHIFT/BOUNCE_BOARD/WIDE_GATE)に対する
    /// CPUの選択(GAME_RULES.md 9章)。Weakは有効な範囲内でランダムに選ぶだけだが、Strongは
    /// `state.Field.StarWallColumn`(最高得点の的「星」がある列)を意識して選ぶ。
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

        public static EffectChoice Choose(GameState state, EffectId effectId, CpuDifficulty difficulty, Rng rng)
        {
            switch (effectId)
            {
                case EffectId.WallRemove:
                    return ChooseWallRemove(state, difficulty, rng);
                case EffectId.WallShift:
                    return ChooseWallShift(state, difficulty, rng);
                case EffectId.BounceBoard:
                    return ChooseBounceBoard(rng);
                case EffectId.WideGate:
                    return ChooseWideGate(difficulty, rng);
                default:
                    return default;
            }
        }

        private static EffectChoice ChooseWallRemove(GameState state, CpuDifficulty difficulty, Rng rng)
        {
            IReadOnlyList<WallPlacement> walls = state.Field.DefenderWalls;
            if (walls.Count == 0)
            {
                return default;
            }

            // 除去するのは、星を最も塞いでいる(星の列に最も近い)壁。
            WallPlacement target = difficulty == CpuDifficulty.Strong
                ? ClosestToColumn(walls, state.Field.StarWallColumn)
                : walls[rng.NextInt(0, walls.Count)];

            return new EffectChoice { WallTargetCellIndex = target.CellIndex };
        }

        private static EffectChoice ChooseWallShift(GameState state, CpuDifficulty difficulty, Rng rng)
        {
            IReadOnlyList<WallPlacement> walls = state.Field.DefenderWalls;
            if (walls.Count == 0)
            {
                return default;
            }

            var occupied = new HashSet<int>(walls.Select(w => w.CellIndex));

            if (difficulty != CpuDifficulty.Strong)
            {
                int fromCellWeak = walls[rng.NextInt(0, walls.Count)].CellIndex;
                return new EffectChoice { WallTargetCellIndex = fromCellWeak, WallDestinationCellIndex = RandomUnoccupiedCell(occupied, rng) };
            }

            // 星を最も塞いでいる壁を、星から最も遠い空きセルへどかす。
            int fromCell = ClosestToColumn(walls, state.Field.StarWallColumn).CellIndex;
            int toCell = FarthestFromColumn(occupied, state.Field.StarWallColumn, rng);
            return new EffectChoice { WallTargetCellIndex = fromCell, WallDestinationCellIndex = toCell };
        }

        private static WallPlacement ClosestToColumn(IReadOnlyList<WallPlacement> walls, int? column)
        {
            if (column == null)
            {
                return walls[0];
            }

            int columns = GameConfig.WallGridColumns;
            WallPlacement best = walls[0];
            int bestDistance = int.MaxValue;

            foreach (WallPlacement wall in walls)
            {
                int distance = System.Math.Abs(wall.CellIndex % columns - column.Value);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = wall;
                }
            }

            return best;
        }

        private static int FarthestFromColumn(HashSet<int> occupied, int? column, Rng rng)
        {
            if (column == null)
            {
                return RandomUnoccupiedCell(occupied, rng);
            }

            int columns = GameConfig.WallGridColumns;
            int bestCell = -1;
            int bestDistance = -1;

            for (int cell = 0; cell < GameConfig.WallGridCellCount; cell++)
            {
                if (occupied.Contains(cell))
                {
                    continue;
                }

                int distance = System.Math.Abs(cell % columns - column.Value);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestCell = cell;
                }
            }

            return bestCell >= 0 ? bestCell : RandomUnoccupiedCell(occupied, rng);
        }

        private static int RandomUnoccupiedCell(HashSet<int> occupied, Rng rng)
        {
            int attempts = 0;
            int maxAttempts = GameConfig.WallGridCellCount * 2;
            while (attempts < maxAttempts)
            {
                int candidate = rng.NextInt(0, GameConfig.WallGridCellCount);
                if (!occupied.Contains(candidate))
                {
                    return candidate;
                }

                attempts++;
            }

            return 0;
        }

        private static EffectChoice ChooseBounceBoard(Rng rng)
        {
            float x = BounceXMin + (float)rng.NextFloat01() * (BounceXMax - BounceXMin);
            float y = BounceYMin + (float)rng.NextFloat01() * (BounceYMax - BounceYMin);
            return new EffectChoice { BouncePosition = new Vec2(x, y) };
        }

        private static EffectChoice ChooseWideGate(CpuDifficulty difficulty, Rng rng)
        {
            if (difficulty == CpuDifficulty.Strong)
            {
                // 拡大するなら一番得点の高い的(星)一択。
                return new EffectChoice { WideGateZone = TargetZoneId.Score500 };
            }

            var zones = new[] { TargetZoneId.Score500, TargetZoneId.Score300, TargetZoneId.Score100 };
            return new EffectChoice { WideGateZone = zones[rng.NextInt(0, zones.Length)] };
        }
    }
}
