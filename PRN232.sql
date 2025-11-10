create database PRN232_ClockShop;
go
use PRN232_ClockShop;
go
drop database PRN232_ClockShop;

-- Bảng Vai trò (Roles)
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng Người dùng
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20),
    Address NVARCHAR(255),
    RoleID INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

CREATE TABLE WebPages (
    PageID INT PRIMARY KEY IDENTITY(1,1),
    PageName nvarchar(50) not null,
	URL nvarchar(250) not null
);

CREATE TABLE Permissions (
    PermissionID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT NOT NULL,
	PageID INT NOT NULL,
    CanAdd bit default 0,
	CanEdit bit default 0,
    CanDelete bit default 0,
	CanView bit default 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID),
	FOREIGN KEY (PageID) REFERENCES WebPages(PageID),
	UNIQUE(RoleID, PageID),
);

-- Bảng Thương hiệu đồng hồ
CREATE TABLE Brands (
    BrandID INT PRIMARY KEY IDENTITY(1,1),
    BrandName NVARCHAR(100) NOT NULL UNIQUE,
    Country NVARCHAR(50),
    Description NVARCHAR(500),
    Logo NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng Danh mục sản phẩm
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    ParentCategoryID INT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID)
);

-- Bảng Sản phẩm đồng hồ
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductCode NVARCHAR(50) NOT NULL UNIQUE,
    ProductName NVARCHAR(200) NOT NULL,
    BrandID INT NOT NULL,
    CategoryID INT NOT NULL,
    Description NVARCHAR(MAX),
	Image nvarchar(max),
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BrandID) REFERENCES Brands(BrandID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

-- Bảng Đơn hàng
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    OrderCode NVARCHAR(50) NOT NULL UNIQUE,
    CustomerID INT NOT NULL,    
    OrderStatus NVARCHAR(50) 
    CHECK (OrderStatus IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã hủy')) 
    DEFAULT N'Chờ xác nhận',
    PaymentStatus NVARCHAR(50)
	check (PaymentStatus in (N'Chưa thanh toán', N'Đã thanh toán'))
	DEFAULT N'Chưa thanh toán',
    PaymentMethod NVARCHAR(50), -- COD, Chuyển khoản, Ví điện tử
    Note NVARCHAR(500),    
    ProcessedBy INT, -- Nhân viên xử lý
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),    
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (ProcessedBy) REFERENCES Users(UserID)
);

-- Bảng Chi tiết đơn hàng
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL, -- Lưu tên sản phẩm tại thời điểm mua
    Price DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);


-- Bảng Đánh giá sản phẩm
CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    CustomerID INT NOT NULL,
    OrderID INT, -- Chỉ cho phép đánh giá nếu đã mua
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(1000),
    ApprovedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
);

-- Bảng Giỏ hàng
CREATE TABLE Cart (
    CartID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    UNIQUE (CustomerID, ProductID)
);

-- Bảng Danh sách yêu thích
CREATE TABLE Wishlist (
    WishlistID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT NOT NULL,
    ProductID INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    UNIQUE (CustomerID, ProductID)
);

-- Bảng Giao dịch thanh toán
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(50)
	check (PaymentStatus in (N'Chưa thanh toán', N'Đã thanh toán'))
	DEFAULT N'Chưa thanh toán',
    Method NVARCHAR(255),
    Note NVARCHAR(1000),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
);

-- 1. Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị hệ thống'),
(N'Supplier', N'Nhân viên bán hàng và xử lý đơn'),
(N'Customer', N'Khách hàng');

-- 2. Users
INSERT INTO Users (Username, PasswordHash, Email, FullName, PhoneNumber, Address, RoleID)
VALUES
(N'admin', N'adminhash', N'admin@clockshop.vn', N'Nguyễn Văn A', N'0987654321', N'Hà Nội', 1),
(N'Supplier1', N'staff1hash', N'staff1@clockshop.vn', N'Trần Thị B', N'0912345678', N'Hồ Chí Minh', 2),
(N'Supplier2', N'customer1hash', N'customer1@gmail.com', N'Lê Văn C', N'0901234567', N'Hà Nội', 3),
(N'Supplier3', N'customer2hash', N'customer2@gmail.com', N'Phạm Thị D', N'0938765432', N'Đà Nẵng', 3);

