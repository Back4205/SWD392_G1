# 📦 Electronics Warehouse Management System (EWMS)
### Hệ thống Quản lý Kho Thiết bị Điện tử

[![SWD392](https://img.shields.io/badge/Course-SWD392-blue.svg)](https://github.com/)
[![ASP.NET Core](https://img.shields.io/badge/Backend-ASP.NET%20Core%208-purple.svg)]()
[![React](https://img.shields.io/badge/Frontend-React%2018-cyan.svg)]()
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red.svg)]()

## 📖 Giới thiệu dự án
**EWMS** là một nền tảng web tập trung được phát triển nhằm thay thế các quy trình quản lý kho thủ công (Excel, giấy tờ) hiện tại của công ty phân phối thiết bị điện tử. Hệ thống giải quyết các bài toán:
- Đồng bộ dữ liệu theo thời gian thực giữa các kho.
- Tự động hóa việc theo dõi mã Serial/IMEI của từng thiết bị.
- Tối ưu hóa quy trình Nhập kho (Inbound), Xuất kho (Outbound), Điều chuyển (Transfer) và Kiểm kê (Inventory Count).
- Quản lý cấu trúc kho vật lý (Khu vực, Kệ, Bin) và giới hạn tải trọng.

## 🛠️ Công nghệ sử dụng (Tech Stack)
- **Frontend:** React 18, TypeScript, Vite, TailwindCSS / Ant Design.
- **Backend:** C# / ASP.NET Core Web API (.NET 8), Entity Framework Core.
- **Database:** Microsoft SQL Server.
- **Authentication:** JWT (JSON Web Tokens), BCrypt.
- **Tools:** Git, GitHub, Swagger (API Docs), Postman, Visual Studio / VS Code.

## 🚀 Tính năng chính
### 1. Core 1 (Nhóm tính năng nền tảng)
- **Authentication & Authorization:** Đăng nhập, phân quyền RBAC (Admin, Manager, Purchasing, Sales, Warehouse Staff).
- **Inbound Process:** Tạo yêu cầu nhập kho, tạo Goods Receipt, quét Serial/IMEI.
- **Outbound Process:** Tạo yêu cầu xuất kho, duyệt xuất kho, đóng gói và bàn giao.

### 2. Core 2 (Nhóm tính năng mở rộng)
- **Transfer Order Process:** Điều chuyển hàng hóa giữa các Bin (nội bộ) và giữa các Kho. Quản lý trạng thái: `Pending` ➔ `Approved` ➔ `In_Transit` ➔ `Completed` / `Completed_With_Discrepancy`.
- **Inventory Count Process:** Tạo phiếu kiểm kê, quét Serial thực tế, đối chiếu System vs Actual, xử lý chênh lệch (Adjustment).
- **Serial/IMEI Tracking:** Quản lý vòng đời của từng linh kiện (In_Stock, Reserved, In_Transit, Dispatched, Lost).

---

## 📂 Cấu trúc thư mục (Repository Structure)
Dự án được tổ chức theo mô hình **Monorepo** (chứa cả Frontend và Backend chung một repo).

```text
EWMS-SE1972-NET/
├── backend/                  # ASP.NET Core Web API
│   ├── EWMS.API/             # Controllers, Middlewares, Program.cs
│   ├── EWMS.Application/     # Services, DTOs, Validators, Interfaces
│   ├── EWMS.Domain/          # Entities, Enums, Interfaces cốt lõi
│   ├── EWMS.Infrastructure/  # DbContext, Repositories, Migrations
│   └── EWMS.sln              # Solution file
│
├── frontend/                 # React 18 Application
│   ├── src/
│   │   ├── components/       # UI components dùng chung
│   │   ├── pages/            # Các màn hình (Transfer, Inventory...)
│   │   ├── services/         # Gọi API (Axios)
│   │   ├── store/            # Quản lý state (Redux/Zustand)
│   │   └── utils/            # Hàm tiện ích, constants
│   ├── package.json
│   └── vite.config.ts
│
├── docs/                     # Chứa tài liệu dự án
│   ├── SRS_Report_1.pdf
│   ├── Detailed_Design_Report_2.pdf
│   └── Database_ERD.png
│
└── README.md                 # File này
