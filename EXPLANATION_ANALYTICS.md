# 📊 Giải Thích Cách Biểu Đồ Analytics Hoạt Động

## 🔄 Luồng Dữ Liệu: Database → API → View → Chart

### 1. **Database (SQL Server)**
- Bảng `Orders`: Lưu thông tin đơn hàng (OrderCode, CustomerId, CreatedAt, OrderStatus...)
- Bảng `OrderDetails`: Lưu chi tiết đơn hàng (ProductId, Quantity, Price, TotalPrice)
- Bảng `Products`: Lưu thông tin sản phẩm (ProductName, CategoryId, BrandId...)
- Bảng `Categories`: Lưu danh mục sản phẩm

### 2. **API Endpoints (ServerSide)**
Các API trong `AnalyticsController.cs` truy vấn database và trả về JSON:

#### `GET /api/Analytics/SalesTrend?period=daily`
- **Daily**: Lấy đơn hàng 30 ngày qua, nhóm theo **ngày** (CreatedAt.Date)
- **Weekly**: Lấy đơn hàng 12 tuần qua, nhóm theo **tuần** (Week/Year)
- **Monthly**: Lấy đơn hàng 12 tháng qua, nhóm theo **tháng** (Year-Month)

**Ví dụ SQL tương đương:**
```sql
-- Daily
SELECT CAST(CreatedAt AS DATE) AS Date, 
       SUM(TotalPrice) AS Sales
FROM Orders o
JOIN OrderDetails od ON o.OrderId = od.OrderId
WHERE CreatedAt >= DATEADD(DAY, -30, GETDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date
```

#### `GET /api/Analytics/SalesByCategory`
- Join `OrderDetails` → `Products` → `Categories`
- Nhóm theo `CategoryName` và tính tổng doanh thu

#### `GET /api/Analytics/MonthlySales?year=2025`
- Lấy tất cả đơn hàng trong năm 2025
- Nhóm theo tháng (1-12)
- Fill các tháng không có đơn hàng = 0

### 3. **ClientSide Controller**
`AnalyticsController.cs` gọi các API trên và đổ dữ liệu vào `AnalyticsViewModel`

### 4. **View (Razor)**
`Views/Analytics/Index.cshtml`:
- Serialize dữ liệu từ ViewModel sang JSON
- Truyền vào JavaScript để render Chart.js

### 5. **Chart.js (JavaScript)**
- Nhận JSON data
- Render biểu đồ tương ứng (Line, Bar, Doughnut, Pie)

---

## 📅 Sự Khác Biệt Giữa Daily/Weekly/Monthly

### **Daily (Hàng Ngày)**
- **Dữ liệu**: 30 ngày gần nhất
- **Nhóm theo**: Mỗi ngày (2025-01-01, 2025-01-02...)
- **Dùng khi**: Muốn xem xu hướng ngắn hạn, theo dõi doanh thu hàng ngày

### **Weekly (Hàng Tuần)**
- **Dữ liệu**: 12 tuần gần nhất
- **Nhóm theo**: Mỗi tuần (Week 1/2025, Week 2/2025...)
- **Dùng khi**: Muốn xem xu hướng trung hạn, so sánh tuần này với tuần trước

### **Monthly (Hàng Tháng)**
- **Dữ liệu**: 12 tháng gần nhất
- **Nhóm theo**: Mỗi tháng (2025-01, 2025-02...)
- **Dùng khi**: Muốn xem xu hướng dài hạn, phân tích theo mùa

---

## 🧪 Cách Tạo Sample Data

### **Cách 1: Dùng Button trên Analytics Page**
1. Vào `/Analytics` (phải đăng nhập Admin)
2. Click nút **"🧪 Tạo Sample Data"**
3. Xác nhận → Hệ thống sẽ tạo:
   - **30 đơn hàng** cho 30 ngày qua (để test Daily)
   - **5-15 đơn hàng** cho mỗi tháng trong năm (để test Monthly)

### **Cách 2: Tạo Đơn Hàng Thật**
1. Đăng nhập với tài khoản Customer
2. Mua sản phẩm từ trang Products
3. Đơn hàng sẽ tự động được tạo và hiển thị trên biểu đồ

---

## 🔍 Kiểm Tra Dữ Liệu

### Xem trong Database:
```sql
-- Xem số lượng đơn hàng
SELECT COUNT(*) FROM Orders

-- Xem đơn hàng theo ngày
SELECT CAST(CreatedAt AS DATE) AS Date, COUNT(*) AS Count
FROM Orders
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date DESC

-- Xem doanh thu theo tháng
SELECT 
    YEAR(CreatedAt) AS Year,
    MONTH(CreatedAt) AS Month,
    SUM(od.TotalPrice) AS Sales
FROM Orders o
JOIN OrderDetails od ON o.OrderId = od.OrderId
GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)
ORDER BY Year DESC, Month DESC
```

### Xem qua API:
- Mở browser: `https://localhost:5000/swagger`
- Test các endpoint trong `AnalyticsController`
- Hoặc dùng Postman/curl

---

## ⚠️ Lưu Ý

1. **Sample Data** có OrderCode bắt đầu bằng "ORD" - có thể xóa sau khi test
2. **Real Data** là đơn hàng thật từ khách hàng - không nên xóa
3. Biểu đồ chỉ hiển thị đơn hàng có `OrderStatus = "Đã Xác Nhận"`
4. Cần có ít nhất 1 sản phẩm và 1 khách hàng để tạo sample data

---

## 🎯 Kết Luận

**Biểu đồ lấy dữ liệu TRỰC TIẾP từ Database** thông qua:
- Entity Framework Core query
- LINQ to SQL
- Group by và Aggregate functions

**Không cần** phải lưu dữ liệu riêng cho biểu đồ - mọi thứ được tính toán real-time từ bảng Orders và OrderDetails!

