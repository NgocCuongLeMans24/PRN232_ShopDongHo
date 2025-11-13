CREATE DATABASE PRN232_ClockShop;

USE PRN232_ClockShop;
GO

-- Nếu tồn tại các bảng cũ, xóa theo thứ tự ràng buộc (chỉ nếu đang làm mới cấu trúc)
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE Reviews;
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE OrderDetails;
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE Payments;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('dbo.Brands', 'U') IS NOT NULL DROP TABLE Brands;
IF OBJECT_ID('dbo.Wishlist', 'U') IS NOT NULL DROP TABLE Wishlist;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE Roles;
IF OBJECT_ID('dbo.WebPages', 'U') IS NOT NULL DROP TABLE WebPages;
IF OBJECT_ID('dbo.Permissions', 'U') IS NOT NULL DROP TABLE Permissions;
GO

-- =============================================
-- 1️⃣ BẢNG Roles
-- =============================================
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- =============================================
-- 2️⃣ BẢNG Users (có xác thực email)
-- =============================================
CREATE TABLE [Users] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    FullName NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(50),
    [Address] NVARCHAR(255),
    RoleId INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    VerificationToken NVARCHAR(255),
    VerificationTokenExpire DATETIME,
    IsVerified BIT DEFAULT 0,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleID)
);

-- =============================================
-- 3️⃣ WebPages & Permissions (phân quyền)
-- =============================================
CREATE TABLE WebPages (
    PageID INT PRIMARY KEY IDENTITY(1,1),
    PageName NVARCHAR(50) NOT NULL,
    URL NVARCHAR(250) NOT NULL
);

CREATE TABLE Permissions (
    PermissionID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT NOT NULL,
    PageID INT NOT NULL,
    CanAdd BIT DEFAULT 0,
    CanEdit BIT DEFAULT 0,
    CanDelete BIT DEFAULT 0,
    CanView BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID),
    FOREIGN KEY (PageID) REFERENCES WebPages(PageID),
    UNIQUE(RoleID, PageID)
);

-- =============================================
-- 4️⃣ Brands
-- =============================================
CREATE TABLE Brands (
    BrandID INT PRIMARY KEY IDENTITY(1,1),
    BrandName NVARCHAR(100) NOT NULL UNIQUE,
    Country NVARCHAR(50),
    Description NVARCHAR(500),
    Logo NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- =============================================
-- 5️⃣ Categories
-- =============================================
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    ParentCategoryID INT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID)
);

-- =============================================
-- 6️⃣ Products
-- =============================================
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductCode NVARCHAR(50) NOT NULL UNIQUE,
    ProductName NVARCHAR(200) NOT NULL,
    BrandID INT NOT NULL,
    CategoryID INT NOT NULL,
    Description NVARCHAR(MAX),
    Image NVARCHAR(MAX),
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    SupplierID INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BrandID) REFERENCES Brands(BrandID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    FOREIGN KEY (SupplierID) REFERENCES Users(UserID)
);

-- =============================================
-- 7️⃣ Orders
-- =============================================
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    OrderCode NVARCHAR(50) NOT NULL UNIQUE,
    CustomerID INT NOT NULL,
    OrderStatus NVARCHAR(50)
        CHECK (OrderStatus IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã hủy'))
        DEFAULT N'Chờ xác nhận',
    PaymentStatus NVARCHAR(50)
        CHECK (PaymentStatus IN (N'Chưa thanh toán', N'Đã thanh toán'))
        DEFAULT N'Chưa thanh toán',
    PaymentMethod NVARCHAR(50),
    Note NVARCHAR(500),
    ProcessedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (ProcessedBy) REFERENCES Users(UserID)
);

-- =============================================
-- 8️⃣ OrderDetails
-- =============================================
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- =============================================
-- 9️⃣ Reviews (1 Order chỉ có 1 Review)
-- =============================================
CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT UNIQUE NOT NULL, -- 1 đơn hàng chỉ 1 đánh giá
    ProductID INT NOT NULL,
    CustomerID INT NOT NULL,
    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(1000),
    ApprovedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
);

-- =============================================
-- 🔟 Wishlist (giữ nguyên)
-- =============================================
CREATE TABLE Wishlist (
    WishlistID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT NOT NULL,
    ProductID INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    UNIQUE (CustomerID, ProductID)
);

-- =============================================
-- 1️⃣1️⃣ Payments
-- =============================================
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(50)
        CHECK (PaymentStatus IN (N'Chưa thanh toán', N'Đã thanh toán'))
        DEFAULT N'Chưa thanh toán',
    Method NVARCHAR(255),
    Note NVARCHAR(1000),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

-- =============================================
-- 🌟 DỮ LIỆU MẪU (giữ nguyên toàn bộ)
-- =============================================

INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị hệ thống'),
(N'Supplier', N'Nhân viên bán hàng và xử lý đơn'),
(N'Customer', N'Khách hàng');

