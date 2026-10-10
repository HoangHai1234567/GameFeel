# FeedbackKit

Hệ thống feedback (game feel) nhỏ gọn cho Unity, chạy trên **DOTween**. Thiết kế học theo Feel (More Mountains) — danh sách feedback nguyên tử, chạy xuôi/ngược, pause chia nhịp, intensity — nhưng không cần mua Feel.

> Quyết định: các game mới **không dùng Feel**, dùng FeedbackKit. Lý do và nghiên cứu nền: `feel-demos-study.md`. Quy tắc game feel chung: `game-feel-guide.md`.

---

## 1. Yêu cầu & cài đặt

- **Unity 2022.3+ hoặc Unity 6.** Đã biên dịch thử với 6000.0.73f1 và 2022.3.62f1 (C# 9).
- **DOTween** (bản free là đủ) đã cài và chạy *Setup DOTween*. FeedbackKit chỉ dùng phần lõi `DOTween.dll`, không cần module UI của DOTween.
- **UGUI** (`com.unity.ugui`) — cho `JuicyButton`, `CanvasGroupFadeFeedback`, `ColorFlashFeedback`.
- Không cần TextMeshPro.

**Cài:** chép cả thư mục `FeedbackKit/` vào `Assets/` (ví dụ `Assets/Plugins/FeedbackKit/`). Thư mục `Editor/` tự biên dịch vào editor assembly. Nếu muốn tách assembly riêng thì tự thêm `.asmdef` (Runtime tham chiếu DOTween + UnityEngine.UI; Editor tham chiếu Runtime, chỉ nền tảng Editor).

```
FeedbackKit/
├── Runtime/
│   ├── FeedbackPlayer.cs        ← component chính
│   ├── Feedback.cs              ← lớp gốc, Timing, Context
│   ├── FeedbackGlobal.cs        ← công tắc toàn cục (Settings)
│   ├── FeedbackServices.cs      ← nối âm thanh / haptics của game
│   ├── JuicyButton.cs           ← nút nhún
│   ├── FeedbackLab.cs           ← bảng thử trên máy
│   └── Feedbacks/               ← các feedback có sẵn
└── Editor/FeedbackPlayerEditor.cs
```

---

## 2. Khởi động: nối âm thanh, haptics, settings

Làm một lần khi game khởi động:

```csharp
using FeedbackKit;
using UnityEngine;

public static class FeedbackBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // Âm thanh: mặc định FeedbackKit tự tạo pool AudioSource 2D. Muốn đi qua SoundManager của game:
        // FeedbackServices.Audio = new MySoundAdapter();

        // Haptics: mặc định không rung. Gán adapter (mục 8):
        // FeedbackServices.Haptics = new NiceVibrationsV4Haptics();

        // Đọc settings của người chơi
        FeedbackGlobal.SoundEnabled   = PlayerPrefs.GetInt("fx_sound", 1) == 1;
        FeedbackGlobal.HapticsEnabled = PlayerPrefs.GetInt("fx_haptics", 1) == 1;
        FeedbackGlobal.ReduceMotion   = PlayerPrefs.GetInt("fx_reduce_motion", 0) == 1;
    }
}
```

Màn Settings chỉ cần ghi lại vào `FeedbackGlobal.*` (và lưu PlayerPrefs).

---

## 3. Khái niệm

| Khái niệm | Ý nghĩa |
|---|---|
| **FeedbackPlayer** | Component chứa danh sách feedback. **Mỗi khoảnh khắc một player** (TileClear, ButtonClick, PopupOpen, LevelWin…). |
| **Feedback** | Một hiệu ứng nguyên tử: một punch, một âm, một lần rung… Thêm bằng nút **+ Add Feedback** trong Inspector. |
| **Parallel / Sequential** | Parallel: mọi feedback bắt đầu cùng lúc, Pause chia nhóm. Sequential: mỗi feedback chờ cái trước xong. |
| **Pause** | Feedback bên dưới bắt đầu sau `Seconds`. |
| **Holding Pause** | Chờ mọi feedback phía trên xong (trừ cái đánh dấu `ExcludeFromHoldingPause`) rồi chờ thêm `Seconds`. |
| **PlayReverse** | Chạy ngược danh sách *và* ngược từng tween → một player vừa mở vừa đóng popup. `Timing.PlayWhen` = OnlyForward / OnlyReverse để bước chỉ chạy một chiều (ví dụ âm "mở" chỉ khi mở). |
| **Intensity** | `Play(intensity)` nhân biên độ punch/shake/flash. `Timing.UseIntensityRange` bật feedback theo bậc (combo 1–2: chỉ âm; ≥3: thêm shake…). |
| **Step** | `PlayStep(n)`: chọn nốt thứ n trong `SoundFeedback` chế độ Ladder (pitch leo theo combo). |
| **Time mode** | UI dùng **Unscaled** để vẫn chạy khi game pause / freeze. |
| **Cooldown / CanPlayWhilePlaying** | Chống spam. |
| **ReduceMotion** | Feedback chuyển động giảm biên độ (×0.35); `ShakeFeedback` có thể bỏ hẳn. |

### API
```csharp
player.Play();                         // xuôi, intensity 1
player.Play(1.5f);                     // mạnh hơn
player.Play(worldPos, 1f);             // particles dời tới worldPos
player.PlayStep(comboIndex);           // nốt thứ comboIndex trong Ladder
player.PlayReverse();                  // đóng thứ vừa mở
player.Toggle();                       // mở ↔ đóng
player.Stop();                         // dừng, punch về vị trí nghỉ
player.SkipToEnd();                    // nhảy tới trạng thái cuối (tua ăn mừng)
player.RestoreInitialValues();
yield return player.PlayAndWait();     // trong coroutine: chờ chạy xong
player.ComputeTotalDuration();         // ước lượng thời lượng
player.OnCompleted.AddListener(...);   // UnityEvent: OnPlayStarted / OnCompleted / OnStopped
```
Các hàm không tham số hoặc một `float` gắn được thẳng vào UnityEvent trong Inspector.

---

## 4. Feedback có sẵn

| Menu | Class | Dùng cho | Mặc định |
|---|---|---|---|
| Transform/Scale Punch | `ScalePunchFeedback` | Nảy khi chạm, nhận item, đổi số | +15%, 0.25s, vibrato 6, elasticity 0.6 |
| Transform/Scale Tween | `ScaleTweenFeedback` | Popup mở/đóng, xuất hiện. `Shape = PeakThenSettle`: From → Peak → To hai đoạn (nhỏ → to quá một chút → về), intensity nhân độ vượt | 0.85→1, 0.3s, OutBack (ngược: InCubic); Peak 1.12, PeakAt 0.55 |
| Transform/Squash & Stretch | `SquashStretchFeedback` | Rơi chạm đáy, nhấc lên, va chạm | 20%, 0.3s, giữ diện tích |
| Transform/Position Punch | `PositionPunchFeedback` | Nhún lên, giật theo hướng | (0,12,0), 0.25s |
| Transform/Rotation Punch | `RotationPunchFeedback` | Lắc icon, mascot | 12°, 0.3s |
| Transform/Shake | `ShakeFeedback` | Từ chối (rung ngang), rung container UI | (10,0,0), 0.3s, vibrato 15 |
| UI/CanvasGroup Fade | `CanvasGroupFadeFeedback` | Hiện/ẩn panel, chặn raycast khi ẩn | 0→1, 0.25s |
| Renderer/Color Flash | `ColorFlashFeedback` | Loé màu khi trúng | đỏ nhạt, 0.15s |
| Audio/Sound | `SoundFeedback` | Mọi âm thanh; Random pitch hoặc Ladder | ±0.5 bán cung, max 3 bản cùng lúc |
| Haptics/Haptic | `HapticFeedback` | Rung (qua adapter) | Light |
| Particles/Particles | `ParticlesFeedback` | Hạt có sẵn hoặc pool từ prefab | — |
| Events/Unity Event | `UnityEventFeedback` | Gọi logic khác trong chuỗi | — |
| GameObject/Set Active | `SetActiveFeedback` | Bật/tắt object (ngược = đảo) | — |
| Flow/Pause | `PauseFeedback` | Ngắt nhịp | 0.2s |
| Flow/Holding Pause | `HoldingPauseFeedback` | Chờ nhóm trên xong | 0s |

Mọi feedback có **Timing**: InitialDelay, Cooldown, Repeats, RepeatDelay, PlayWhen (chiều), ConstantIntensity, IntensityRange, RandomizeIntensity, ExcludeFromHoldingPause; và **Chance** (%).

**Lưu ý giá trị:** thông số mặc định là điểm khởi đầu theo `game-feel-guide.md` — chỉnh khi playtest trên máy thật.

---

## 5. Công thức nhanh

### 5.1 Nút bấm — `JuicyButton`
1. Cấu trúc: `Button` (Image raycast, **không scale**) → child `Visual` (hình + chữ).
2. Gắn `JuicyButton` lên cùng object với `Button`, kéo `Visual` vào.
3. Tuỳ chọn: tạo các player (Unscaled):
   - `DownFeedback`: Haptic Selection.
   - `ClickFeedback`: Sound click (PitchJitter 0.3) + Haptic Light.
   - `DeniedFeedback`: Sound "nope" + Haptic Warning (nút bị khoá tự rung ngang).

Mặc định: nhấn 0.92 trong 0.08s (OutQuad), nhả về 1 trong 0.2s (OutBack, overshoot 1.5), kéo ra ngoài thì bật về.

### 5.2 Chạm chọn một vật
Player "Select" trên vật, Target = child Visual:
- Scale Punch (+10%, 0.15s) · Sound tick (PitchJitter 0.5) · Haptic Selection.

### 5.3 Thao tác bị từ chối
Player "Denied", `Cooldown = 0.2`:
- Shake (10,0,0) 0.25s · Sound "nope" trầm, nhẹ · Haptic Warning.

### 5.4 Di chuyển rồi va chạm / tiếp đất (bắn bởi sự kiện thật, không đoán delay)
```csharp
selectFx.Play();                                   // pointer down: lấy đà
transform.DOMove(target, 0.18f).SetEase(Ease.OutCubic)
         .OnComplete(() => landFx.Play(target));   // đúng frame tới nơi
```
`landFx`: Squash & Stretch (Amount −0.2 = bẹp trước) · Particles (pool) · Sound va chạm · Haptic Light.

### 5.5 Clear một nhóm: lan từng phần tử, leo pitch
```csharp
static readonly WaitForSeconds Stagger = new WaitForSeconds(0.04f);

IEnumerator ClearGroup(List<Tile> tiles, int combo)
{
    groupFx.PlayStep(combo);                    // âm Ladder theo combo + rung Medium (1 lần)
    foreach (Tile t in tiles)
    {
        t.ClearFx.Play(t.transform.position);   // punch + hạt + điểm của từng ô
        yield return Stagger;
    }
}
```
Bật bậc theo combo: gọi `groupFx.PlayStep(combo, combo)` (intensity = combo). Trên feedback Shake của `groupFx` đặt `Timing.UseIntensityRange = true, IntensityMin = 3, IntensityMax = 99` **và `Timing.ConstantIntensity = true`** — combo < 3 không rung màn hình, combo ≥ 3 rung với biên độ cố định (không bị nhân theo số combo). Điều kiện khoảng được kiểm tra trên intensity gốc, trước khi ConstantIntensity đưa về 1.

### 5.6 Popup mở / đóng bằng một player
Player "Popup" (Unscaled) trên object cha (đừng đặt trên chính object bị tắt):
1. Set Active (Target = panel, State = true)
2. **Holding Pause** (0s) — bắt buộc
3. CanvasGroup Fade (0→1, 0.25s)
4. Scale Tween (0.85→1, 0.3s, OutBack; ngược InCubic)
5. Sound "open" — `Timing.PlayWhen = OnlyForward`

Mở: `popupFx.Play()`. Đóng: `popupFx.PlayReverse()`.

Vì sao cần Holding Pause: ở chế độ Parallel mọi feedback bắt đầu **cùng một frame**. Khi chạy ngược, Holding Pause bắt Set Active (tắt panel) chờ fade/scale chạy xong; thiếu nó panel sẽ biến mất ngay lập tức. Khi chạy xuôi, Set Active bật panel trước rồi fade/scale mới bắt đầu.

### 5.7 Thắng màn (chuỗi có nhịp, tua được)
Player "Win" (Unscaled):
1. Shake container UI (8,8,0) 0.3s · Sound hero · Haptic Success
2. Holding Pause 0.15s
3. Scale Tween sao 1 + Sound (Ladder) → Pause 0.3s → sao 2 → Pause 0.3s → sao 3
4. Holding Pause 0.1s
5. CanvasGroup Fade nút Next

Chạm màn hình → `winFx.SkipToEnd()`. Nạp sẵn level sau trong lúc chuỗi đang chạy.

---

## 6. FeedbackLab (thử trên máy)

Thêm component `FeedbackLab` vào một object trong scene dev → góc trái có nút **Feedback Lab**: danh sách mọi FeedbackPlayer, nút Play / Rev / Stop / Skip / Reset, thanh Intensity, Step, công tắc Sound / Haptics / Reduce motion / tắt toàn bộ (so sánh có–không feedback). **Bỏ khỏi bản phát hành.**

Trong Editor: chọn object có FeedbackPlayer khi đang Play mode → Inspector có khu **Test** với các nút tương tự.

---

## 7. Quy tắc dùng (rút từ nghiên cứu Feel)

1. **Mỗi khoảnh khắc một player**, đặt tên rõ (`Title`).
2. **Target là child Visual**, không phải object chứa logic/hitbox/raycast.
3. **Bắn bởi sự kiện thật** (pointer down, tween start, OnComplete, va chạm) — không căn bằng delay đoán.
4. **Chia pha:** lấy đà → hành động → va chạm/ổn định, mỗi pha một player.
5. **Mọi input đều có phản hồi**, kể cả bị từ chối (player Denied + cooldown).
6. **Feedback mạnh chỉ cho thành công và khoảnh khắc hiếm**; thao tác lặp lại giữ nhẹ, ngắn.
7. **UI → Unscaled.** Object dùng pool → giữ `StopOnDisable` bật.
8. **Không tắt chính GameObject chứa player** giữa chuỗi (OnDisable dừng player).
9. Haptics: không gắn Haptic vào feedback chạy hàng loạt (từng item trong một lượt nhặt) — đặt ở player tổng. `FeedbackServices.MinHapticInterval` (0.06s) đã chặn rung dồn dập.

---

## 8. Adapter mẫu

> Các adapter dưới đây **chưa biên dịch thử** (không có package tương ứng lúc viết) — kiểm tra lại tên API theo bản bạn cài.

**Nice Vibrations v4 (Lofelt / Feel):**
```csharp
using FeedbackKit;
using Lofelt.NiceVibrations;

public sealed class NiceVibrationsV4Haptics : IFeedbackHaptics
{
    public NiceVibrationsV4Haptics() { HapticController.Init(); } // tránh iOS mất haptic ~30s đầu

    public void Play(HapticKind kind)
    {
        HapticPatterns.PlayPreset(kind switch
        {
            HapticKind.Selection => HapticPatterns.PresetType.Selection,
            HapticKind.Light     => HapticPatterns.PresetType.LightImpact,
            HapticKind.Medium    => HapticPatterns.PresetType.MediumImpact,
            HapticKind.Heavy     => HapticPatterns.PresetType.HeavyImpact,
            HapticKind.Rigid     => HapticPatterns.PresetType.RigidImpact,
            HapticKind.Soft      => HapticPatterns.PresetType.SoftImpact,
            HapticKind.Success   => HapticPatterns.PresetType.Success,
            HapticKind.Warning   => HapticPatterns.PresetType.Warning,
            _                    => HapticPatterns.PresetType.Failure,
        });
    }
}
```

**Nice Vibrations v1.x (MMVibrationManager):**
```csharp
using FeedbackKit;
using MoreMountains.NiceVibrations;

public sealed class NiceVibrationsV1Haptics : IFeedbackHaptics
{
    public NiceVibrationsV1Haptics() { MMVibrationManager.iOSInitializeHaptics(); }

    public void Play(HapticKind kind)
    {
        MMVibrationManager.Haptic(kind switch
        {
            HapticKind.Selection                    => HapticTypes.Selection,
            HapticKind.Light or HapticKind.Soft     => HapticTypes.LightImpact,
            HapticKind.Medium or HapticKind.Rigid   => HapticTypes.MediumImpact,
            HapticKind.Heavy                        => HapticTypes.HeavyImpact,
            HapticKind.Success                      => HapticTypes.Success,
            HapticKind.Warning                      => HapticTypes.Warning,
            _                                       => HapticTypes.Failure,
        });
    }
}
```

**SoundManager riêng của game:**
```csharp
public sealed class MySoundAdapter : FeedbackKit.IFeedbackAudio
{
    public void Play(UnityEngine.AudioClip clip, float volume, float pitch, int maxInstances)
    {
        // Gọi API thật của SoundManager trong game, ví dụ:
        // SoundManager.Instance.PlaySfx(clip, volume, pitch, maxInstances);
    }
}
```

---

## 9. Trạng thái & việc tiếp theo

**Đã kiểm chứng:** Runtime và Editor biên dịch không lỗi, không cảnh báo với DLL của Unity 6000.0.73f1 và 2022.3.62f1 + DOTween (C# 9).

**Chưa kiểm chứng:** chưa chạy trong Unity Editor/trên máy (chưa có project thử); chưa có test tự động. Khi đưa vào game đầu tiên, nên thử: thêm/xoá/sắp xếp feedback trong Inspector, prefab có FeedbackPlayer, chạy xuôi/ngược popup, SkipToEnd, chạm nút liên tục, object trong pool bật/tắt.

**Chưa có (bản sau):**
- Spring (bump/moveTo, damping/frequency) cho chuyển động bị ngắt liên tục.
- Floating text có pool (điểm, combo).
- Freeze frame / time scale có stack.
- Looper, MMF-like chain (player gọi player).
- Particle cho UI (cần UIParticle hoặc camera riêng).