-- 3. Brands
INSERT INTO Brands (BrandName, Country, Description)
VALUES
(N'Casio', N'Japan', N'Đồng hồ Nhật Bản nổi tiếng, bền, giá cả hợp lý'),
(N'Seiko', N'Japan', N'Thương hiệu đồng hồ Nhật Bản cao cấp'),
(N'Orient', N'Japan', N'Đồng hồ cơ Nhật Bản, giá mềm'),
(N'Citizen', N'Japan', N'Đồng hồ Nhật Bản chất lượng, phổ biến tại Việt Nam'),
(N'Daniel Wellington', N'Sweden', N'Đồng hồ thời trang, phong cách trẻ trung'),
(N'Fossil', N'USA', N'Đồng hồ phong cách Mỹ, thời trang và bền');

-- 4. Categories
INSERT INTO Categories (CategoryName, Description)
VALUES
(N'Đồng hồ nam', N'Đồng hồ dành cho nam giới'),
(N'Đồng hồ nữ', N'Đồng hồ dành cho nữ giới'),
(N'Đồng hồ đôi', N'Đồng hồ dành cho cặp đôi'),
(N'Đồng hồ cơ', N'Đồng hồ cơ tự động'),
(N'Đồng hồ điện tử', N'Đồng hồ điện tử hiện đại');

-- 5. Products
INSERT INTO Products (ProductCode, ProductName, BrandID, CategoryID, Description, Price, StockQuantity)
VALUES
(N'CAS001', N'Casio G-Shock GA-2100', 1, 4, N'Đồng hồ cơ G-Shock siêu bền, mặt vuông', 3500000, 20),
(N'CAS002', N'Casio Baby-G BA-110', 1, 2, N'Đồng hồ nữ Baby-G chống nước', 2800000, 15),
(N'SEI001', N'Seiko 5 SNK809', 2, 4, N'Đồng hồ cơ Seiko 5, dây vải', 4200000, 10),
(N'OR001', N'Orient Bambino Gen 2', 3, 4, N'Đồng hồ cơ Orient, kính cong', 3500000, 8),
(N'DW001', N'Daniel Wellington Classic Black', 5, 2, N'Đồng hồ thời trang dây da', 3200000, 25),
(N'CIT001', N'Citizen Eco-Drive BM7100', 4, 4, N'Đồng hồ cơ Eco-Drive, năng lượng ánh sáng', 6000000, 5),
(N'FOS001', N'Fossil Grant Chronograph', 6, 1, N'Đồng hồ nam phong cách Mỹ, chronograph', 4500000, 12);

-- 6. Orders
INSERT INTO Orders (OrderCode, CustomerID, OrderStatus, PaymentStatus, PaymentMethod, Note, ProcessedBy)
VALUES
(N'ORD001', 3, N'Đã xác nhận', N'Đã thanh toán', N'COD', N'Giao hàng nhanh', 2),
(N'ORD002', 4, N'Chờ xác nhận', N'Chưa thanh toán', N'Transfer', N'Giao vào buổi chiều', NULL);

-- 7. OrderDetails
INSERT INTO OrderDetails (OrderID, ProductID, ProductName, Price, Quantity, TotalPrice)
VALUES
(1, 1, N'Casio G-Shock GA-2100', 3500000, 1, 3500000),
(1, 3, N'Seiko 5 SNK809', 4200000, 1, 4200000),
(2, 5, N'Daniel Wellington Classic Black', 3200000, 2, 6400000);

-- 8. Payments
INSERT INTO Payments (OrderID, Amount, PaymentStatus, Method, Note)
VALUES
(1, 7700000, N'Đã thanh toán', N'COD', N'Thanh toán khi nhận hàng'),
(2, 6400000, N'Chưa thanh toán', N'Transfer', N'Chuyển khoản ngân hàng');

-- 9. Reviews
INSERT INTO Reviews (ProductID, CustomerID, OrderID, Rating, Comment, ApprovedBy)
VALUES
(1, 3, 1, 5, N'Đồng hồ rất đẹp và bền', 2),
(3, 3, 1, 4, N'Đẹp nhưng dây hơi cứng', 2);

-- 10. Cart
INSERT INTO Cart (CustomerID, ProductID, Quantity)
VALUES
(3, 2, 1),
(4, 1, 2);

-- 11. Wishlist
INSERT INTO Wishlist (CustomerID, ProductID)
VALUES
(3, 5),
(4, 6);
