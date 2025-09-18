# Hướng dẫn Session-based Authentication

## Tổng quan
Hệ thống đã được chuyển đổi hoàn toàn từ JWT sang session-based authentication đơn giản và an toàn hơn.

## Luồng Authentication

### 1. Login
- **Endpoint**: `POST /Auth/Login` hoặc `POST /api/AuthApi/login`
- **Request**: Username/Email + Password
- **Response**: 
  ```json
  {
    "success": true,
    "message": "Login successful",
    "user": {
      "id": "guid",
      "username": "username",
      "email": "email@example.com",
      "fullName": "Full Name",
      "role": "RoleName"
    }
  }
  ```
- **Server Actions**:
  - Tạo Claims (User ID, Username, Email, Role)
  - Tạo Authentication Cookie (HttpOnly, Secure)
  - Lưu session trong server memory
  - Trả về thông tin user trong JSON response

### 2. Session Management
- **Storage**: Authentication cookie được lưu tự động bởi browser
- **Lifetime**: 7 ngày (có thể cấu hình)
- **Security**: 
  - `HttpOnly`: Không thể truy cập từ JavaScript
  - `SameSite=Strict`: Chỉ gửi trong same-site requests
  - `Secure`: Tự động set theo request scheme

### 3. API Calls
- **Authentication**: Tự động gửi authentication cookie
- **Authorization**: Sử dụng `[Authorize]` attribute trên controllers/actions
- **No Token Management**: Không cần quản lý token ở frontend

### 4. Logout
- **Endpoint**: `POST /api/AuthApi/logout` hoặc `GET /Auth/Logout`
- **Process**:
  - Xóa authentication cookie
  - Xóa session khỏi server memory
  - Redirect về trang login

## Bảo mật

### Authentication Cookie
- **Storage**: HttpOnly Cookie (không thể truy cập từ JavaScript)
- **Lifetime**: 7 ngày với sliding expiration
- **Security Features**:
  - `HttpOnly`: Bảo vệ khỏi XSS attacks
  - `SameSite=Strict`: Bảo vệ khỏi CSRF attacks
  - `Secure`: Chỉ gửi qua HTTPS trong production

### Session Storage
- **Location**: Server memory (có thể chuyển sang Redis cho production)
- **Data**: Claims (User ID, Username, Email, Role)
- **Cleanup**: Tự động xóa khi hết hạn hoặc logout

## Frontend Integration

### JavaScript API
```javascript
// Kiểm tra authentication status
window.AuthManager.isAuthenticated()

// Logout
await window.AuthManager.logout()
```

### Auto Session Management
- Tự động gửi authentication cookie trong tất cả requests
- Tự động redirect về login khi session hết hạn
- Tự động cập nhật navigation menu

## Cấu hình

### Session Settings (Program.cs)
```csharp
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

### Protected Routes
```csharp
[Authorize]
public class UsersController : BaseController
{
    // Tất cả actions trong controller này yêu cầu authentication
}

[Authorize]
public class RolesController : BaseController
{
    // Tất cả actions trong controller này yêu cầu authentication
}
```

## Testing

### Test Login Flow
1. Mở Developer Tools → Application → Cookies
2. Login với user hợp lệ
3. Kiểm tra:
   - Authentication cookie được tạo (HttpOnly)
   - Navigation menu cập nhật
   - Có thể truy cập protected routes

### Test Session Persistence
1. Đóng và mở lại browser
2. Truy cập protected route
3. Kiểm tra:
   - Vẫn được authenticate
   - Không cần login lại

### Test Logout
1. Click logout button
2. Kiểm tra:
   - Authentication cookie bị xóa
   - Redirect về trang login
   - Không thể truy cập protected routes

## So sánh với JWT

### Ưu điểm của Session Authentication
- **Đơn giản hơn**: Không cần quản lý token ở frontend
- **Bảo mật hơn**: Cookie HttpOnly không thể bị đánh cắp qua JavaScript
- **Dễ debug**: Session state được lưu trên server
- **Tự động cleanup**: Session tự động hết hạn

### Nhược điểm
- **Server memory**: Cần lưu session trên server (có thể dùng Redis)
- **Scalability**: Cần sticky sessions hoặc shared session store cho multiple servers

## Troubleshooting

### Common Issues
1. **Session không persist**: Kiểm tra cookie settings và SameSite policy
2. **Logout không hoàn toàn**: Kiểm tra SignOutAsync được gọi đúng cách
3. **401 trên protected routes**: Kiểm tra [Authorize] attribute và authentication middleware

### Debug Tips
- Sử dụng Developer Tools để kiểm tra cookies
- Kiểm tra Network tab để xem authentication headers
- Xem Console logs để debug session validation

## Migration từ JWT

### Đã loại bỏ
- JWT Service và Refresh Token logic
- Token management ở frontend
- Custom Authentication Middleware
- Refresh token fields trong database

### Đã thêm
- Cookie Authentication
- Session management
- [Authorize] attributes
- Simplified frontend authentication logic