INSERT INTO Users (Username, PasswordHash, Email, FullName, PhoneNumber, Address, RoleID)
VALUES
(N'admin', N'adminhash', N'admin@clockshop.vn', N'Nguyễn Văn A', N'0987654321', N'Hà Nội', 1),
(N'Supplier1', N'staff1hash', N'staff1@clockshop.vn', N'Trần Thị B', N'0912345678', N'Hồ Chí Minh', 2),
(N'Supplier2', N'staff2hash', N'staff2@clockshop.vn', N'Lê Văn C', N'0901234567', N'Hà Nội', 2),
(N'Supplier3', N'staff3hash', N'staff3@clockshop.vn', N'Phạm Thị D', N'0938765432', N'Đà Nẵng', 2),
(N'Customer1', N'cust1hash', N'customer1@gmail.com', N'Ngô Thanh E', N'0988123456', N'Hải Phòng', 3),
(N'Customer2', N'cust2hash', N'customer2@gmail.com', N'Lý Minh F', N'0909654321', N'Cần Thơ', 3);

INSERT INTO Brands (BrandName, Country, Description)
VALUES
(N'Casio', N'Japan', N'Đồng hồ Nhật Bản nổi tiếng, bền, giá cả hợp lý'),
(N'Seiko', N'Japan', N'Thương hiệu đồng hồ Nhật Bản cao cấp'),
(N'Orient', N'Japan', N'Đồng hồ cơ Nhật Bản, giá mềm'),
(N'Citizen', N'Japan', N'Đồng hồ Nhật Bản chất lượng, phổ biến tại Việt Nam'),
(N'Daniel Wellington', N'Sweden', N'Đồng hồ thời trang, phong cách trẻ trung'),
(N'Fossil', N'USA', N'Đồng hồ phong cách Mỹ, thời trang và bền');

INSERT INTO Categories (CategoryName, Description)
VALUES
(N'Đồng hồ nam', N'Đồng hồ dành cho nam giới'),
(N'Đồng hồ nữ', N'Đồng hồ dành cho nữ giới'),
(N'Đồng hồ đôi', N'Đồng hồ dành cho cặp đôi'),
(N'Đồng hồ cơ', N'Đồng hồ cơ tự động'),
(N'Đồng hồ điện tử', N'Đồng hồ điện tử hiện đại');
-- 5. Products
INSERT INTO Products (ProductCode, ProductName, BrandID, CategoryID, Description, Price, StockQuantity, SupplierID)
VALUES
(N'CAS001', N'Casio G-Shock GA-2100', 1, 4, N'Đồng hồ cơ G-Shock siêu bền, mặt vuông', 3500000, 20, 2),
(N'CAS002', N'Casio Baby-G BA-110', 1, 2, N'Đồng hồ nữ Baby-G chống nước', 2800000, 15, 2),
(N'SEI001', N'Seiko 5 SNK809', 2, 4, N'Đồng hồ cơ Seiko 5, dây vải', 4200000, 10, 3),
(N'OR001', N'Orient Bambino Gen 2', 3, 4, N'Đồng hồ cơ Orient, kính cong', 3500000, 8, 3),
(N'DW001', N'Daniel Wellington Classic Black', 5, 2, N'Đồng hồ thời trang dây da', 3200000, 25, 4),
(N'CIT001', N'Citizen Eco-Drive BM7100', 4, 4, N'Đồng hồ cơ Eco-Drive, năng lượng ánh sáng', 6000000, 5, 4),
(N'FOS001', N'Fossil Grant Chronograph', 6, 1, N'Đồng hồ nam phong cách Mỹ, chronograph', 4500000, 12, 4);

INSERT INTO Orders (OrderCode, CustomerID, OrderStatus, PaymentStatus, PaymentMethod, Note, ProcessedBy)
VALUES
(N'ORD001', 5, N'Đã xác nhận', N'Đã thanh toán', N'COD', N'Giao hàng nhanh', 2),
(N'ORD002', 6, N'Chờ xác nhận', N'Chưa thanh toán', N'Transfer', N'Giao vào buổi chiều', NULL);

INSERT INTO OrderDetails (OrderID, ProductID, ProductName, Price, Quantity, TotalPrice)
VALUES
(1, 1, N'Casio G-Shock GA-2100', 3500000, 1, 3500000),
(1, 3, N'Seiko 5 SNK809', 4200000, 1, 4200000),
(2, 5, N'Daniel Wellington Classic Black', 3200000, 2, 6400000);

INSERT INTO Payments (OrderID, Amount, PaymentStatus, Method, Note)
VALUES
(1, 7700000, N'Đã thanh toán', N'COD', N'Thanh toán khi nhận hàng'),
(2, 6400000, N'Chưa thanh toán', N'Transfer', N'Chuyển khoản ngân hàng');

INSERT INTO Reviews (OrderID, ProductID, CustomerID, Rating, Comment, ApprovedBy)
VALUES
(1, 5, 1, 5, N'Đồng hồ rất đẹp và bền', 2),
(2, 5, 1, 4, N'Đẹp nhưng dây hơi cứng', 2);


-- 11. Wishlist
INSERT INTO Wishlist (CustomerID, ProductID)
VALUES
(5, 5),
(6, 6);
