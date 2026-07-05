using System.Collections.Generic;

namespace LinkShot.Core.Effects
{
    /// <summary>壁除去。防御側の壁を1枚除去する（常設壁も対象。除去対象は攻撃側が選ぶ）（MEDALS.md 4章#1）。</summary>
    public sealed class WallRemoveEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.WallRemove;

        public override void OnResolve(GameState state, EffectChoice choice)
        {
            if (choice.WallTargetCellIndex == null)
            {
                return;
            }

            int targetCell = choice.WallTargetCellIndex.Value;
            List<WallPlacement> walls = state.Field.DefenderWalls;
            for (int i = 0; i < walls.Count; i++)
            {
                if (walls[i].CellIndex == targetCell)
                {
                    walls.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
