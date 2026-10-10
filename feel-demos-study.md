# Nghiên cứu: Feel (More Mountains) — Demos, kiến trúc & áp dụng cho mobile puzzle

> Nghiên cứu sâu trang https://feel-docs.moremountains.com/demos.html và toàn bộ tài liệu liên quan (core concepts, MMF_Player, list feedback, springs, recipes, screen shakes, MMTools, Nice Vibrations, API Doxygen, changelog). Tài liệu đi kèm [game-feel-guide.md](game-feel-guide.md).
>
> **Giới hạn nguồn — đọc trước:**
> - Tài liệu hiện tại là **Feel 6.1** (9/2026), yêu cầu **Unity 6000.5**.
> - **Danh sách feedback cụ thể (loại, thứ tự, thông số) bên trong từng demo KHÔNG được công bố** — chúng nằm trong scene/prefab của asset trả phí. Thứ kiểm chứng được: mô tả demo, API của các script demo (field + default), recipes, changelog. Phần suy luận ghi **[suy luận]**.
> - Video YouTube, bản WebGL và Wayback đều không đọc được bằng công cụ; danh sách video nên xem tay ở mục 12.
>
> **Quyết định:** không mua/cài Feel. Các game mới dùng **FeedbackKit** — hệ thống tự viết trên DOTween theo kiến trúc ở mục 11 (xem `FeedbackKit/README.md`). Tài liệu này giữ vai trò tham khảo: bài học, công thức và giá trị từ Feel để cấu hình FeedbackKit.

---

