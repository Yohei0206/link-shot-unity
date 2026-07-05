using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>壁・バウンド板に使う共有PhysicsMaterial2D（GameConfig.WallRestitution=完全弾性）。</summary>
    public static class PhysicsMaterials
    {
        private static PhysicsMaterial2D _bouncy;

        public static PhysicsMaterial2D Bouncy
        {
            get
            {
                if (_bouncy == null)
                {
                    _bouncy = new PhysicsMaterial2D("WallBounce")
                    {
                        bounciness = GameConfig.WallRestitution,
                        friction = 0f,
                    };
                }

                return _bouncy;
            }
        }
    }
}
