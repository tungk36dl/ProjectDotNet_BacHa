# 🎭 Role Management Module Guide

## Tổng quan

Module Role Management đã được tạo hoàn chỉnh với đầy đủ tính năng CRUD và tích hợp với hệ thống User management.

### ✨ Tính năng chính

1. **Role Entity** - Bảng Role với foreign key relationship với User
2. **Role Management** - CRUD operations cho Role
3. **User-Role Integration** - User có thể được assign Role
4. **Role-based Authorization** - Phân quyền dựa trên Role
5. **Seed Data** - 3 roles mặc định: Admin, Manager, User

## 🏗️ Kiến trúc

### Database Schema

```sql
-- Roles table
CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL
);

-- Users table (updated)
ALTER TABLE [Users] ADD [RoleId] uniqueidentifier NULL;
ALTER TABLE [Users] ADD [RoleName] nvarchar(max) NULL; -- Legacy field
ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Roles_RoleId] 
    FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE SET NULL;
```

### Entity Relationships

```
User (1) ←→ (0..1) Role
- User.RoleId → Role.Id (Foreign Key)
- Role.Users → Collection of Users
```

## 🚀 Cách sử dụng

### 1. Truy cập Role Management

- **URL**: `/Roles`
- **Yêu cầu**: Đăng nhập (kế thừa BaseController)
- **Quyền**: Tất cả user đã đăng nhập

### 2. CRUD Operations

#### **Create Role**
```csharp
// Controller
public async Task<IActionResult> Create(RoleCreateVM model)
{
    var role = new Role
    {
        Name = model.Name,
        Description = model.Description,
        IsActive = model.IsActive
    };
    
    var result = await _roleService.AddAsync(role);
    return HandleServiceResponse(result, "Index");
}
```

#### **Read Roles**
```csharp
// Get all roles with search and pagination
public async Task<IActionResult> Index([FromQuery] RoleSearch? search)
{
    var resp = await _roleService.GetAllAsync(search);
    return View(resp.Data ?? new List<Role>());
}

// Get role by ID
public async Task<IActionResult> Details(Guid id)
{
    var resp = await _roleService.GetByIdAsync(id);
    return View(resp.Data);
}
```

#### **Update Role**
```csharp
public async Task<IActionResult> Edit(Guid id, RoleUpdateVM model)
{
    var role = new Role
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description,
        IsActive = model.IsActive
    };
    
    var resp = await _roleService.UpdateAsync(role);
    return HandleServiceResponse(resp, "Index");
}
```

#### **Delete Role**
```csharp
public async Task<IActionResult> DeleteConfirmed(Guid id)
{
    var resp = await _roleService.DeleteAsync(id);
    // Prevents deletion if role is assigned to users
    return RedirectToAction(nameof(Index));
}
```

### 3. User-Role Integration

#### **Assign Role to User**
```csharp
// In User Create/Edit forms
<div class="form-group">
    <label asp-for="RoleId" class="form-label">Role</label>
    <select asp-for="RoleId" class="form-control" asp-items="ViewBag.Roles">
        <option value="">-- Select Role --</option>
    </select>
</div>
```

#### **Load Roles for Dropdown**
```csharp
private async Task LoadRolesAsync()
{
    var rolesResp = await _roleService.GetAllAsync();
    if (rolesResp.Success && rolesResp.Data != null)
    {
        ViewBag.Roles = new SelectList(rolesResp.Data.Where(r => r.IsActive), "Id", "Name");
    }
}
```

## 📋 API Endpoints

### Role Management
- `GET /Roles` - Danh sách roles
- `GET /Roles/Create` - Form tạo role mới
- `POST /Roles/Create` - Xử lý tạo role
- `GET /Roles/Details/{id}` - Chi tiết role
- `GET /Roles/Edit/{id}` - Form chỉnh sửa role
- `POST /Roles/Edit/{id}` - Xử lý cập nhật role
- `GET /Roles/Delete/{id}` - Form xóa role
- `POST /Roles/Delete/{id}` - Xử lý xóa role

### API Methods
- `GET /Roles/CheckNameExists?name={name}&excludeId={id}` - Kiểm tra tên role trùng
- `GET /Roles/GetRolesWithUserCount` - Lấy roles với số lượng user

## 🎯 Features

