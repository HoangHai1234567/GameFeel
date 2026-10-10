# Game Feel Guide — Mobile Casual / Puzzle

> Tài liệu hướng dẫn cho agent (và cho người) khi làm hoặc cải thiện **game feel** của game mobile casual/puzzle: phản hồi khi tương tác, độ trễ, UI animation, tween, nút bấm, VFX, âm thanh, haptics, chuyển màn và loading.
>
> Tổng hợp từ: GDC talks, nghiên cứu học thuật về "juiciness", tài liệu nền tảng (Apple HIG, Material Design, Android Developers, Unity docs), code demo (deepnight/gamefeel, GDevelop game feel demo, Feel by More Mountains, Mix and Jam balatro-feel), thảo luận Reddit/Hacker News, và phân tích các game hit (Pixel Flow, Loop Sort, Colony Flow, Royal Match, Candy Crush, Block Blast, Hexa Sort…). Nguồn ở mục 15.
>
> **Cách đọc các con số:**
> - `[nguồn: …]` = giá trị hoặc nguyên tắc có trong nguồn được dẫn.
> - Số **không có nhãn** = giá trị khởi điểm theo thông lệ/kinh nghiệm. Dùng làm mặc định rồi **tinh chỉnh khi playtest trên máy thật**.
> - Phần riêng Unity được ghi rõ "(Unity)". Nguyên tắc còn lại áp dụng cho mọi engine.
>
> **Tài liệu đi kèm:**
> - `FeedbackKit/README.md` — hệ thống feedback dùng để hiện thực guide này trong Unity (tự viết trên DOTween).
> - `feel-demos-study.md` — nghiên cứu Feel (More Mountains): demo, kiến trúc, công thức, bẫy giá trị mặc định.

---

## Mục lục

