# Virtual Xbox 360 Controller (Keyboard + Mouse → Gamepad)
### Universal Virtual Controller for Windows PC & Steam Games (Windows 10/11 64-bit)

Ứng dụng Windows hoàn chỉnh chuyển đổi tín hiệu **Bàn phím + Chuột** thành **Tay cầm ảo Xbox 360 Controller** thông qua driver chuẩn công nghiệp **ViGEmBus (Virtual Gamepad Emulation Bus)**.

Hệ điều hành và game nhận diện thiết bị như một tay cầm Xbox 360 vật lý chính hãng (DirectInput / XInput), không can thiệp vào file game, không inject mã vào tiến trình, hoàn toàn an toàn và trong sạch.

---

## 1. Kiến trúc hệ thống (Architecture)

```text
               ┌───────────────────────┐
               │    Physical Inputs    │
               │   (Keyboard & Mouse)  │
               └───────────┬───────────┘
                           │ Low-level Windows Hooks (WH_KEYBOARD_LL, WH_MOUSE_LL)
                           ▼
               ┌───────────────────────┐
               │      Input Layer      │
               │ (InputState & Deltas) │
               └───────────┬───────────┘
                           │ Raw keys & relative mouse movement
                           ▼
               ┌───────────────────────┐
               │    Mapping Engine     │
               │  - Diagonal Norm      │
               │  - Deadzones & Curves │
               │  - Decay & Smoothing  │
               └───────────┬───────────┘
                           │ Normalized ControllerState (0..1 triggers, -1..1 sticks)
                           ▼
               ┌───────────────────────┐
               │ Virtual Controller    │
               │      Backend          │
               │   (Nefarius.ViGEm)    │
               └───────────┬───────────┘
                           │ Atomic report packet via IOCTL
                           ▼
               ┌───────────────────────┐
               │  ViGEmBus Driver (R0) │
               └───────────┬───────────┘
                           │ Windows XInput Device
                           ▼
               ┌───────────────────────┐
               │   PC Games / Steam    │
               └───────────────────────┘
```

---

## 2. Profile Mặc Định: Default Keyboard + Mouse

### Bàn phím (Keyboard)
| Phím vật lý | Nút ảo Xbox 360 | Chức năng mặc định |
| :--- | :--- | :--- |
| **W** | Left Stick Up | Đi lên / Nhìn lên |
| **S** | Left Stick Down | Đi xuống / Ngồi |
| **A** | Left Stick Left | Di chuyển sang trái |
| **D** | Left Stick Right | Di chuyển sang phải |
| **Space** | **A** | Nhảy (Jump) / Xác nhận |
| **Left Shift** | **B** | Đánh cận chiến / Hủy |
| **E** | **X** | Tương tác / Bắn súng chính |
| **Q** | **Y** | Chiêu đặc biệt / Đổi vũ khí |
| **R** | **RB** | Nút vai phải (Right Bumper) |
| **F** | **LB** | Nút vai trái (Left Bumper) |
| **Tab** | **Back** | Xem bản đồ / Điểm số |
| **Enter** | **Start** | Menu tạm dừng (Pause) |
| **Escape** | **Start** | Menu / Thoát |

### Chuột (Mouse)
| Thao tác chuột | Bộ phận ảo Xbox 360 | Ghi chú |
| :--- | :--- | :--- |
| **Mouse X** | **Right Stick X** | Dịch chuyển ngang |
| **Mouse Y** | **Right Stick Y** | Dịch chuyển dọc |
| **Left Mouse** | **RT (Right Trigger)** | Cò súng phải (Analog 0..255) |
| **Right Mouse** | **LT (Left Trigger)** | Cò súng trái (Analog 0..255) |
| **Middle Mouse** | **RS Click** | Nhấn analog phải (Right Thumb) |

---

## 3. Thuật toán Xử lý Analog Stick

### A. Keyboard → Stick (Chuẩn hóa vector đường chéo)
Khi nhấn 1 hướng đơn:
* Nhấn `W` $\rightarrow X = 0, Y = +1.0$
* Nhấn `S` $\rightarrow X = 0, Y = -1.0$
* Nhấn `A` $\rightarrow X = -1.0, Y = 0$
* Nhấn `D` $\rightarrow X = +1.0, Y = 0$

Khi nhấn đường chéo 2 phím cùng lúc (ví dụ `W + D`):
* Vector thô: $X_{raw} = +1.0, Y_{raw} = +1.0$
* Độ dài thô: $L = \sqrt{1.0^2 + 1.0^2} = \sqrt{2} \approx 1.4142$
* Chuẩn hóa:
  $$X = \frac{X_{raw}}{L} = \frac{1}{\sqrt{2}} \approx +0.7071$$
  $$Y = \frac{Y_{raw}}{L} = \frac{1}{\sqrt{2}} \approx +0.7071$$
* **Độ lớn vector:** $\sqrt{0.7071^2 + 0.7071^2} = 1.0000$ (tuyệt đối không vượt quá magnitude 1.0).
* Deadzone và Sensitivity được tính toán trên độ lớn sau chuẩn hóa:
  $$\text{ScaledMagnitude} = \text{Clamp}\left(\frac{\text{Mag} - \text{Deadzone}}{1.0 - \text{Deadzone}} \times \text{Sensitivity}, 0.0, 1.0\right)$$

