using System;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// ボールの物理と接触検出（ARCHITECTURE.md 2.2章）。
    /// 最初に触れた対象（的/壁/バウンド板/場外）だけをイベントとしてCore/側（MatchDirector）に通知する。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        public event Action<ShotOutcomeKind, TargetZoneId?> ShotResolved;

        /// <summary>GHOST_BALL発動時、最初の壁1枚だけを貫通する。</summary>
        public bool PassThroughFirstWall;

        private bool _resolved;
        private bool _usedGhostPass;
        private Collider2D _collider;

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
                Resolve(ShotOutcomeKind.TargetHit, target.ZoneId);
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
                Resolve(ShotOutcomeKind.OutOfField, null);
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

            Resolve(ShotOutcomeKind.WallHit, null);
        }

        /// <summary>制限時間切れ（GAME_RULES.md 5.2章）をMatchDirectorから通知するためのフック。</summary>
        public void ResolveTimeout()
        {
            Resolve(ShotOutcomeKind.Timeout, null);
        }

        private void Resolve(ShotOutcomeKind outcome, TargetZoneId? zone)
        {
            _resolved = true;
            ShotResolved?.Invoke(outcome, zone);
        }
    }
}
