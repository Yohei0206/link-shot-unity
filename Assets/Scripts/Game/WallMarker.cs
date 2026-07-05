using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>実行時生成された壁のマーカー。Core.WallPlacementのセル情報を保持する。</summary>
    public class WallMarker : MonoBehaviour
    {
        public int CellIndex;
        public bool IsDefaultWall;
    }
}
