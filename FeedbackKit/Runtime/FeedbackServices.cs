using System.Collections.Generic;
using UnityEngine;

namespace FeedbackKit
{
    public enum HapticKind
    {
        Selection,
        Light,
        Medium,
        Heavy,
        Rigid,
        Soft,
        Success,
        Warning,
        Failure
    }

    /// <summary>Phát âm thanh. Game có thể thay bằng adapter tới SoundManager riêng.</summary>
    public interface IFeedbackAudio
    {
        /// <param name="maxInstances">Số bản sao tối đa của cùng clip phát đồng thời (0 = không giới hạn).</param>
        void Play(AudioClip clip, float volume, float pitch, int maxInstances);
    }

    /// <summary>Phát haptic. Game gán adapter (Nice Vibrations, plugin riêng…).</summary>
    public interface IFeedbackHaptics
    {
        void Play(HapticKind kind);
    }

    /// <summary>
    /// Điểm nối giữa FeedbackKit và hệ thống âm thanh / rung của game.
    /// Gán <see cref="Audio"/> và <see cref="Haptics"/> một lần khi game khởi động.
    /// </summary>
    public static class FeedbackServices
    {
        static IFeedbackAudio _audio;
        static IFeedbackHaptics _haptics;
        static float _lastHapticTime = float.NegativeInfinity;
        static int _lastHapticWeight;

        /// <summary>Mặc định: <see cref="DefaultFeedbackAudio"/> (pool AudioSource 2D riêng).</summary>
        public static IFeedbackAudio Audio
        {
            get
            {
                if (_audio == null) _audio = new DefaultFeedbackAudio();
                return _audio;
            }
            set => _audio = value;
        }

        /// <summary>Mặc định: không rung. Gán adapter khi khởi động.</summary>
        public static IFeedbackHaptics Haptics
        {
            get
            {
                if (_haptics == null) _haptics = new NullFeedbackHaptics();
                return _haptics;
            }
            set => _haptics = value;
        }

        /// <summary>
        /// Khoảng cách tối thiểu (giây, thời gian thực) giữa hai lần rung.
        /// Trong khoảng này, lần rung không mạnh hơn lần trước bị bỏ qua.
        /// </summary>
        public static float MinHapticInterval = 0.06f;

        public static void PlaySound(AudioClip clip, float volume, float pitch, int maxInstances)
        {
            if (!FeedbackGlobal.SoundEnabled || clip == null) return;
            Audio.Play(clip, volume, pitch, maxInstances);
        }

        public static void PlayHaptic(HapticKind kind)
        {
            if (!FeedbackGlobal.HapticsEnabled) return;

            float now = Time.unscaledTime;
            int weight = Weight(kind);
            if (now - _lastHapticTime < MinHapticInterval && weight <= _lastHapticWeight) return;

            _lastHapticTime = now;
            _lastHapticWeight = weight;
            Haptics.Play(kind);
        }

        static int Weight(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Selection:
                    return 0;
                case HapticKind.Light:
                case HapticKind.Soft:
                    return 1;
                case HapticKind.Medium:
                case HapticKind.Rigid:
                case HapticKind.Warning:
                    return 2;
                default:
                    return 3;
            }
        }
    }

    public sealed class NullFeedbackHaptics : IFeedbackHaptics
    {
        public void Play(HapticKind kind) { }
    }

    /// <summary>Pool AudioSource 2D đơn giản, sống qua các scene.</summary>
    public sealed class DefaultFeedbackAudio : IFeedbackAudio
    {
        readonly List<AudioSource> _sources = new List<AudioSource>();
        readonly int _poolSize;
        GameObject _root;

        public DefaultFeedbackAudio(int poolSize = 12)
        {
            _poolSize = Mathf.Max(1, poolSize);
        }

        public void Play(AudioClip clip, float volume, float pitch, int maxInstances)
        {
            if (clip == null) return;
            EnsurePool();

            if (maxInstances > 0)
            {
                int playing = 0;
                AudioSource oldest = null;
                foreach (AudioSource s in _sources)
                {
                    if (!s.isPlaying || s.clip != clip) continue;
                    playing++;
                    if (oldest == null || s.time > oldest.time) oldest = s;
                }
                if (playing >= maxInstances && oldest != null) oldest.Stop();
            }

            // Ưu tiên nguồn rảnh; hết nguồn rảnh thì lấy nguồn đã phát lâu nhất.
            AudioSource source = null;
            foreach (AudioSource s in _sources)
            {
                if (!s.isPlaying)
                {
                    source = s;
                    break;
                }
                if (source == null || s.time > source.time) source = s;
            }

            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        void EnsurePool()
        {
            if (_root != null) return;

            _root = new GameObject("[FeedbackKit Audio]");
            if (Application.isPlaying) Object.DontDestroyOnLoad(_root);

            _sources.Clear();
            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource s = _root.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sources.Add(s);
            }
        }
    }
}
