# Virtual Xbox 360 Controller (Keyboard + Mouse → Gamepad)
### Universal Virtual Controller for Windows PC & Steam Games (Windows 10/11 64-bit)

[![Build & Release](https://github.com/sanrockk2995/Virtual-Controller/actions/workflows/build.yml/badge.svg)](https://github.com/sanrockk2995/Virtual-Controller/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/sanrockk2995/Virtual-Controller?color=blue&label=Release)](https://github.com/sanrockk2995/Virtual-Controller/releases)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20WPF-purple)](https://dotnet.microsoft.com/)
[![Driver](https://img.shields.io/badge/Driver-ViGEmBus%20v1.22.0-brightgreen)](https://github.com/nefarius/ViGEmBus)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Language](https://img.shields.io/badge/Language-Ti%E1%BA%BFng%20Vi%E1%BB%87t%20%7C%20English-orange)](#-tiếng-việt)

---

**Languages / Chọn ngôn ngữ:**  
👉 **[ 🇻🇳 Tiếng Việt ](#-tiếng-việt)** &nbsp;|&nbsp; 👉 **[ 🇬🇧 English ](#-english)**

---

<br/>

# 🇻🇳 Tiếng Việt

Ứng dụng Windows hoàn chỉnh chuyển đổi tín hiệu **Bàn phím + Chuột** thành **Tay cầm ảo Xbox 360 Controller** thông qua driver chuẩn công nghiệp **ViGEmBus (Virtual Gamepad Emulation Bus)**.

Hệ điều hành Windows và trò chơi nhận diện thiết bị như một tay cầm Xbox 360 vật lý chính hãng (DirectInput / XInput), không can thiệp vào file game, không inject mã vào tiến trình game, hoàn toàn an toàn và trong sạch.

## Mục lục
1. [Tính năng nổi bật](#1-tính-năng-nổi-bật)
2. [Kiến trúc hệ thống](#2-kiến-trúc-hệ-thống)
3. [Profile mặc định (Default Controls)](#3-profile-mặc-định-default-controls)
4. [Thuật toán dịch chuyển Chuột sang Cần Analog (Mouse-to-Stick)](#4-thuật-toán-dịch-chuyển-chuột-sang-cần-analog-mouse-to-stick)
5. [Hệ thống Macro & Combo Tay cầm](#5-hệ-thống-macro--combo-tay-cầm)
6. [Giao diện người dùng (WPF GUI)](#6-giao-diện-người-dùng-wpf-gui)
7. [Bảo vệ an toàn: Tray & Phím tắt khẩn cấp](#7-bảo-vệ-an-toàn-tray--phím-tắt-khẩn-cấp)
8. [Hướng dẫn cài đặt Driver ViGEmBus](#8-hướng-dẫn-cài-đặt-driver-vigembus)
9. [Hướng dẫn Build, Test & Chạy](#9-hướng-dẫn-build-test--chạy)
10. [Cấu trúc mã nguồn](#10-cấu-trúc-mã-nguồn)
11. [Giấy phép (License)](#11-giấy-phép-license)

---

### 1. Tính năng nổi bật

- **Mô phỏng Xbox 360 chuẩn phần cứng:** Giao tiếp trực tiếp với kernel qua ViGEmBus, tương thích 100% với Steam, Epic Games, giả lập console và mọi tựa game hỗ trợ XInput.
- **Bộ dịch chuột sang analog chuyên sâu (Mouse-to-Gamepad Translation Engine):**
  - Sử dụng **Windows Raw Input (`WM_INPUT`)** chạy trên thread riêng biệt, thu nhận dữ liệu vi sai thô của chuột không bị giới hạn bởi mép màn hình hay tăng tốc con trỏ Windows.
  - **Độ nhạy độc lập (Independent X/Y Sensitivity):** Tùy chỉnh riêng độ nhạy trục ngang và trục dọc.
  - **Bù vùng chết game (Anti-Deadzone / Initial Deflection):** Đẩy tức thì analog vượt qua deadzone nội tại của game để phản hồi ngay khi chuột vừa dịch chuyển.
  - **Đường cong phản hồi (Response Curves):** Tuyến tính (*Linear*), Trễ nhẹ (*Delay*), Bứt tốc (*Aggressive*), Mượt mà Hermite (*Smooth*), Tức thì (*Instant*).
  - **Chuẩn hóa hình học cần gạt (Stick Geometry):** Hỗ trợ dạng Tròn (*Radial*) và dạng Chiếu vuông (*Square* - giúp đạt 100% deflection khi di chuyển góc chéo).
  - **Bộ lọc nhiễu (Noise Gate) & Làm mượt (Exponential Smoothing):** Lọc rung lắc chuột ở tốc độ cực thấp và làm êm chuyển động.
  - **Tùy chọn tắt cần gạt (`None / Vô hiệu hóa`):** Cho phép tắt hoàn toàn tín hiệu cần gạt phải hoặc trái khi dùng chuột, giữ cần gạt luôn ở tâm $(0.0, 0.0)$ để tránh xoay camera ngoài ý muốn khi chỉ gán nút chuột cho chức năng khác.
  - **Chặn phím bàn phím vật lý (Keyboard Input Blocking):** Ngăn chặn cửa sổ game nhận trùng lặp các phím vật lý đã được gán (W, A, S, D, Space, Shift, E, Q...), giải quyết triệt để hiện tượng xung đột điều khiển kép (dual-input). Tích hợp thuật toán chống kẹt phím thông minh và tự động whitelist bảo toàn phím khẩn cấp (`Ctrl + Alt + F10`) cùng các phím điều hướng hệ thống (`Alt + Tab`, `LWin`, `RWin`, `Ctrl + Shift + Esc`).
- **Hệ thống Macro & Combo Tay cầm Chuyên sâu (Advanced Macro & Combo System):**
  - **3 Chế độ thực thi linh hoạt:** Chạy 1 lần (*Play Once*), Lặp liên tục khi giữ phím (*Repeat While Held / Turbo*), và Bật/Tắt lặp tự động (*Toggle Loop*).
  - **Tổ hợp đa nút & Thao tác Cần Analog:** Gộp nhiều nút tay cầm trong một bước (ví dụ: `LB + RB` Chiêu cuối), kết hợp các hướng cần gạt analog (ví dụ: `LS_Up + B` Lộn nhào né đòn), và tạo chuỗi đòn đánh đa bước (`X -> X -> Y` Melee Combo).
  - **Bộ ghi Macro Trực tiếp (Live Macro Recorder):** Ghi lại nhịp bấm phím và độ trễ mili-giây theo thời gian thực với độ chính xác cao.
  - **Bộ Mẫu Gaming dựng sẵn (Built-in Presets):** 6 cấu hình nạp sẵn 1-click: Rapid Fire RT, Ultimate Combo, Melee Combo, Dodge Roll, Auto Bunny Hop, Auto Sprint.
  - **Tích hợp Chặn Phím Bàn Phím Vật Lý:** Tự động đưa phím kích hoạt macro vào danh sách chặn khi tính năng chặn phím vật lý hoạt động, ngăn phím macro bị rò rỉ vào game.
- **Hỗ trợ Đa ngôn ngữ Runtime (Localization):** Chuyển đổi linh hoạt giữa **Tiếng Việt 🇻🇳** và **Tiếng Anh 🇬🇧** trực tiếp trong cài đặt, cập nhật giao diện ngay lập tức mà không cần khởi động lại ứng dụng.
- **Quản lý Profile toàn diện:** Lưu trữ, xuất (Export), nhập (Import), tạo mới và nhân bản profile cấu hình dưới dạng JSON.
- **Trình kiểm tra trực quan thời gian thực (Controller Tester):** Màn hình mô phỏng phản hồi tay cầm thời gian thực với tọa độ cần analog và độ nhạy trigger.
- **Khay hệ thống & Hotkey an toàn:** Chạy ngầm tại System Tray, phím nóng khẩn cấp `Ctrl + Alt + F10` (tùy biến tự do trong Cài đặt) ngắt tín hiệu tay cầm ngay lập tức.
- **Tự động kiểm tra Cập nhật (GitHub Releases Auto-Update):** Tự động phát hiện phiên bản mới khi khởi động hoặc kiểm tra thủ công, hiển thị chi tiết Release Notes và liên kết tải file ZIP trực tiếp.
- **CI/CD Tự Động:** Tích hợp GitHub Actions tự động kiểm thử 76 bộ test và đóng gói bản phát hành release x64.

---

### 2. Kiến trúc hệ thống

```text
               ┌────────────────────────────────────────────────────────┐
               │                    Physical Inputs                     │
               │   - Keyboard Keys (Low-Level Windows Hook)             │
               │   - Mouse Buttons (Low-Level Windows Hook)             │
               │   - Mouse Movement (Windows Raw Input WM_INPUT Sink)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                      Input Layer                       │
               │  - InputState (Thread-safe atomic updates)             │
               │  - RawInputReceiver (Dedicated Win32 message thread)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                    Mapping Engine                      │
               │  - Key-to-Button & Trigger Binding Engine              │
               │  - MouseToStickTranslator (Velocity, Curves, Geometry) │
               │  - Independent Left/Right Stick Source (None/Mouse/KB) │
               │  - MacroEngine & Recorder (Tick-based State Machine)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                Virtual Controller Backend              │
               │  - IVirtualController / ViGEmController                │
               │  - Nefarius.ViGEm.Client (Kernel IOCTL bridge)         │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                   ViGEmBus Driver (R0)                 │
               │  - Virtual Xbox 360 Peripheral in Windows Device Mgr   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │               PC Games / Steam (XInput)                │
               └────────────────────────────────────────────────────────┘
```

---

### 3. Profile mặc định (Default Controls)

#### Bàn phím (Keyboard)
| Phím vật lý | Nút ảo Xbox 360 | Chức năng thông dụng |
| :--- | :--- | :--- |
| **W** | Left Stick Up | Đi lên / Tiến về trước |
| **S** | Left Stick Down | Đi xuống / Lùi lại |
| **A** | Left Stick Left | Di chuyển sang trái |
| **D** | Left Stick Right | Di chuyển sang phải |
| **Space** | **A** | Nhảy (Jump) / Xác nhận |
| **Left Shift** | **B** | Đánh cận chiến / Hủy |
| **E** | **X** | Tương tác / Nạp đạn |
| **Q** | **Y** | Đổi vũ khí / Chiêu đặc biệt |
| **R** | **RB** | Nút vai phải (Right Bumper) |
| **F** | **LB** | Nút vai trái (Left Bumper) |
| **Tab** | **Back** | Xem bản đồ / Bảng điểm |
| **Enter** | **Start** | Menu tạm dừng (Pause) |
| **Escape** | **Start** | Menu / Thoát |

#### Chuột (Mouse)
| Thao tác chuột | Thành phần ảo Xbox 360 | Ghi chú |
| :--- | :--- | :--- |
| **Di chuột X** | **Right Stick X** | Xoay góc nhìn ngang (trừ khi đặt là `None`) |
| **Di chuột Y** | **Right Stick Y** | Xoay góc nhìn dọc (trừ khi đặt là `None`) |
| **Chuột trái (LMB)** | **RT (Right Trigger)** | Bắn súng / Cò phải (Analog 0..255) |
| **Chuột phải (RMB)** | **LT (Left Trigger)** | Ngắm bắn / Cò trái (Analog 0..255) |
| **Chuột giữa (MMB)** | **RS Click** | Nhấn cần gạt phải (Right Thumbstick Click) |

---

### 4. Thuật toán dịch chuyển Chuột sang Cần Analog (Mouse-to-Stick)

Bộ dịch `MouseToStickTranslator` chuyển đổi vi sai chuột thành góc nghiêng analog theo chuỗi xử lý toán học chuẩn xác:

1. **Tính vận tốc chuột ($\text{counts/ms}$):**
   $$V_x = \frac{\Delta x}{\Delta t}, \quad V_y = \frac{\Delta y}{\Delta t}$$
2. **Áp dụng độ nhạy trục riêng biệt (Axis Sensitivity):**
   $$S_x = V_x \times \text{SensitivityX}, \quad S_y = V_y \times \text{SensitivityY}$$
3. **Lọc cổng nhiễu (Noise Gate / Noise Threshold):**
   Nếu độ lớn $\text{Mag} = \sqrt{S_x^2 + S_y^2} < \text{NoiseThreshold}$ thì output = 0.
4. **Đường cong phản hồi (Response Curve):**
   - *Linear:* $f(x) = x$
   - *Delay:* $f(x) = x^2$ (tăng độ chính xác khi di chuyển nhỏ)
   - *Aggressive:* $f(x) = \sqrt{x}$ (phản hồi nhanh, nhạy ở dải đầu)
   - *Smooth:* $f(x) = 3x^2 - 2x^3$ (đường cong Hermite)
   - *Instant:* $f(x) = 1.0$ (khi vượt ngưỡng)
5. **Bù vùng chết game (Anti-Deadzone):**
   $$\text{Deflection} = \text{AntiDeadzone} + (1.0 - \text{AntiDeadzone}) \times \text{Curve}(x)$$
6. **Chuẩn hóa hình học (Stick Geometry):**
   - *Radial:* Giới hạn độ dài vector $\le 1.0$ theo hình tròn.
   - *Square:* Chiếu vector hình tròn sang hình vuông để trục chéo luôn đạt trọn vẹn $1.0$ góc deflection.
7. **Làm mượt thời gian (Exponential Filter / Smoothing):**
   $$\vec{O}_{\text{t}} = \alpha \vec{O}_{\text{target}} + (1 - \alpha) \vec{O}_{\text{t-1}}$$

---

### 5. Hệ thống Macro & Combo Tay cầm

Virtual Controller tích hợp engine thực thi macro xác định theo nhịp tick đồng bộ (`MacroEngine`), cho phép tự động hóa chuỗi thao tác nút bấm và cần analog với độ trễ chuẩn xác từng mili-giây.

#### Các tính năng trọng tâm:

1. **3 Chế độ thực thi (Execution Modes):**
   - **Chạy 1 lần (Play Once):** Kích hoạt chuỗi bước một lần duy nhất khi nhấn phím (bắt sườn lên - *rising edge*). Phù hợp tối đa cho combo tuyệt chiêu, né đòn hoặc nạp đạn nhanh.
   - **Lặp liên tục khi giữ phím (Repeat While Held / Turbo):** Tự động lặp lại chuỗi thao tác liên tục chừng nào phím kích hoạt còn được giữ, và dừng ngay khi nhả phím (bắt sườn xuống - *falling edge*). Tùy chỉnh tham số **Độ trễ lặp (Loop Delay)** giữa các vòng. Tối ưu cho chế độ bắn nhanh liên thanh (Rapid Fire RT) hoặc nhảy liên tục (Auto Bunny Hop).
   - **Bật/Tắt lặp tự động (Toggle Loop):** Nhấn phím lần thứ nhất để bật chế độ tự động lặp vô hạn trong nền; nhấn lại lần thứ hai để ngắt chu kỳ ngay lập tức. Hoàn hảo cho chạy nhanh liên tục (Auto Sprint) hoặc giữ nút rảnh tay dài hạn.

2. **Tổ hợp Đa nút & Thao tác Cần Analog (Multi-Button Combos & Stick Actions):**
   - **Tổ hợp nhiều nút trong một bước:** Cho phép gộp đồng thời nhiều nút ảo trong cùng một bước duy nhất (ví dụ: nhấn đồng thời `LB + RB` để tung Chiêu Cuối - Ultimate Skill).
   - **Tích hợp hướng cần gạt Analog:** Hỗ trợ trực tiếp các hướng cần gạt trái/phải (`LS_Up`, `LS_Down`, `LS_Left`, `LS_Right`, `RS_Up`, `RS_Down`, `RS_Left`, `RS_Right`) kết hợp với nút bấm (ví dụ: đẩy cần tiến `LS_Up + B` để thực hiện động tác Lộn nhào né đòn - Dodge Roll Forward).
   - **4 Loại thao tác đa dạng trong từng bước:**
     - `Tap` (Nhấn & Nhả): Giữ nút trong khoảng thời gian `DurationMs` rồi nhả ra và chờ `DelayAfterMs`.
     - `PressDown / Hold` (Giữ nút): Nhấn đè nút và duy trì trạng thái qua các bước tiếp theo.
     - `ReleaseUp` (Nhả nút): Nhả nút đang được giữ đè.
     - `Wait` (Nghỉ / Chờ): Chờ một khoảng thời gian trước khi chuyển sang bước tiếp theo.
   - **Chuỗi đòn đánh phức tạp:** Dễ dàng lập trình các chuỗi combo đòn đánh có nhịp điệu chính xác như `X -> X -> Y` (Melee Combo).

3. **Bộ ghi Macro Trực tiếp (Live Macro Recorder):**
   - Ghi lại các thao tác bấm nút ảo và độ trễ thực tế theo thời gian thực (real-time recording) nhờ đồng hồ hiệu năng cao (`Stopwatch`).
   - Tự động đo đạc chính xác thời gian giữ nút (`DurationMs`) và khoảng cách giữa các lần bấm (`DelayAfterMs`), hoặc có thể chọn chế độ chuẩn hóa thời gian cố định (Fixed Timing).

4. **Bộ Mẫu Gaming Dựng sẵn (Built-in Gaming Presets):**
   - Cung cấp sẵn 6 mẫu macro phổ biến trong game, nạp nhanh chỉ với 1 cú click:
     - **Rapid Fire RT (Turbo Shoot):** Bắn liên thanh nhấp nhả cò RT siêu tốc (Hold 35ms, Delay 35ms, Loop Delay 10ms, Chế độ: Repeat While Held).
     - **Ultimate Skill (LB + RB):** Nhấn đồng thời 2 nút vai LB + RB (Duration 80ms, Chế độ: Play Once).
     - **Melee Combo (X -> X -> Y):** Chuỗi cận chiến gồm 2 đòn đánh nhẹ tiếp nối 1 đòn đánh mạnh (Chế độ: Play Once).
     - **Dodge Roll Forward (LS_Up + B):** Đẩy cần trái tiến lên trước kết hợp nút B để né đòn né đạn (Chế độ: Play Once).
     - **Auto Bunny Hop (Turbo A):** Nhấp nhả nút nhảy A liên tục khi giữ phím (Hold 45ms, Delay 45ms, Chế độ: Repeat While Held).
     - **Auto Sprint Hold (LS):** Bật/tắt giữ nút nhấn cần gạt trái `LS` để chạy nhanh liên tục (Chế độ: Toggle Loop).

5. **Tích hợp Chặn Phím Bàn Phím Vật Lý (Physical Keyboard Blocking Integration):**
   - Khi tính năng "Chặn phím bàn phím vật lý" (Physical Keyboard Blocking) được kích hoạt, phím kích hoạt macro (`TriggerInput`) sẽ tự động được thêm vào danh sách chặn của Low-Level Keyboard Hook (`WH_KEYBOARD_LL`).
   - Ngăn chặn hoàn toàn hiện tượng phím kích hoạt macro (ví dụ: `V`, `Z`, `G`, `CapsLock`...) bị lọt ký tự vào trò chơi, loại bỏ hoàn toàn xung đột điều khiển kép (dual-input).
   - Tuyệt đối an toàn: Tự động bảo toàn phím khẩn cấp (`Ctrl + Alt + F10`) và các tổ hợp phím hệ thống Windows (`Alt + Tab`, `Win`, `Ctrl + Shift + Esc`).

---

### 6. Giao diện người dùng (WPF GUI)

1. **Tổng quan (Dashboard):** Hiển thị trạng thái kết nối tay cầm, nút Bật/Tắt (Enable/Disable), chọn hồ sơ nhanh và phím tắt kiểm tra.
2. **Gán nút (Button Mapping):** Ma trận gán phím chi tiết, nhận diện phím bấm vật lý tự động khi nhấn phím.
3. **Cần gạt (Analog Settings):**
   - Chọn nguồn tín hiệu độc lập: `Bàn phím (WASD)`, `Chuột`, hoặc `Không (Vô hiệu hóa)`.
   - Điều chỉnh thanh trượt: Sensitivity X/Y, Deadzone, Anti-Deadzone, Response Curve, Geometry, Smoothing, Invert X/Y.
4. **Macro & Combo:** Thiết lập chuỗi nút bấm, cấu hình chế độ chạy (Play Once, Repeat While Held, Toggle Loop), ghi trực tiếp (Live Recorder) và áp dụng mẫu có sẵn (Presets).
5. **Kiểm tra tay cầm (Controller Tester):** Giám sát trực tiếp chuyển động 2 cần analog, lực bóp cò LT/RT và phản hồi toàn bộ các nút bấm.
6. **Hồ sơ (Profiles):** Quản lý lưu/xóa/đổi tên profile, nhập file `.json` hoặc xuất file để chia sẻ cho game thủ khác.
7. **Cài đặt (Settings):** Khởi động cùng Windows, khởi động thu nhỏ, tự động kết nối, chuyển đổi ngôn ngữ (Tiếng Việt / English) và nút kiểm tra cài đặt Driver ViGEmBus.

---

### 7. Bảo vệ an toàn: Tray & Phím tắt khẩn cấp

- **System Tray:** Đóng ứng dụng sẽ thu nhỏ xuống khay đồng hồ, giữ nguyên tay cầm ảo đang chạy ngầm trong game. Nhấn chuột phải vào icon tray để mở menu thao tác nhanh.
- **Tổ hợp phím khẩn cấp (Emergency Kill-Switch):**
  ```text
  Ctrl + Alt + F10 (Mặc định - có thể tùy chỉnh tự do trong tab Cài đặt)
  ```
  Nhấn tổ hợp này bất kỳ lúc nào để ngắt kết nối ảo, nhả toàn bộ các nút và reset cần analog về tâm ngay tức khắc. Người dùng có thể đổi sang phím bất kỳ (ví dụ: `Ctrl + Shift + F12`, `F11`, `Alt + Q`...).
- **Thu hồi driver an toàn:** Tự động giải phóng cổng ảo khỏi Windows Device Manager khi thoát chương trình.

---

### 8. Hướng dẫn cài đặt Driver ViGEmBus

Ứng dụng yêu cầu driver **ViGEmBus (Virtual Gamepad Emulation Bus)** để tạo tay cầm ảo.

#### Tự động cài đặt 1-Click (Khuyên dùng)
- Ứng dụng tự động kiểm tra driver khi khởi động. Nếu máy chưa có, ứng dụng sẽ hiện thông báo xác nhận và tự động tải bộ cài chính hãng từ GitHub về cài đặt cho bạn.
- Bạn cũng có thể nhấn nút **`Cài đặt ViGEmBus Driver`** trong tab Cài đặt bất kỳ lúc nào.

#### Cài đặt thủ công
1. Tải bộ cài chính thức: [ViGEmBus Release v1.22.0](https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe).
2. Chạy file cài đặt với quyền Administrator và làm theo hướng dẫn.
3. Mở lại Virtual Controller.

---

### 9. Hướng dẫn Build, Test & Chạy

#### Yêu cầu môi trường
- Hệ điều hành: Windows 10 / Windows 11 (64-bit).
- .NET 8.0 SDK (nếu biên dịch mã nguồn) hoặc .NET 8.0 Desktop Runtime (nếu chạy bản dựng).

#### Sử dụng PowerShell Scripts (Nhanh nhất)
```powershell
# 1. Chạy kiểm thử tự động 76 unit tests
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 2. Xuất bản ứng dụng độc lập vào thư mục dist/
powershell -ExecutionPolicy Bypass -File .\publish.ps1

# 3. Kiểm tra driver và khởi chạy app
powershell -ExecutionPolicy Bypass -File .\run.ps1
```

#### Sử dụng .NET CLI thủ công
```powershell
dotnet restore
dotnet build -c Release
dotnet test tests/VirtualController.Tests/VirtualController.Tests.csproj
dotnet publish src/VirtualController.App/VirtualController.App.csproj -c Release -r win-x64 --self-contained false -o dist
```

---

### 10. Cấu trúc mã nguồn

```text
Virtual-controller/
├── .github/workflows/
│   └── build.yml                     # Pipeline CI/CD kiểm thử và đóng gói GitHub Release
├── config/
│   └── settings.json                 # Cấu hình app (Ngôn ngữ, Khởi động cùng Windows...)
├── dist/                             # Thư mục chứa file chạy hoàn chỉnh (VirtualController.exe)
├── profiles/
│   └── Default Keyboard + Mouse.json # Profile mặc định
├── src/
│   ├── VirtualController.Core/       # Thư viện lõi logic (Không phụ thuộc GUI)
│   │   ├── Core/
│   │   │   ├── ControllerState.cs    # Trạng thái dữ liệu tay cầm Xbox 360
│   │   │   ├── InputState.cs         # Bộ đệm trạng thái phím & delta chuột an toàn luồng
│   │   │   ├── MacroEngine.cs        # Engine thực thi macro theo state machine & tick-based timing
│   │   │   ├── MacroRecorder.cs      # Bộ ghi macro thời gian thực (Live Recorder)
│   │   │   └── MappingEngine.cs      # Engine ánh xạ phím & điều phối analog
│   │   ├── Input/
│   │   │   ├── WindowsHookManager.cs # Low-level Windows Hooks (WH_KEYBOARD_LL, WH_MOUSE_LL)
│   │   │   ├── RawInputReceiver.cs   # Windows Raw Input (WM_INPUT) cho chuột vi sai chuẩn xác
│   │   │   ├── MouseToStickTranslator.cs # Bộ dịch chuyển chuột sang cần analog cấp cao
│   │   │   └── InputManager.cs       # Quản lý vòng đời đầu vào
│   │   ├── Profiles/
│   │   │   ├── Profile.cs            # Định nghĩa model hồ sơ cấu hình
│   │   │   ├── StickConfig.cs        # Cấu hình cần gạt (Curves, Deadzone, Geometry, None/Mouse/KB)
│   │   │   ├── MacroDefinition.cs    # Model định nghĩa Macro, Step, Action Types & Execution Modes
│   │   │   ├── MacroPresets.cs       # Danh sách 6 mẫu macro gaming dựng sẵn
│   │   │   └── ProfileManager.cs     # Quản lý CRUD và lưu file profile
│   │   ├── VirtualController/
│   │   │   ├── IVirtualController.cs # Interface trừu tượng hóa tay cầm ảo
│   │   │   ├── ViGEmController.cs    # Giao tiếp với driver ViGEmBus
│   │   │   ├── DriverDetector.cs     # Kiểm tra trạng thái cài đặt driver
│   │   │   └── DriverInstaller.cs    # Tự động tải & cài đặt ngầm driver ViGEmBus
│   │   └── Services/
│   │       ├── AppSettings.cs        # Quản lý cài đặt & tự khởi động cùng Windows
│   │       └── AppLogger.cs          # Ghi log hoạt động tại logs/application.log
│   └── VirtualController.App/        # Ứng dụng giao diện WPF (.NET 8.0)
│       ├── Resources/
│       │   ├── Strings.vi.xaml       # Từ điển ngôn ngữ Tiếng Việt
│       │   └── Strings.en.xaml       # Từ điển ngôn ngữ Tiếng Anh
│       ├── Services/
│       │   ├── LocalizationService.cs# Dịch vụ tráo đổi ngôn ngữ runtime
│       │   └── TrayService.cs        # Xử lý khay hệ thống System Tray
│       └── Views/                    # Các màn hình chức năng (Dashboard, Mapping, Analog, Macro...)
│           └── MacroViewModels.cs    # ViewModel điều phối danh sách & trạng thái Macro
├── tests/
│   └── VirtualController.Tests/      # 76 bộ Unit Tests tự động
├── build.ps1                         # Script build & test tự động
├── publish.ps1                       # Script đóng gói ứng dụng
└── run.ps1                           # Script kiểm tra driver & chạy ứng dụng
```

---

### 11. Giấy phép (License)

Dự án được phân phối dưới giấy phép **MIT License**. Xem chi tiết tại file [LICENSE](LICENSE).

<br/>
<hr/>
<br/>

# 🇬🇧 English

A comprehensive, low-latency Windows desktop utility that translates **Keyboard + Mouse** input into a virtual **Xbox 360 Controller** backed by the industry-standard **ViGEmBus (Virtual Gamepad Emulation Bus)** kernel driver.

Windows and games detect the device as an authentic physical Xbox 360 controller (supporting both DirectInput and XInput). It operates entirely cleanly without game file tampering, code injection, or memory manipulation.

## Table of Contents
1. [Key Features](#1-key-features)
2. [System Architecture](#2-system-architecture)
3. [Default Keybindings](#3-default-keybindings)
4. [Mouse-to-Analog Stick Translation Algorithm](#4-mouse-to-analog-stick-translation-algorithm)
5. [Gamepad Macro & Combo System](#5-gamepad-macro--combo-system)
6. [User Interface (WPF GUI)](#6-user-interface-wpf-gui)
7. [Safety Controls: System Tray & Emergency Kill-Switch](#7-safety-controls-system-tray--emergency-kill-switch)
8. [ViGEmBus Driver Installation](#8-vigembus-driver-installation)
9. [Build, Test & Run Guide](#9-build-test--run-guide)
10. [Source Code Structure](#10-source-code-structure)
11. [License](#11-license)

---

### 1. Key Features

- **Genuine Xbox 360 Emulation:** Communicates with the Windows kernel via ViGEmBus, delivering 100% compatibility with Steam, Epic Games Store, emulators, and all XInput-enabled titles.
- **Advanced Mouse-to-Gamepad Translation Engine:**
  - **Windows Raw Input (`WM_INPUT`):** Uses a dedicated message sink thread (`RIDEV_INPUTSINK`), capturing raw mouse counts without screen boundary clipping or Windows cursor acceleration distortion.
  - **Independent X/Y Sensitivity:** Fine-tune horizontal and vertical turning speeds independently.
  - **Anti-Deadzone (Initial Deflection):** Injects a configurable baseline offset to immediately bypass in-game controller deadzones upon mouse movement.
  - **Configurable Response Curves:** Linear, Delay (quadratic ease-in), Aggressive (square-root snap), Smooth (cubic Hermite smoothstep), and Instant.
  - **Stick Geometry Normalization:** Toggle between standard Radial clamping and Square projection (enabling full 100% deflection along diagonals).
  - **Noise Gate & Exponential Smoothing:** Filters micro-jitter at low speeds while maintaining responsive flick tracking.
  - **Independent Stick Input Disabling (`None`):** Allows completely disabling the Right Stick or Left Stick while using the mouse, locking the axis at neutral $(0.0, 0.0)$ to prevent accidental camera rotation when using mouse buttons for actions.
  - **Physical Keyboard Input Blocking:** Eliminates dual-input conflicts by selectively swallowing mapped physical keys (W, A, S, D, Space, Shift, E, Q...) via low-level hook (`WH_KEYBOARD_LL`) so the game only receives virtual Xbox 360 controller telemetry. Features a robust anti-stuck key state machine and safety whitelists for emergency hotkeys (`Ctrl + Alt + F10`) and Windows system navigation (`Alt + Tab`, `LWin`, `RWin`, `Ctrl + Shift + Esc`).
- **Advanced Gamepad Macro & Combo System:**
  - **3 Execution Modes:** Play Once (single sequence execution), Repeat While Held / Turbo (continuous looping while held), and Toggle Loop (background loop on/off toggle).
  - **Multi-Button Combos & Analog Stick Actions:** Group multiple buttons in a single step (e.g. `LB + RB` Ultimate Skill), blend directional stick deflections (e.g. `LS_Up + B` Dodge Roll Forward), and sequence complex combat strings (`X -> X -> Y` Melee Combo).
  - **Live Macro Recorder:** High-precision real-time recording of controller button presses and millisecond delays backed by high-resolution timers.
  - **Built-in Gaming Presets:** 6 instant one-click presets: Rapid Fire RT, Ultimate Combo, Melee Combo, Dodge Roll, Auto Bunny Hop, and Auto Sprint.
  - **Physical Keyboard Blocking Integration:** Macro trigger keys are automatically intercepted and swallowed by the low-level hook when Keyboard Blocking is active, preventing key leaks into games.
- **Runtime UI Localization:** Seamless on-the-fly switching between **Tiếng Việt 🇻🇳** and **English 🇬🇧** without restarting the application.
- **Robust Profile Management:** Create, clone, rename, delete, import, and export profiles serialized as clean JSON files.
- **Real-Time Controller Tester:** Visual diagnostic view showing real-time thumbstick coordinates, trigger pressures, and button presses.
- **System Tray & Emergency Kill-Switch:** Minimizes to the notification area; customizable failsafe shortcut (defaults to `Ctrl + Alt + F10`) releases all inputs immediately.
- **Automatic GitHub Releases Update Checker:** Proactively checks for new releases on startup or on-demand from Settings, presenting release notes and direct download links.
- **Automated CI/CD Pipeline:** Fully configured GitHub Actions workflow running 76 automated unit tests and publishing standalone win-x64 release packages.

---

### 2. System Architecture

```text
               ┌────────────────────────────────────────────────────────┐
               │                    Physical Inputs                     │
               │   - Keyboard Keys (Low-Level Windows Hook)             │
               │   - Mouse Buttons (Low-Level Windows Hook)             │
               │   - Mouse Movement (Windows Raw Input WM_INPUT Sink)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                      Input Layer                       │
               │  - InputState (Thread-safe atomic updates)             │
               │  - RawInputReceiver (Dedicated Win32 message thread)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                    Mapping Engine                      │
               │  - Key-to-Button & Trigger Binding Engine              │
               │  - MouseToStickTranslator (Velocity, Curves, Geometry) │
               │  - Independent Left/Right Stick Source (None/Mouse/KB) │
               │  - MacroEngine & Recorder (Tick-based State Machine)   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                Virtual Controller Backend              │
               │  - IVirtualController / ViGEmController                │
               │  - Nefarius.ViGEm.Client (Kernel IOCTL bridge)         │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │                   ViGEmBus Driver (R0)                 │
               │  - Virtual Xbox 360 Peripheral in Windows Device Mgr   │
               └───────────────────────────┬────────────────────────────┘
                                           │
                                           ▼
               ┌────────────────────────────────────────────────────────┐
               │               PC Games / Steam (XInput)                │
               └────────────────────────────────────────────────────────┘
```

---

### 3. Default Keybindings

#### Keyboard
| Physical Key | Virtual Xbox 360 Button | Standard Function |
| :--- | :--- | :--- |
| **W** | Left Stick Up | Move Forward / Up |
| **S** | Left Stick Down | Move Backward / Down |
| **A** | Left Stick Left | Strafe Left |
| **D** | Left Stick Right | Strafe Right |
| **Space** | **A** | Jump / Confirm |
| **Left Shift** | **B** | Melee / Cancel |
| **E** | **X** | Interact / Reload |
| **Q** | **Y** | Swap Weapon / Ultimate |
| **R** | **RB** | Right Bumper |
| **F** | **LB** | Left Bumper |
| **Tab** | **Back** | Map / Scoreboard |
| **Enter** | **Start** | Pause Menu |
| **Escape** | **Start** | Menu / Escape |

#### Mouse
| Mouse Action | Virtual Xbox 360 Target | Notes |
| :--- | :--- | :--- |
| **Mouse Move X** | **Right Stick X** | Horizontal look / aim (unless set to `None`) |
| **Mouse Move Y** | **Right Stick Y** | Vertical look / aim (unless set to `None`) |
| **Left Click (LMB)** | **RT (Right Trigger)** | Fire weapon (Analog 0..255) |
| **Right Click (RMB)**| **LT (Left Trigger)** | Aim down sights (Analog 0..255) |
| **Middle Click (MMB)**| **RS Click** | Right Thumbstick Click |

---

### 4. Mouse-to-Analog Stick Translation Algorithm

The `MouseToStickTranslator` computes stick displacement from relative mouse motion through a disciplined mathematical pipeline:

1. **Calculate Mouse Velocity ($\text{counts/ms}$):**
   $$V_x = \frac{\Delta x}{\Delta t}, \quad V_y = \frac{\Delta y}{\Delta t}$$
2. **Apply Independent Axis Sensitivity:**
   $$S_x = V_x \times \text{SensitivityX}, \quad S_y = V_y \times \text{SensitivityY}$$
3. **Noise Gate Filtering:**
   If the magnitude $\text{Mag} = \sqrt{S_x^2 + S_y^2} < \text{NoiseThreshold}$, output defaults to zero.
4. **Response Curve Scaling:**
   - *Linear:* $f(x) = x$
   - *Delay:* $f(x) = x^2$ (fine control during micro-aiming)
   - *Aggressive:* $f(x) = \sqrt{x}$ (rapid snap engagement)
   - *Smooth:* $f(x) = 3x^2 - 2x^3$ (cubic Hermite smoothstep)
   - *Instant:* $f(x) = 1.0$ (immediate maximum deflection)
5. **Anti-Deadzone Floor Injection:**
   $$\text{Deflection} = \text{AntiDeadzone} + (1.0 - \text{AntiDeadzone}) \times \text{Curve}(x)$$
6. **Stick Geometry Normalization:**
   - *Radial:* Restricts the vector magnitude within the unit circle ($\le 1.0$).
   - *Square:* Projects circular deflection to square bounds so diagonal aim reaches full saturation.
7. **Temporal Exponential Filtering (Smoothing):**
   $$\vec{O}_{\text{t}} = \alpha \vec{O}_{\text{target}} + (1 - \alpha) \vec{O}_{\text{t-1}}$$

---

### 5. Gamepad Macro & Combo System

Virtual Controller features a high-performance, deterministic macro state machine engine (`MacroEngine`) running synchronously with the input polling loop, delivering millisecond-precise execution cadence and comprehensive automation for any PC or Steam game.

#### Core Capabilities:

1. **3 Execution Modes:**
   - **Play Once (Chạy 1 lần):** Executes the entire sequence once upon detecting the trigger keypress (rising edge). Perfect for discrete skill combinations, dodge roll evasions, or quick weapon swap cancels.
   - **Repeat While Held / Turbo (Lặp liên tục khi giữ phím):** Loops the sequence continuously as long as the trigger key remains depressed, immediately releasing when the key is released (falling edge). Configurable inter-cycle **Loop Delay (ms)** ensures tunable fire rates. Ideal for Rapid Fire RT semi-auto weapons or Auto Bunny Hop.
   - **Toggle Loop (Bật/Tắt lặp tự động):** Press once to initiate continuous background cycling; press again to disengage immediately. Optimal for fatigue-free Auto Sprint or long-duration background holding.

2. **Multi-Button Combos & Stick Actions:**
   - **Simultaneous Multi-Button Combinations:** Any step can group multiple virtual controller buttons together (e.g. simultaneous `LB + RB` to cast an Ultimate Skill with a single keystroke).
   - **Directional Stick Actions:** Full support for Left and Right thumbstick directional deflections (`LS_Up`, `LS_Down`, `LS_Left`, `LS_Right`, `RS_Up`, `RS_Down`, `RS_Left`, `RS_Right`) blended with button inputs (e.g. `LS_Up + B` for directional Dodge Roll Forward).
   - **4 Discrete Step Types:**
     - `Tap`: Press and release button(s) with custom `DurationMs` and follow-up `DelayAfterMs`.
     - `PressDown / Hold`: Depresses and maintains button state across subsequent steps.
     - `ReleaseUp`: Releases previously held button(s).
     - `Wait`: Dedicated delay interval before advancing to the next step.
   - **Melee Combo Sequencing:** Seamlessly author multi-stage strike sequences like `X -> X -> Y` (Melee Combo) with millisecond-exact timings.

3. **Live Macro Recorder:**
   - Capture controller button inputs and timings in real time backed by high-resolution Windows performance counters (`Stopwatch`).
   - Automatically records human button press durations (`DurationMs`) and inter-press intervals (`DelayAfterMs`), with optional Fixed Timing normalization for metered execution.

4. **Built-in Gaming Presets:**
   - Deploy 6 battle-tested presets instantly with a single click:
     - **Rapid Fire RT (Turbo Shoot):** High-cadence semi-auto trigger cycling (Tap RT 35ms / delay 35ms, 10ms loop delay, Mode: Repeat While Held).
     - **Ultimate Skill (LB + RB):** Simultaneous dual bumper activation (Duration 80ms, Mode: Play Once).
     - **Melee Combo (X -> X -> Y):** Classic light-light-heavy melee strike sequence (Mode: Play Once).
     - **Dodge Roll Forward (LS_Up + B):** Forward stick deflection combined with button B dodge (Mode: Play Once).
     - **Auto Bunny Hop (Turbo A):** Rapid rhythmic jump cycling with button `A` (Hold 45ms, Delay 45ms, Mode: Repeat While Held).
     - **Auto Sprint Hold (LS):** Hands-free continuous sprint activation via Left Thumbstick click toggle (Mode: Toggle Loop).

5. **Physical Keyboard Blocking Integration:**
   - When **Physical Keyboard Blocking** is enabled, all configured macro trigger keys (`TriggerInput`) are automatically registered into the low-level keyboard hook (`WH_KEYBOARD_LL`) swallow table.
   - Prevents physical macro trigger keys (e.g. `V`, `Z`, `G`, `CapsLock`...) from leaking raw keyboard events into games, eliminating dual-input conflicts and unintentional in-game chat or menu popups.
   - Fully safe: Whitelists emergency kill-switch shortcuts (`Ctrl + Alt + F10`) and essential Windows navigation hotkeys (`Alt + Tab`, `LWin`, `RWin`, `Ctrl + Shift + Esc`).

---

### 6. User Interface (WPF GUI)

1. **Dashboard:** Connection status indicator, Enable/Disable controller toggle, active profile selector, and quick test button.
2. **Button Mapping:** Grid layout for mapping physical keys and mouse buttons to Xbox 360 inputs with auto-key-detection.
3. **Analog Settings:**
   - Choose input source: `Keyboard (WASD)`, `Mouse`, or `None (Disabled)`.
   - Sliders for Sensitivity X/Y, Deadzone, Anti-Deadzone, Response Curve selector, Stick Geometry toggle, Smoothing, and Axis Inversion.
4. **Macro & Combo:** Configure automated controller sequences, select from 3 execution modes (Play Once, Repeat While Held, Toggle Loop), record real-time inputs with Live Recorder, and load instant presets.
5. **Controller Tester:** Real-time visual feedback showing stick displacement coordinates, trigger progress bars, and button states.
6. **Profiles Manager:** Create, duplicate, rename, delete, import, and export profile `.json` files.
7. **Settings:** Toggle start with Windows, start minimized, auto-enable, switch runtime UI language (Tiếng Việt / English), and 1-click ViGEmBus installer.

---

### 7. Safety Controls: System Tray & Emergency Kill-Switch

- **System Tray Integration:** Closing the main window minimizes the app to the notification area, maintaining active emulation in the background. Right-click the tray icon for quick access.
- **Emergency Kill-Switch Hotkey:**
  ```text
  Ctrl + Alt + F10 (Default - fully customizable in Settings tab)
  ```
  Triggers a global failsafe that immediately unplugs the virtual controller, releases all depressed buttons, and centers all thumbsticks. Can be rebound to any shortcut (e.g. `Ctrl + Shift + F12`, `F11`, `Alt + Q`...).
- **Clean Process Tear-Down:** Safely disposes of the virtual USB device handle during application shutdown or crash events via `AppDomain.ProcessExit`.

---

### 8. ViGEmBus Driver Installation

The application requires the open-source **ViGEmBus (Virtual Gamepad Emulation Bus)** kernel driver.

#### Automatic 1-Click Installer (Recommended)
- The app checks for the driver on startup. If missing, it prompts to download and install the official release directly from GitHub.
- Alternatively, click **`Install ViGEmBus Driver`** on the Settings or Dashboard view at any time.

#### Manual Installation
1. Download the official installer: [ViGEmBus Release v1.22.0](https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe).
2. Run the executable as Administrator and follow the setup wizard.
3. Re-launch Virtual Controller.

---

### 9. Build, Test & Run Guide

#### Prerequisites
- Windows 10 or Windows 11 (64-bit).
- .NET 8.0 SDK (for building) or .NET 8.0 Desktop Runtime (for running published binaries).

#### PowerShell Automation (Recommended)
```powershell
# 1. Build and run all 76 unit tests
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 2. Publish standalone binary to dist/
powershell -ExecutionPolicy Bypass -File .\publish.ps1

# 3. Check driver status and launch application
powershell -ExecutionPolicy Bypass -File .\run.ps1
```

#### Manual .NET CLI
```powershell
dotnet restore
dotnet build -c Release
dotnet test tests/VirtualController.Tests/VirtualController.Tests.csproj
dotnet publish src/VirtualController.App/VirtualController.App.csproj -c Release -r win-x64 --self-contained false -o dist
```

---

### 10. Source Code Structure

```text
Virtual-controller/
├── .github/workflows/
│   └── build.yml                     # GitHub Actions CI/CD release workflow
├── config/
│   └── settings.json                 # User preferences (Language, Start with Windows...)
├── dist/                             # Published executable output folder
├── profiles/
│   └── Default Keyboard + Mouse.json # Default control scheme
├── src/
│   ├── VirtualController.Core/       # Core business logic (Headless, no GUI dependencies)
│   │   ├── Core/
│   │   │   ├── ControllerState.cs    # Normalized Xbox 360 controller state container
│   │   │   ├── InputState.cs         # Thread-safe keyboard key & mouse delta buffer
│   │   │   ├── MacroEngine.cs        # Deterministic tick-based macro state machine engine
│   │   │   ├── MacroRecorder.cs      # Real-time macro recording engine
│   │   │   └── MappingEngine.cs      # Core mapping & stick dispatch engine
│   │   ├── Input/
│   │   │   ├── WindowsHookManager.cs # Low-level Windows hooks (WH_KEYBOARD_LL, WH_MOUSE_LL)
│   │   │   ├── RawInputReceiver.cs   # Windows Raw Input (WM_INPUT) receiver
│   │   │   ├── MouseToStickTranslator.cs # Mouse velocity & curve translation pipeline
│   │   │   └── InputManager.cs       # Input lifecycle coordinator
│   │   ├── Profiles/
│   │   │   ├── Profile.cs            # Profile serialization model
│   │   │   ├── StickConfig.cs        # Analog stick configuration model (Curves, Geometry...)
│   │   │   ├── MacroDefinition.cs    # Macro, step, action type & execution mode model
│   │   │   ├── MacroPresets.cs       # Built-in collection of 6 gaming macro presets
│   │   │   └── ProfileManager.cs     # Profile persistence & file operations
│   │   ├── VirtualController/
│   │   │   ├── IVirtualController.cs # Virtual controller abstraction interface
│   │   │   ├── ViGEmController.cs    # ViGEmClient kernel interface wrapper
│   │   │   ├── DriverDetector.cs     # Driver registry & device detection
│   │   │   └── DriverInstaller.cs    # Automatic driver downloader & installer
│   │   └── Services/
│   │       ├── AppSettings.cs        # Preferences & Windows registry persistence
│   │       └── AppLogger.cs          # Rolling file logger (logs/application.log)
│   └── VirtualController.App/        # WPF Desktop Application (.NET 8.0 Windows)
│       ├── Resources/
│       │   ├── Strings.vi.xaml       # Vietnamese localized string dictionary
│       │   └── Strings.en.xaml       # English localized string dictionary
│       ├── Services/
│       │   ├── LocalizationService.cs# Runtime language dictionary switcher
│       │   └── TrayService.cs        # System tray icon and context menu service
│       └── Views/                    # UI Views (Dashboard, Mapping, Analog, Macro...)
│           └── MacroViewModels.cs    # Macro list and state inspector ViewModels
├── tests/
│   └── VirtualController.Tests/      # 76 automated unit tests
├── build.ps1                         # PowerShell automated build and test runner
├── publish.ps1                       # PowerShell standalone publication script
└── run.ps1                           # PowerShell pre-flight driver check and launcher
```

---

### 11. License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for complete details.
