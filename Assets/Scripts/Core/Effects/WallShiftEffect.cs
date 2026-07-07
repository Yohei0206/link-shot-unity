using System.Collections.Generic;
using System.Linq;

namespace LinkShot.Core.Effects
{
    /// <summary>
    /// 壁移動。壁1枚（常設壁含む）を隣接マスに1つずらす（移動先は攻撃側が選ぶ。空きマスのみ）（CARDS.md 4章#4）。
    /// 移動先に既に壁がある場合は移動できない（CARDS.md 4章補足）。
    /// </summary>
    public sealed class WallShiftEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.WallShift;

        public override void OnResolve(GameState state, EffectChoice choice)
        {
            if (choice.WallTargetCellIndex == null || choice.WallDestinationCellIndex == null)
            {
                return;
            }

            int fromCell = choice.WallTargetCellIndex.Value;
            int toCell = choice.WallDestinationCellIndex.Value;

            List<WallPlacement> walls = state.Field.DefenderWalls;
            bool destinationOccupied = walls.Any(w => w.CellIndex == toCell);
            if (destinationOccupied)
            {
                return;
            }

            int index = walls.FindIndex(w => w.CellIndex == fromCell);
            if (index < 0)
            {
                return;
            }

            walls[index] = new WallPlacement(toCell, walls[index].IsDefaultWall);
        }
    }
}
