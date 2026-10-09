using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FeedbackKit
{
    /// <summary>
    /// Chạy một danh sách feedback. Mỗi "khoảnh khắc" của game nên có một player riêng
    /// (ví dụ: TileClear, ButtonClick, PopupOpen). Gọi Play() từ code hoặc từ UnityEvent.
    /// </summary>
    [AddComponentMenu("FeedbackKit/Feedback Player")]
    public class FeedbackPlayer : MonoBehaviour
    {
        public enum InitModes
        {
            Awake,
            Start,
            Manual
        }

        public enum PlayModes
        {
            /// <summary>Mọi feedback bắt đầu cùng lúc; Pause / HoldingPause chia nhóm.</summary>
            Parallel,

            /// <summary>Mỗi feedback chờ feedback trước chạy xong.</summary>
            Sequential
        }

        public enum TimeModes
        {
            Scaled,
            Unscaled
        }

        [SerializeReference] public List<Feedback> Feedbacks = new List<Feedback>();

        [Tooltip("Tên gợi nhớ, hiện trong FeedbackLab (ví dụ: TileClear, ButtonClick).")]
        public string Title = "";

        public InitModes Initialization = InitModes.Start;

        public PlayModes PlayMode = PlayModes.Parallel;

        [Min(0f)] public float SequentialDelay;

        [Tooltip("UI nên dùng Unscaled để vẫn chạy khi game pause / freeze frame.")]
        public TimeModes TimeMode = TimeModes.Scaled;

        [Tooltip("Nhân mọi thời lượng, delay và pause.")]
        [Min(0f)] public float DurationMultiplier = 1f;

        [Min(0f)] public float InitialDelay;

        [Tooltip("Chống spam: bỏ qua Play nếu lần trước cách chưa đủ thời gian này.")]
        [Min(0f)] public float Cooldown;

        [Tooltip("Tắt: bỏ qua Play khi lần trước chưa chạy xong.")]
        public bool CanPlayWhilePlaying = true;

        public bool PlayOnEnable;

        [Tooltip("Dừng (và đưa punch về vị trí nghỉ) khi object bị tắt — nên bật cho object dùng pool.")]
        public bool StopOnDisable = true;

        public bool RestoreOnDisable;

        public UnityEvent OnPlayStarted = new UnityEvent();
        public UnityEvent OnCompleted = new UnityEvent();
        public UnityEvent OnStopped = new UnityEvent();

        bool _initialized;
        bool _hasPlayed;
        int _activeSequences;
        float _lastPlayTime = float.NegativeInfinity;

        public bool IsPlaying => _activeSequences > 0;

        public bool LastDirectionReversed { get; private set; }

        public float Now => TimeMode == TimeModes.Unscaled ? Time.unscaledTime : Time.time;

        public float DeltaTime => TimeMode == TimeModes.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;

        protected virtual void Awake()
        {
            if (Initialization == InitModes.Awake) Initialize();
        }

        protected virtual void Start()
        {
            if (Initialization == InitModes.Start) Initialize();
        }

        protected virtual void OnEnable()
        {
            if (PlayOnEnable) Play();
        }

        protected virtual void OnDisable()
        {
            if (StopOnDisable) Stop();
            if (RestoreOnDisable) RestoreInitialValues();
        }

        protected virtual void OnDestroy()
        {
            if (!_initialized) return;
            foreach (Feedback f in Feedbacks) f?.Stop();
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            foreach (Feedback f in Feedbacks) f?.Initialize(this);
        }

        // ---- Play -------------------------------------------------------------------------------

        public void Play() => PlayInternal(transform.position, false, 1f, -1, false);

        public void Play(float intensity) => PlayInternal(transform.position, false, intensity, -1, false);

        /// <summary>Play tại một vị trí (particles dời tới đó nếu bật UsePlayPosition).</summary>
        public void Play(Vector3 position, float intensity = 1f) => PlayInternal(position, true, intensity, -1, false);

        /// <summary>Play với chỉ số bậc combo/cascade (SoundFeedback Ladder dùng để chọn nốt).</summary>
        public void PlayStep(int step, float intensity = 1f) => PlayInternal(transform.position, false, intensity, step, false);

        public void PlayStep(Vector3 position, int step, float intensity = 1f) => PlayInternal(position, true, intensity, step, false);

        /// <summary>Chạy ngược danh sách (và ngược từng tween): dùng để đóng thứ vừa mở.</summary>
        public void PlayReverse() => PlayInternal(transform.position, false, 1f, -1, true);

        public void PlayReverse(float intensity) => PlayInternal(transform.position, false, intensity, -1, true);

        /// <summary>Lần đầu chạy xuôi, sau đó luân phiên ngược / xuôi (mở ↔ đóng).</summary>
        public void Toggle()
        {
            if (!_hasPlayed || LastDirectionReversed) Play();
            else PlayReverse();
        }

        /// <summary>Play rồi chờ chạy xong (dùng trong coroutine: yield return player.PlayAndWait()).</summary>
        public IEnumerator PlayAndWait(float intensity = 1f)
        {
            Play(intensity);
            while (IsPlaying) yield return null;
        }

        // ---- Control ----------------------------------------------------------------------------

        public void Stop()
        {
            bool wasPlaying = IsPlaying;
            StopAllCoroutines();
            _activeSequences = 0;
            if (_initialized)
            {
                foreach (Feedback f in Feedbacks) f?.Stop();
            }
            if (wasPlaying) OnStopped.Invoke();
        }

        /// <summary>Nhảy tới trạng thái cuối của lần chạy gần nhất (tua màn ăn mừng, đóng popup ngay).</summary>
        public void SkipToEnd()
        {
            if (!_initialized) Initialize();
            bool wasPlaying = IsPlaying;
            StopAllCoroutines();
            _activeSequences = 0;
            bool reverse = LastDirectionReversed;
            foreach (Feedback f in Feedbacks)
            {
                if (f == null || !f.Active || f is PauseFeedback || !f.MatchesDirection(reverse)) continue;
                f.Skip(reverse);
            }
            if (wasPlaying) OnCompleted.Invoke();
        }

        public void RestoreInitialValues()
        {
            StopAllCoroutines();
            _activeSequences = 0;
            if (!_initialized) return;
            foreach (Feedback f in Feedbacks) f?.Restore();
        }

        public void ResetCooldown()
        {
            _lastPlayTime = float.NegativeInfinity;
        }

        // ---- Build from code --------------------------------------------------------------------

        public T AddFeedback<T>() where T : Feedback, new()
        {
            var f = new T();
            Feedbacks.Add(f);
            if (_initialized) f.Initialize(this);
            return f;
        }

        public T GetFeedback<T>(string label = null) where T : Feedback
        {
            foreach (Feedback f in Feedbacks)
            {
                if (f is T typed && (label == null || f.Label == label)) return typed;
            }
            return null;
        }

        /// <summary>Ước lượng tổng thời lượng một lần chạy (giây), theo cùng quy tắc với lúc chạy thật.</summary>
        public float ComputeTotalDuration(bool reverse = false)
        {
            float t = InitialDelay * DurationMultiplier;
            float holdUntil = t;
            float allUntil = t;
            int count = Feedbacks.Count;

            for (int k = 0; k < count; k++)
            {
                Feedback f = Feedbacks[reverse ? count - 1 - k : k];
                if (f == null || !f.Active) continue;

                if (f is PauseFeedback pause)
                {
                    if (pause is HoldingPauseFeedback) t = Mathf.Max(t, holdUntil);
                    t += pause.Seconds * DurationMultiplier;
                    if (pause is HoldingPauseFeedback) holdUntil = t;
                    continue;
                }

                if (!f.MatchesDirection(reverse)) continue;

                float end = t + f.TotalDuration(DurationMultiplier);
                allUntil = Mathf.Max(allUntil, end);
                if (!f.Timing.ExcludeFromHoldingPause) holdUntil = Mathf.Max(holdUntil, end);
                if (PlayMode == PlayModes.Sequential) t = end + SequentialDelay * DurationMultiplier;
            }

            return Mathf.Max(allUntil, t);
        }

        // ---- Internals --------------------------------------------------------------------------

        void PlayInternal(Vector3 position, bool hasPosition, float intensity, int step, bool reverse)
        {
            if (!FeedbackGlobal.Enabled || !isActiveAndEnabled) return;
            if (!_initialized) Initialize();

            float now = Now;
            if (Cooldown > 0f && now - _lastPlayTime < Cooldown) return;
            if (!CanPlayWhilePlaying && IsPlaying) return;

            _lastPlayTime = now;
            _hasPlayed = true;
            LastDirectionReversed = reverse;

            var ctx = new FeedbackContext
            {
                Player = this,
                Position = position,
                HasPosition = hasPosition,
                Intensity = intensity,
                Step = step,
                Reverse = reverse,
                Unscaled = TimeMode == TimeModes.Unscaled,
                DurationMultiplier = DurationMultiplier
            };

            // Coroutine chạy đồng bộ tới yield đầu tiên → feedback đầu tiên phát ngay trong frame này.
            StartCoroutine(SequenceRoutine(ctx));
        }

        IEnumerator SequenceRoutine(FeedbackContext ctx)
        {
            _activeSequences++;
            OnPlayStarted.Invoke();

            if (InitialDelay > 0f) yield return Wait(InitialDelay * DurationMultiplier);

            float holdUntil = Now; // kết thúc muộn nhất của các feedback (không bị loại) kể từ HoldingPause trước
            float allUntil = Now;  // kết thúc muộn nhất của mọi feedback
            int count = Feedbacks.Count;

            for (int k = 0; k < count; k++)
            {
                int index = ctx.Reverse ? count - 1 - k : k;
                if (index >= Feedbacks.Count) break;

                Feedback f = Feedbacks[index];
                if (f == null || !f.Active) continue;

                if (f is PauseFeedback pause)
                {
                    bool holding = pause is HoldingPauseFeedback;
                    if (holding)
                    {
                        float remaining = holdUntil - Now;
                        if (remaining > 0f) yield return Wait(remaining);
                    }

                    float seconds = pause.GetSeconds() * DurationMultiplier;
                    if (seconds > 0f) yield return Wait(seconds);

                    if (holding) holdUntil = Now;
                    continue;
                }

                if (!f.CanPlay(ctx)) continue;

                f.Play(ctx);
                float total = f.TotalDuration(DurationMultiplier);
                float end = Now + total;
                allUntil = Mathf.Max(allUntil, end);
                if (!f.Timing.ExcludeFromHoldingPause) holdUntil = Mathf.Max(holdUntil, end);

                if (PlayMode == PlayModes.Sequential)
                {
                    float wait = total + SequentialDelay * DurationMultiplier;
                    if (wait > 0f) yield return Wait(wait);
                }
            }

            float tail = allUntil - Now;
            if (tail > 0f) yield return Wait(tail);

            _activeSequences = Mathf.Max(0, _activeSequences - 1);
            if (_activeSequences == 0) OnCompleted.Invoke();
        }

        internal IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += DeltaTime;
            }
        }
    }
}