### 1. Search & Filter
- **Text Search**: Tìm kiếm theo tên và mô tả role
- **Status Filter**: Lọc theo trạng thái Active/Inactive
- **Pagination**: Phân trang kết quả

### 2. Validation
- **Name Uniqueness**: Tên role phải duy nhất
- **Required Fields**: Tên role bắt buộc
- **Length Limits**: Giới hạn độ dài tên và mô tả

### 3. Business Rules
- **Delete Protection**: Không thể xóa role đang được sử dụng
- **User Count**: Hiển thị số lượng user sử dụng role
- **Active Status**: Chỉ hiển thị role active trong dropdown

### 4. UI/UX Features
- **Real-time Validation**: Kiểm tra tên trùng ngay khi nhập
- **Success/Error Messages**: Thông báo kết quả operations
- **Responsive Design**: Giao diện responsive với Bootstrap
- **Auto-hide Alerts**: Tự động ẩn thông báo sau 5 giây

## 🔧 Configuration

### 1. Seed Data
3 roles mặc định được tạo khi migration:
- **Admin** (ID: 11111111-1111-1111-1111-111111111111)
- **Manager** (ID: 22222222-2222-2222-2222-222222222222)  
- **User** (ID: 33333333-3333-3333-3333-333333333333)

### 2. Database Migration
```bash
dotnet ef migrations add AddRoleTableAndUserRoleRelationship
dotnet ef database update
```

### 3. Dependency Injection
```csharp
// Program.cs
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
```

## 🛡️ Security & Authorization

### 1. Access Control
- Tất cả actions yêu cầu đăng nhập (BaseController)
- Có thể thêm role-based authorization cho từng action

### 2. Data Protection
- Validation input để tránh SQL injection
- CSRF protection với ValidateAntiForgeryToken
- XSS protection với HTML encoding

### 3. Business Logic Protection
- Không cho phép xóa role đang được sử dụng
- Kiểm tra uniqueness trước khi tạo/cập nhật

## 📊 Performance

### 1. Database Optimization
- Index trên RoleId foreign key
- Lazy loading cho User collection
- Pagination để giảm tải

### 2. Caching
- Có thể thêm caching cho danh sách roles
- Cache role dropdown để tăng performance

## 🐛 Troubleshooting

### 1. Common Issues

#### **Role không hiển thị trong dropdown**
- Kiểm tra role có IsActive = true
- Kiểm tra LoadRolesAsync() được gọi
- Kiểm tra ViewBag.Roles có data

#### **Không thể xóa role**
- Kiểm tra role có đang được sử dụng bởi user
- Xem thông báo lỗi trong TempData

#### **Validation errors**
- Kiểm tra tên role có trùng không
- Kiểm tra required fields

### 2. Debug Tips

```csharp
// Log role operations
_logger.LogInformation("Creating role: {RoleName}", role.Name);

// Check role assignment
var userCount = await _roleRepository.FindAll(r => r.Id == roleId)
    .SelectMany(r => r.Users)
    .CountAsync();
```

## 🚀 Future Enhancements

### 1. Advanced Features
- **Role Permissions**: Thêm permissions cho từng role
- **Role Hierarchy**: Cấu trúc phân cấp roles
- **Bulk Operations**: Thao tác hàng loạt
- **Role Templates**: Mẫu role có sẵn

### 2. Integration
- **Audit Logging**: Ghi log thay đổi roles
- **API Endpoints**: RESTful API cho mobile
- **Export/Import**: Xuất/nhập roles

### 3. UI Improvements
- **Drag & Drop**: Sắp xếp roles
- **Bulk Edit**: Chỉnh sửa nhiều roles
- **Advanced Search**: Tìm kiếm nâng cao

## 📚 Examples

### Tạo Role mới
```csharp
var role = new Role
{
    Name = "Editor",
    Description = "Content Editor Role",
    IsActive = true
};

var result = await _roleService.AddAsync(role);
if (result.Success)
{
    // Role created successfully
}
```

### Assign Role to User
```csharp
var user = await _userService.GetByIdAsync(userId);
if (user.Success && user.Data != null)
{
    user.Data.RoleId = roleId;
    await _userService.UpdateAsync(user.Data);
}
```

### Check User Role
```csharp
var user = await _userService.GetByIdAsync(userId);
var roleName = user.Data?.Role?.Name ?? user.Data?.RoleName ?? "No Role";
```

Module Role Management đã sẵn sàng để sử dụng! 🎉
