# Auto Close Windows — Hướng dẫn cài đặt & sử dụng

## Yêu cầu hệ thống
- Windows 10/11 (64-bit)
- .NET 6.0 SDK hoặc .NET 8.0 SDK
  → Tải tại: https://dotnet.microsoft.com/download

---

## Cách build (2 bước)

### Bước 1 — Mở Terminal / Command Prompt
Vào thư mục dự án:
```
cd đường\dẫn\đến\AutoCloseWindows
```

### Bước 2 — Chạy lệnh build
```
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

File `.exe` sẽ xuất hiện tại:
```
bin\Release\net6.0-windows\win-x64\publish\AutoCloseWindows.exe
```

> 💡 File exe này chạy độc lập, không cần cài .NET trên máy người dùng.

---

## Cách sử dụng

### ▶ Khởi động
Double-click vào `AutoCloseWindows.exe`

### ⚡ Đóng tất cả cửa sổ
**Cách 1:** Double-click vào icon ở góc dưới bên phải (system tray / taskbar)
**Cách 2:** Nhấn nút **"⚡ Đóng Tất Cả Ngay"** trong giao diện chính
**Cách 3:** Right-click vào icon → chọn "Đóng Tất Cả Cửa Sổ"

### 🔍 Xem danh sách cửa sổ
Nhấn nút **"🔍 Xem Danh Sách Cửa Sổ"** để cập nhật danh sách.

### ⚙ Tùy chọn
| Tùy chọn | Mô tả |
|----------|-------|
| Hỏi xác nhận | Hiển thị hộp thoại xác nhận trước khi đóng |
| Khởi động thu nhỏ | Ẩn vào system tray khi mở app |

---

## Cơ chế hoạt động

Tool sử dụng Windows API (`EnumWindows` + `PostMessage WM_CLOSE`) để:
1. Liệt kê tất cả cửa sổ đang hiện thị trên màn hình
2. Lọc ra các cửa sổ "thật" (có title bar + nút X)
3. Bỏ qua các process hệ thống quan trọng (explorer, dwm, winlogon, ...)
4. Gửi lệnh đóng đến từng cửa sổ — **không kill process** nên app có thể hỏi "Save?" trước khi đóng

---

## Ghi chú bảo mật
- Tool **KHÔNG kill process** — chỉ gửi WM_CLOSE như khi bạn nhấn nút X
- Các app có dữ liệu chưa lưu vẫn có thể hiện hộp thoại hỏi bạn
- Explorer/Desktop/Taskbar/Task Manager được bảo vệ, **không bị đóng**
