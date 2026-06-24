# SmartEdu — Hệ Thống Quản Lý Tài Liệu & Embedding

> Nền tảng học tập thông minh hỗ trợ upload tài liệu, chunking tự động và embedding vector thời gian thực.

---

## 🌟 Tính Năng Chính

- Upload tài liệu **(PDF / DOCX)** cho từng môn học.
- Lưu file lên `wwwroot/uploads` và quản lý metadata (tiêu đề, kích thước, trạng thái).
- **Chunking văn bản tự động** (character-based, mặc định `chunkSize=800`, overlap=10%).
- Gọi dịch vụ embedding **(Hugging Face)** để tính embedding cho từng chunk.
- Hiển thị **tiến trình embedding theo chunk** theo thời gian thực (SignalR).
- Xem nội dung chunk từng document qua modal "Xem chunk".
- **Phân quyền truy cập:**
  - Trưởng nhóm (leader): upload, trigger embedding, xóa.
  - Giảng viên được gán: xem và download tài liệu.
- API nội bộ/handlers cho AJAX: `?handler=Documents`, `?handler=Chunks`, `?handler=TriggerEmbedding`, `?handler=Delete`, `?handler=Download`.

---

## 🏗 Kiến Trúc & Công Nghệ

| Thành phần | Mô tả |
|---|---|
| **ASP.NET Core Razor Pages (.NET 8)** | UI chính (`SmartEdu.RazorWeb`) |
| **SmartEdu.Business** | Business layer — `DocumentService` xử lý chunking, embedding, notifications |
| **SmartEdu.DataAccess** | EF Core — các entity `Document`, `DocumentChunk`, v.v. |
| **SignalR** | `SubjectHub` / `DocumentHub` — push tiến trình và log realtime |
| **PdfPig** | Trích xuất văn bản từ PDF |
| **OpenXML SDK** | Xử lý file DOCX |
| **HttpClient** | Gọi Hugging Face Inference API |
| **Database** | SQL Server / SQLite (tuỳ `ConnectionString`) |
| **DI** | `IServiceCollection` + scoped background tasks qua `IServiceScopeFactory` |

---

## 📁 Cấu Trúc Thư Mục Quan Trọng

```
SmartEdu.RazorWeb/
├── Pages/Documents/
│   └── UploadDocument.cshtml(.cs)   # UI upload & list, modal chunk review, client JS
├── wwwroot/uploads/                  # Nơi lưu file upload

SmartEdu.Business/
└── Services/
    └── DocumentService.cs            # Chunking, embedding loop, notifications

SmartEdu.DataAccess/                  # EF entities, migrations, repositories
```

---

## 🔧 Khởi Tạo Cơ Sở Dữ Liệu

**1. Cấu hình connection string** (xem phần User Secrets / appsettings).

**2. Tạo migration** (nếu cần):
```bash
dotnet ef migrations add InitialCreate \
  --project SmartEdu.DataAccess \
  --startup-project SmartEdu.RazorWeb
```

**3. Áp dụng migration:**
```bash
dotnet ef database update \
  --project SmartEdu.DataAccess \
  --startup-project SmartEdu.RazorWeb
```

> **Lưu ý:** Khi chạy trong Visual Studio, chọn `SmartEdu.RazorWeb` là Startup Project và dùng Package Manager Console hoặc CLI tương ứng.

---

## 🔐 Cấu Hình API Key Bảo Mật (User Secrets)

Sử dụng **Secret Manager** (khuyến nghị cho môi trường dev):

```bash
# Di chuyển vào thư mục RazorWeb
cd SmartEdu.RazorWeb

# Khởi tạo secret manager
dotnet user-secrets init

# Thiết lập secrets
dotnet user-secrets set "HuggingFace:Token" "<YOUR_HUGGINGFACE_TOKEN>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

Tham chiếu trong app:
```csharp
IConfiguration["HuggingFace:Token"]
```

> ⚠️ **Không commit secrets lên repo.** Trên production, dùng environment variables hoặc Secret Store của hosting.

---

## 📖 Hướng Dẫn Sử Dụng (Dev)

### Bước 1 — Lấy code & cấu hình
```bash
git clone <repo-url>
cd SmartEdu.RazorWeb
# Thiết lập user-secrets như hướng dẫn trên
```
Đảm bảo DB có thể truy cập theo `ConnectionStrings:DefaultConnection`.

### Bước 2 — Chạy migration
```bash
dotnet ef database update \
  --project SmartEdu.DataAccess \
  --startup-project SmartEdu.RazorWeb
```

### Bước 3 — Chạy ứng dụng
```bash
dotnet run --project SmartEdu.RazorWeb
```
Hoặc mở solution trong **Visual Studio**, đặt `SmartEdu.RazorWeb` là Startup Project rồi nhấn **F5**.

### Bước 4 — Đăng nhập
Dùng account có quyền **leader** hoặc **giảng viên** (nếu đã seed dữ liệu).

### Bước 5 — Sử dụng tính năng
Truy cập `/Documents/UploadDocument`, sau đó:

1. Chọn môn học và upload file PDF/DOCX.
2. Nếu bạn là **leader**: bấm **"Kích hoạt Embedding"** để bắt đầu xử lý nền.
3. Theo dõi tiến trình qua modal progress hoặc thông báo realtime từ SignalR.
4. Bấm **"Xem chunk"** trên mỗi document để xem danh sách chunk đã tạo.

> ℹ️ Nếu embed server (Hugging Face model) đang khởi động, server có thể trả `ServiceUnavailable` — thông báo sẽ hiển thị; chờ vài giây rồi bấm lại.

---

## ⚙️ Tuỳ Chỉnh Chunking / Embedding

Chỉnh sửa trong `SmartEdu.Business/Services/DocumentService.cs`:

```csharp
ChunkText(text, chunkSize: 800, overlapFraction: 0.1)
```

- Thay đổi `chunkSize` hoặc `overlapFraction` để điều chỉnh kích thước / độ chồng lấp.
- Để dùng **token-aware** hoặc **sentence-aware chunking**, thay hàm `ChunkText` bằng tokenizer / sentence splitter phù hợp trước khi gọi embedding.

---

## 📝 Ghi Chú Vận Hành

- Embedding chạy dưới dạng **background task** (`Task.Run` với scope) — theo dõi log/exception để đảm bảo không crash.
- Cân nhắc thêm **rate-limit / retry** khi gọi external API (Hugging Face).
- Vector `EmbeddingJson` được lưu trong bảng `DocumentChunks`. Nếu vectors lớn, cân nhắc chuyển sang **vector database** chuyên dụng thay vì lưu trực tiếp trong SQL.
