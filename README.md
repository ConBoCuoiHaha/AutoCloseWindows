# Auto Close Windows

> Đóng toàn bộ cửa sổ đang mở chỉ với một cú click

---

## Tính năng

- **Đóng tất cả cửa sổ** chỉ bằng một click hoặc double-click vào tray icon
- **Loại trừ cửa sổ** — bỏ tick những cửa sổ muốn giữ lại, chỉ đóng các cửa sổ được chọn
- **Hỗ trợ mọi loại app**: trình duyệt (Chrome, Brave, Edge), ứng dụng Electron (VS Code, Claude), Zalo, File Explorer,...
- **Không kill process** — gửi lệnh WM_CLOSE như nhấn nút X, app có thể hỏi "Save?" trước khi đóng
- **Bảo vệ hệ thống** — Taskbar, Desktop, Task Manager không bị ảnh hưởng
- Dark theme UI, chạy ẩn dưới system tray

---

## Yêu cầu hệ thống

- Windows 10 / 11 (64-bit)
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) trở lên

---

## Cách build

```bash
cd AutoCloseWindows
dotnet build -c Release
```

File exe xuất hiện tại:
```
bin\Release\net6.0-windows\win-x64\AutoCloseWindows.exe
```

Để build file exe độc lập (không cần cài .NET trên máy người dùng):
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## Cách sử dụng

### Khởi động
Double-click vào `AutoCloseWindows.exe`

### Đóng tất cả cửa sổ
| Cách | Mô tả |
|------|-------|
| **Double-click** tray icon | Đóng ngay, nhanh nhất |
| Nút **⚡ Đóng Tất Cả Ngay** | Đóng tất cả cửa sổ đang được tick |
| Right-click tray → **Đóng Tất Cả** | Từ menu tray |

### Loại trừ cửa sổ
1. Nhấn **"🔍 Làm Mới Danh Sách"** để xem danh sách cửa sổ đang mở
2. **Click vào cửa sổ** muốn giữ lại → bỏ tick ☐
3. Nhấn **"⚡ Đóng Tất Cả Ngay"** → chỉ đóng các cửa sổ còn được tick ✅

### Tùy chọn
| Tùy chọn | Mô tả |
|----------|-------|
| Hỏi xác nhận trước khi đóng | Hiển thị hộp thoại xác nhận + danh sách cửa sổ sẽ đóng |

---

## Cơ chế hoạt động

Tool sử dụng Windows API để phát hiện và đóng cửa sổ:

1. `EnumWindows` — liệt kê tất cả top-level window
2. Lọc theo: visible + có title + không phải tool window + không phải system process
3. `Process.MainWindowHandle` — bắt thêm các app frameless (VS Code, Zalo, Claude...)
4. `PostMessage(WM_CLOSE)` — gửi lệnh đóng an toàn, không force kill

---

## Tác giả

**Trần Yên Hưng** · 2003 · Hồ Chí Minh

---

## License

MIT
