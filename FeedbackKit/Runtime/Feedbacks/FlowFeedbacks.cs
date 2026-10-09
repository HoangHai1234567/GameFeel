using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace FeedbackKit
{
    /// <summary>
    /// Phát particle. PlayExisting: hệ hạt có sẵn. SpawnFromPool: nhân bản prefab thành pool lúc khởi tạo
    /// (con của player). Nếu hạt cần đứng yên khi player di chuyển, đặt Simulation Space = World trong prefab.
    /// </summary>
    [Serializable, FeedbackMenu("Particles/Particles")]
    public class ParticlesFeedback : Feedback
    {
        public enum Modes
        {
            PlayExisting,
            SpawnFromPool
        }

        public Modes Mode = Modes.PlayExisting;

        [Tooltip("PlayExisting: hệ hạt trong scene. SpawnFromPool: prefab.")]
        public ParticleSystem Particles;

        [Tooltip("Số bản sao trong pool (SpawnFromPool). Nên ≥ số lần phát chồng nhau tối đa.")]
        [Min(1)] public int PoolSize = 4;

        [Tooltip("Dời hạt tới vị trí truyền vào player.Play(position, …).")]
        public bool UsePlayPosition = true;

        [Tooltip("Xoá hạt đang bay trước khi phát lại (PlayExisting).")]
        public bool ClearBeforePlay;

        [NonSerialized] ParticleSystem[] _pool;
        [NonSerialized] int _next;

        public override float Duration => 0f;

        protected override void OnInitialize()
        {
            if (Mode != Modes.SpawnFromPool || Particles == null) return;

            _pool = new ParticleSystem[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                ParticleSystem ps = Object.Instantiate(Particles, Owner.transform);
                ps.gameObject.name = Particles.name + " (pool " + i + ")";
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _pool[i] = ps;
            }
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            ParticleSystem ps = Particles;
            if (Mode == Modes.SpawnFromPool)
            {
                if (_pool == null || _pool.Length == 0) return;
                ps = _pool[_next];
                _next = (_next + 1) % _pool.Length;
            }
            if (ps == null) return;

            if (UsePlayPosition && ctx.HasPosition) ps.transform.position = ctx.Position;
            if (ClearBeforePlay || Mode == Modes.SpawnFromPool) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    /// <summary>Gọi UnityEvent. Chạy ngược: gọi ReverseEvent nếu có, nếu không gọi Event.</summary>
    [Serializable, FeedbackMenu("Events/Unity Event")]
    public class UnityEventFeedback : Feedback
    {
        public UnityEvent Event = new UnityEvent();

        public UnityEvent ReverseEvent = new UnityEvent();

        public override float Duration => 0f;

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (ctx.Reverse && ReverseEvent.GetPersistentEventCount() > 0) ReverseEvent.Invoke();
            else Event.Invoke();
        }
    }

    /// <summary>
    /// Bật/tắt GameObject. Chạy ngược thì đảo trạng thái.
    /// Đừng tắt chính GameObject chứa player khi chuỗi chưa xong (OnDisable sẽ dừng player).
    /// </summary>
    [Serializable, FeedbackMenu("GameObject/Set Active")]
    public class SetActiveFeedback : Feedback
    {
        public GameObject Target;

        [Tooltip("Trạng thái khi chạy xuôi; chạy ngược thì đảo lại.")]
        public bool State = true;

        [NonSerialized] bool _rest;
        [NonSerialized] bool _hasRest;

        public override float Duration => 0f;

        protected override void OnInitialize()
        {
            if (Target == null) return;
            _rest = Target.activeSelf;
            _hasRest = true;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target != null) Target.SetActive(ctx.Reverse ? !State : State);
        }

        protected override void OnStop() { }

        protected override void OnSkip(bool reverse)
        {
            if (Target != null) Target.SetActive(reverse ? !State : State);
        }

        protected override void OnRestore()
        {
            if (Target != null && _hasRest) Target.SetActive(_rest);
        }
    }

    /// <summary>
    /// Dừng chuỗi một khoảng thời gian; các feedback bên dưới bắt đầu sau đó.
    /// (Chế độ Parallel: các feedback phía trên vẫn đang chạy song song.)
    /// </summary>
    [Serializable, FeedbackMenu("Flow/Pause")]
    public class PauseFeedback : Feedback
    {
        [Min(0f)] public float Seconds = 0.2f;

        public bool Randomize;

        public Vector2 RandomRange = new Vector2(0.15f, 0.3f);

        public override float Duration => Seconds;

        internal float GetSeconds()
        {
            return Randomize ? Random.Range(RandomRange.x, RandomRange.y) : Seconds;
        }

        protected override void OnPlay(FeedbackContext ctx) { }
    }

    /// <summary>
    /// Chờ mọi feedback phía trên chạy xong (trừ feedback đánh dấu ExcludeFromHoldingPause),
    /// rồi chờ thêm Seconds.
    /// </summary>
    [Serializable, FeedbackMenu("Flow/Holding Pause")]
    public class HoldingPauseFeedback : PauseFeedback
    {
        public HoldingPauseFeedback()
        {
            Seconds = 0f;
        }
    }
}
