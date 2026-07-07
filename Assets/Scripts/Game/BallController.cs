using System;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// ボールの物理と接触検出（ARCHITECTURE.md 2.2章）。
    /// 的は貫通式（複数命中を1ショットで蓄積できる）で、ボールを止めずに通過する。
    /// ショットを終わらせる（止める）のは壁命中・場外・時間切れの3種類のみ。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        public event Action<ShotOutcomeKind, IReadOnlyList<TargetZoneId>> ShotResolved;

        /// <summary>GHOST_BALL発動時、最初の壁1枚だけを貫通する。</summary>
        public bool PassThroughFirstWall;

        private bool _resolved;
        private bool _usedGhostPass;
        private Collider2D _collider;
        private readonly List<TargetZoneId> _hitZones = new List<TargetZoneId>();

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_resolved)
            {
                return;
            }

            var target = other.GetComponent<TargetZoneMarker>();
            if (target != null)
            {
                // 貫通: 命中した的は記録して消し、ボールは止めずに飛び続ける。
                _hitZones.Add(target.ZoneId);
                Destroy(target.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_resolved)
            {
                return;
            }

            // 場外判定はフィールド全体を覆うトリガーの「退出」で検知する。ボールは発射位置（トリガー内側）で
            // 毎回アクティブ化されるため、進入(Enter)で判定すると再アクティブ化した瞬間に即座に場外扱いになってしまう。
            var outOfField = other.GetComponent<OutOfFieldMarker>();
            if (outOfField != null)
            {
                Resolve(ShotOutcomeKind.OutOfField);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_resolved)
            {
                return;
            }

            var wall = collision.collider.GetComponent<WallMarker>();
            if (wall == null)
            {
                return;
            }

            if (PassThroughFirstWall && !_usedGhostPass)
            {
                _usedGhostPass = true;
                Physics2D.IgnoreCollision(_collider, collision.collider);
                return;
            }

            Resolve(ShotOutcomeKind.WallHit);
        }

        /// <summary>制限時間切れ（GAME_RULES.md 5.2章）をMatchDirectorから通知するためのフック。</summary>
        public void ResolveTimeout()
        {
            Resolve(ShotOutcomeKind.Timeout);
        }

        /// <summary>
        /// 次のショット（DOUBLE_SHOTの2射目を含む）を始める前にMatchDirectorから呼ぶ。
        /// _resolvedは一度trueになったきりリセットされないと、1試合の最初のショット以降は
        /// 何にぶつかってもOnTriggerEnter2D/OnCollisionEnter2Dが早期returnし続け、
        /// 常にタイムアウト扱いになってしまうため、ショット開始のたびに解除する。
        /// </summary>
        public void Rearm()
        {
            _resolved = false;
            _usedGhostPass = false;
            _hitZones.Clear();
        }

        private void Resolve(ShotOutcomeKind outcome)
        {
            _resolved = true;
            ShotResolved?.Invoke(outcome, _hitZones);
        }
    }
}