0. [Hướng dẫn cho agent](#0-hướng-dẫn-cho-agent-đọc-trước-khi-sửa-code)
1. [Nguyên tắc cốt lõi](#1-nguyên-tắc-cốt-lõi)
2. [Độ trễ phản hồi (input → feedback)](#2-độ-trễ-phản-hồi-input--feedback)
3. [Nút bấm & UI](#3-nút-bấm--ui)
4. [Tween, easing, spring](#4-tween-easing-spring)
5. [Bộ công cụ FX (kèm thông số)](#5-bộ-công-cụ-fx-kèm-thông-số)
6. [Checklist theo từng sự kiện](#6-checklist-theo-từng-sự-kiện)
7. [Đặt FX ở đâu](#7-đặt-fx-ở-đâu)
8. [Âm thanh](#8-âm-thanh)
9. [Haptics](#9-haptics)
10. [Chuyển màn, loading, chống giật](#10-chuyển-màn-loading-chống-giật)
11. [Bài học từ các game hit](#11-bài-học-từ-các-game-hit)
12. [Những bẫy cần tránh](#12-những-bẫy-cần-tránh)
13. [Accessibility & Settings](#13-accessibility--settings)
14. [Checklist nghiệm thu](#14-checklist-nghiệm-thu)
15. [Nguồn](#15-nguồn)

---

## 0. Hướng dẫn cho agent (đọc trước khi sửa code)

### 0.1 Phạm vi
- Chỉ cải thiện **lớp phản hồi** (presentation): animation, VFX, SFX, haptics, UI motion, chuyển màn, loading, độ trễ.
- **Không thay đổi luật chơi, độ khó, kết quả logic** (điểm, số nước đi, spawn, RNG…). Nếu một thay đổi feel buộc phải đụng tới logic hoặc timing của gameplay thì dừng lại và hỏi.
- **Không bao giờ bỏ input của người chơi vì đang chạy animation** (xem 2.4). Animation không được khoá input lâu hơn trước khi sửa.
- **Không thêm package/thư viện mới** (DOTween, PrimeTween, plugin audio/haptics…) khi chưa hỏi. Dùng thứ dự án đã có.
- **Không dùng Feel (More Mountains).** Đã quyết định dùng **FeedbackKit** (xem 0.3). Nếu dự án chưa có FeedbackKit thì hỏi trước khi chép vào.
- **Không đổi ProjectSettings / Player Settings / manifest** khi chưa hỏi. Các thiết lập ở mục 2 và 10 (targetFrameRate, DSP buffer, Frame Pacing, import settings…) phải được **đề xuất** kèm lý do, chờ đồng ý rồi mới đổi.

### 0.2 Quy trình
1. **Audit:** liệt kê mọi sự kiện người chơi gây ra hoặc nhìn thấy (bảng ở mục 6), tìm trong code nơi sự kiện xảy ra, ghi lại feedback hiện có (visual / sound / haptic / không có gì). Ghi lại cả: targetFrameRate, cách bắt input (down hay up), có khoá input khi animation chạy không, cách chuyển màn và load level.
2. **Lập bảng gap** so với checklist mục 6 và các mục 2, 3, 10.
3. **Ưu tiên theo thứ tự:**
   - **P0, độ phản hồi:** mọi thao tác chạm có phản hồi trong frame kế tiếp; không bỏ input; frame rate đúng; không giật khi chơi.
   - **P1, sự kiện cốt lõi:** chọn / thả / match / clear / rơi / thu thập về mục tiêu; nút bấm.
   - **P2, leo thang:** combo, cascade, power-up, hoàn thành nhóm.
   - **P3, khoảnh khắc lớn và chuyển cảnh:** thắng, thua, phần thưởng, chuyển level, loading.
   - **P4, ambient:** idle, gợi ý, nền, menu.
4. **Triển khai tập trung bằng FeedbackKit:** mỗi khoảnh khắc một `FeedbackPlayer` (cấu hình trong Inspector/prefab), code chỉ gọi `Play()` / `PlayStep()` / `PlayReverse()`. Không rải tween hard-code khắp nơi. Thông số nằm trong các player để chỉnh mà không cần sửa code. Nút dùng `JuicyButton`.
5. **Kiểm tra trên máy thật** (ít nhất một máy yếu): không lỗi, FPS ổn định, input không trễ, chạm liên tục không bị nuốt, mỗi kênh tắt được riêng (sound / music / haptics / reduce motion).
6. **Báo cáo:** file đã sửa; sự kiện nào thêm feedback gì; thông số mặc định; thiết lập nào **đề xuất nhưng chưa đổi**; việc còn để ngỏ.

### 0.3 Ghi chú cho Unity
- **Hệ thống feedback: FeedbackKit** (thư mục `FeedbackKit/`, chạy trên DOTween). Gồm `FeedbackPlayer`, các feedback Transform/UI/Audio/Haptic/Particles/Flow, `JuicyButton`, `FeedbackLab`. Cách dùng và công thức: `FeedbackKit/README.md`. Kiến trúc học theo Feel (xem `feel-demos-study.md` mục 11), nhưng **không mua/cài Feel**.
- **Nối dịch vụ khi khởi động:** `FeedbackServices.Audio` (SoundManager của game), `FeedbackServices.Haptics` (adapter Nice Vibrations…), `FeedbackGlobal.*` (Sound / Haptics / Reduce motion từ Settings).
- **Tween lẻ ngoài FeedbackKit:** dùng DOTween (FeedbackKit đã phụ thuộc DOTween). PrimeTween không cấp phát bộ nhớ lúc chạy (zero-allocation) `[nguồn: PrimeTween]` — chỉ cân nhắc nếu dự án đã có sẵn.
- **Thiếu trong FeedbackKit** (spring, floating text, freeze frame có stack…): viết thêm feedback mới theo cùng kiến trúc (`[Serializable, FeedbackMenu("…")]` kế thừa `Feedback`/`TweenFeedback`), không cài thư viện khác.
- **Particle, popup, vật bay, AudioSource:** luôn pool. Không `Instantiate`/`Destroy` mỗi lần match.

---

## 1. Nguyên tắc cốt lõi

1. **Mọi input đều có phản hồi ngay lập tức**, trong frame kế tiếp, kể cả ở menu và kể cả khi chạm vào chỗ không làm được gì. `[nguồn: Candy Crush audio breakdown; Apple HIG]`
2. **Phải nhận ra được mọi hành động.** Chỉ nghe âm thanh cũng phải phân biệt được làm đúng hay làm sai. `[nguồn: Reddit r/gamedesign, top comment]`
3. **Juice mạnh chỉ dành cho thành công.** Feedback chỉ xuất hiện khi người chơi làm đúng giúp tăng cảm giác giỏi, tò mò và kiểm soát. Feedback phóng đại chung chung lại **làm giảm** những cảm giác này. `[nguồn: Kao et al., CHI 2024]`
4. **Tỉ lệ với ý nghĩa.** Độ lớn feedback tăng theo tầm quan trọng: chạm < match < combo < power-up < thắng màn. Phải chừa "khoảng trống" để leo thang. Thao tác thường ngày giữ gọn gàng; hiệu ứng to nhất để cho khoảnh khắc hiếm. `[nguồn: Reddit/HN, đồng thuận rộng]`
5. **Nhanh, không cản nhịp.** Animation lặp lại nhiều lần phải ngắn nhất. Animation ngắn → mỗi lượt nhanh → chơi được nhiều lượt hơn. `[nguồn: Naavik về Royal Match; Apple HIG: không bắt người dùng chờ animation, nhất là animation lặp lại]` Lỗi phổ biến là tween quá chậm: khi thấy "vừa", thử nhanh gấp 2–3 lần rồi lùi lại. `[nguồn: Reddit r/Unity2D]`
6. **Rõ ràng hơn hoành tráng.** Puzzle cần đọc được bàn chơi. Quá nhiều feedback làm người chơi chơi kém đi dù họ thích hơn. `[nguồn: Juul & Begy 2016]` Giao diện nhạt còn hơn giao diện juice làm dở. `[nguồn: HN]`
7. **Feedback để dạy luật chơi và cho thấy nhân quả:** cái gì bị phá, đi đâu, cộng vào đâu. `[nguồn: Funovus về Royal Match]`
8. **Mỗi chạm khởi động một chuỗi có thưởng.** Người chơi quyết định *cái gì*, game thực hiện *thế nào*, và phần thực hiện đó chính là màn trình diễn: vật đi và bắn trúng, tự sắp xếp, tự cascade. `[nguồn: Deconstructor of Fun, Playturbo về Pixel Flow, Hexa Sort]`
9. **Luôn có chuyển động, tránh đoạn chết** không có gì thay đổi. `[nguồn: Playturbo; DoF về Pixel Flow]`
10. **Coi UI như vật thể vật lý.** Nút làm bằng gì (mềm, cứng, đàn hồi)? Nặng hay nhẹ? Gắn với xung quanh bằng keo, dây hay lò xo? Animate cho đúng chất liệu đó. `[nguồn: Reddit r/gamedev]`
11. **Đa kênh: visual + sound + haptic**, phát trong cùng một frame. Nhiều người chơi casual tắt tiếng, nên visual phải tự đủ nghĩa. `[nguồn: Apple HIG, Android haptics, Lofelt, Reddit]`
12. **Nhất quán nhưng không đều đặn máy móc.** Cùng sự kiện thì cùng kiểu feedback, nhưng random nhẹ ±5–10% (pitch, scale, số hạt, thời gian), vì "đều tăm tắp trông giả". `[nguồn: GDevelop demo; deepnight randomizeBullets]` Riêng âm "hero" (thắng, coin…) giữ cố định để người chơi nhận ra.
13. **Game đứng về phía người chơi** và cho họ thấy điều đó: xáo bàn miễn phí, power-up đổi mục tiêu có lợi, input buffer, hitbox rộng. `[nguồn: Naavik về Royal Match; deepnight "assists"]`
14. **Tách logic khỏi visual.** Logic đổi trạng thái ngay lập tức; visual đuổi theo bằng tween/spring/smoothing. Visual có thể giật lùi, nảy, nghiêng mà không ảnh hưởng hitbox và logic. `[nguồn: deepnight sprOffset; Balatro-feel: visual đuổi theo logic object]`

---

## 2. Độ trễ phản hồi (input → feedback)

### 2.1 Mục tiêu & con số tham chiếu
- **Phản hồi hình ảnh trong frame kế tiếp** sau khi nhận touch. Không chờ touch-up, không chờ logic xong, không chờ load.
- **Giới hạn của phần cứng:** pipeline hiển thị cần khoảng 3 vsync từ lúc app bắt đầu xử lý tới khi điểm ảnh cuối đổi, tức **~50ms ở 60Hz và ~25ms ở 120Hz**. Triple buffering cần 4 vsync (66.7ms ở 60Hz). `[nguồn: Apple, Understanding hitches]`
  - Các câu kiểu "phản hồi dưới 16ms" (một số bài tổng hợp có viết) là chỉ thời gian xử lý trong 1 frame, không phải tổng độ trễ từ ngón tay tới màn hình.
- **Ngưỡng người chơi nhận ra:** khoảng **69ms với tap** và khoảng **11ms với kéo/drag**. Tap khá dễ dãi; **kéo thả thì cực nhạy**. `[nguồn: Deber et al., CHI 2015 (qua tóm tắt)]`
- **Ngưỡng "tức thì" 0.1s** (Nielsen): phản hồi bấm phải bắt đầu trong ~100ms, tốt nhất là frame kế tiếp. `[nguồn: Nielsen]`
- **Frame budget trên mobile:** chỉ dùng ~65% mỗi frame để chừa chỗ cho nhiệt độ máy, tức **~11ms ở 60fps**. `[nguồn: Unity mobile optimization guide]`

### 2.2 Frame rate (Unity)
- **Đặt `Application.targetFrameRate = 60`.** Mặc định trên iOS/Android là **30fps**, và `QualitySettings.vSyncCount` **bị bỏ qua** trên mobile. Từ 30 lên 60fps là bỏ được ít nhất một frame 33ms khỏi mọi đường input. `[nguồn: Unity docs targetFrameRate]` Dùng giá trị chia hết tần số màn hình.
- **Android: bật Optimized Frame Pacing** (Player > Android > Resolution and Presentation; dùng thư viện Swappy). Nó chống dồn frame trong hàng đợi, vốn thêm 1 frame trễ và làm frame không đều. `[nguồn: Android Frame Pacing]`
- **iOS 120Hz:** Unity 6 có Enable ProMotion Support. Tốn pin; chỉ bật nếu game thật sự cần (ví dụ có kéo thả nhiều). `[nguồn: Unity iOS Player settings]`
- **Tiết kiệm pin khi bàn chơi đứng yên:** `OnDemandRendering.renderFrameInterval` để render thưa hơn, và **đặt lại 1 ngay khi touch-down**. `[nguồn: Unity OnDemandRendering]`

### 2.3 Bắt input
- **Phản hồi trên touch-DOWN, thực thi trên touch-UP.** uGUI `Button.onClick` chỉ bắn khi nhả tay, nên dùng `IPointerDownHandler` để bắt đầu hình, tiếng, haptic ngay. Action chạy khi nhả (hoặc chạy ngay khi down nếu không cần hủy). `[nguồn: Unity uGUI docs; Apple HIG; Reddit]`
- **Input System (Unity):** để Update Mode mặc định (Dynamic), đọc input trong `Update` chứ không phải `FixedUpdate`, dùng **EnhancedTouch** để không mất các touch bắt đầu và kết thúc giữa hai frame. `[nguồn: Unity Input System docs]`
- **Giảm chi phí raycast UI:** tắt **Raycast Target** trên mọi Graphic không tương tác; chỉ gắn GraphicRaycaster ở canvas nhận input. `[nguồn: Unity UI optimization]`
- **Kéo thả:** visual phải bám ngón tay sát nhất có thể (độ trễ kéo dễ thấy nhất). Có thể dự đoán vị trí: iOS có `predictedTouches`, nền tảng khác thì tự ngoại suy. `[nguồn: Apple]`
- **Hitbox rộng hơn hình:** tối thiểu 44×44pt `[nguồn: Apple HIG]`. Với puzzle nên rộng rãi, tương tự "coyote time" trong platformer.

### 2.4 Không bao giờ bỏ input (input buffer / skip-ahead)
- Khi animation đang chạy mà người chơi chạm tiếp, chọn một trong hai:
  - **(a) Xếp hàng:** buffer 1 input, cửa sổ ~100–200ms.
  - **(b) Tua nhanh:** đẩy animation hiện tại tới trạng thái cuối ngay lập tức rồi chạy input mới ("input cancels render").
  `[nguồn: pancelor input buffering for grid games; Reddit r/gamedev]`
- Một dev thấy animation > ~220ms là không chịu nổi nếu không có skip-ahead. Có skip-ahead thì 0.2–0.3s/hành động vẫn ổn, và người chơi còn thấy "mình nhanh hơn game". `[nguồn: Reddit r/gamedev, ý kiến một người]`
- Royal Match cho đi nước tiếp trong lúc cascade còn chạy; Gamigion nhận xét không phải chờ bàn chơi ổn định làm tăng tốc độ chơi. `[nguồn: Gamigion; Medium (chỉ đọc được trích đoạn)]`
- Buffer quá sâu gây khó hiểu; 1 input là đủ. `[nguồn: pancelor]` (deepnight dùng hàng đợi 0.6s cho game hành động `[nguồn: deepnight ControllerQueue]`.)
- Nhịp game và nhịp animation là hai cần gạt khác nhau. 2048 cho đi "vài nước mỗi giây" nên rất đã tay; Threes có cảm giác "nặng". `[nguồn: HN]`

### 2.5 Độ trễ âm thanh (Unity)
- **Project Settings > Audio > DSP Buffer Size = Best Latency.** Để System Sample Rate = 0 (theo máy). `[nguồn: Unity Audio Manager]`
  - Trước Unity 2023.1, Best Latency có thể rè hoặc chậm trên vài máy Android. Từ 2023.1 (và Unity 6) Android xuất qua AAudio. `[nguồn: Unity issue tracker, release notes]`
- **Import SFX ngắn:** Load Type **Decompress On Load**; nén **PCM** (rất ngắn) hoặc **ADPCM** (âm lặp nhiều); **Preload Audio Data = on**; **Load In Background = off** cho âm UI/feedback. Pool AudioSource. `[nguồn: Unity AudioClip docs]`
- Unity cũ trên Android có độ trễ audio cao. Plugin Native Audio (Exceed7) công bố giảm từ ~260–320ms xuống ~66–78ms, nhưng đó là số của nhà bán, đo trên Unity trước 2019.1. Với Unity 2023.1+ hãy **đo trước** khi quyết định cần hay không. `[nguồn: Exceed7]`
- Bluetooth thêm độ trễ lớn mà game không bỏ được.

### 2.6 Độ trễ haptics
- **iOS:** gọi `UIFeedbackGenerator.prepare()` **khi touch-down** (trước sự kiện), bắn khi click. Trạng thái prepared chỉ giữ vài giây. `[nguồn: Apple]`
- **Nice Vibrations:** khởi tạo sớm (`HapticController.Init()` trong Awake, hoặc có HapticReceiver), nếu không iOS có thể không rung trong tới ~30s đầu. `[nguồn: Feel/Nice Vibrations docs]`
- **Android:** dùng hiệu ứng định sẵn (`HapticFeedbackConstants`, `VibrationEffect.EFFECT_CLICK/TICK`), tránh `createOneShot`. **Không dùng `Handheld.Vibrate`** (rung dài, rè, không chỉnh được). `[nguồn: Android haptics principles]`
- **Đồng bộ:** bắn haptic cùng frame với visual và audio. Ngưỡng lệch audio–haptic chấp nhận được khoảng 12ms nếu audio đến sau, 25ms nếu audio đến trước (bản nháp 3GPP). Android thường có audio chậm hơn haptic; nếu cần, hoãn haptic cho khớp. `[nguồn: 3GPP TR 22.870 draft; arXiv 2307.05451]`

### 2.7 Đo độ trễ
- **Quay slow-motion 240fps** (độ phân giải ~4ms): quay ngón tay chạm vào scene test làm màn hình nháy, đếm frame từ lúc chạm tới lúc đổi, lặp 20+ lần. `[nguồn: phương pháp Agawi, DXOMARK]`
- Trong code: so timestamp touch của OS với lúc script xử lý. Android dùng Perfetto / Swappy stats; iOS dùng Instruments (hitches).

---

## 3. Nút bấm & UI

### 3.1 Công thức nút bấm (mặc định cho mọi nút)

**Cấu trúc (Unity):** `Root` (Button + Image raycast, **không scale**) → `Visual` (thứ được scale/dịch/đổi màu) → nội dung.
- Lý do: nếu scale chính RectTransform chứa raycast Graphic, vùng chạm co lại và OnClick bị mất khi nhả tay gần mép (Unity đánh dấu là "By Design"). `[nguồn: Unity issue tracker]`

**Pointer DOWN** (bắt đầu ngay trong frame đó):
- Kill tween đang chạy trên Visual.
- Scale Visual → **0.92–0.95** (0.9 nếu muốn phong cách cartoon mạnh) trong **50–100ms**, ease OutQuad/OutCubic.
  - Một ví dụ PrimeTween dùng 0.9 / 0.10s / OutQuad. `[nguồn: PrimeTween example]`
  - **Thu nhỏ khi nhấn, đừng phóng to**: bị ấn vào thì lúc nhả mới "bật" ra được. `[nguồn: Reddit r/gamedev]`
- **Nút "chunky" 3D kiểu casual:** dịch mặt nút xuống 3–6px, gờ/bóng dưới co về 0, mặt nút tối đi 10–15% (nhân màu ×0.85–0.9). Màu đổi **không overshoot**.
- Haptic: iOS `prepare()`; có thể bắn selection/light ngay.
- Sound: tick nhỏ khi down là tuỳ chọn (dùng cho nút quan trọng).

**Pointer UP / Click:**
- Visual về **1.0** trong **150–250ms** với **OutBack** (ví dụ PrimeTween 0.15s OutBack), hoặc spring damping ~0.5–0.7, có thể vượt tới ~1.05.
- Phát âm click. Haptic light.
- **Action chạy song song với animation**, không đợi animation xong.

**Kéo ngón tay ra khỏi nút** khi đang giữ: nút bật về, hủy action. Kéo lại vào thì nhấn lại. `[nguồn: Apple HIG; Reddit]`

**Nút disabled / khoá:**
- Không bao giờ im lặng. Phát âm "từ chối" nhỏ, trầm.
- Rung ngang ~10px, 3–4 dao động, 0.25–0.35s.
- Hiện tooltip điều kiện mở khoá.
- Trông khác hẳn trước khi chạm (xám, có khoá).

**Giữ lâu (hold):** vòng/thanh fill hiện ngay từ frame đầu, đầy trong 0.4–0.6s, haptic hoặc pitch tăng dần, nhả sớm thì hủy và nút bật về. Chỉ dùng khi thật cần, vì hold-to-confirm là "ngược với juicy". `[nguồn: HN]`

**Chống double-tap:**
- Khoá `interactable` từ lúc click tới khi chuyển màn xong, hoặc bỏ qua click lặp trên cùng nút trong ~300ms. Handler phải idempotent.
- Không gắn bộ nhận double-tap vào nút thường, vì nó làm single tap trễ ~0.5s. `[nguồn: Apple WWDC18]`

**Âm thanh nút:** mọi nút có âm, phân biệt tiến (confirm) với lùi (back/close). "Một tiếng click tốt làm nút chắc tay hơn dù animation ít." `[nguồn: Reddit r/gamedev]`

**Ví dụ (DOTween), tham khảo cấu trúc:**
```csharp
// Gắn lên Root của nút. visual = child được scale; KHÔNG scale Root.
public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] RectTransform visual;
    [SerializeField] float pressScale = 0.92f, pressTime = 0.08f, releaseTime = 0.2f;
    bool pressed;

    public void OnPointerDown(PointerEventData e) {
        pressed = true;
        visual.DOKill();
        visual.DOScale(pressScale, pressTime).SetEase(Ease.OutQuad).SetUpdate(true); // unscaled time cho UI
        Feedback.Play(FeedbackId.ButtonDown); // sound tick / haptic prepare (hệ thống feedback chung)
    }
    public void OnPointerUp(PointerEventData e)   => Release();
    public void OnPointerExit(PointerEventData e) => Release(); // kéo ra ngoài: bật về (Button tự hủy click)
    void Release() {
        if (!pressed) return;
        pressed = false;
        visual.DOKill();
        visual.DOScale(1f, releaseTime).SetEase(Ease.OutBack).SetUpdate(true);
    }
}
```

### 3.2 Popup / panel
- **Mở:** pop-in scale 0.85 → **~1.04 → 1.0** (không từ 0) kèm fade, **250–350ms** (4.7). Nếu dùng một tween OutBack từ 0.85 thì overshoot ≈ 3; mặc định 1.70158 chỉ vượt tới ~1.015, gần như không thấy.
- **Đóng:** nhanh hơn mở, **120–180ms**, ease-in, scale xuống và fade.
- **Overlap:** icon, label, tia sáng vào trễ hơn khung 50–100ms, chạy song song.
- Mỗi khoảnh khắc chỉ một phần tử "hero" được overshoot; còn lại dùng ease-out thường.
- Panel tiện ích (settings…) dùng OutCubic/OutQuad, không nảy.
- Mở popup phần thưởng có thể có anticipation: chìm ~5% trong 60–100ms rồi mới nảy lên.

### 3.3 Chuyển màn hình / menu
- Tween chuyển cảnh; nút bay vào/ra chứ không biến mất đột ngột. `[nguồn: Reddit r/gamedev]`
- Có âm chuyển cảnh lấp khoảng trống trong lúc màn mới vào. `[nguồn: Candy Crush]`
- **Bắt đầu animation chuyển ngay khi chạm**, load phía dưới lớp che (xem mục 10).
- Mỗi màn chỉ **một nút chính** rõ ràng (Play / Next). `[nguồn: Funovus về Royal Match; DoF hybrid-casual]`

### 3.4 HUD & số
- Mọi số tăng đều **đếm dần (count-up)** kèm scale punch khi đổi, không nhảy thẳng. `[nguồn: Feel TMP Count To; Angry Birds score screen]`
- Thanh tiến độ: dùng "delayed bar" (một lớp chạy trễ cho thấy phần vừa mất/được) và bump khi đổi; thay đổi lớn thì bump mạnh hơn. Reset im lặng thì không bump. `[nguồn: Feel MMProgressBar]`
- Giá trị bám theo liên tục (số đếm, thanh, camera) dùng exponential smoothing (4.4).

### 3.5 Hiệu năng UI (Unity)
- Animator làm UI dirty mỗi frame dù giá trị không đổi, nên **dùng tween cho UI phản hồi sự kiện**. `[nguồn: Unity UI optimization]`
- Tách phần tử animate vào **sub-Canvas** riêng; tách canvas tĩnh và canvas động. `[nguồn: Unity]`
- **Không tween size/position của con trong LayoutGroup**. Tween scale của child visual hoặc alpha của CanvasGroup; hoặc tắt LayoutGroup sau khi layout xong.
- Ẩn UI bằng cách tắt component Canvas, không tắt GameObject. `[nguồn: Unity]`
- UI dùng **unscaled time** để vẫn chạy khi game freeze/pause.

---

## 4. Tween, easing, spring

### 4.1 Thời lượng

| Loại | Thời lượng | Ghi chú |
|---|---|---|
| Phản hồi nhấn | Bắt đầu frame kế; 50–100ms | |
| Micro (punch, toggle, chọn ô) | 100–250ms | Material short: 50–200ms `[nguồn: M3 tokens]` |
| Popup vào / ra | 250–350ms / 120–180ms | Ra nhanh hơn vào. Material: enter ~225, exit ~195ms `[nguồn: Material legacy]` |
| Chuyển màn hình | 250–400ms | Material: ~300ms; >400ms "có thể thấy chậm" `[nguồn: Material legacy]` |
| Swap / di chuyển ô | 120–200ms | |
| Vật bay về HUD | 0.4–0.7s | Đường cong, stagger |
| Stagger mỗi phần tử | 30–60ms | Tổng stagger ≤ ~500ms; lưới lớn thì stagger theo hàng hoặc theo khoảng cách tới điểm chạm |
| Ăn mừng thắng | Theo nhịp, **phải tua được** | |

- Quy tắc: quãng đường hoặc diện tích càng lớn thì thời lượng càng dài. `[nguồn: M3]`
- Game được phép "nảy" hơn app, nhưng vẫn theo luật nền tảng: ngắt được, phản hồi tức thì, animation lặp lại phải ngắn.

### 4.2 Easing

| Dùng cho | Easing |
|---|---|
| Phần tử đi vào | Ease-**out**: OutCubic / OutQuad / Material emphasized decelerate `cubic-bezier(0.05,0.7,0.1,1)` |
| Phần tử đi ra | Ease-**in**: InCubic / emphasized accelerate `cubic-bezier(0.3,0,0.8,0.15)` |
| Pop vui (nút, icon, phần thưởng) | **OutBack**. Overshoot mặc định 1.70158 ≈ vượt 10% `[nguồn: DOTween/Penner]`; 1.0–1.2 cho nhẹ hơn |
| Rơi (trọng lực) | InQuad |
| Progress bar, spinner, vòng màu | Linear (chỉ những thứ này) `[nguồn: M3]` |
| Elastic / Bounce | Hiếm, cho khoảnh khắc hero |
| Màu, alpha | Không overshoot (effects motion) `[nguồn: M3]` |

- **Không dùng linear cho chuyển động vị trí/scale.**
- **Trộn easing trên cùng một vật** khi cần, ví dụ scale OutExpo cùng vị trí OutBounce. `[nguồn: Reddit r/Unity2D]`
- **Âm thanh trước, tween sau:** khi có âm đặc trưng, căn timing tween theo âm. `[nguồn: Reddit r/Unity2D, ý kiến một người]`

### 4.3 Spring
- **Khi nào dùng spring:** thứ có thể bị ngắt, đảo chiều giữa chừng, hoặc mang vận tốc: nhấn/nhả nút, toggle, kéo thả rồi thả, snap, chạm liên tục. Spring "luôn sẵn sàng đi chỗ khác" và spam không xung đột. `[nguồn: Apple WWDC18; Feel springs docs]`
- **Khi nào dùng tween:** chuỗi có biên đạo, thời lượng cố định (popup, chuỗi phần thưởng, chuyển cảnh).
- **AnimationCurve:** hình dạng do designer vẽ, ví dụ anticipation + overshoot trong một curve.
- **Thông số tham khảo:**
  - M3 Expressive (hợp với game): fast spatial damping **0.6** / stiffness **800** (nút, phần tử nhỏ); default 0.8/380; slow 0.8/200. `[nguồn: AndroidX ExpressiveMotionTokens]`
  - M3 standard (app, ít nảy): fast 0.9/1400; default 0.9/700; slow 0.9/300. Effects (màu, alpha) damping 1.0 (không nảy). `[nguồn: M3]`
  - Apple: mặc định không nảy (bounce 0); "nảy nhẹ" `spring(duration: 0.5, bounce: 0.15)`. Tap không có quán tính nên Apple không cho overshoot trong app. `[nguồn: WWDC18, WWDC23]` Game casual thường có overshoot nhẹ khi nhả nút; giữ ≤ ~1.05.
  - Feel MMSpring mặc định: damping **0.4**, frequency **6** (nảy nhiều); bump 20–30. Spring lỏng chỉ cần bump nhỏ, spring căng cần bump lớn. `[nguồn: Feel springs]`
- **Spring "jelly" tự viết** (GDevelop demo), mỗi frame:
  ```
  dX = lerp(dX, (target - scaleX) * 0.5, 0.2)
  scaleX += dX
  scaleY = 2 * target - scaleX   // giữ thể tích; clamp ≥ 0.5
  ```
  Hệ số lerp cao hơn → cứng hơn, nhanh hơn. `[nguồn: GDevelop game-feel-demo.json]`

### 4.4 Smoothing không phụ thuộc frame rate
- `x = lerp(x, target, 1 - exp(-λ * dt))`. Half-life = ln2/λ; **λ ≈ 5–20** cho UI. `[nguồn: Rory Driscoll; Freya Holmér; Howling Moon]`
- **Không dùng `x += (target - x) * 0.1` mỗi frame**: chạy nhanh/chậm khác nhau theo FPS.
- Quy đổi: "20% khoảng cách mỗi frame ở 60fps" (deepnight) tương đương λ ≈ 13.4/s.

### 4.5 Thực hành tween (DOTween / PrimeTween)
- **Kill trước khi tween mới trên cùng property.** DOTween không tự ghi đè. Một reference tween cho mỗi property. `[nguồn: DOTween docs; Reddit r/godot]`
- **Punch/shake là tương đối**, nên ngắt giữa chừng sẽ làm lệch dần (scale trôi). Trước mỗi punch: `DOComplete()` / `DOKill(true)` và reset về scale nghỉ đã lưu. `[nguồn: DOTween; Balatro-feel kill punch trước punch mới]`
- **Vòng đời:** `SetLink(gameObject)` (với Sequence thì link chính Sequence), hoặc kill trong `OnDestroy`. PrimeTween an toàn khi target bị destroy. `[nguồn: DOTween, PrimeTween]`
- **Unscaled time:** `SetUpdate(true)` (DOTween), `useUnscaledTime: true` (PrimeTween). `[nguồn: docs]`
- **Thông số punch/shake trông đẹp:**
  - Punch nút/icon: `DOPunchScale(Vector3.one * 0.1–0.2, 0.2–0.3s, vibrato 5–8, elasticity 0.5–1)`. Phần thưởng: 0.25–0.3.
  - Rung báo lỗi: `DOShakeAnchorPos(0.3f, new Vector2(10, 0), 15, randomness 0–20, fadeOut: true)`.
  - Shake randomness > 90 "trông dở". `[nguồn: DOTween docs]`
- **Sequence:** Append / Join / Insert / AppendInterval (DOTween), Chain / Group / Insert (PrimeTween). Ease của chính Sequence mặc định là Linear.
- **Hiệu năng:**
  - `DOTween.SetTweensCapacity(...)` lúc khởi động. Mặc định 200 tweener / 50 sequence; nới capacity giữa game gây giật. `[nguồn: DOTween]`
  - Callback PrimeTween: truyền `target:` thay vì capture `this` (lambda capture gây cấp phát). `[nguồn: PrimeTween]`
  - Tránh các hàm trả về list (TweensByTarget…) trong vòng lặp nóng.
- **Hover/enter/exit nhanh** làm các tween "đánh nhau" và giật. Luôn kill tween cũ của property đó. `[nguồn: Reddit r/godot]`

### 4.6 Tách hành động thành pha
- Chia mỗi hành động lớn thành **lấy đà (charge/anticipation) → hành động → tiếp đất/ổn định (land/settle)**, mỗi pha một feedback nhỏ, độc lập. Đổi logic thì timing feedback không vỡ. `[nguồn: Feel Bounce demo]`
- Feedback "tất cả trong một" khó bảo trì hơn feedback nhỏ, nguyên tử. `[nguồn: Feel Blob demo]`
- Hiện/ẩn cùng một thứ có thể dùng một player chạy xuôi để hiện, chạy ngược để ẩn. `[nguồn: Feel Cards UI demo]`

### 4.7 Pop-in: nhỏ → lớn hơn → scale gốc
Khi một vật **xuất hiện** (hiện ra, spawn, được nhận, mở ra), đừng tween thẳng từ nhỏ lên scale gốc. Cho nó **vượt quá scale gốc một chút rồi mới về**: **nhỏ → lớn hơn gốc → gốc**. Pha vượt cho vật "lực" và trọng lượng; tween thẳng trông như trượt vào, nhạt.

Dùng cho: popup/panel, ô hoặc vật mới spawn, sao kết màn, icon phần thưởng, item về HUD, dấu tick hoàn thành, chữ khen combo, nút xuất hiện, power-up vừa tạo.

| Loại vật | Từ | Đỉnh | Về | Tổng thời lượng |
|---|---|---|---|---|
| Vật nhỏ (ô, icon, coin, tick) | 0–0.3 | 1.15–1.2 | 1 | 0.2–0.3s |
| Khoảnh khắc hero (sao, power-up, chữ combo lớn) | 0 | 1.25–1.3 | 1 | 0.3–0.4s |
| Nút / chữ xuất hiện trên màn kết quả | 0.5–0.8 | 1.08–1.1 | 1 | 0.2–0.3s |
| Popup / panel lớn | 0.8–0.85 (không từ 0, xem 3.2) | 1.03–1.05 | 1 | 0.25–0.35s |

- **Nhịp:** pha lên chiếm ~60–70% thời lượng (ease-out: OutQuad/OutCubic), pha về ~30–40% (OutQuad hoặc InOutSine). **Vượt một lần là đủ**; nảy nhiều lần (Elastic) chỉ cho khoảnh khắc hero.
- **Vật càng lớn thì vượt càng ít** — panel lớn vượt 3–5% đã thấy rõ.
- **Vật xuất hiện hàng loạt, lặp liên tục** (ô rơi lấp chỗ): đỉnh thấp (1.05–1.08) hoặc dùng squash khi chạm đáy (5) thay vì pop, để không rối mắt.
- **Nhiều vật cùng xuất hiện:** stagger 30–60ms (4.1).
- **Alpha/màu không overshoot** (4.2): fade chạy ease-out thường, song song, đạt 1 trước hoặc cùng lúc với đỉnh scale.

**Cách làm:**
- **(a) Một tween OutBack** — gọn nhất. **Mức vượt của OutBack tính theo quãng đường (To − From), không theo scale gốc.** Với overshoot `s`, phần vượt ≈ `4s³ / (27(s+1)²)` × quãng đường:

  | Overshoot `s` | Vượt (% quãng đường) | 0 → 1 cho đỉnh | 0.85 → 1 cho đỉnh |
  |---|---|---|---|
  | 1.0 | ~4% | 1.04 | 1.006 |
  | 1.70158 (mặc định DOTween) | ~10% | 1.10 | 1.015 |
  | 2.2 | ~15% | 1.15 | 1.023 |
  | 3.0 | ~25% | 1.25 | 1.038 |
  | 4.0 | ~38% | 1.38 | 1.057 |

  - **Bẫy:** quãng ngắn (0.85 → 1, hay nhả nút 0.92 → 1) với overshoot mặc định gần như **không thấy vượt**. Popup 0.85 → 1 cần `s ≈ 3` để có đỉnh ~1.04; nhả nút 0.92 → 1 cần `s ≈ 4` để có đỉnh ~1.03.
- **(b) Hai bước (Sequence)** — kiểm soát đỉnh và nhịp chính xác; dùng khi quãng ngắn hoặc cần đỉnh cụ thể:
  ```csharp
  // rest = scale gốc đã lưu; from / peak là hệ số (ví dụ 0 và 1.15)
  _pop?.Kill();
  target.localScale = rest * from;
  _pop = DOTween.Sequence()
      .Append(target.DOScale(rest * peak, duration * 0.65f).SetEase(Ease.OutQuad))
      .Append(target.DOScale(rest, duration * 0.35f).SetEase(Ease.InOutSine))
      .SetUpdate(true)                 // UI: unscaled time
      .SetLink(target.gameObject);
  ```
- **(c) AnimationCurve** `(0 → from, 0.65 → peak, 1 → 1)` khi designer muốn tự vẽ hình.

**Biến mất (chiều ngược):** gốc → hơi lớn hơn (1.05–1.1, lấy đà 40–80ms) → nhỏ về 0 với ease-in, **nhanh hơn lúc vào**. DOTween: tween 1 → 0 với `InBack` cho đúng hình này. Panel lớn đóng thì bỏ pha lấy đà, chỉ ease-in (3.2).

**FeedbackKit:** dùng `ScaleTweenFeedback` với **`Shape = PeakThenSettle`** (From → Peak → To, `PeakAt`, `PeakEase`, `SettleEase`) — chính là cách (b), đặt đỉnh trực tiếp, không phải dò Overshoot. Intensity và Reduce motion chỉ co giãn phần vượt. `Shape = Direct` + OutBack là cách (a). Xem `FeedbackKit/README.md` mục 5.8.

---

## 5. Bộ công cụ FX (kèm thông số)

| Kỹ thuật | Dùng khi | Thông số khởi điểm |
|---|---|---|
| **Scale punch** | Chạm, nhận item, đổi số | 1 → 1.15 → 1, 120–200ms, OutBack. Feel mặc định 0.2s, đỉnh ở 30% thời lượng `[nguồn: Feel MMF_Scale]` |
| **Pop-in (xuất hiện)** | Vật, popup, sao, icon hiện ra | **Nhỏ → vượt gốc → gốc**: vật nhỏ 0 → 1.15–1.2 → 1; hero 0 → 1.25–1.3 → 1; popup 0.85 → 1.03–1.05 → 1; 0.2–0.35s. Xem 4.7 (bẫy OutBack với quãng ngắn) |
| **Press down** | Nút, ô | Xem 3.1 |
| **Squash & stretch** | Ô rơi chạm đáy, nảy, nổ | **Giữ thể tích:** `sx = s, sy = 2 - s` (hoặc 1/s). Hồi về 1 với ~20%/frame ở 60fps `[nguồn: deepnight]`. Chạm đáy: sy 0.85, sx 1.1–1.15, 80–120ms. **Tăng theo độ mạnh va chạm** (rơi càng cao càng squash mạnh) `[nguồn: deepnight heroSquash: 1 - 0.5*pow]` |
| **Wobble khi xuất hiện/bị đánh** | Ô mới, phần thưởng | Keyframe scale: f0 (0.8, 1.2) → f5 (1.1, 0.9) → f10 (0.95, 1.05) → f20 (1, 1) `[nguồn: Reddit, một người]`. Lệch pha x và y cho cảm giác "jelly" |
| **Anticipation** | Trước power-up, trước khi ô vỡ | Co/rung 100–200ms trước hành động chính. deepnight: "ready gun" 0.33s cho game hành động |
| **Hit blink / flash** | Ô bị đánh, vỡ | Cộng màu trắng (additive) **0.06s**, rồi R/G/B giảm 0.60/0.55/0.50 mỗi frame (trắng "nguội" qua vàng, cam) `[nguồn: deepnight]`. GDevelop: tint 0.06s `[nguồn: GDevelop]`. Feel Flicker 0.2s, chu kỳ 0.04s |
| **Glow / light spot** | Điểm match, điểm merge | Halo additive scale 2–3, alpha 0.1–0.2, mờ trong ~0.1s `[nguồn: deepnight lighting]` |
| **Particles** | Phá ô, nổ, thu thập | 8–20 hạt cho match thường; màu theo ô; random mọi tham số trong khoảng. Pool (deepnight pool 2048) |
| **Impact nhiều lớp** | Va chạm quan trọng | Flash tức thì (0.03–0.12s) → tia/mảnh rơi (0.4–2s) → ánh/than âm ỉ (2–3s) `[nguồn: deepnight bulletImpact/Dust/Burn]` |
| **Vật bị xoá bay ra, lộn vòng** | Ô bị clear | Thay vì biến mất: bay ra, xoay, nảy rồi mờ `[nguồn: deepnight cadavers; Juice it or lose it]` |
| **Lưu dấu (persistence)** | Mảnh vỡ, vết | Mảnh/vết ở lại một lúc trên bàn trước khi mờ `[nguồn: Art of Screenshake; deepnight]`. Không che ô cần đọc |
| **Phản ứng lan sang hàng xóm** | Nổ, ô rơi mạnh | Ô lân cận lắc/nảy nhẹ, giảm theo khoảng cách `[nguồn: deepnight enemyPhysicalReactions]`. Juice it or lose it: *mọi* gạch cùng scale nhẹ khi bóng chạm |
| **Trail** | Vật bay, power-up | Vệt ngắn alpha ~0.3, sống ~0.1s `[nguồn: deepnight bulletTail]` |
| **Stagger** | Nhiều vật cùng chuyển động | Lệch 20–60ms |
| **Camera/board bump (có hướng)** | Thả ô, swipe, va chạm | Đẩy 2–6px theo hướng hành động, hồi ×0.85 mỗi frame `[nguồn: deepnight camBump]`. Hay hơn shake ngẫu nhiên cho sự kiện vừa |
| **Zoom punch** | Combo lớn, thắng | +2–3% zoom, hồi ×0.9 mỗi frame `[nguồn: deepnight camBumpZoom +0.03]` |
| **Screen shake** | **Chỉ** power-up/combo lớn | Biên độ nhỏ, 150–300ms, giảm dần. deepnight: trục x, y khác tần số, biên độ giảm tuyến tính, **shake yếu không cắt shake mạnh đang chạy**. GDevelop dùng noise. Feel recipe: camera 0.3s / amp 2 / freq 40; UI 0.5s / amp 20 `[nguồn: các demo]` |
| **Hitstop / freeze frame** | Va chạm cực lớn (bom + bom) | 30–80ms (Feel mặc định 0.1s, ví dụ 0.05s). Ưu tiên **chậm 90%** hơn là đứng hẳn; không cộng dồn (sẽ thành slow-mo liên tục) `[nguồn: GDevelop; Feel]` |
| **Slow-motion** | Nước quyết định, ô cuối | Không chậm quá 20% tốc độ; vào nhanh (60%/frame), ra chậm (20%/frame) `[nguồn: deepnight addSlowMo]`. Peggle: zoom + slow-mo khi sắp xoá peg cuối `[nguồn: Gamezebo]` |
| **Screen flash toàn màn** | Thắng, power-up lớn | Alpha ≤ 0.04–0.1, ~0.1s `[nguồn: deepnight flashbang 0.04]`. **Không nhấp nháy > 3 lần/giây** |
| **Number popup** | Điểm, combo | Hiện tại chỗ sự kiện, bay lên 40–80px, mờ 600–800ms. Biến thể "crit": bay lên, đổi màu nhanh (~0.07s/lần) cho combo lớn `[nguồn: GDevelop float text]` |
| **Fly-to-target** | Item/goal về HUD | Bezier 0.4–0.7s, stagger 30–60ms, đích scale punch khi nhận |
| **Bounce có hướng** | Đổi giá trị do nguồn nào đó | Nảy *ra xa* phía nguồn gây ra thay đổi, để chuyển động kể được nguyên nhân `[nguồn: Reddit, ví dụ Wandersong]` |
| **Idle "sống"** | Ô/thẻ/power-up đứng yên | Lắc/nghiêng sin nhẹ, **mỗi vật lệch pha** để không đồng bộ `[nguồn: Balatro-feel]` |
| **Hover/chọn kiểu Balatro** | Thẻ, ô chọn được | Scale 1.15 (hover) / 1.25 (chọn), 0.15s OutBack; punch xoay 5°; visual đuổi theo logic với lerp `30*dt`; nghiêng theo vận tốc (×20, clamp ±60°) `[nguồn: Mix and Jam balatro-feel]` |

**Quy tắc chung cho bộ công cụ:**
- **Random hoá** cường độ (Feel mặc định ×0.8–1), thời lượng, số hạt, pitch. `[nguồn: Feel core concepts]`
- **Chống spam:** cooldown cho feedback do người chơi kích hoạt liên tục; Chance < 100% cho hiệu ứng phụ (ví dụ 25% ≈ 1/4 lần). `[nguồn: Feel MMF_Player]` Art of Screenshake còn có 33% cơ hội nổ ngẫu nhiên vô hại cho vui.
- **Cường độ theo mức sự kiện:** một player nhận intensity, khoảng thấp chạy feedback nhẹ, khoảng cao chạy feedback nặng. `[nguồn: Feel Intensity Intervals]`
- **Mỗi loại shake có một ý nghĩa cố định**, và tắt được toàn cục (accessibility). `[nguồn: Feel screen shakes]`

---

## 6. Checklist theo từng sự kiện

> Mỗi sự kiện: **Visual / Sound / Haptic**. Sự kiện nào game không có thì bỏ qua; sự kiện đặc thù thì tự suy ra theo mục 1.

### 6.1 Chạm UI (nút, tab, toggle)
- Công thức 3.1. UI sound < 200ms, nhận ra được dưới 200ms `[nguồn: VCU sonic feedback]`. Phân biệt âm tiến và lùi.
- Chuyển màn có âm và animation lấp khoảng trống `[nguồn: Candy Crush]`.
- Haptic: selection/light.

### 6.2 Chạm / chọn ô
- **Visual:** phản hồi ngay trên touch-down. Ô phóng 1.08–1.12 hoặc nhô lên, sáng viền. Phản hồi **lộ ra ngoài vùng ngón tay** (viền, glow lan, hiệu ứng phía trên).
- **Sound:** tick nhẹ, chất liệu theo vật (gỗ, nhựa, kính…).
- **Haptic:** light/selection.

### 6.3 Kéo / swap / thả hợp lệ
- **Visual:** khi kéo, visual bám ngón tay sát (độ trễ kéo dễ thấy nhất, 2.1). Thả/swap 120–200ms có ease; hơi vòng cung để đọc hướng. Chạm đáy hoặc vào ô thì squash nhẹ, bump có hướng.
- **Sound:** whoosh ngắn khi đi, "clack" khi vào chỗ.
- **Haptic:** light khi vào chỗ (snap).

### 6.4 Thao tác không hợp lệ
- **Visual:** đi nửa đường rồi bật về, hoặc rung ngang ~10px trong ~0.25–0.3s. Không dùng đỏ gắt, không phạt nặng nề.
- **Sound:** âm lỗi **nhẹ, trầm** `[nguồn: Candy Crush; VCU]`.
- **Haptic:** warning nhẹ. Đây là một trong số ít chỗ nên rung kể cả khi game dùng haptic rất ít `[nguồn: Reddit]`.

### 6.5 Chạm → khởi động chuỗi (kiểu Pixel Flow / Loop Sort / Colony Flow)
- Chạm vào nguồn (pig, truck, hộp kiến…) thì nó **phản hồi ngay** (nảy, squash), rồi bắt đầu đi.
- Trên đường đi, **mỗi lần trúng/nhặt/xếp đều có feedback nhỏ** (flash ô, hạt, tick âm, pitch tăng dần trong chuỗi). Chạm một lần mà hưởng feedback kéo dài. `[nguồn: Playturbo]`
- Hiển thị **sức chứa / số lượng còn lại** rõ ràng (đạn trên đầu pig, slot chờ), đổi số có punch. Sức chứa hiện rõ chính là nguồn căng thẳng. `[nguồn: DoF, Playturbo về Pixel Flow]`
- Cho phép **chạm liên tiếp nhanh** ("slinging" của Pixel Flow): không khoá input khi đơn vị trước còn đang chạy. `[nguồn: DoF]`
- Đơn vị đi phải **nhanh và dứt khoát**. Người chơi chê kiến "chậm kinh khủng", và Food Hunt phải thêm chế độ 2×. `[nguồn: App Store reviews]`

### 6.6 Match / phá ô
- **Visual:**
  1. Anticipation rất ngắn (co hoặc sáng ~50–80ms).
  2. Flash trắng 1–3 frame (additive, nguội dần).
  3. Phóng to rồi biến mất (1 → 1.2 → 0, 150–250ms), hoặc bay ra lộn vòng.
  4. Particles màu theo ô bung ra **xung quanh**.
  5. Glow tại điểm match.
  6. Popup điểm.
  7. Ô lân cận lắc nhẹ (nếu hợp phong cách).
- **Sound:** pop/break theo chất liệu; random pitch nhỏ (8.4); chuỗi trong cùng một nước thì tăng pitch (6.8).
- **Haptic:** light/medium, gộp thành **một nhịp** khi nhiều ô vỡ cùng lúc.

### 6.7 Ô rơi / lấp chỗ trống
- **Visual:** rơi InQuad; chạm đáy squash tỉ lệ với độ cao rơi; stagger theo cột/hàng 20–40ms.
- **Sound:** âm chạm nhẹ, **giới hạn 2–3 voice** cùng lúc.
- **Haptic:** không.

### 6.8 Combo / cascade / chuỗi
- **Sound:** **mỗi bước tăng pitch** `[nguồn: Candy Crush tăng một bán cung mỗi hàng xoá trong cùng một nước; Peggle; GDevelop +0.1 mỗi phát]`. Quy về **thang âm theo key của nhạc** (8.5). Giới hạn ~1–1.5 quãng tám rồi giữ hoặc lặp; **reset khi chuỗi kết thúc** (hoặc sau ~0.5s không có sự kiện `[nguồn: GDevelop]`).
- **Visual:** chữ khen theo cấp (Good → Great → Amazing…), mỗi cấp to hơn; hạt nhiều dần; cấp cao có biến thể "crit".
- **Voice/text:** câu ngắn, dễ nhớ ("Delicious", "Divine") kèm chữ trên màn `[nguồn: Candy Crush]`. Không để câu thoại chồng nhau.
- **Haptic:** light → medium → heavy theo cấp. Android: chuỗi tick tăng dần cường độ dẫn tới một "snap" `[nguồn: Android haptics]`.
- **Screen shake:** chỉ từ combo lớn, biên độ nhỏ.

### 6.9 Hoàn thành một nhóm (sort xong một stack/màu, merge)
- Khoảnh khắc "chốt" rõ: nhóm nảy cùng lúc (hoặc stagger nhanh), flash, glow, âm "hoàn thành" khác âm từng phần tử (cao hơn, sáng hơn), haptic medium/success.
- Nếu hoàn thành mở ra thứ gì (rèm kéo lên, ô mở khoá) thì chuyển động tiếp nối **ngay sau**, không có khoảng chết. `[nguồn: Loop Sort store: rèm mở khi sort xong màu]`
- Hexa Sort: người chơi khen riêng "tiếng khi chúng merge". `[nguồn: App Store review]`

### 6.10 Tạo power-up
- **Visual:** các ô tham gia hút về điểm tạo; power-up bật ra với pop-in lớn (0 → 1.25–1.3 → 1, 4.7) và glow; khi nằm yên có ambient (lấp lánh, "thở").
- **Sound:** âm "tạo ra" riêng. **Haptic:** medium.

### 6.11 Kích hoạt power-up
1. Anticipation 100–200ms (co lại, sáng, rung).
2. Hiệu ứng chính rõ hướng (tia, sóng, vật bay).
3. Ô bị ảnh hưởng vỡ **lan theo thứ tự** (stagger theo khoảng cách).
4. Board bump/shake nhỏ; hitstop chỉ cho combo power-up + power-up.
- Nếu power-up đổi mục tiêu cho có lợi thì **cho người chơi thấy** nó đổi hướng `[nguồn: Naavik về propeller của Royal Match]`.
- Animation **ngắn** `[nguồn: Naavik]`. Sound lớp to hơn, duck nhạc 3–6dB. Haptic heavy.

### 6.12 Thu thập / tiến độ mục tiêu
- Item bay từ bàn về ô mục tiêu trên HUD (bezier, trail, stagger). Đích scale punch khi nhận; số đếm giảm có animation; đạt đủ thì dấu tick có hiệu ứng. `[nguồn: Funovus về Royal Match]`
- Âm "nhận" mỗi khi item chạm HUD: một lần mỗi 40–60ms nếu nhiều, pitch tăng dần có giới hạn. Âm riêng khi hoàn thành mục tiêu.
- Haptic: **không rung cho từng item trong một loạt**. Một nhịp light, và success khi hoàn thành `[nguồn: Reddit: dev hối hận vì rung mỗi lần nhặt tiền]`.

### 6.13 Chướng ngại vật nhiều lớp
- Mỗi lần trúng có trạng thái hình khác (nứt, mất lớp) để biết còn bao nhiêu `[nguồn: Funovus]`. Lớp cuối vỡ mạnh hơn.

### 6.14 Sắp hết lượt / căng thẳng
- Nhắc nhẹ: bộ đếm nhấp nháy, đổi màu, nhân vật lo lắng `[nguồn: Funovus: nhân vật King của Royal Match lo lắng]`; slot gần đầy thì slot cuối nổi bật.
- Không còi báo động, không rung liên tục.

### 6.15 Thắng màn
- **Cao trào** ngay khi mục tiêu cuối xong (kiểu Extreme Fever của Peggle) `[nguồn: Gamezebo]`.
- **Chuỗi gợi ý:**
  1. Dừng ngắn ~0.2s hoặc slow-mo cho sự kiện cuối.
  2. Chữ/âm ăn mừng (âm hero cố định).
  3. Nếu có: khoá input, đổi tài nguyên còn lại thành thưởng từng cái một với pitch tăng dần `[nguồn: Sugar Crush của Candy Crush]`.
  4. Màn kết quả: điểm đếm dần (tạo hồi hộp "có được sao thứ 3 không?"); sao **đập vào** lần lượt ~0.3s/sao (pop-in 0 → ~1.3 → 1, 4.7), mỗi sao một nốt cao hơn, punch và nổ hạt, có thể làm cả khung rung nhẹ cho có trọng lượng `[nguồn: Reddit: Jetpack Joyride, Angry Birds, Clash of Clans]`.
  5. Phần thưởng bay vào ví/HUD.
- **Luôn tua được bằng chạm**, nút hiện ngay. Màn thưởng không tua được bị gọi là "nỗi phiền lớn" `[nguồn: Reddit về Jetpack Joyride]`.
- **Kết quả hoàn hảo phải đặc biệt** (đổi màu, "Perfect!", confetti), thay vì làm kết quả thiếu sao trông xám xịt `[nguồn: Reddit, ý kiến một người]`.
- **Nạp sẵn level sau trong lúc ăn mừng** (10.4). Nút **Next** là nút chính duy nhất; bấm là vào level mới gần như ngay.
- Haptic: success.

### 6.16 Thua màn
- Nhẹ nhàng, không trừng phạt (Candy Crush: thang piano đi xuống, giữ tông vui) `[nguồn: Candy Crush]`.
- Cho thấy **đã gần thắng** đến đâu ("close win" tạo động lực chơi lại `[nguồn: SayGames]`). Retry nhanh, một chạm.
- Haptic: không, hoặc warning rất nhẹ.

### 6.17 Gợi ý khi idle
- Sau ~5–8s không thao tác: các ô của một nước hợp lệ lắc hoặc sáng lên theo chu kỳ. Không sound, không haptic.

### 6.18 Hết nước / xáo bàn
- Tự xáo với animation rõ (bay xoáy rồi về chỗ) và một dòng thông báo ngắn, để thấy game đang giúp `[nguồn: Naavik]`.

### 6.19 Meta / phần thưởng / rương
- Anticipation (rương rung, lực tăng) → bung ra → item bay vào ví → số đếm dần. "Hồi hộp rồi giải toả" như gacha/loot box `[nguồn: Reddit]`. Starbound: tiếng còi party và confetti khi mở túi `[nguồn: Reddit]`.
- Xem trước rõ ràng hành động sẽ làm gì cũng "cảm giác tốt" `[nguồn: Reddit]`.

---

## 7. Đặt FX ở đâu

1. **Ngón tay che chỗ đang chạm.** Phản hồi chạm phải lộ ra **ngoài vùng ngón tay**: viền/glow lan ra, hạt bung xung quanh, popup bay lên trên. `[nguồn: Vogel & Baudisch, Shift, CHI 2007; HN]`
2. **Nguồn → đường đi → đích.** Hiệu ứng tại nơi xảy ra, vệt bay thể hiện hướng, phản hồi tại nơi nhận (HUD). Đủ ba điểm thì người chơi hiểu nhân quả.
3. **Không che thông tin cần thiết.** Hạt/popup không che lâu các ô cần đọc; popup mờ nhanh; hạt lớn chỉ cho sự kiện lớn.
4. **Phân lớp z-order:** bàn chơi < hiệu ứng trên bàn < vật bay về HUD < HUD < popup thông báo < overlay chuyển cảnh.
5. **Rung màn hình chỉ cho sự kiện lớn**; với sự kiện vừa, dùng **bump có hướng** (5) vì dễ đọc hơn.
6. **Ambient nhẹ, chậm, tương phản thấp**, không tranh chú ý với feedback từ hành động.
7. **Hình ảnh sạch, tương phản cao** để vẫn đọc được khi bàn rối `[nguồn: AppSamurai về Pixel Flow]`. "Đống đồ trông hỗn loạn nhưng thao tác thì gọn" `[nguồn: DoF về Match 3D]`.

---

## 8. Âm thanh

### 8.1 Thứ bậc & vai trò
- Thứ bậc nổi bật: **brand > hero > cảnh báo > UX chính > UX phụ**. Âm **hero** chỉ cho khoảnh khắc hiếm và quan trọng, **dùng lại đúng một âm** để người chơi gắn nó với thành công. Hành động cực thường xuyên có thể im lặng hoặc rất nhẹ. Âm nối tiếp nhau trong một flow nên chung âm sắc hoặc giai điệu. `[nguồn: Material sound guidelines]`
- **Mỗi sự kiện có âm**, kể cả sự kiện tự động (cascade, xáo bàn) `[nguồn: patent Candy Crush]`.
- **Chủ yếu tích cực**, như "vỗ vai" khi làm đúng; âm tiêu cực ít và nhẹ `[nguồn: Candy Crush]`.
- **Hợp phong cách hình ảnh**: cùng "độ ấm" với màu sắc `[nguồn: patent Candy Crush]`. Âm gắt thay bằng âm "thoáng", hoà vào nhạc `[nguồn: Peggle]`.
- **Chuẩn của thể loại:** âm "ASMR" dễ chịu: click, clink, merge, nhai, lăn `[nguồn: Mobidictum; store Hexa Sort, Food Hunt, Marble Sort]`.

### 8.2 Độ dài & envelope
- Tap/click **30–150ms**; phần lớn âm UI **< 300ms**; thông báo ≤ ~500ms; hero/thưởng ~0.5–2s. `[nguồn: Violet Recording]`
- **Cắt khoảng lặng đầu file** để transient nằm ngay sample 0. Đuôi ngắn để chạm nhanh vẫn tách bạch. Ít hoặc không reverb cho âm UI nhanh.

### 8.3 Layering
- **Transient** (click/snap vài ms, quyết định độ "giòn") + **body** (lớp tonal/chất liệu 30–150ms) + **sweetener** (lấp lánh, shimmer).
- Công thức "punchy": transient click ngắn → một cú "kick" trầm → lớp "tính cách" ở trên; thử distortion nhẹ để có lực. `[nguồn: Reddit, sound designer]`
- Attack sắc cho cảm giác "tức thì"; attack tròn cho UI mềm, thân thiện.
- **Tactile/ASMR:** chia họ chất liệu (gỗ, nhựa, gốm, kính); foley thu gần, ngắn; pitch theo kích thước vật; nhiều lớp theo vận tốc. Unpacking có hơn 14.000 âm theo vật × bề mặt, và dùng chung một âm cho đồ khác nhau thì "vỡ immersion". `[nguồn: Audiokinetic về Unpacking]`

### 8.4 Biến thể (chống nhàm tai)
- **3–5 biến thể** mỗi âm hay lặp, cộng pitch random nhỏ; có thể random thêm low-pass và decay, ra ~20–30 biến thể cảm nhận. `[nguồn: Reddit "power of pitch shifting"]`
- **Biên độ random pitch:**
  - Âm **tonal**: ±0.3–1 bán cung (pitch ≈ ±2–6%).
  - Âm **noise** (vỡ, rơi): ±0.1 (≈ ±1.7 bán cung) vẫn ổn.
  - Volume ±1–2dB.
  - Random pitch quá lớn nghe như "chipmunk".
- **Âm iconic giữ cố định** (coin, thắng, hero). Đổi pitch âm quan trọng làm mất "trọng lượng". `[nguồn: Reddit, một sound designer]`

### 8.5 Pitch tăng theo chuỗi
- 1 bán cung = ×2^(1/12) ≈ **1.0595**. `[nguồn: Game Developer "power of pitch shifting"]`
- **Quy về thang âm** theo key nhạc nền, ví dụ ngũ cung trưởng với các bước (0, 2, 4, 7, 9, 12, …) bán cung, để không bao giờ chỏi với nhạc. Peggle 2 cho tiếng peg leo theo thang diatonic, bám hoà âm của câu nhạc đang chạy, 26 nốt. `[nguồn: audiogang Peggle 2]` Chỉ cộng pitch đều đều mà không theo nốt nhạc thì "nghe lệch". `[nguồn: Reddit]`
- Giới hạn ~1–1.5 quãng tám rồi giữ hoặc lặp (Candy Crush Soda lặp 3 âm cuối sau cascade thứ 12 `[nguồn: fan wiki]`). Reset khi hết chuỗi.
- Một sample gốc cho mỗi quãng tám, để không sample nào bị dịch quá ~6 bán cung.
- **Hướng đi:** đi lên, quãng thuận cho thành công; đi xuống, nghịch cho thất bại.

### 8.6 Loa điện thoại
- Loa nhỏ cộng hưởng quanh ~1kHz, rơi ~12dB/octave bên dưới; dải dùng được khoảng **300Hz–4kHz**. Đo thực tế thấy đỉnh ở 1k, 2.5k, 3.5kHz; 4–5kHz dễ chói. `[nguồn: Audiokinetic đo loa điện thoại]`
- **Tạo cảm giác bass bằng saturation/hài âm**, vì tần số gốc không phát ra được. `[nguồn: Game Developer, mobile audio]`
- High-pass âm UI ở ~100–150Hz; đặt "bản sắc" ở 1–4kHz; kiềm 2–5kHz cho đỡ chói; kiểm tra mono; **nghe thử trên loa máy thật** lẫn tai nghe, trên nhiều máy.

### 8.7 Mix & loudness
- Mục tiêu loudness: **−16 đến −18 LUFS** integrated, true peak ≤ −1 dBTP. Sony ASWG mobile: −18 ±2. IESD: −16. `[nguồn: Audiokinetic/Sony ASWG; IESD]`
- Âm UI thường xuyên thấp hơn vài dB so với âm ăn mừng hiếm. Nhạc thấp hơn SFX. **Duck nhạc 3–6dB** dưới âm hero. Chừa headroom, đừng để mọi âm ở volume 1.0. `[nguồn: Reddit sound designer]`
- **Giới hạn voice:** mỗi SFX tối đa 2–4 instance (Feel mặc định 3 `[nguồn: Feel MMSoundManager]`), cooldown phát lại 30–60ms. **Không phát lại cùng clip trong khi nó còn đang kêu** (bị phasing). `[nguồn: Reddit; FMOD max instances]`
- Coi chừng mix làm mất độ "đã" của SFX `[nguồn: dev match-3, itch.io]`. Nhạc lặp lâu gây mệt `[nguồn: Candy Crush breakdown]`. Tránh sample rate thấp và nén quá mạnh. Không để câu thoại chồng nhau.

### 8.8 Nền tảng & settings
- **iOS audio session:** category **Ambient** (người chơi vẫn nghe được nhạc của họ) hoặc SoloAmbient (game có soundtrack riêng); tôn trọng nút gạt im lặng; **không đổi volume hệ thống**. `[nguồn: Apple HIG Playing audio]`
- **Toggle riêng:** Sound, Music, Haptics. Volume chỉnh được ngay trong lúc chơi.
- **Nhiều người chơi tắt tiếng** → visual phải tự đủ nghĩa. `[nguồn: Reddit, HN, Lofelt]`

---

## 9. Haptics

- Haptics **bổ sung** cho hình và tiếng, không thay thế `[nguồn: Meta, Apple HIG]`. Quan trọng với casual vì nhiều người tắt tiếng `[nguồn: Lofelt]`. Pixel Flow và Block Blast dùng haptic cho các cú trúng/clear cốt lõi `[nguồn: DoF; Hungry Studio]`.
- **Nguyên tắc Android:**
  - "Ít là nhiều".
  - Click gọn **10–20ms**. Nếu chỉ có rung rè thì **thà không rung**.
  - Cường độ theo tầm quan trọng và tần suất: sự kiện thường xuyên rung tinh tế, sự kiện quan trọng rung mạnh.
  - Thiết kế đồng thời với hình và tiếng.
  `[nguồn: Android haptics principles]`
- **Ánh xạ** (presets của Nice Vibrations / iOS):

| Sự kiện | Kiểu haptic |
|---|---|
| Chạm nút / chọn ô | Selection / Light |
| Match thường, vào chỗ | Light |
| Combo, tạo power-up, hoàn thành nhóm | Medium |
| Kích hoạt power-up lớn | Heavy |
| Hoàn thành mục tiêu, thắng | Success (light → heavy) |
| Thao tác sai | Warning (nhẹ) |
| Thua | Không / Failure rất nhẹ |

- **Nhất quán:** cùng sự kiện thì cùng kiểu rung `[nguồn: Apple HIG]`. Có thể random nhẹ frequency để đỡ đơn điệu `[nguồn: Nice Vibrations]`. Chỉ một clip phát một lúc; dùng priority: âm thường ưu tiên thấp, khoảnh khắc lớn ưu tiên cao `[nguồn: Nice Vibrations]`.
- **Không rung quá dày:** khoảng cách tối thiểu ~50–100ms; gộp nhiều sự kiện cùng lúc thành một; **không rung từng item trong một loạt nhặt**.
- **Đang tranh cãi:** nhiều người chơi tắt rung (pin, giống có cuộc gọi). Một dev rung mỗi lần nhặt tiền và khuyên "đừng làm vậy", vì phần lớn người chơi bỏ game trước khi kịp tìm nút tắt `[nguồn: Reddit]`. → Rung tinh tế, chỉ ở sự kiện có ý nghĩa; toggle Haptics dễ thấy.
- **Nền tảng:**
  - iOS đầy đủ từ iPhone 8 / iOS 13.
  - Android cần API 26 cho amplitude control, không chỉnh được tần số, và khác nhau nhiều giữa các máy → cần fallback đơn giản.
  - iOS chặn haptic khi đang thu mic.
  `[nguồn: Nice Vibrations docs]`
- Độ trễ và đồng bộ: xem 2.6.

---

## 10. Chuyển màn, loading, chống giật

### 10.1 Cấu trúc scene (Unity)
- **Bootstrap scene giữ suốt game** (managers, audio, input, lớp UI, overlay chuyển cảnh). Nội dung load bằng `LoadSceneMode.Additive`, `SetActiveScene`, rồi unload scene cũ. `[nguồn: thực hành cộng đồng]`
- **Puzzle: không reload scene giữa các level.** Giữ một scene gameplay, chỉ thay **dữ liệu level** (ScriptableObject/JSON) và reset các ô đã pool. Reload scene thì chạy lại Awake/Start cho mọi object, load lại asset, và tốn một lượt GC.

### 10.2 Chuyển level cho nhanh (cảm nhận)
- **Bắt đầu animation chuyển ngay khi chạm**, load bên dưới lớp che: che vào → đổi → mở ra.
- **Nạp sẵn level kế tiếp trong lúc ăn mừng thắng.** Dữ liệu level gần như miễn phí; asset nặng có thể preload với `backgroundLoadingPriority = Low`.
- **Không có thêm chạm nào trước khi vào level**: không tutorial chen ngang, vào chơi ngay. `[nguồn: DoF hybrid-casual]` Thắng xong vào thẳng meta hoặc level kế.
- Chờ lâu hơn ~1s thì hiện hoạt động có thật: progress (`progress / 0.9`) hoặc skeleton.
- **Không bao giờ để màn hình đứng hơn một frame.** Chia việc nặng qua nhiều frame (warmup tuần tự, instantiate theo đợt nhỏ).

### 10.3 API async & bẫy (Unity)
- `LoadSceneAsync` với `allowSceneActivation = false`: progress dừng ở **0.9**, và **mọi async operation khác bị chặn** (kể cả unload) cho tới khi activate. Bước activate (gồm Awake/Start) chạy trên main thread, nên giấu sau lớp che. `[nguồn: Unity docs]`
- **Addressables:**
  - `activateOnLoad: false` chặn cả hàng đợi Addressables tới khi activate. Load asset của màn loading trước; mỗi lần chỉ stage một scene.
  - Mỗi lần load phải có một lần Release tương ứng. Bundle chỉ rời bộ nhớ khi **mọi** asset của nó được release; release rồi load lại ngay gây "churn".
  - Nội dung remote: gọi `DownloadDependenciesAsync` sớm.
  `[nguồn: Addressables docs]`
- **`Application.backgroundLoadingPriority`** (thời gian main thread cho loading mỗi frame): Low 2ms / BelowNormal 4ms (mặc định) / Normal 10ms / High 50ms. Dùng **High** khi đang che loading, **Low** khi preload trong lúc chơi. `[nguồn: Unity docs]`
- **Async upload:** `asyncUploadTimeSlice` mặc định 2ms, buffer 16MB. Chỉ texture/mesh tắt Read/Write mới đi đường này. Tăng time slice khi đang che loading, giảm lại khi chơi. `[nguồn: Unity blog]`
- `Object.InstantiateAsync` (2022.3.20+ / Unity 6): phần integration và Awake vẫn chạy trên main thread; có bug đã biết với particle có sub-emitter. `[nguồn: Unity docs]`

### 10.4 Chống giật (hitch)
- **GC:**
  - Giữ Incremental GC (mặc định).
  - Không cấp phát mỗi frame hay mỗi lần chạm: cache `WaitForSeconds`, dùng `CompareTag`, không LINQ hay boxing ở đường nóng.
  - Gọi `Resources.UnloadUnusedAssets()` + `GC.Collect()` **khi màn hình đang bị che**.
  `[nguồn: Unity GC docs, Unity blog]`
- **Pool** tạo sẵn lúc loading/menu (`UnityEngine.Pool.ObjectPool<T>`, 2021.1+). Gồm: ô, hạt, popup, vật bay, AudioSource.
- **DOTween `SetTweensCapacity`** lúc khởi động (4.5).
- **Shader:**
  - Unity ≤ 2022: shader compile lần đầu dùng gây khựng. `ShaderVariantCollection.WarmUp` **không chính xác** trên Vulkan/Metal/DX12, nên ưu tiên render mỗi material một lần ngoài màn hình khi đang load. `[nguồn: Unity shader loading]`
  - Unity 6: ghi `GraphicsStateCollection` trong development build, lưu mỗi graphics API/platform một file, rồi `WarmUp`/`WarmUpProgressively` lúc loading. API này còn **experimental**. `[nguồn: Unity PSO tracing]`
  - Kiểm tra Profiler xem còn `Shader.CreateGPUProgram` / `CreateGraphicsPipeline` lúc chơi không.
- **Chi phí lần đầu** (particle/VFX đầu tiên, glyph font mới): spawn và play mỗi thứ một lần ngoài màn hình khi loading. Checkbox **Prewarm của ParticleSystem không phải cách chữa khựng** (nó chỉ mô phỏng trước hệ thống loop). Dùng TMP font atlas tĩnh hoặc điền sẵn.
- **Texture:** ASTC (ETC2 fallback cho Android cũ), Sprite Atlas, UI/2D không mipmap, tắt Read/Write, max size nhỏ nhất còn chấp nhận được. `[nguồn: Unity graphics tips]`
- **Profile lần đầu tiên** mỗi level và mỗi hiệu ứng chạy, trên máy yếu, không chỉ lúc chạy ổn định.

### 10.5 Khởi động app
- **Scene 0 thật nhẹ** (bootstrap). Splash Unity hiện trong lúc scene 0 load; `Start()` nặng ở scene đầu kéo dài splash. Dời việc nặng ra sau frame đầu tiên. `[nguồn: Unity splash docs, issue tracker]`
- Giữ thư mục `Resources/` nhỏ, vì nó được index khi khởi động; dùng Addressables. `[nguồn: Unity docs]`
- Android: IL2CPP; gọi `ReportFullyDrawn` khi game tương tác được; **đo trên release build**.
- Threes bị chê vì khởi động 5–10s. `[nguồn: HN]`

### 10.6 Ngân sách frame & nhiệt
- ~65% frame (~11ms ở 60fps). **Test sau 10–15 phút chơi**, vì máy throttle về sau. `[nguồn: Unity]`
- Android: Adaptive Performance (ADPF) để đọc thermal headroom. Không chạy 120Hz nếu không cần.
- Giới hạn tổng số hạt; chú ý **overdraw** của hạt trong suốt chồng nhau; quality tier giảm hiệu ứng trên máy yếu. Post-processing đắt trên mobile. `[nguồn: Feel docs]`

---

## 11. Bài học từ các game hit

> Không nguồn công khai nào cho thông số khung hình (curve, ms) của các game này. Phần dưới là cơ chế và cảm giác **đã kiểm chứng** từ store, phân tích, review. Phần "suy ra" có ghi rõ. Muốn có con số chính xác thì phải quay video gameplay và đo từng frame.

**Pixel Flow! (Loom Games, Scopely mua phần lớn cổ phần; ~200 triệu USD người chơi chi tiêu năm đầu)**
- Chạm một pig, nó chạy trên băng chuyền quanh bức tranh pixel và bắn bóng vào khối cùng màu. Số trên đầu là đạn. Hết đạn thì rời sân; còn đạn thì vào 1 trong 5 slot chờ. Băng chuyền có sức chứa. `[nguồn: App Store]`
- "Mọi thứ phản hồi tức thì", haptic "đập" đúng lúc, "mỗi chạm đều có ý nghĩa". Kỹ năng "slinging": chạm nối nhanh có thể dồn tới 10 đơn vị vượt giới hạn khay. Bàn chơi tất định, nên thua là do mình. `[nguồn: Deconstructor of Fun]`
- Băng chuyền chạy liên tục quanh tác phẩm → "nhanh" và "sống". Áp lực khay hiển thị rõ chứ không phải hình phạt ẩn. `[nguồn: DoF]`
- "Sạch và nhanh", không có timer. `[nguồn: Gamigion]` Hình ảnh tương phản cao, hiểu được trong 30 giây đầu. `[nguồn: AppSamurai]`

**Loop Sort (VOODOO / Garawell)**
- Chạm xe tải, xe đổ khối lên băng chuyền, khối **tự sắp xếp và ghép cặp**. Chướng ngại: khối ẩn, rèm kéo lên khi sort xong một màu, băng, rào. Tagline: "Satisfying Sorting Puzzle". `[nguồn: App Store]`
- Quảng cáo cả thể loại dùng âm ASMR dễ chịu `[nguồn: Mobidictum]`. Các game cùng dòng của Voodoo: marble "lăn, click, đổ" (Marble Sort); cát chảy làm tranh biến dạng (Sand Loop). Voodoo tạo khác biệt bằng vật lý thật. `[nguồn: Gamigion]`

**Game kiến: không xác định chắc "Ant Colony"; khả năng cao là Colony Flow! (ABI Global)**
- Chạm một hộp, đàn kiến cùng màu bò ra, khuân khối cùng màu từ tranh pixel về tổ. 5 slot, không timer. Người chơi: "thích xem kiến đi tới đi lui". Store copy: animation thoả mãn, nhạc êm. `[nguồn: App Store]`
- Food Hunt: Pixel Puzzle (Funfinity) làm concept kiến trước. Kiến *ăn* khối, quảng cáo "ASMR chomping", có **chế độ 2× tốc độ** vì người chơi chê kiến chậm. `[nguồn: App Store]`

**Royal Match (Dream Games)**
- ~25 nước/level, chỉ **4 màu** → cascade tự động nhiều hơn. Combo "ngắn, mượt, vừa đủ thoả mãn". Xáo bàn miễn phí. Propeller đổi mục tiêu giữa chừng. `[nguồn: Naavik]`
- Tài nguyên bay từ chỗ mở khoá về container; King lo lắng khi sắp thua; một nút Play duy nhất. `[nguồn: Funovus]`
- Được đi nước tiếp trong lúc cascade. `[nguồn: Medium/Gamigion, trích đoạn]`

**Candy Crush:** pitch +1 bán cung mỗi hàng xoá thêm, kết bằng voice "Delicious"; thua là thang piano đi xuống, tông vui; âm chuyển cảnh báo menu đã sẵn sàng; gần như không có feedback tiêu cực; Sugar Crush cuối màn khoá input và tự nổ kẹo đặc biệt còn lại. `[nguồn: Game Developer; fan wiki]`

**Block Blast (Hungry Studio):** đội làm game nêu rõ VFX và rung là mục tiêu polish; chạy 50+ A/B test mỗi tuần, kể cả cho SFX, ~3% có kết quả tích cực. `[nguồn: Baijing]` Có feedback rõ khi đi nước "perfect"; độ căng "đọc được". `[nguồn: DoF]`

**Hexa Sort:** tự sắp xếp → "bàn chơi luôn tự giải" → vòng feedback nhanh hơn. `[nguồn: DoF]` Người chơi khen tiếng merge. `[nguồn: review]`

**Match 3D / Triple Match:** đổi từ kéo sang **chạm** để bớt ma sát; cảm giác "như đồ chơi fidget"; slot giới hạn thay timer làm điều kiện thua. `[nguồn: DoF]`

**Screw Jam / Screwdom:** "đọc nhanh, tương tác đã tay", fantasy "tháo rời"; vài phút đầu thư thả rồi mới chặt dần. `[nguồn: DoF]` Biến thể sợi len: kéo mượt hơn, chất liệu mềm hơn. `[nguồn: Playturbo]`

**Hướng dẫn từ publisher:**
- **Voodoo:** snackable, trực quan (hiểu trong vài giây), dễ tha thứ, "Youtubable" (nhiều chuyển động nhìn thấy được), gameplay trước tiên. `[nguồn: GameAnalytics]`
- **Homa:** "thoả mãn ngay từ đầu": làm lớp visual và thưởng trước khi test; checklist prototype có "juicy visuals" + "basic visual and haptic feedback". `[nguồn: GameAnalytics, trích đoạn]`
- **Kwalee:** người mới phải hiểu trong 3 giây; "hình ảnh thoả mãn và khoảnh khắc thoả mãn". `[nguồn: PocketGamer.biz]`
- **Supersonic:** cho thấy core mechanic sớm nhất có thể; ăn mừng sớm (confetti, thưởng làm "mỗi chạm đều thoả mãn"); feedback giác quan tức thì (lấp lánh, SFX rõ); mỗi lần chỉ một mechanic mới; sau level khó là level thở. `[nguồn: Supersonic blog]`
- **SayGames:** cài đỉnh căng "close win"; level quá dài gây mệt, quá ngắn không có cảm xúc; tránh level "hố đen". `[nguồn: SayGames blog]`
- **CrazyLabs:** một mục tiêu mỗi level, nói được trong 2–3 từ; "tạo sự thoả mãn là then chốt". `[nguồn: Mobidictum, CrazyLabs]`

**Mẫu chung (tổng hợp):**
- Chạm → phản hồi tức thì → **một chuỗi tự chạy và liên tục trả thưởng**.
- **Chạm tốt hơn kéo** cho thao tác chính.
- Chuyển động **liên tục, có vật lý**; phản hồi cốt lõi **dứt khoát, nhanh**, không lề mề (suy ra từ review chê kiến chậm).
- Hoàn thành nhóm có khoảnh khắc nghe và nhìn rõ; thưởng bay về bộ đếm.
- Sức chứa và slot hiển thị rõ là nguồn căng thẳng; một mục tiêu, một nút chính.
- Âm ASMR dễ chịu; tích cực là chủ đạo; thua nhẹ nhàng.
- Không chạm thừa trước level; ăn mừng ngắn, tua được; level sau sẵn sàng ngay.
- Độ khó hình răng cưa có level thở; bàn chơi tất định.

---

## 12. Những bẫy cần tránh

- **Over-juice:** cái gì cũng to thì không gì nổi bật; khó đọc bàn chơi, chơi kém đi `[nguồn: Juul & Begy; Kao et al.]`. Dreamscaper dùng hitstop và shake cho mọi đòn đánh thường, và đến màn 1 đã thấy khó chịu `[nguồn: Reddit]`.
- **Quá nảy:** UI dùng spring nảy nhiều bị chê "Nausea Simulator", gây say. Chữ nhảy múa làm chậm việc đọc kết quả. `[nguồn: Reddit]` Polish kiểu Threes với một số người là "quá dẻo, khó chịu" `[nguồn: HN]` → có tuỳ chọn giảm.
- **Juice không cứu được gameplay nhạt** `[nguồn: Wayline; Reddit]`; SimCity 5 có feel tốt hơn Cities: Skylines nhưng không phải game hay hơn `[nguồn: Reddit]`.
- **Animation dài bắt chờ**, nhất là animation lặp mỗi nước; **màn thưởng không tua được**.
- **Bỏ input khi đang animate** hoặc khoá input không có buffer.
- **Phản hồi trên touch-up** thay vì touch-down; scale nút làm mất click (3.1).
- **targetFrameRate mặc định 30** trên mobile.
- **Âm lặp nhàm** (không biến thể) hoặc **ồn** (quá nhiều voice); random pitch quá rộng cho âm tonal; pitch combo không theo nốt nhạc.
- **Rung mỗi item, rung rè** (`Handheld.Vibrate`).
- **Rung màn hình cho sự kiện nhỏ**; hitstop cộng dồn thành slow-mo.
- **Feedback tiêu cực nặng nề.**
- **Thiếu feedback cho sự kiện tự động** (cascade, xáo bàn).
- **Tween chồng nhau** (không kill), punch bị trôi scale.
- **Vật xuất hiện tween thẳng lên scale gốc** (không vượt) trông nhạt; hoặc dùng OutBack mặc định cho quãng ngắn (0.85 → 1, 0.92 → 1) nên gần như không thấy vượt (4.7).
- **Khựng lần đầu** (shader, particle, font) và **GC spike** khi chạm.
- **Reload scene giữa các level.**

---

## 13. Accessibility & Settings

- **Toggle bắt buộc:** Sound, Music, Haptics, **Reduce motion** (tắt shake/flash, giảm hạt, rút ngắn ăn mừng, bỏ bounce). Với FeedbackKit: `FeedbackGlobal.SoundEnabled`, `HapticsEnabled`, `ReduceMotion` (giảm biên độ chuyển động, bỏ shake), `Enabled` (tắt toàn bộ). `[nguồn: Feel (FeedbackTypeAuthorized, GlobalMMFeedbacksActive); Reddit, đồng thuận]`
- **Không nhấp nháy toàn màn > 3 lần/giây** (WCAG, an toàn cho người dễ lên cơn động kinh).
- Không chỉ dùng màu để phân biệt ô/power-up: thêm hình dạng hoặc ký hiệu (mù màu).
- Chuyển động không là tín hiệu duy nhất: luôn có tiếng hoặc rung đi kèm `[nguồn: Apple HIG]`. Ngược lại, tiếng không là tín hiệu duy nhất, vì nhiều người tắt tiếng.
- Hitbox ≥ 44pt; nút chính trong vùng ngón cái với tư thế cầm phổ biến `[nguồn: Apple; Mobile Free To Play]`.

---

## 14. Checklist nghiệm thu

**Độ phản hồi**
- [ ] `targetFrameRate` = 60 (hoặc mức đã chọn có chủ đích); Android bật Frame Pacing (đã hỏi trước khi đổi setting).
- [ ] Mọi nút và mọi lần chạm ô phản hồi trên **touch-down**, trong frame kế tiếp, kể cả thao tác không hợp lệ.
- [ ] Chạm liên tục khi animation đang chạy **không bị nuốt** (buffer hoặc skip-ahead).
- [ ] Kéo thả: visual bám ngón tay, không trễ thấy được.
- [ ] Âm SFX ngắn: Decompress On Load, Preload, không Load In Background; DSP buffer Best Latency (đã hỏi).
- [ ] Haptic khởi tạo sớm; iOS prepare khi down; không dùng `Handheld.Vibrate`.

**Nút & UI**
- [ ] Nút có cấu trúc Root (không scale) → Visual; press 0.92–0.95 khi down, OutBack khi up; kéo ra ngoài thì hủy.
- [ ] Nút disabled có phản hồi; chống double-tap.
- [ ] Popup mở 250–350ms, đóng nhanh hơn; chuyển màn ≤ ~400ms.
- [ ] Vật/popup xuất hiện có pop-in **nhỏ → lớn hơn → gốc** với đỉnh nhìn thấy được (4.7).
- [ ] Số trên HUD đếm dần và có punch.

**Gameplay feedback**
- [ ] Match, rơi, thu thập có visual + sound; sự kiện quan trọng có haptic.
- [ ] Combo/chuỗi leo thang rõ (pitch theo thang âm, chữ khen to dần), reset khi hết chuỗi.
- [ ] Hoàn thành nhóm có khoảnh khắc "chốt".
- [ ] Power-up có anticipation, có hướng, phá lan theo thứ tự.
- [ ] Item thu thập bay về HUD; HUD phản hồi khi nhận.
- [ ] Thắng có cao trào; màn kết quả từng bước; **tua được**.
- [ ] Thua nhẹ nhàng; cho thấy đã gần thắng; retry một chạm.

**Âm thanh & haptics**
- [ ] Âm lặp có biến thể; random pitch đúng biên độ; âm hero cố định.
- [ ] Giới hạn voice; không phasing; nhạc thấp hơn SFX; duck dưới âm hero.
- [ ] Nghe ổn trên loa điện thoại; loudness khoảng −16 đến −18 LUFS.
- [ ] Haptic không rung từng item; có toggle.

**Loading & hiệu năng**
- [ ] Không reload scene giữa các level; level sau được preload trong lúc ăn mừng.
- [ ] Chuyển cảnh bắt đầu ngay khi chạm; không đứng hình > 1 frame.
- [ ] Shader/particle/font đã warmup; không có GC spike khi chạm (kiểm bằng Profiler).
- [ ] Pool cho ô, hạt, popup, vật bay, AudioSource; DOTween capacity đặt sẵn.
- [ ] FPS ổn định trên máy yếu nhất được hỗ trợ, sau 10–15 phút chơi.

**Settings & accessibility**
- [ ] Toggle Sound / Music / Haptics / Reduce motion hoạt động; iOS audio session đúng category.
- [ ] Không flash > 3 lần/giây; không chỉ dựa vào màu.
- [ ] Thông số feedback nằm trong config, chỉnh không cần sửa code.

---

## 15. Nguồn

**Talk, sách, bài nền tảng**
- Jonasson & Purho, "Juice It or Lose It" (GDC Europe 2012): https://www.gamedeveloper.com/design/video-is-your-game-juicy-enough-
- Jan Willem Nijman, "The Art of Screenshake": https://www.youtube.com/watch?v=AJdEqssNZ-U · tóm tắt: https://rpgplayground.com/research-making-a-juicy-game/ · thử nghiệm: https://www.bluetengu.com/2014/12/12/art-of-screenshake-experiments/
- Steve Swink, *Game Feel* (sách, 2008)
- GDC Vault, "Game Feel: Why Your Death Animation Sucks": https://gdcvault.com/play/1022759/Game-Feel-Why-Your-Death
- GDC Vault, "Oh My! That Sound Made the Game Feel Better!": https://gdcvault.com/play/1022808/Oh-My-That-Sound-Made
- GameAnalytics, "Squeezing more juice out of your game design": https://www.gameanalytics.com/blog/squeezing-more-juice-out-of-your-game-design

**Code demo**
- deepnight/gamefeel (Haxe, 33 toggle): https://github.com/deepnight/gamefeel · https://deepnight.net/games/game-feel/
- GDevelop Game Feel Demo: https://gdevelop.io/game-example/free/game-feel-demo · source: https://github.com/GDevelopApp/GDevelop-examples/tree/main/examples/game-feel-demo
- GDevelop extensions: https://wiki.gdevelop.io/gdevelop5/extensions/camera-shake/ · https://wiki.gdevelop.io/gdevelop5/extensions/character-game-feel/
- Feel (More Mountains): https://feel-docs.moremountains.com/demos.html · https://feel-docs.moremountains.com/springs.html · https://feel-docs.moremountains.com/screen-shakes.html · https://feel-docs.moremountains.com/recipes.html · https://feel-docs.moremountains.com/nice-vibrations.html
- Mix and Jam, balatro-feel (Unity): https://github.com/mixandjam/balatro-feel
- GameFeelSimulator (Godot, tham số di chuyển platformer; ý tưởng jump buffer → input buffer)

**Casual / puzzle / game hit**
- Candy Crush audio breakdown: https://www.gamedeveloper.com/audio/why-candy-crush-saga-is-so-engaging-an-audio-breakdown
- Peggle: https://www.gamezebo.com/news/peggle-how-a-spark-started-a-fever/ · https://www.audiogang.org/peggle2-sonic-joy/ · https://www.audiogang.org/peggle-2-storytelling-through-adaptive-music/ · https://www.audiogang.org/realtime-synthesis-for-sound-creation-in-peggle-blast/
- Naavik về Royal Match: https://naavik.co/digest/why-dream-games-success-is-a-challenge-to-replicate/
- Funovus về Royal Match: https://www.funovus.com/blogs/royal-match-dominates-match-3-what-can-all-designers-learn/
- Pixel Flow: https://apps.apple.com/us/app/pixel-flow/id6751056652 · https://www.deconstructoroffun.com/blog/2026/2/13/pixel-flow-the-publishers-dream · https://www.deconstructoroffun.com/blog/pixel-flow-and-the-rise-of-sort-puzzles · https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born · https://appsamurai.com/blog/pixel-flow-an-analysis-of-its-rapid-rise/ · https://mobilegamer.biz/scopely-acquires-pixel-flow-maker-loom-games/
- Loop Sort: https://apps.apple.com/us/app/loop-sort/id6751101794 · https://mobidictum.com/?p=409557 · https://www.gamigion.com/?p=34075
- Colony Flow!: https://apps.apple.com/us/app/colony-flow/id6779167923 · Food Hunt: https://apps.apple.com/us/app/food-hunt-pixel-puzzle/id6769314786
- Playturbo (conveyor/pixel sort creatives): https://www.playturbo.com/blog/pixel-sorting-game-creative-patterns · screw puzzle: https://www.playturbo.com/blog/screw-puzzle-game-creative-best-practices-2026
- Gamigion, Color Block Jam deconstruction: https://gamigion.substack.com/p/color-block-jam-level-deconstruction
- Block Blast: https://www.baijing.cn/article/50876 · https://www.deconstructoroffun.com/blog/2026/1/19/from-tetris-to-block-blast-why-block-puzzles-never-stop-printing
- Match 3D: https://www.deconstructoroffun.com/blog/the-making-of-match-3d · Screw puzzle: http://www.deconstructoroffun.com/blog/the-screw-puzzle-gold-rush · Hybrid-casual puzzles: https://www.deconstructoroffun.com/blog/2025/2/3/hybridcasual-puzzles-expanding-the-puzzle-market
- Publisher: Voodoo https://www.gameanalytics.com/blog/voodoo-guide-mobile-game-design-keep-things-simple · Kwalee https://www.pocketgamer.biz/news/72394/kwalee-on-what-exactly-makes-a-hypercasual-game/ · Supersonic https://supersonic.com/learn/blog/optimizing-ftue/ · https://supersonic.com/learn/blog/the-basics-and-best-practices-for-puzzle-games/ · SayGames https://blog.say.games/posts/how-to-work-with-difficulty-in-hybrid-puzzles-so-players-dont-leave--but-stay-and-pay · CrazyLabs https://mobidictum.com/how-crazylabs-ideated-scaled-makeup-kit-color-mixing-to-the-top/
- Mobile Free To Play, touch control design: https://mobilefreetoplay.com/control-mechanics/

**Diễn đàn**
- r/gamedesign, good examples of game juice: https://www.reddit.com/r/gamedesign/comments/198fctp/good_examples_of_game_juice_game_feel/
- r/gamedev: juicy buttons/UI https://www.reddit.com/r/gamedev/comments/e56why/ · menu button press https://www.reddit.com/r/gamedev/comments/5p8ker/ · go-to juicing https://www.reddit.com/r/gamedev/comments/5y5yro/ · UI springs https://www.reddit.com/r/gamedev/comments/heo663/ · juicy SFX https://www.reddit.com/r/gamedev/comments/pb63sq/ · juice it or lose it https://www.reddit.com/r/gamedev/comments/u75rh/ · pitch shifting https://www.reddit.com/r/gamedev/comments/5mn4yg/ · haptics on phones https://www.reddit.com/r/gamedev/comments/bzsupu/ · juicy score screen https://www.reddit.com/r/gamedev/comments/20ers6/ · turn-based feel prototype https://www.reddit.com/r/gamedev/comments/51slq0/
- r/Unity2D UI tween: https://www.reddit.com/r/Unity2D/comments/vhe4mo/ · r/godot tween glitch: https://www.reddit.com/r/godot/comments/1biuxzb/
- Hacker News: https://news.ycombinator.com/item?id=35273139 · https://news.ycombinator.com/item?id=8030413 · https://news.ycombinator.com/item?id=29963267
- Input buffering cho game lưới: https://pancelor.bearblog.dev/input-buffering-for-grid-games/

**Nghiên cứu**
- Juul & Begy, "Good Feedback for Bad Players?" (2016): https://adk.elsevierpure.com/en/publications/good-feedback-for-bad-players-a-preliminary-study-of-juicy-interf
- Kao et al., "How does juicy game feedback motivate?" (CHI 2024): https://spiral.imperial.ac.uk/entities/publication/253c32f5-d124-4a23-88b5-9aaaf114608d
- Hicks et al., "Good Game Feel" (DiGRA 2018): https://dl.digra.org/index.php/dl/article/view/936
- Smets & van der Spek, "That Sound's Juicy!" (2021): https://research.tue.nl/en/publications/that-sounds-juicy-exploring-juicy-audio-effects-in-video-games/
- Juicy Haptic Design (Waterloo): https://uwaterloo.ca/haptic-experience-lab/projects/juicy-haptic-design-vibrotactile-embellishments-can-improve
- Vogel & Baudisch, Shift (CHI 2007): https://projects.csail.mit.edu/hci-reading/papers/chi2007-vogel.pdf
- Deber et al., touch latency perception (CHI 2015), DOI 10.1145/2702123.2702300
- Audio–haptic latency: https://arxiv.org/pdf/2307.05451 · 3GPP TR 22.870: https://itecspec.com/3gpp/22.870/s/14.2.4.2

**Nền tảng: motion, haptics, audio**
- Material 3 motion tokens: https://github.com/material-components/material-components-android/blob/master/docs/theming/Motion.md · Expressive: https://github.com/androidx/androidx/blob/androidx-main/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/tokens/ExpressiveMotionTokens.kt · Legacy: https://m1.material.io/motion/duration-easing.html
- Material sound: https://m2.material.io/design/sound/applying-sound-to-ui.html · https://m2.material.io/design/sound/sound-choreography.html
- Apple: Designing Fluid Interfaces (WWDC18) https://developer.apple.com/videos/play/wwdc2018/803/ · Animate with springs (WWDC23) https://developer.apple.com/videos/play/wwdc2023/10158/ · HIG Motion https://developer.apple.com/design/human-interface-guidelines/motion · Buttons https://developer.apple.com/design/human-interface-guidelines/buttons · Playing audio https://developer.apple.com/design/human-interface-guidelines/playing-audio · Playing haptics https://developer.apple.com/design/human-interface-guidelines/playing-haptics · Understanding hitches https://developer.apple.com/documentation/xcode/understanding-hitches-in-your-app · UIFeedbackGenerator.prepare https://developer.apple.com/documentation/uikit/uifeedbackgenerator/prepare() · Predicted touches https://developer.apple.com/documentation/uikit/minimizing-latency-with-predicted-touches
- Android: Haptics principles https://developer.android.com/develop/ui/views/haptics/haptics-principles · Frame pacing https://developer.android.com/games/sdk/frame-pacing · Oboe low latency https://developer.android.com/games/sdk/oboe/low-latency-audio · Audio latency https://developer.android.com/ndk/guides/audio/audio-latency · ADPF https://developer.android.com/games/optimize/adpf
- Meta, Haptic feedback: https://developers.facebook.com/documentation/games/build/haptic-feedback
- Nielsen, time scales of UX: https://jakobnielsenphd.substack.com/p/time-scale-ux

**Unity**
- targetFrameRate: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html · vSyncCount: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html · OnDemandRendering: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.OnDemandRendering.html
- Input System settings: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/Settings.html · Touch: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Touch.html · uGUI Button: https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-Button.html
- Audio Manager: https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/class-audio-manager · AudioClip: https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html · Native Audio: https://exceed7.com/native-audio/
- allowSceneActivation: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html · backgroundLoadingPriority: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-backgroundLoadingPriority.html · Async upload: https://unity.com/blog/engine-platform/understanding-the-async-upload-pipeline · InstantiateAsync: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.InstantiateAsync.html
- Addressables: https://docs.unity3d.com/Packages/com.unity.addressables@2.2/manual/LoadingScenes.html · https://docs.unity3d.com/Packages/com.unity.addressables@2.3/manual/MemoryManagement.html
- Incremental GC: https://docs.unity3d.com/6000.0/Documentation/Manual/performance-incremental-garbage-collection.html · Shader loading: https://docs.unity3d.com/2022.3/Documentation/Manual/shader-loading.html · PSO tracing: https://docs.unity3d.com/Manual/shader-pso-trace.html · Splash: https://docs.unity3d.com/Manual/class-PlayerSettingsSplashScreen.html
- Mobile optimization: https://unity.com/blog/games/optimize-your-mobile-game-performance-tips-on-profiling-memory-and-code-architecture-from · https://unity.com/blog/games/optimize-your-mobile-game-performance-get-expert-tips-on-physics-ui-and-audio-settings · https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets · UI: https://unity.com/how-to/unity-ui-optimization-tips
- DOTween: https://dotween.demigiant.com/documentation.php · PrimeTween: https://github.com/KyryloKuzyk/PrimeTween
- Smoothing: https://howlingmoonsoftware.com/improved-lerp-smoothing/

**Âm thanh**
- Violet Recording, UI sounds: https://violetrecording.com/how-to-design-ui-sounds/
- Audiokinetic, loa điện thoại: https://www.audiokinetic.com/en/blog/loudness-and-frequency-response-on-popular-smart-phones · Unpacking foley: https://www.audiokinetic.com/en/blog/unpacking-the-fun-behind-the-foley · Mastering: https://www.audiokinetic.com/en/mastering-a-game-with-wwise-part1
- Game Developer: mobile audio https://www.gamedeveloper.com/audio/making-good-sounding-audio-for-mobile-games · pitch shifting https://www.gamedeveloper.com/audio/the-power-of-pitch-shifting
- IESD loudness: https://www.audiogang.org/wp-content/uploads/2015/04/IESD-Mix-Ref-Levels-v03.02.pdf
- FMOD instance limiting: https://www.fmod.com/docs/2.03/studio/event-macro-controls-reference.html
- VCU Sonic feedback: https://ghostnet.vcu.edu/sonic-feedback
- Wayline, khi juice thành cái nạng: https://www.wayline.io/blog/the-seductive-squeeze-when-juice-in-game-development-becomes-a-crutch
