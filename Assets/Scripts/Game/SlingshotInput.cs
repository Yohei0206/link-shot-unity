using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LinkShot.Game
{
    /// <summary>
    /// スリングショット操作（GAME_RULES.md 8章）。引っ張った方向と逆にボールを発射する（Angry Birds方式）。
    /// 新Input System（マウス/タッチ両対応、CLAUDE.mdおよびARCHITECTURE.md 1章準拠）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SlingshotInput : MonoBehaviour
    {
        public float MaxPullDistance = 1.2f;
        public float MaxLaunchSpeed = 10f;
        public float VelocityMultiplier = 1f;

        /// <summary>MaxPullDistanceに対する比率。これ未満の引っ張りは誤タップ/ノイズとみなして発射しない。</summary>
        public float MinPullRatio = 0.15f;

        public event Action Launched;

        private Rigidbody2D _rigidbody;
        private Camera _camera;
        private Vector2 _launchOrigin;
        private bool _dragging;
        private bool _launched;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        /// <summary>次のショットに向けて発射円の中心にボールを再配置し、入力を受け付け可能にする。</summary>
        public void BeginAt(Vector2 launchOrigin)
        {
            _launchOrigin = launchOrigin;
            transform.position = launchOrigin;
            _rigidbody.linearVelocity = Vector2.zero;
            _dragging = false;
            _launched = false;
        }

        private void Update()
        {
            if (_launched)
            {
                return;
            }

            bool pressed = false;
            bool released = false;
            Vector2 screenPos = default;

            if (Mouse.current != null)
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                screenPos = Mouse.current.position.ReadValue();
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pressed = pressed || Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                released = released || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (pressed)
            {
                _dragging = true;
            }

            if (_dragging)
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }

                Vector2 world = _camera != null ? (Vector2)_camera.ScreenToWorldPoint(screenPos) : _launchOrigin;
                Vector2 pull = Vector2.ClampMagnitude(world - _launchOrigin, MaxPullDistance);
                transform.position = _launchOrigin + pull;
            }

            if (released && _dragging)
            {
                _dragging = false;
                Release();
            }
        }

        private void Release()
        {
            Vector2 pull = (Vector2)transform.position - _launchOrigin;
            float pullRatio = pull.magnitude / MaxPullDistance;

            transform.position = _launchOrigin;

            if (pullRatio < MinPullRatio)
            {
                // 誤タップ・ごくわずかなドラッグはノイズとして無視する。摩擦のないフィールドでは
                // どんなに小さい初速でも最終的に場外に出てしまい、ショットが無駄になるため。
                return;
            }

            float power = Mathf.Clamp01(pullRatio);
            Vector2 launchDirection = -pull.normalized;

            _rigidbody.linearVelocity = launchDirection * power * MaxLaunchSpeed * VelocityMultiplier;
            _launched = true;
            Launched?.Invoke();
        }
    }
}
