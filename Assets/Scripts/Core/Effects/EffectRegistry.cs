using System.Collections.Generic;

namespace LinkShot.Core.Effects
{
    /// <summary>
    /// EffectId から ICardEffect インスタンスへのマッピング（CARDS.md 6章）。
    /// 新しい効果の追加は、このディレクトリにクラスを1つ足してここに登録するだけで済む。
    /// </summary>
    public static class EffectRegistry
    {
        private static readonly Dictionary<EffectId, ICardEffect> Effects = BuildRegistry();

        private static Dictionary<EffectId, ICardEffect> BuildRegistry()
        {
            ICardEffect[] all =
            {
                new DoubleShotEffect(),
                new PositionChoiceEffect(),
                new WallReturnEffect(),
                new WallRemoveEffect(),
                new BounceBoardEffect(),
                new RangeBoostEffect(),
                new WallShiftEffect(),
                new GhostBallEffect(),
                new WideGateEffect(),
                new RerollEffect(),
                new CurveShotEffect(),
                new PowerShotEffect(),
                new ScoreDoubleEffect(),
                new SafetyNetEffect(),
                new MiniBallEffect(),
            };

            var dict = new Dictionary<EffectId, ICardEffect>();
            foreach (var effect in all)
            {
                dict[effect.Id] = effect;
            }

            return dict;
        }

        public static ICardEffect Get(EffectId id) => Effects[id];
    }
}
