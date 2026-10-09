using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FeedbackKit
{
    /// <summary>
    /// Phát âm thanh qua <see cref="FeedbackServices.Audio"/>.
    /// Random: pitch lệch nhẹ mỗi lần để đỡ nhàm. Ladder: pitch leo theo thang âm (combo, chuỗi nhặt đồ).
    /// </summary>
    [Serializable, FeedbackMenu("Audio/Sound")]
    public class SoundFeedback : Feedback
    {
        public enum PitchModes
        {
            Random,
            Ladder
        }

        public AudioClip[] Clips = new AudioClip[0];

        [Tooltip("Bật: chọn clip ngẫu nhiên, không lặp clip vừa phát. Tắt: phát lần lượt.")]
        public bool RandomClip = true;

        [Range(0f, 1f)] public float Volume = 1f;

        [Tooltip("Lệch volume ngẫu nhiên ± (0.1 ≈ ±1 dB).")]
        [Range(0f, 0.5f)] public float VolumeJitter = 0.05f;

        [Range(0.25f, 3f)] public float BasePitch = 1f;

        public PitchModes PitchMode = PitchModes.Random;

        [Tooltip("Random: lệch pitch ± bán cung. Âm có cao độ rõ: 0.3–1. Âm tiếng động (vỡ, rơi): 1–2.")]
        [Range(0f, 3f)] public float PitchJitterSemitones = 0.5f;

        [Tooltip("Ladder: các bậc (bán cung). Mặc định: ngũ cung trưởng, 2 quãng tám — không chỏi với nhạc cùng key.")]
        public int[] LadderSemitones = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

        [Tooltip("Ladder tự động: quay về bậc 0 nếu không phát trong khoảng này (giây, thời gian thực). " +
                 "Gọi player.PlayStep(n) để chọn bậc trực tiếp.")]
        [Min(0f)] public float LadderResetTime = 0.6f;

        [Tooltip("Số bản sao tối đa của cùng clip phát đồng thời (0 = không giới hạn).")]
        [Min(0)] public int MaxInstances = 3;

        [NonSerialized] int _ladderIndex = -1;
        [NonSerialized] float _lastLadderTime = float.NegativeInfinity;
        [NonSerialized] int _lastClip = -1;
        [NonSerialized] int _sequence;

        public override float Duration => 0f;

        protected override void OnPlay(FeedbackContext ctx)
        {
            AudioClip clip = PickClip();
            if (clip == null) return;

            float semitones;
            if (PitchMode == PitchModes.Ladder && LadderSemitones != null && LadderSemitones.Length > 0)
            {
                int index;
                if (ctx.Step >= 0)
                {
                    index = ctx.Step;
                }
                else
                {
                    float now = Time.unscaledTime;
                    _ladderIndex = now - _lastLadderTime > LadderResetTime ? 0 : _ladderIndex + 1;
                    _lastLadderTime = now;
                    index = _ladderIndex;
                }
                // Vượt quá thang thì giữ bậc cao nhất.
                semitones = LadderSemitones[Mathf.Clamp(index, 0, LadderSemitones.Length - 1)];
            }
            else
            {
                semitones = Random.Range(-PitchJitterSemitones, PitchJitterSemitones);
            }

            float pitch = BasePitch * Mathf.Pow(2f, semitones / 12f);
            float volume = Mathf.Clamp01(Volume * (1f + Random.Range(-VolumeJitter, VolumeJitter)));
            FeedbackServices.PlaySound(clip, volume, pitch, MaxInstances);
        }

        AudioClip PickClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            if (Clips.Length == 1) return Clips[0];

            int i;
            if (!RandomClip)
            {
                i = _sequence % Clips.Length;
                _sequence++;
            }
            else if (_lastClip < 0)
            {
                i = Random.Range(0, Clips.Length);
            }
            else
            {
                i = Random.Range(0, Clips.Length - 1);
                if (i >= _lastClip) i++;
            }

            _lastClip = i;
            return Clips[i];
        }
    }

    /// <summary>Rung qua <see cref="FeedbackServices.Haptics"/> (có chống rung dồn dập).</summary>
    [Serializable, FeedbackMenu("Haptics/Haptic")]
    public class HapticFeedback : Feedback
    {
        public HapticKind Kind = HapticKind.Light;

        public override float Duration => 0f;

        protected override void OnPlay(FeedbackContext ctx)
        {
            FeedbackServices.PlayHaptic(Kind);
        }
    }
}