### B. Cấu hình Cần Analog Độc Lập (Dual-Stick Flexibility)
Cả **Left Stick** và **Right Stick** đều có thể tự do gán nguồn điều khiển độc lập:
* **Keyboard:** Nhập 4 phím hướng (Up, Down, Left, Right).
* **Mouse:** Điều khiển bằng tọa độ vi sai chuột (Relative Mouse Delta).

### C. Mouse → Stick (Tương đối & Làm mượt)
* Đọc relative delta ($\Delta X, \Delta Y$) từ Low-level Mouse Hook.
* Bộ lọc làm mượt hàm mũ (EMA - Exponential Moving Average):
  $$\text{Current} = \alpha \times \text{NewDelta} + (1 - \alpha) \times \text{Previous}$$
* Hỗ trợ Invert X, Invert Y, Deadzone, Max Magnitude, Acceleration, và Recentering Decay khi ngừng di chuột.

---

## 4. Giao diện trực quan (GUI Tabs)

1. **Dashboard:**
   - Trạng thái kết nối thời gian thực (Connected / Disconnected).
   - Nút bật/tắt (Enable / Disable Controller).
   - Tự động hiển thị nút cài đặt ViGEmBus nếu hệ thống thiếu driver.
   - Chọn profile hoạt động và mở trình test nhanh.
2. **Button Mapping:**
   - Bảng ánh xạ từng nút ảo Xbox 360 với phím vật lý hoặc chuột.
   - Nhận diện phím tự động khi bấm.
   - Nút `Reset to Defaults` khôi phục thiết lập gốc.
3. **Analog Settings:**
   - Cấu hình nguồn điều khiển độc lập cho Left Stick và Right Stick (Keyboard / Mouse).
   - Tinh chỉnh Deadzone, Sensitivity, Smoothing, Invert X/Y, Acceleration, Response Curve.
4. **Controller Tester:**
   - Mô phỏng tay cầm thời gian thực với hai cần analog tròn, các nút bấm ABXY, D-Pad, Bumper và thanh hiển thị Trigger LT/RT.
5. **Profiles Manager:**
   - Quản lý danh sách profile dạng JSON trong thư mục `profiles/`.
   - Tạo mới, nhân bản, đổi tên, xóa, Import JSON / Export JSON.
6. **Settings & System Tray:**
   - Khởi động cùng Windows (`Start with Windows`).
   - Khởi động thu nhỏ vào khay hệ thống (`Start minimized`).
   - Tự động bật tay cầm khi mở app (`Auto-enable controller`).
   - Nút kiểm tra và cài đặt tự động ViGEmBus.

---

## 5. System Tray & Hotkey Khẩn cấp (Safety)

* **Thu nhỏ vào System Tray:** Khi bấm nút đóng $(X)$ cửa sổ, chương trình **không tắt** mà thu nhỏ xuống khay hệ thống (System Tray). Có menu ngữ cảnh: Enable, Disable, Profiles, Open Dashboard, Exit.
* **Emergency Hotkey:**
  ```text
  Ctrl + Alt + F10
  ```
  Khi bấm tổ hợp phím này ở bất cứ đâu trong Windows hoặc trong game:
  - Ngắt tín hiệu tay cầm ngay lập tức.
  - Release toàn bộ các nút ảo.
  - Reset cần analog và trigger về 0.
* **Cơ chế thoát an toàn:** Khi app bị crash hoặc tắt (`AppDomain.ProcessExit`), bộ xử lý dọn dẹp tự động thu hồi virtual gamepad sạch sẽ khỏi Windows Device Manager.

---

## 6. Hướng dẫn Cài đặt Driver ViGEmBus (Tự Động 1-Click)

Ứng dụng sử dụng driver nguồn mở chính thức **ViGEmBus (Virtual Gamepad Emulation Bus)**.

### Tính năng Tự Động Cài Đặt (Khuyên dùng):
Chương trình tích hợp sẵn cơ chế **tự động kiểm tra và cài đặt 1-click**:
* **Khi mở ứng dụng:** Nếu máy tính chưa cài ViGEmBus, ứng dụng sẽ hiện thông báo hỏi và tự động tải bản cài đặt chính thức từ GitHub Releases rồi cài đặt cho bạn.
* **Trên Dashboard hoặc Settings:** Có sẵn nút **`Install ViGEmBus Driver`** để kích hoạt cài đặt tự động bất kỳ lúc nào.
* **Khi chạy `.\run.ps1`:** Script khởi chạy tự động phát hiện và cài đặt driver ngầm trước khi mở app.

### Cách cài đặt thủ công (Nếu muốn):
1. Tải bộ cài chính thức ViGEmBus từ GitHub:
   👉 **[ViGEmBus Release v1.22.0](https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe)**