## Mục lục
1. [TL;DR — 12 bài học quan trọng nhất](#1-tldr--12-bài-học-quan-trọng-nhất)
2. [Feel hoạt động thế nào (mô hình tư duy)](#2-feel-hoạt-động-thế-nào)
3. [Từng demo: làm gì, dạy gì, áp dụng puzzle](#3-từng-demo)
4. [Mẫu thiết kế rút ra từ các demo](#4-mẫu-thiết-kế-rút-ra-từ-các-demo)
5. [Công thức (recipe) cho game puzzle](#5-công-thức-cho-game-puzzle)
6. [Bẫy giá trị mặc định](#6-bẫy-giá-trị-mặc-định)
7. [Springs & Tweens trong Feel](#7-springs--tweens-trong-feel)
8. [MMTools hữu ích cho mobile puzzle](#8-mmtools-hữu-ích-cho-mobile-puzzle)
9. [Danh mục feedback (rút gọn)](#9-danh-mục-feedback-rút-gọn)
10. [Nice Vibrations: v1.x so với v4](#10-nice-vibrations-v1x-so-với-v4)
11. [Quyết định: không dùng Feel, dùng FeedbackKit](#11-quyết-định-không-dùng-feel-dùng-feedbackkit)
12. [Việc còn mở & video nên xem](#12-việc-còn-mở--video-nên-xem)
13. [Nguồn](#13-nguồn)

---

## 1. TL;DR — 12 bài học quan trọng nhất

1. **Mọi input đều có phản hồi, kể cả input bị từ chối.** *Mọi* controller demo đều có `CooldownDuration` + một **`DeniedFeedback`** riêng phát khi input bị chặn (Blob, Duck, Barbarian…). `[API demo]`
2. **Tách logic (root) và hình (child "Model")**. Feedback chỉ chạm vào child → punch/squash không bao giờ làm lệch grid hay hitbox. `[Getting Started]`
3. **Chia hành động thành pha, mỗi pha bắn bởi một sự kiện gameplay thật**: *charge → jump → landing* (Bounce). Không căn thời gian bằng delay đoán mò → đổi logic không phải chỉnh lại feedback. `[demos: "recommended approach"]`
4. **Bắn feedback va chạm đúng frame va chạm** (Duck: `Land()` được gọi từ animator đúng frame chạm đất). Puzzle: bắn từ callback `onComplete` của tween. `[API Duck]`
5. **Một feedback "toàn cục" + feedback "từng phần tử"**: Barbarian có `AttackFeedback` (một lần) + `IndividualAttackFeedback` (mỗi đòn), và **mỗi đòn sau nhanh hơn 0.1s** → combo có cảm giác dồn dập. Snake chạy feedback "ăn" lan dọc từng đốt thân. `[API Barbarian, Snake]`
6. **UI đảo ngược được bằng một player**: Cards UI chỉ dùng 2 MMF_Player, chạy xuôi để mở/chia bài, chạy ngược để đóng/thu bài; một số bước chỉ chạy một chiều. Nút bị **chặn khi chuỗi đang chạy** (`BlockerFeedbacks`). `[demos, API Cards UI]`
7. **Trạng thái liên tục = feedback "start" + "stop"** (Wheel, Falcon): bắt đầu khi trạng thái bắt đầu, hoàn nguyên khi kết thúc. Falcon giả tốc độ bằng texture trôi + wiggle + particle, không cần chuyển động thật. `[API Wheel, Falcon]`
8. **Rung màn hình là cả một họ công cụ** (Toaster: 22 kiểu) — chọn 1–2 kiểu, **giảm biên độ** so với demo (demo cố tình phóng đại), và **tắt được toàn cục**. Rung **chỉ UI** là một lựa chọn riêng. `[screen-shakes]`
9. **Feedback nguyên tử, ghép bằng player**; "tất cả trong một" (Blob) khó bảo trì hơn. `[demos]`
10. **Cường độ (intensity) là tham số**: `PlayFeedbacks(position, intensity)`; dùng **Intensity Interval** để mỗi bậc combo bật một bộ feedback khác. `[core concepts]`
11. **Một tín hiệu điều khiển nhiều vật** (Brass/MMRadio: 1 broadcaster → mỗi khối một receiver) — rẻ hơn hàng trăm feedback riêng. `[mmradio, demos]`
12. **Có "phòng thí nghiệm feedback"** (MMF_PlayerDemo: mỗi nút một feedback) để chỉnh từng hiệu ứng cô lập. `[demos]`

---

## 2. Feel hoạt động thế nào

### 2.1 Thành phần
- **`MMF_Player`** (MonoBehaviour) giữ **danh sách feedback** chạy từ trên xuống; điều khiển play / pause / stop / reverse / skip.
- **Feedback** = một hành động nguyên tử (âm thanh, flash, scale…). Feel 6.1 có **196 feedback / 23 nhóm**. Từ v3.0 feedback **không phải MonoBehaviour** mà là class serializable nằm trong player (rẻ hơn).
- **Shaker**: một số feedback chỉ phát sự kiện; component "Shaker" trên mục tiêu (camera, volume…) nhận và áp dụng, lọc theo **Channel**.
- Lịch sử: `MMFeedbacks` (cũ) → `MMF_Player` (v3.0, 2/2022); feedback legacy bị xoá ở v4.0; springs thêm ở v4.1 (5/2024); chế độ Sequential thêm ở v6.0.

### 2.2 Thiết lập chính của player (mặc định)
| Nhóm | Field (mặc định) | Ghi chú |
|---|---|---|
| Khởi tạo | `InitializationMode = Start`, `AutoInitialization = true` | Docs: "phần lớn trường hợp dùng Start" |
| Chiều | `Direction = TopToBottom`, `AutoChangeDirectionOnEnd = false` | BottomToTop = chạy ngược |
| Kiểu chạy | `PlayMode = Parallel` (hoặc `Sequential` + `SequentialDelay`) | Parallel: mọi thứ bắt đầu cùng lúc trừ khi có Pause chia nhóm |
| Cường độ | `FeedbacksIntensity = 1` | 0 = không chạy gì |
| Thời gian | `PlayerTimescaleMode = Scaled`, `ForceTimescaleMode = false` (khi bật → Unscaled), `DurationMultiplier = 1`, `RandomizeDuration` (0.5–1.5) | UI nên Unscaled |
| Cổng | `CanPlay = true`, **`CanPlayWhileAlreadyPlaying = true`**, `CooldownDuration = 0`, `ChanceToPlay = 100` | Tắt "while playing" hoặc dùng cooldown để chống spam |
| Vòng đời | `StopFeedbacksOnDisable = false`, `RestoreInitialValuesOnDisable = false` | Bật cho UI pool/toggle |
| Toàn cục | static `GlobalMMFeedbacksActive = true` | Công tắc tắt toàn bộ |

### 2.3 Field chung của mọi feedback
- `Active`, `Label`, `Chance = 100`; static **`FeedbackTypeAuthorized`** (chặn một loại feedback ở mọi nơi — dùng cho "Reduce motion").
- **Ngẫu nhiên**: `RandomizeOutput` (×0.8–1), `RandomizeDuration` (×0.5–2) — tắt mặc định.
- **Timing** (`MMFeedbackTiming`): `TimescaleMode = Scaled`, `InitialDelay = 0`, `CooldownDuration = 0`, `InterruptsOnStop = true`, `NumberOfRepeats = 0`, `RepeatForever`, `DelayBetweenRepeats = 1`, `LimitPlayCount` (max 3), `ExcludeFromHoldingPauses`, `ContributeToTotalDuration = true`, **`MMFeedbacksDirectionCondition = Always`** (Always / OnlyWhenForwards / OnlyWhenBackwards), **`PlayDirection`** (Follow / Opposite / AlwaysNormal / AlwaysRewind), `ConstantIntensity`, **`UseIntensityInterval`** (min/max).
- **Target acquisition** tự động: Self / AnyChild / ChildAtIndex / Parent / Reference holder.

### 2.4 Mô hình thời gian
- **Pause** (`MMF_Pause`, mặc định 1s): nhóm dưới bắt đầu sau khi pause trôi qua.
- **Holding Pause**: chờ **mọi** feedback phía trên xong rồi mới đếm pause (ví dụ: feedback chậm nhất 2s + pause 0.25s → nhóm sau bắt đầu ở 2.25s). Feedback đánh dấu `ExcludeFromHoldingPauses` không bị chờ.
- **Script-driven pause**: player dừng tới khi code gọi `ResumeFeedbacks()` (có tự resume sau `AutoResumeAfter`, mặc định 0.25s).
- **Looper** (mặc định 2 vòng) quay lại pause gần nhất / Looper Start / đầu danh sách.
- **Đảo chiều** không chỉ đảo thứ tự danh sách mà **chạy ngược cả curve** của từng feedback.
- Thời lượng cuối của một feedback = `duration × random(feedback) × DurationMultiplier(player) × random(player)`.
- Spring không có thời lượng cố định → field `DeclaredDuration` để holding pause biết chờ bao lâu.
- `SkipToTheEnd()` đặt trạng thái cuối, **có thể mất tới 3 frame**.

### 2.5 Cường độ, channel, sự kiện
- **Đường đi của intensity:** `PlayFeedbacks(pos, intensity)` → nhân falloff theo khoảng cách (nếu bật) → từng feedback áp `ConstantIntensity` + random → cổng Intensity Interval → curve chuẩn hoá 0–1 rồi remap (`RemapZero`/`RemapOne`) → `RelativeValues` cộng vào giá trị gốc.
- **Channel**: feedback chỉ điều khiển shaker/spring cùng channel (số nguyên hoặc **MMChannel asset** — docs khuyên dùng asset vì dễ đọc).
- **Điều khiển từ xa**: `MMF_PlayerEvent.Trigger(channel, …, mode)` với các mode Play / Stop / Pause / Resume / PlayInReverse / SkipToTheEnd / RestoreInitialValues…
- **Sự kiện**: OnPlay, OnPause, OnResume, OnStop, OnChangeDirection, **OnComplete**, OnSkipToTheEnd…; chờ xong bằng `await PlayFeedbacksTask()` hoặc `yield return PlayFeedbacksCoroutine()`.
- **Context**: truyền dữ liệu `Dictionary<string, object>` vào feedback qua `PlayFeedbacks(..., context)`.

### 2.6 API hay dùng
`PlayFeedbacks()`, `PlayFeedbacks(pos, intensity)`, `PlayFeedbacksInReverse()`, `PlayFeedbacksTask()`, `StopFeedbacks()`, `PauseFeedbacks()/ResumeFeedbacks()`, `SetDirectionTopToBottom()/BottomToTop()`, `ChangeDirection()`, `SkipToTheEnd()`, `RestoreInitialValues()`, `ResetCooldown()`, `GetFeedbackOfType<T>()`, `AddFeedback(typeof(T))`, `ComputeCachedTotalDuration()` (sau khi đổi timing lúc chạy), `Initialization()` (sau khi đổi giá trị lúc chạy).

---

## 3. Từng demo

> Ký hiệu: **[KC]** = kiểm chứng (docs/API/changelog), **[SL]** = suy luận.

### Bảng tổng quan

| Demo | Thể hiện điều gì | Độ liên quan puzzle |
|---|---|---|
| **MMF_PlayerDemo** | Catalogue 150+ feedback, mỗi nút một feedback | ★★★ (ý tưởng "feedback lab") |
| **GettingStartedTutorial** | Tách logic/hình; xếp lớp feedback nhảy & tiếp đất | ★★★ |
| **Snake** | Feedback ở mọi cấp; lan dọc thân; spawn có "appear" | ★★★ |
| **Barbarians** | Global vs per-hit; tăng tốc mỗi đòn; floating damage text | ★★★ |
| **Blob** | Một feedback "tất cả trong một" có pause | ★ (chỉ cho cutscene) |
| **Bounce** | Chia pha charge → jump → landing; có bản không feedback để so sánh | ★★★ |
| **Brass** | Đồng bộ nhạc (MMAudioAnalyzer), 1 tín hiệu → nhiều vật | ★★ |
| **Cards UI** | UI đảo ngược, điều kiện theo chiều, chặn input, lật bài | ★★★ |
| **Duck** | Bắn feedback đúng frame chạm đất; rung chỉ UI | ★★★ |
| **Letters** | Animate xương 2D bằng feedback, không cần clip | ★★ (mascot) |
| **Tactical** | Floating text theo channel, 3 kiểu; text dạng icon | ★★★ |
| **Templates** | Prefab làm mẫu feedback | ★★ |
| **Toaster** | 22 kiểu rung màn hình | ★★ (chọn lọc) |
| **Wheel** | Feedback trạng thái start/stop | ★★ |
| **Falcon (HDRP)** | Giả tốc độ; volume overrides | ★ |
| **Strike (URP)** | Game bowling một chạm — nội dung "Chill Bowling" trên App Store | ★★★ (cùng thể loại một chạm) |
| **MMSoundManager Track / Playlist** | Track âm thanh, playlist | ★★★ |
| **MMProgressBar** | Thanh tiến độ có delay & bump | ★★★ |
| **MMSceneLoading A/B** | Chế độ load scene & chuyển cảnh | ★★ |
| **MMSquashAndStretch / Springs / MMFollowTarget** | Chuyển động đàn hồi, spring vs lerp | ★★★ |
| **Nice Vibrations demos** | Haptics: presets, emphasis, continuous, clips | ★★★ |

### MMF_PlayerDemo
- [KC] Gần như toàn bộ 150+ feedback, mỗi nút một MMF_Player (`UI > Canvas > FeedbackScrollRect > ScrollRectContent`) — làm tham chiếu cấu hình.
- **Bài học:** mỗi feedback là một hiệu ứng nguyên tử, chỉnh độc lập.
- **Puzzle:** làm một **scene "Feedback Lab"** — mỗi nút một hiệu ứng ở đúng kích thước ô/UI thật — để designer chỉnh thông số mà không cần chơi.

### GettingStartedTutorial
- [KC] Hero = cube có Rigidbody (khoá xoay); **hình nằm ở child "Model"**. `Physics.gravity = down × 30` ("nhảy cảm giác tốt hơn"); `JumpForce = 8` (Impulse). Tiếp đất = vận tốc dọc frame trước < 0 và hiện tại |vy| < 0.1.
- [KC] **JumpFeedback**: Rotation trên Model 0.5s, remap 0→90° trục X, curve S → cube lộn ¼ vòng; Sound (Cached) "Hat1".
- [KC] **LandingFeedback**: Sound "Bass1"; Cinemachine Impulse (6D Shake, velocity 1,1,1); Chromatic Aberration; Lens Distortion (remap 50).
- [KC] Gọi `JumpFeedback?.PlayFeedbacks()` cuối `Jump()`, `LandingFeedback?.PlayFeedbacks()` khi chạm đất; hoặc nối UnityEvent `OnJump`/`OnLand`.
- **Bài học:** gameplay ở parent, hình ở child; xếp lớp: chuyển động → âm thanh → camera → hậu kỳ. Âm thanh nhảy cao (hi-hat), tiếp đất trầm (bass).
- **Puzzle:** vị trí grid/logic ở root, mọi punch ở child "Visual".

### Snake
- [KC] Snake hoàn chỉnh, chỉ rẽ được một chiều, không thua; ăn vào thân thì mất đốt. "Feedback trên từng micro-interaction".
- [KC] Đầu rắn: `TurnFeedback`, `TeleportFeedback`, `TeleportOnceFeedback` (xuyên mép), `EatFeedback`, `LoseFeedback`; `Speed 5`, **tăng tốc ×2 trong 2s sau khi ăn**, tối đa 10 đốt, `MinTimeBetweenLostParts 2` (thời gian ân hạn).
- [KC] Coroutine `EatCo()` chạy feedback "ăn" trên **từng đốt**; `SnakeBodyPart.Eat(float intensity)`; đốt mới có `NewFeedback`. Thức ăn có `AppearFeedback`, `EatFeedback`, trễ 1s trước khi dời chỗ.
- [SL] Phần thưởng chạy như **sóng dọc thân** (lệch nhịp từng đốt, cường độ theo vị trí).
- **Bài học:** feedback ở mọi cấp (đầu, thân, mồi, spawner); gắn một chút gameplay vào feel (tăng tốc sau khi ăn).
- **Puzzle:** clear một nhóm → feedback **từng phần tử lệch nhịp** thay vì một vụ nổ chung; vật mới spawn có feedback "xuất hiện".

### Barbarians
- [KC] Combat kiểu ARPG, nhấn mạnh **floating damage text động** (v2.0).
- [KC] `Barbarian`: tìm mục tiêu xung quanh, lướt tới từng con. `AttackFeedback` (một lần khi bắt đầu chuỗi), `IndividualAttackFeedback` (mỗi đòn), `DeniedFeedback` (khi cooldown chặn). `CooldownDuration 0.1`, `AttackDuration 2.5`, curve `EaseInOutOverhead`, **`IntervalDecrement 0.1` → mỗi cú lướt sau nhanh hơn 0.1s**.
- [KC] `BarbarianEnemy.TakeDamage()` → `DamageFeedback`, `DamageCooldown 1s` (bất tử tạm thời). Có post-processing profile vignette đỏ.
- [SL] DamageFeedback gồm floating text + flicker/flash + rung.
- **Bài học:** tách feedback chuỗi tổng / từng đòn; **tăng tốc dần** để combo "dồn"; mục tiêu có cooldown riêng.
- **Puzzle:** chuỗi clear/combo: một feedback bắt đầu + mỗi ô một hit, **mỗi bước nhanh hơn**.

### Blob
- [KC] Blob di chuyển theo vòng khi giữ phím; một `MoveFeedback` dài có **pause** điều khiển tất cả; `CooldownDuration 1` + `DeniedFeedback`; 2 âm "note"; vignette tím.
- **Bài học:** cách sắp một timeline dài bằng Pause/Holding Pause — nhưng chính docs nói **tách nhỏ dễ bảo trì hơn**.
- **Puzzle:** chỉ dùng kiểu này cho đoạn kịch bản (intro level, cutscene booster), không cho thao tác cốt lõi.

### Bounce
- [KC] Space để nhảy; scene có `SceneWithFeedbacks` và `SceneWithoutFeedbacks` để **bật tắt so sánh** (v3.0).
- [KC] `BounceFeedbacks` được gọi từ **Animator event**: `PlayCharge()`, `PlayJump()`, `PlayLanding()`. `BounceRocks.Bounce()` tung các mảnh đá Rigidbody với lực/xoay ngẫu nhiên [SL: lúc tiếp đất].
- [KC] Docs gọi việc chia **charge → jump → landing** là "cách khuyến nghị" vì giảm phụ thuộc logic: đổi trọng lực hay nhảy analog không phải chỉnh lại timing feedback.
- **Bài học:** pha + sự kiện thật; và **luôn có bản không-feedback để so sánh**.
- **Puzzle:** tap-to-move = **nhấn (anticipation) → bay (launch) → tới nơi (impact)**, bắn bởi input down / tween start / tween complete — không bao giờ bằng delay đoán.

### Brass
- [KC] Scene đồng bộ nhạc bằng **MMAudioAnalyzer**: nguồn AudioSource / Global / Microphone; khuyên ~1–8 band; quy trình "Find Peaks" (play mode) → "Paste Peaks" để có giá trị chuẩn hoá; mỗi **Beat** có band, ngưỡng, khoảng tối thiểu, UnityEvent.
- [KC] Band #2 (cymbal) bật flash; khối visualizer dùng FloatController; khối sàn chuyển động qua **MMRadio** (1 broadcaster → receiver trên mỗi khối); `light.intensity = NormalizedBufferedAmplitude × 5`; sàn 10×10. **Không chạy trên WebGL**.
- **Bài học:** một tín hiệu điều khiển nhiều vật.
- **Puzzle:** bàn chơi/HUD "thở" theo nhịp nhạc rất rẻ; gợi ý "chạm vào đây" đập theo beat. (FFT tốn CPU — dùng ít band.)

### Cards UI
- [KC] Chia bài, lật từng lá, úp lại, gom lại, chơi lại — gần như toàn bằng feedback, **chỉ 2 MMF_Player**: một cho chia/gom, một trên mỗi lá cho lật/úp. Cả hai **chạy xuôi rồi ngược**; một số feedback chỉ chạy một chiều (Timing → Direction Condition / Play Direction). Docs thừa nhận tách chia và gom thành 2 player sẽ đơn giản hơn.
- [KC] `FeelCardsUIStackButton`: `StackFeedback` + **`BlockerFeedbacks`** — nút không làm gì khi bất kỳ blocker nào còn đang chạy.
- [KC] **MMTwoSidedUI**: Front/Back, `FlipAxis`, `ScaleThreshold 0`, `OnFlip` — scale > ngưỡng hiện mặt trước, < ngưỡng hiện mặt sau. [SL] Lật = Scale X đi qua 0, mặt tự đổi.
- [KC] Lá bài có video (v4.0) và hiệu ứng highlight khi chọn (sửa ở v5.8).
- **Bài học:** UI đảo ngược một player; bước có điều kiện theo chiều; chặn input khi chuỗi đang chạy.
- **Puzzle:** thẻ thưởng, chọn booster, sao kết màn, popup — **một player mỗi panel**, xuôi để mở, ngược để đóng; chặn nút bằng `IsPlaying`.

### Duck
- [KC] Demo nhảy 2D: `FeelDuckJumpStartFeedback`, `FeelDuckLandingFeedback`; `CooldownDuration 1` + `DeniedFeedback`. **`Land()` được animator gọi đúng frame vịt chạm đất** (docs: raycast/va chạm cũng được).
- [KC] Landing feedback có **Lens Distortion** (recipe gọi `GetFeedbackOfType<MMF_LensDistortion>()`); scene có **rung chỉ UI**; có âm "Quack".
- [SL] Squash & stretch khi bắt đầu nhảy/tiếp đất.
- **Bài học:** feedback va chạm đúng frame; rung UI thay vì rung thế giới.
- **Puzzle:** impact bắn từ `onComplete` của tween; phần thưởng HUD (coin, sao) chỉ rung container HUD.

### Letters
- [KC] Chữ F-E-E-L rig bằng 2D Bones (cần package 2D Animation), **không có animation clip**; feedback Rotation/Scale trên xương làm chúng sống động. `PlayF()`, `PlayE1()`… mỗi chữ một player.
- **Puzzle:** phản ứng "sống" (lắc, gật, giãn) cho mascot/chướng ngại có rig mà không cần làm clip.

### Tactical
- [KC] Giữ Space để bắn; `CooldownDuration 0.1`/phát, băng đạn 15; `ShootFeedback`, `ShootStopFeedback`, `ReloadFeedback`.
- [KC] **3 MMFloatingTextSpawner** dưới `Managers`, mỗi người bắn một spawner theo **Channel**: (1) text cơ bản, (2) text tự chạy MMF_Player riêng animate xoay/vị trí/màu/giãn chữ, (3) TextMeshPro. Recipe đổi giá trị thành `<sprite="FeelDuck" index=0>` → **floating icon**.
- **Bài học:** mỗi kiểu text một spawner/channel; giá trị truyền qua intensity; có nhịp riêng cho "dừng bắn" và "nạp đạn".
- **Puzzle:** spawner "+10", spawner "COMBO x3", spawner icon coin/gem (TMP sprite).

### Templates (Damage, Guns)
- [KC] Gói `FeelTemplatesDemo.unitypackage`: prefab dùng làm mẫu MMF_Player. (Changelog 3.4 nói demo bị thay bằng Unity Presets — docs vẫn liệt kê.)
- **Puzzle:** chuẩn hoá các mẫu "TileHit", "TileClear", "UIButton", "RewardPopup" bằng Preset / Copy All.

### Toaster
- [KC] Menu chơi **22 kiểu rung màn hình**; scene **không có script** (thuần feedback); có thể tắt feedback của chính cái lò nướng. Các họ:
  - Vị trí camera: Camera Shake (MMCameraShaker/MMWiggle), Cinemachine Impulse/Noise, Position Shake.
  - Xoay camera; FOV / orthographic size / zoom.
  - **View shake** qua render texture (tác giả thích nhất).
  - Hậu kỳ: lens distortion, chromatic aberration, vignette, color grading, bloom.
  - **Rung chỉ UI**.
- [KC] Thông số tài liệu: Camera Shake lên Cinemachine **0.3s / amplitude 2 / frequency 40**; **UI shake: container có MMWiggle + MMCameraShaker, 0.5s / amplitude 20 / frequency 0.03**. Nhiều preset **cố tình phóng đại**.
- [KC] Tắt một loại toàn cục: `MMF_CinemachineImpulse.FeedbackTypeAuthorized = false`.
- **Bài học:** shake là họ công cụ; giảm biên độ; mỗi loại một ý nghĩa; có công tắc toàn cục.
- **Puzzle:** tối đa 1–2 kiểu: rung UI cho phần thưởng, scale-shake nhỏ trên bàn cho clear lớn; nối "Reduce motion" vào `FeedbackTypeAuthorized`.

### Wheel
- [KC] `RotationSpeed 20`; `TurnFeedback` phát ở frame đầu bánh xe bắt đầu quay, `TurnStopFeedback` khi dừng.
- **Bài học:** feedback "trong lúc": vào trạng thái khi bắt đầu, hoàn nguyên khi kết thúc.
- **Puzzle:** băng chuyền đang chạy / bộ đếm giờ áp lực → player start (âm loop, wiggle) + player stop (hoàn nguyên).

### Falcon (HDRP)
- [KC] Space để lái qua sa mạc. `SetCar(bool)` bật/tắt cùng lúc: danh sách MMWiggle + wiggle camera, texture mặt đất trôi (MMPanningTexture), particle loop (đá), particle emitter (gió, khói), bánh xe tự quay, + `DriveFeedback`/`StopFeedback`. Scene trình diễn mọi feedback volume HDRP.
- **Bài học:** giả cảm giác tốc độ không cần di chuyển thật.
- **Puzzle:** băng chuyền "chạy nhanh" bằng texture trôi + item lắc nhẹ + particle, bật tắt như một trạng thái.

### Strike (URP)
- [KC] Bowling mini, Space để ném, có điểm; điều khiển URP Volume overrides. Link App Store trong docs là **"Chill Bowling"** của More Mountains (một chạm, không quảng cáo). Trang Feel nói Chill Bowling và Chill Snake "hoàn toàn chạy bằng Feel" và **code có kèm trong asset**.
- [KC] Mâu thuẫn phiên bản: docs nói Strike có từ v2.3, changelog 2.3 chỉ ghi Falcon.
- **Bài học:** đây là game mobile một chạm đã ship, juice hoàn toàn bằng Feel → **tham chiếu gần thể loại nhất** nếu mua Feel.

### MMSoundManager Track Control & Playlist Manager
- Xem mục 8.1.

### MMProgressBar · MMSceneLoading · MMSquashAndStretch · Springs · MMFollowTargetDemo · MMTweenPlotter
- Xem mục 7 và 8.

### Nice Vibrations demos
- [KC] 7 demo: Wobble, Ball, Continuous, HapticClips, Emphasis, Car, Base Presets. Xem mục 10.

### MMTools demos khác (ít liên quan puzzle)
- MMBezierLineRenderer (line nối điểm — puzzle nối dây), MMControlsDemo (điều khiển cảm ứng), MMDebugMenu (menu chỉnh số liệu trên máy), MMGhostCamera, MMGizmo, MMGyroscope (parallax theo con quay), MMObservable (giá trị phản ứng không cần Update), MMOrbitalCamera, MMRadio, MMPrototypeTextures, ParallaxUI.

---

## 4. Mẫu thiết kế rút ra từ các demo

| Mẫu | Demo nguồn | Áp dụng puzzle |
|---|---|---|
| **Denied feedback + cooldown** cho mọi input | Blob, Duck, Barbarian, Bounce | Chạm sai / ô khoá / spam → luôn có phản hồi "không được" |
| **Logic root / Visual child** | Getting Started | Grid & hitbox không bao giờ lệch vì punch |
| **Pha bắn bởi sự kiện thật** | Bounce, Duck | Press → launch (tween start) → impact (onComplete) |
| **Global + per-item, lệch nhịp, tăng tốc** | Barbarian, Snake | Clear nhóm/combo lan từng ô, mỗi bước nhanh hơn |
| **Spawn có feedback "appear"** | Snake (food, body part) | Ô mới rơi xuống/xuất hiện có pop |
| **Một player đảo ngược** | Cards UI | Popup/panel mở = xuôi, đóng = ngược |
| **Chặn input khi chuỗi đang chạy** | Cards UI (BlockerFeedbacks) | Chống bấm đôi khi chuyển cảnh — *không* áp cho thao tác gameplay cốt lõi (xem guide 2.4) |
| **Start/stop state** | Wheel, Falcon | Băng chuyền, đếm giờ, trạng thái "sốt" |
| **Một tín hiệu → nhiều vật** | Brass, MMRadio | Cả bàn "thở", mờ khi thua |
| **Text theo channel/kiểu** | Tactical | Spawner riêng cho điểm, combo, icon |
| **Shake chọn lọc, tắt được** | Toaster | 1–2 kiểu, Reduce motion |
| **Mẫu dùng lại** | Templates | Preset TileHit / TileClear / UIButton |
| **So sánh có/không feedback** | Bounce | Có công tắc debug tắt toàn bộ feedback để kiểm tra |
| **Feedback lab** | MMF_PlayerDemo | Scene chỉnh từng hiệu ứng |

---

## 5. Công thức cho game puzzle

> Giá trị **[mặc định]** = default API Feel; **[đề xuất]** = điểm khởi đầu, chỉnh khi playtest. Viết theo thuật ngữ Feel nhưng làm được bằng DOTween (mục 11).

**R1. Chạm chọn (nhấc vật lên)** — pha: nhấn → nhấc → giữ. Player trên child Visual:
- Squash & Stretch 0.08–0.12s, remap 1→1.15 [đề xuất]
- Position (AlongCurve Y) 0.12s, nhấc 0.1–0.2 đơn vị
- Sound pitch 0.95–1.05, MaxConcurrentInstances 3 [mặc định]
- Haptic Selection/Light
- Bỏ chọn = **cùng player chạy ngược**; sound đặt OnlyWhenForwards.

**R2. Chạm bị từ chối** (DeniedFeedback):
- Position trục X với curve lắc mặc định (0, 1, −1, 0), remap 0→0.08, 0.2s — hoặc Position Shake 0.2s
- Âm "nope" mềm; haptic Warning
- Cooldown player 0.2s để không bắn liên thanh.

**R3. Di chuyển vào slot** (tách pha kiểu Bounce) — 3 player bắn bởi sự kiện:
- **Charge** (input down): squash 0.06s
- **Launch** (tween start): stretch theo hướng bay + whoosh
- **Land** (tween onComplete): squash YtoXZ 0.15s remap 1→1.2; particle (pool, size ≥ số tối đa trên màn); âm va chạm; haptic Light; Scale Shake nhỏ (speed 20, range ≈0.05) trên slot nhận.
- Không bao giờ căn landing bằng Pause/InitialDelay.

**R4. Clear / cascade** (Snake EatCo + Barbarian global/individual):
- **Player tổng** (một lần): rung UI/bàn nhỏ, âm pitch tăng theo combo, haptic Medium
- **Player từng ô** `PlayFeedbacks(tilePos, intensity)` lệch 0.03–0.06s × index: scale punch 1.2 → scale về 0 trong 0.15s, particle pool, floating text điểm
- Rồi **Holding Pause** → feedback rơi/lấp đầy lệch nhịp + squash khi chạm đáy.

**R5. Leo thang combo** (Barbarian tăng tốc + Intensity Interval):
- `intensity = comboLevel`; mỗi feedback có `UseIntensityInterval`.
- Bậc 1: chỉ âm. Bậc 2: + Camera Zoom nhỏ (relative). Bậc 3: + Freeze Frame **0.03–0.06s** (mặc định 0.1 quá dài cho puzzle), Flash, haptic Heavy.
- **Pitch tăng "miễn phí"**: MMSoundManager Sound với `SequentialOrder = true`, `SequentialOrderHoldLast = true`, `SequentialOrderHoldCooldownDuration = 2` [mặc định] — đặt các nốt theo thứ tự tăng dần trong danh sách clip.
- Mỗi bước rút ngắn khoảng cách (ý tưởng IntervalDecrement).

**R6. Điểm bay lên** (Tactical/Barbarians):
- Spawner [mặc định]: PoolSize 20; Lifetime 1s; bay Y 0→**5 đơn vị** (quá xa cho UI/board orthographic — giảm); scale curve (0,0)(0.15,1)(0.85,1)(1,0); alpha curve (0,0)(0.2,1)(0.8,1)(1,0).
- Bật **IntensityImpactsScale** → điểm lớn thì chữ to.
- Mỗi kiểu một channel (MMChannel asset); icon coin/gem bằng `<sprite=…>`.

**R7. Panel / thẻ thưởng đảo ngược** (Cards UI):
- Một player mỗi popup: SetActive (tự đảo khi chạy ngược) + CanvasGroup (0.2s mặc định) + Scale 0.8→1 (EaseOutOverhead) + sound OnlyWhenForwards.
- Scale phải là pop-in **nhỏ → lớn hơn → gốc** (đỉnh ~1.03–1.05 cho popup). Quãng 0.8→1 ngắn nên overshoot mặc định gần như không thấy vượt — tăng overshoot hoặc dùng hai bước (guide 4.7; FeedbackKit: Scale Tween `Shape = PeakThenSettle`).
- Mở = TopToBottom, đóng = BottomToTop.
- Lật thẻ: Scale X 1 → 0 → 1 với MMTwoSidedUI ngưỡng 0.
- Chặn nút khi `IsPlaying` (BlockerFeedbacks).

**R8. Trạng thái liên tục** (Wheel/Falcon): player start (âm loop có FadeIn/FadeOutOnStop, ScaleShake PermanentShake hoặc MMWiggle, texture băng chuyền trôi, Looper/RepeatForever) + player stop (StopFeedbacks + RestoreInitialValues).

**R9. Thắng màn** — tuần tự với MMF_Pause 0.15–0.25s (mặc định 1s quá dài):
1. Rung container UI (giá trị recipe UI shake, giảm biên độ)
2. Sao hiện từng cái: scale punch + particle + nốt cao dần + haptic Success
3. Holding Pause
4. TMP Text Reveal
5. Nút fade in
- **Tua được** bằng `SkipToTheEnd()` (có thể mất tới 3 frame — đừng tắt player ngay).

---

## 6. Bẫy giá trị mặc định

| Mặc định | Vấn đề với puzzle | Sửa |
|---|---|---|
| **MMF_Scale**: 0.2s, remap 1→2, curve đỉnh 1.5 tại 30% | Đỉnh ≈ **2.5×** [SL, remap tuyến tính] — quá to cho ô | RemapCurveOne ≈ 1.12–1.2 |
| **MMF_SquashAndStretch**: cùng mặc định | Như trên | Như trên |
| **AllowAdditivePlays = false** (Scale/Position/Rotation) | Chạm nhanh liên tiếp bị **bỏ qua** giữa animation | Bật additive, hoặc dùng **Scale Spring (Bump)** — spam an toàn |
| **CanPlayWhileAlreadyPlaying = true** (player) | Spam chồng feedback | Cooldown ở player hoặc Timing |
| **MMF_FreezeFrame 0.1s** | Quá dài cho puzzle | 0.03–0.06s, chỉ combo lớn |
| **MMF_Pause 1s** | Chuỗi ăn mừng lê thê | 0.15–0.25s |
| **Floating text bay 5 đơn vị** | Quá xa trên UI/board | Giảm theo tỉ lệ màn |
| **Mọi feedback TimescaleMode = Scaled** | UI đứng hình khi freeze/pause | UI dùng Unscaled |
| **Sound MinPitch = MaxPitch = 1** | Không có biến thể | 0.95–1.05 (noise) / nhỏ hơn cho âm tonal |
| **DoNotPlayIfClipAlreadyPlaying = false**, MaxConcurrent 3 | OK, nhưng cần chọn có chủ đích | 2–4 cho âm lặp nhiều |
| **NV v4 preset Android dài gấp đôi v1.x** (Light 40 vs 20ms…) | Rung "nặng" hơn sau khi nâng cấp | Chỉnh lại trên máy Android yếu |
| **Spring Damping 0.4 / Frequency 6** | Khá nảy | Nút: damping cao hơn (≈0.6–0.8) |

---

## 7. Springs & Tweens trong Feel

### 7.1 Springs (v4.1+)
- **Không có thời lượng**: `MoveTo` (đi tới giá trị) hoặc `Bump` (cộng vào vận tốc). Spam thoải mái, các lần bump **cộng dồn** có giới hạn; không phụ thuộc frame rate.
- `Damping` 0–1 (gần 0 = lỏng, dao động lâu; gần 1 = căng, dừng nhanh) mặc định **0.4**; `Frequency` (dao động/giây) mặc định **6**. Clamp min/max, có "bounce" tại biên.
- Kiểu: `MMSpringFloat / Vector2 / Vector3 / Vector4 / Color`; API `UpdateSpringValue(dt)`, `MoveTo`, `MoveToAdditive`, `MoveToRandom`, `MoveToInstant`, `Bump`, `BumpRandom`, `Stop`, `Finish`, `RestoreInitialValue`.
- **70+ component spring** (Position, Rotation, Scale, SquashAndStretch, RectTransform position/size, Image alpha/color/fill, TMP font size/spacing/color, Camera FOV/ortho, AudioSource pitch/volume, Light, TimeScale…). Tự tắt khi vận tốc < 0.001, có sự kiện `OnEquilibriumReached`.
- Feedback spring: `Command = Bump`, `BumpAmount 75`, có thể override damping (0.8) / frequency (5); **`DeclaredDuration`** để holding pause biết chờ.
- **Độ lớn bump** lớn so với giá trị (scale 20–30, FOV 1000, lens distortion 5000) vì cộng vào vận tốc; spring lỏng chỉ cần bump nhỏ.
- **MMSquashAndStretch** (theo vận tốc): `Intensity 0.02`, `MaximumVelocity 1`, scale tối thiểu 0.5 / tối đa 2, `RotateToMatchDirection`, tự squash khi dừng (0.25–0.5s), tuỳ chọn spring (damping 0.3, freq 3).
- **MMFollowTarget**: chế độ RegularLerp / **MMLerp** (mặc định) / MMSpring, speed 10, spring damping 0.3 / freq 3. Demo so với lerp thường: MMFollowTarget rất mượt; MMSpring thêm "sức sống". → Vật đang kéo trễ nhẹ sau ngón tay.

### 7.2 Tweens
- **MMTween**: 33 curve (Linear, AntiLinear, AlmostIdentity + In/Out/InOut của Quad, Cubic, Quart, Quint, Sine, Bounce, **Overhead** (= Back/overshoot), Expo, Elastic, Circular). Hàm `Tween(t, t0, t1, a, b, curve)` cho float/Vector/Quaternion; coroutine `MoveTransform`, `MoveRectTransform`.
- Feedback dùng **curve chuẩn hoá 0–1 + Remap** (Zero/One), absolute hoặc relative.

### 7.3 Khi nào dùng gì
- **Tween**: cần biết trước thời lượng — chuỗi có pause, chuyển cảnh UI, đếm số, fade, mở/đóng đảo ngược.
- **Spring**: bị kích hoạt lặp/spam hoặc mục tiêu thay đổi liên tục — bump khi chạm, kéo theo ngón tay, hover, bộ đếm combo.
- Trộn trong một player tuần tự: đặt `DeclaredDuration` cho spring hoặc `ExcludeFromHoldingPauses`.

---

## 8. MMTools hữu ích cho mobile puzzle

### 8.1 MMSoundManager
- Singleton bền, dựa trên AudioMixer; **track**: Master, Music, SFX, UI, Other (mixer group có volume exposed).
- **Pool** AudioSource `PoolSize 10` (≥ số âm tối đa cùng lúc), `PoolCanExpand = true` (tắt để không tạo lúc chạy).
- **Lưu cài đặt**: `AutoLoad = true`, **`AutoSave = false`** → phải gọi `SaveSettings()` (hoặc feedback Save/Load). Hàm sẵn cho màn Settings: `MuteMusic/UnmuteMusic`, `SetVolumeMusic/Sfx/UI/Master(float)`.
- Fade track/âm, solo một track hoặc mọi track. **Không có ducking** — dùng solo, Track Fade, hoặc AudioMixer Snapshot Transition.
- **Feedback `MMF_MMSoundManagerSound`**: random clip (`RandomSfx`), **`SequentialOrder` + `HoldLast` + cooldown 2s** (thang pitch combo), `RandomUnique`, Min/Max Volume & Pitch (mặc định 1/1), **`MaximumConcurrentInstances 3`**, `DoNotPlayIfClipAlreadyPlaying`, fade in/out, track, priority.
- **Playlist**: thứ tự Normal/Reverse/Random/RandomUnique; PlayForever/Once/X lần; fade 1s; tự xử lý pause app; tuỳ chọn gắn pitch theo timescale.
- **Puzzle [SL]**: click UI vào track UI, match/clear vào SFX; slider settings gọi `SetVolumeX` + `SaveSettings()` khi đóng panel.

### 8.2 MMProgressBar
- FillMode: LocalScale / FillAmount / Width / Height / Anchor; hướng; **UnscaledTime** mặc định.
- Thanh chính lerp (speed 15 hoặc fixed duration 0.2s); **thanh trễ** tăng/giảm chờ 1s rồi đuổi theo.
- **Bump khi đổi**: 0.2s, scale curve đỉnh 1.05, đổi màu trắng thoáng qua.
- **`UpdateBar()`** = có animation + bump; **`SetBar()`** = đặt tức thì không bump (dùng cho khởi tạo/khôi phục).
- Sự kiện OnBump, bắt đầu/dừng tăng/giảm → nối sound + haptic. *(Danh sách field lấy từ bản MMTools cũ hơn — có thể khác chút trong 6.1.)*
- **Puzzle**: thanh mục tiêu, thanh sao; thanh trễ màu vàng cho "vừa được cộng".

### 8.3 MMSceneLoading
- Chế độ: Direct, DirectAdditive, MMSceneLoadingManager (tuần tự), **MMAdditiveSceneLoadingManager** (mặc định — màn loading load additive phủ lên).
- Additive: fade vào 0.25s, chờ 0.1s, chờ trước khi kích hoạt 0.25s, fade ra 0.2s, `ThreadPriority High`, **nội suy progress** (thanh không nhảy), tốc độ thanh theo từng khoảng progress.
- Chuỗi sự kiện đầy đủ (LoadStarted → … → ExitFade → UnloadSceneLoader) + **Hold** (giữ trước khi kích hoạt / trước khi fade ra — ví dụ chờ sinh level xong).
- **Bài học chuyển cảnh**: fade trước rồi mới load; progress nội suy; trễ ngắn để thanh chạm 100% thấy được; giữ trước fade ra tới khi level sẵn sàng. *(Với puzzle, vẫn nên đổi dữ liệu level trong cùng scene thay vì load scene — xem guide mục 10.)*

### 8.4 MMTimeManager
- Timescale **xếp chồng**: mỗi thay đổi đẩy vào stack, `Unfreeze()` lùi lại một bước, `Reset()` về bình thường. Docs: **luôn đi qua manager**, đặt `Time.timeScale` trực tiếp sẽ phá stack.
- Freeze Frame 0.1s (bỏ qua nếu timescale đã < 0.1); Timescale Modifier: Shake (tạm thời) / Change / Reset / Unfreeze, 0.5 trong 1s, có lerp.
- **Puzzle**: freeze 0.03–0.08s chỉ cho combo lớn; UI để Unscaled.

### 8.5 MMFloatingText
- Pool 20, nhiều prefab, channel; lifetime, offset; curve chuyển động/scale/alpha/màu riêng từng trục; căn hướng (cố định / theo hướng ban đầu / theo hướng bay); **intensity ảnh hưởng lifetime/chuyển động/scale**; bản TMP.
- Feedback: `Value`, **`UseIntensityAsValue`** + làm tròn, ép màu, ép lifetime, vị trí theo feedback/play/target.

### 8.6 Khác
| Tool | Dùng cho puzzle |
|---|---|
| **MMSequencer** (BPM 160, 8 bước) + `Timing.Quantized`/`TargetBPM` | Cascade rơi đúng nhịp nhạc (1/8 beat) |
| **MMAudioAnalyzer** | Pulse gợi ý/nút theo beat (tốn FFT) |
| **MMRadio** | Một tín hiệu điều khiển cả bàn |
| **MMBlink / MMWiggle** | Gợi ý nhấp nháy, lắc khi idle |
| **MMFollowTarget (spring)** | Vật kéo trễ sau ngón tay |
| **MMObservable** | Điểm/số nước/HUD phản ứng không cần Update |
| **MMDebugMenu** + **MMFPSUnlock** | Chỉnh thông số trên máy thật; đặt FPS |
| **MMParallaxUI (gyro)** | Chiều sâu cho menu |
| **MMAspectRatioSafeZones** | Kiểm layout quanh tai thỏ |
| **MMBezierLineRenderer** | Puzzle nối dây |
| **MMTwoSidedUI** | Lật thẻ |

**Xếp hạng cho mobile puzzle [SL]:** 1) MMSoundManager + Sound feedback; 2) Nice Vibrations v4 presets/emphasis + `hapticsEnabled`; 3) MMFloatingText; 4) MMProgressBar; 5) MMTween + TMP Count To; 6) MMTimeManager; 7) Additive scene loading; 8) MMBlink/MMWiggle; 9) MMFollowTarget spring; 10) MMObservable; 11) MMDebugMenu.

---

## 9. Danh mục feedback (rút gọn)

196 feedback / 23 nhóm. ★ = liên quan nhất cho mobile UI/puzzle.

- **Transform (16):** Position★, Rotation★, Scale★, Position Spring★, Scale Spring★, Squash & Stretch Spring★, SquashAndStretch★, Wiggle★, DestinationTransform★, Scale Shake★, Position Shake, Rotation Shake, Rotation Spring, Rotate Position Around, Look At, Set Parent.
- **UI (22):** CanvasGroup★, CanvasGroupBlocksRaycasts★, Image★, Image Fill★, Floating Text★, Graphic, Graphic CrossFade, Image Alpha/Material/Sprite/Texture Offset/Scale, Raycast Target, RectTransform Anchor/Pivot/Offset/SizeDelta, Text, TextColor, TextFontSize, VideoPlayer.
- **TextMesh Pro (15):** Count To★, Text★, Color★, TextReveal★, Font Size, Character/Word/Line/Paragraph Spacing, Alpha, Outline, Dilate, Softness, Count To Long.
- **Audio (19):** MMSoundManager Sound★, Sound★, Track Control/Fade, Sound Fade/Control, Save and Load, AudioSource Pitch/Volume, filters, Mixer Snapshot…
- **Nice Vibrations (5):** Preset★, Emphasis★, Clip, Continuous, Control.
- **Particles (4):** Particles Play★, Particles Instantiation★ (có cache/pool), Visual Effect, VFX Set Property.
- **Renderer (15):** Flicker★, MMBlink★, SpriteRenderer★, Material, Shader Controller…
- **Camera (12):** Camera Shake★, Flash★, Fade★, Orthographic Size★, Zoom, FOV, Cinemachine…
- **Time (2):** Freeze Frame★, Time Modifier.
- **Pause/Loop (4):** Pause★, Holding Pause★, Looper★, Looper Start.
- **Feedbacks (3):** MMF Player Chain★, Feedbacks Player, Player Control.
- **Events (3):** Unity Events★, MMGameEvent, Random Unity Events.
- **GameObject (13):** Set Active★, Property★, Instantiate, Destroy, Enable Behaviour…
- **Springs (5):** Float★, Vector2/3/4, Color.
- **Animation (5):** Animation Parameter★…
- Nhóm khác: Scene (LoadScene/UnloadScene), Post Processing / URP / HDRP Volume, UI Toolkit (17), Various (Light), Debug.

---

## 10. Nice Vibrations: v1.x so với v4

FeedbackKit không phụ thuộc thư viện haptics nào — game gán adapter qua `FeedbackServices.Haptics` (mẫu cho cả v1.x và v4 trong `FeedbackKit/README.md` mục 8). Phần dưới giúp chọn bản Nice Vibrations khi làm adapter.

**Lưu ý về v1.x (`MMVibrationManager`):** `HapticsSupported()` chỉ trả true trên iOS (Android luôn false) — nhánh Android của `Haptic()` không kiểm tra hàm này nên Android vẫn rung; đừng dùng hàm này để quyết định có rung hay không. Tránh `Handheld.Vibrate()` (rung dài, rè, không chỉnh được).

### 10.1 v1.x so với v4

| Khả năng | v1.x (bản cũ) | v4 (trong Feel / Lofelt mã nguồn mở) |
|---|---|---|
| Namespace / API | `MoreMountains.NiceVibrations.MMVibrationManager` | `Lofelt.NiceVibrations`: `HapticPatterns`, `HapticController`, `HapticSource` |
| Preset | 7 | 9 (+Rigid, Soft) + None |
| Cường độ | Chỉ Android API 26+ | Emphasis/Constant amplitude, `clipLevel`, `outputLevel` |
| Sharpness/tần số | Không | Có (iOS Core Haptics) |
| Rung liên tục, biến thiên | Không | Constant, clip, điều chế thời gian thực |
| Pattern tự thiết kế | Mảng long[] Android | Clip `.haptic` đa nền tảng (80+ mẫu) |
| Loop/seek/stop | Chỉ Android | Có (trừ preset iOS) |
| Công tắc / volume toàn cục | Không | `hapticsEnabled`, `outputLevel` |
| Ưu tiên | Không | `HapticSource.priority` 0–255 (chỉ 1 clip một lúc) |
| Kiểm tra thiết bị | Sai trên Android | `DeviceCapabilities` |
| Fallback | iOS → Handheld.Vibrate | `fallbackPreset`, tự hạ cấp |

- **v4 API chính:** `HapticPatterns.PlayPreset(PresetType)`, `PlayEmphasis(amplitude, frequency)` (cú 0.1s), `PlayConstant(amp, freq, duration)`; `HapticController.Init()` (**gọi ở Awake** — nếu không, iOS có thể mất haptic ~30s đầu), `Play(clip)`, `Stop()`, `hapticsEnabled`.
- Tầng thiết bị: iOS đầy đủ từ iPhone 8 / iOS 13; Android cần API 26 + amplitude control cho clip; Android không chỉnh tần số; loop giật trước Android 12.
- Lưu ý v4: `LofeltHaptics.Release()` trước quảng cáo AdMob và `Initialize()` sau (sửa lỗi mất âm thanh iOS) — game có SDK quảng cáo (AdMob, AppLovin MAX…) nên kiểm tra có bị ảnh hưởng không.
- Repo Lofelt (MIT) **đã archive 8/2025** — dùng được độc lập không cần mua Feel, nhưng không còn được cập nhật.

### 10.2 Chuyển một project từ v1.x lên v4
1. `using MoreMountains.NiceVibrations` → `using Lofelt.NiceVibrations`.
2. `Haptic(HapticTypes.X)` → `HapticPatterns.PlayPreset(HapticPatterns.PresetType.X)` (tên giống nhau).
3. `Vibrate()` → `PlayPreset(MediumImpact)` [SL]; `AndroidVibrate(ms, amp)` → `PlayConstant(amp/255f, 0, ms/1000f)` [SL].
4. `iOSInitializeHaptics()` → `HapticController.Init()` trong Awake; cờ bật/tắt rung của game → `HapticController.hapticsEnabled` (hoặc `FeedbackGlobal.HapticsEnabled` nếu đi qua FeedbackKit).
5. Xoá `MMVibrationManager.cs` + plugin iOS cũ; kiểm tra trùng manifest Android; **không** giải nén gói NV 3.9 cũ của Feel cạnh v1.x (trùng class).
6. **Chỉnh lại cảm giác**: preset Android v4 dài gấp đôi (Light 40 / Medium 80 / Heavy 160ms) so với v1.x → test trên Android yếu.

---

## 11. Quyết định: không dùng Feel, dùng FeedbackKit

**Đã chọn:** tự làm hệ thống feedback nhỏ trên DOTween — **FeedbackKit** (thư mục `FeedbackKit/`).

| Phương án đã cân nhắc | Ưu | Nhược |
|---|---|---|
| A. Mua Feel | Đầy đủ nhất; đọc được thông số thật trong demo; ít code | Tốn phí; nặng; phụ thuộc bên thứ ba; Feel 6.1 cần Unity 6000.5 nên project trên Unity cũ hơn phải tìm bản tương thích; thêm package |
| **B. FeedbackKit trên DOTween (đã chọn)** | Không phụ thuộc mới ngoài DOTween; nhẹ; kiểm soát hoàn toàn; dùng lại cho mọi game | Tốn công viết; không có demo thật để tham chiếu |
| C. B + nâng Nice Vibrations lên v4 | Haptics tốt hơn | NV v4 (Lofelt) đã ngừng cập nhật — với FeedbackKit đây chỉ là chọn adapter, quyết định theo từng game |

Muốn có **thông số thật** của các demo Feel thì có thể mua Feel và mở trong một project riêng chỉ để tham khảo.

### Kiến trúc tối giản kiểu Feel

> **Đã hiện thực trong FeedbackKit (bản đầu):** `FeedbackPlayer` (Parallel/Sequential, xuôi/ngược, intensity, step, cooldown, Scaled/Unscaled, StopOnDisable/RestoreOnDisable, UnityEvent, SkipToEnd, ComputeTotalDuration), `Feedback` + `FeedbackTiming` (delay, cooldown, repeats, điều kiện chiều, ConstantIntensity, khoảng intensity, random intensity, ExcludeFromHoldingPause, Chance), Pause / HoldingPause, các feedback Transform/UI/Audio/Haptic/Particles/Event/SetActive, `JuicyButton`, `FeedbackLab`, Inspector có menu thêm feedback.
> **Chưa có:** spring, looper, freeze frame/time scale có stack, floating text, công tắc theo từng loại feedback, chế độ additive. Danh sách bên dưới là bản thiết kế đầy đủ để làm tiếp.
- **`FeedbackPlayer : MonoBehaviour`**: `[SerializeReference] List<Feedback>`; `InitMode`, `PlayMode {Parallel, Sequential}`, `Direction {Forward, Reverse}`, `intensity`, `durationMultiplier`, `initialDelay`, `cooldown`, `allowReplayWhilePlaying`, `timeMode {Scaled, Unscaled}`, `restoreOnDisable`, `stopOnDisable`, UnityEvent `onPlay/onComplete/onStop`. API: `Play(intensity, ctx)`, `PlayReverse()`, `PlayAsync()`, `Stop()`, `SkipToEnd()`, `RestoreInitialValues()`, `IsPlaying`, `TotalDuration`. Static `GlobalEnabled` + hệ số "Reduce motion".
- **`[Serializable] abstract class Feedback`**: `active`, `label`, `chance`, `Timing` (initialDelay, cooldown, repeats, repeatDelay, interruptsOnStop, excludeFromHolding, contributesToTotal, `DirectionCondition {Always, ForwardOnly, ReverseOnly}`, `constantIntensity`, `intensityRange`, `timeMode`, `allowAdditive`), random intensity (0.8–1). Hook: `Duration`, `Init`, `OnPlay`, `OnStop`, `OnSkip`, `OnRestore`. Static `TypeEnabled` cho từng loại.
- **`TweenFeedback<TTarget,TValue>`**: mode {OverTime, Instant, ToDestination}, duration 0.2, ease (DOTween Ease hoặc AnimationCurve), remapZero/One, relative; lưu giá trị gốc ở Init, khôi phục ở OnRestore; khi đảo chiều dùng `t' = 1 − t`. Cụ thể: Scale, AnchoredPosition, Rotation, CanvasGroupAlpha, GraphicColor, ImageFill, TMPCountTo.
- **Spring**: struct `Spring1/2/3` (damping 0.4, freq 6, clamp), feedback Bump/MoveTo + `declaredDuration`; component tự tắt khi đứng yên.
- **Điều khiển**: `Pause`, `HoldingPause`, `Looper`, `LoopStart`.
- **Keo**: `PlaySound` (pitch/volume random, max instances, sequential), `Haptic`, `Particles` (pool), `UnityEventFeedback`, `SetActive`, `FreezeFrame` (qua time manager có stack).
- **Lịch chạy** (tính một lần khi Init): t0 = now + initialDelay×M; duyệt danh sách theo chiều; Pause cộng thời gian; HoldingPause = max(kết thúc các feedback không bị loại kể từ hold trước) + pause; Looper nhảy về; cổng kiểm theo thứ tự: global → type → active → chance → direction → intensity range → cooldown → additive/đang chạy; tick tất cả từ một `Update` trung tâm.

---

## 12. Việc còn mở & video nên xem

**Chưa kiểm chứng được:**
1. Danh sách feedback/thông số thật bên trong player của từng demo (chỉ Getting Started và lens distortion của Duck là chắc).
2. Tên và thông số 22 nút của Toaster.
3. Nội dung `Strike.cs` (tính điểm, feedback pin, override URP).
4. Phím điều khiển của Blob/Letters/Wheel/Barbarians; nhịp chia bài của Cards UI.
5. Field của các feedback Nice Vibrations trong Feel (API trống).

**Video nên xem tay** (có thể có thông số trên màn hình):
- "Feel Tutorial : Screen Shakes" — https://www.youtube.com/watch?v=0WyIlR0NA98 (Toaster)
- "Feel Tutorial : MMF Player" — https://www.youtube.com/watch?v=wef688wkbPc
- "Feel Tutorial : Feedback pauses, loops, shakers, direction, and more" — https://www.youtube.com/watch?v=pMOPgoTNYFk
- Bài devlog có áp dụng: https://rcxa.itch.io/vaults-and-vampires/devlog/662233/jumping-and-landing-feedback (squash theo độ cao rơi, rơi mạnh thì hạt lớn hơn, rung 6D tần số thấp ngắn).

**Mâu thuẫn trong docs:** mặc định Direction Condition (docs nói "only forward", API là `Always`); sự kiện "OnRevert" trong docs không tồn tại trong API (là `OnChangeDirection`); mô tả API của BounceManager copy từ Duck; ngày ra Strike.

---

## 13. Nguồn

**Feel docs** (gốc https://feel-docs.moremountains.com/): demos.html · index.html · getting-started.html · core-concepts.html · mmf-player.html · list_mmfeedbacks.html · creating_mmfeedbacks.html · screen-shakes.html · springs.html · recipes.html · tweens-events.html · nice-vibrations.html · mmtools.html · mmsoundmanager.html · mmaudioanalyzer.html · mmsequencer.html · mmradio.html · mm-progress-bar.html · mm-scene-loading.html · mmtimemanager.html · mm-floating-text.html · API: http://feel-docs.moremountains.com/API/ (class pages MMF_Player, MMFeedbacks, MMF_Feedback, MMFeedbackTiming, MMF_Pause/HoldingPause/Looper, MMF_Scale, MMF_Position, MMF_ScaleSpring, MMF_SquashAndStretchSpring, MMF_SpringFloat, MMSpringFloat/Vector3, MMSquashAndStretch, MMFollowTarget, MMTween, MMTweenType, MMSoundManager, các class demo: Snake, SnakeBodyPart, SnakeFood, Barbarian, BarbarianEnemy, Blob, BounceFeedbacks, BounceManager, BounceRocks, FeelBrass, FeelCardsUIStackButton, MMTwoSidedUI, Duck, LettersDemoManager, Tactical, Wheel, Falcon, GettingStartedTutorialHeroReference)

**Feel site & changelog:** https://feel.moremountains.com/ · https://feel.moremountains.com/feel-releases · Asset Store: https://assetstore.unity.com/packages/tools/particles-effects/feel-183370 · Chill Bowling: https://apps.apple.com/app/id1571411330

**Nice Vibrations:** https://nice-vibrations.moremountains.com/ · https://nice-vibrations-docs.moremountains.com/ · Lofelt NiceVibrations (GitHub, MIT, archived 8/2025) · mirror v1.2: github.com/UnityPackage/vibration