2. Chạy file cài đặt với quyền Administrator và hoàn tất quá trình cài đặt.
3. Khởi động lại ứng dụng hoặc nhấn "Re-Check Driver Status" trong tab Settings.

### Cách gỡ bỏ driver (Uninstall):
Mở **Windows Settings → Apps → Installed apps**, tìm `ViGEmBus Driver` và chọn `Uninstall`.

---

## 7. Yêu cầu Môi trường & Dependencies

* **Hệ điều hành:** Windows 10 / Windows 11 (64-bit).
* **Runtime:** .NET 8.0 Windows Desktop Runtime (hoặc .NET 8.0 SDK nếu tự build).
* **Thư viện NuGet:**
  - `Nefarius.ViGEm.Client` (v1.21.256) - Giấy phép MIT.
  - `xunit` & `Microsoft.NET.Test.Sdk` (Dành cho Unit Tests).

---

## 8. Hướng dẫn Build và Chạy

### Cách 1: Sử dụng các script PowerShell có sẵn (Khuyên dùng)

1. **Build và chạy toàn bộ Unit Tests:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```
2. **Publish ra bản phân phối độc lập (`/dist/VirtualController.exe`):**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\publish.ps1
   ```
3. **Kiểm tra driver và khởi chạy ứng dụng:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\run.ps1
   ```

### Cách 2: Sử dụng .NET CLI thủ công

1. **Restore dependencies:**
   ```powershell
   dotnet restore
   ```
2. **Build Release:**
   ```powershell
   dotnet build -c Release
   ```
3. **Chạy Unit Tests:**
   ```powershell
   dotnet test tests/VirtualController.Tests/VirtualController.Tests.csproj
   ```
4. **Publish độc lập:**
   ```powershell
   dotnet publish src/VirtualController.App/VirtualController.App.csproj -c Release -r win-x64 --self-contained false -o dist
   ```

---

## 9. Cấu trúc Source Code

```text
/
├── config/
│   └── settings.json                     # Cấu hình app (Start with Windows, Hotkey...)
├── dist/                                 # Thư mục đầu ra sản phẩm (VirtualController.exe)
├── profiles/
│   └── Default Keyboard + Mouse.json     # Profile mặc định
├── src/
│   ├── VirtualController.Core/           # Core library (no GUI dependency)
│   │   ├── Core/
│   │   │   ├── ControllerState.cs        # Trạng thái tay cầm Xbox 360
│   │   │   ├── InputState.cs             # Bộ đệm phím & delta chuột
│   │   │   └── MappingEngine.cs          # Engine tính toán analog & nút bấm
│   │   ├── Input/
│   │   │   ├── WindowsHookManager.cs     # WH_KEYBOARD_LL & WH_MOUSE_LL hooks
│   │   │   └── InputManager.cs           # Bộ điều phối luồng input
│   │   ├── Profiles/
│   │   │   ├── Profile.cs                # Model Profile (JSON)
│   │   │   ├── StickConfig.cs            # Cấu hình độc lập Left/Right Stick
│   │   │   └── ProfileManager.cs         # Quản lý CRUD profiles
│   │   ├── VirtualController/
│   │   │   ├── IVirtualController.cs     # Abstraction interface
│   │   │   ├── ViGEmController.cs        # Backend triển khai ViGEmBus
│   │   │   ├── DriverDetector.cs         # Kiểm tra trạng thái driver hệ thống
│   │   │   └── DriverInstaller.cs        # Trình tự động tải & cài đặt ViGEmBus
│   │   └── Services/
│   │       ├── AppSettings.cs            # Quản lý cài đặt & Windows Run registry
│   │       └── AppLogger.cs              # Ghi log xoay vòng tại logs/application.log
│   │
│   └── VirtualController.App/            # WPF Desktop GUI (.NET 8.0 Windows)
│       ├── MainWindow.xaml               # Khung sườn giao diện chính (Sidebar)
│       ├── Views/
│       │   ├── DashboardView.xaml        # Màn hình chính & Bật/Tắt
│       │   ├── MappingEditorView.xaml    # Chỉnh sửa phím bấm
│       │   ├── AnalogSettingsView.xaml   # Chỉnh analog & chuột
│       │   ├── ControllerTesterView.xaml # Test realtime các nút & stick
│       │   ├── ProfilesView.xaml         # Quản lý Profiles
│       │   ├── SettingsView.xaml         # Cài đặt hệ thống & Driver
│       │   └── DriverInstallDialog.xaml  # Cửa sổ tiến trình tải & cài đặt ViGEmBus
│       └── Services/
│           └── TrayService.cs            # Thu nhỏ System Tray & Context Menu
│
├── tests/
│   └── VirtualController.Tests/          # 24 Unit Tests tự động
├── build.ps1                             # Script tự động build & test
├── publish.ps1                           # Script xuất bản dist/VirtualController.exe
├── run.ps1                               # Script pre-flight kiểm tra driver & chạy
├── LICENSE                               # MIT License
└── README.md                             # Tài liệu hướng dẫn chi tiết
```

---

## 10. License

Dự án được phân phối dưới giấy phép **MIT License**. Xem chi tiết tại file [LICENSE](LICENSE).
