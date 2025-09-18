# Sửa lỗi Bearer Authentication Error

## Lỗi gặp phải
```
InvalidOperationException: No authentication handler is registered for the scheme 'Bearer'. 
The registered schemes are: Cookies. Did you forget to call AddAuthentication().Add[SomeAuthHandler]("Bearer",...)?
```

## Nguyên nhân
Khi chuyển từ JWT sang session-based authentication, một số controller vẫn đang sử dụng JWT Bearer scheme thay vì Cookie scheme.

## Vấn đề cụ thể
`BaseController.cs` vẫn có:
```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
```

Nhưng trong `Program.cs` chỉ có:
```csharp
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", ...)
```

## Giải pháp đã áp dụng

### 1. Cập nhật BaseController.cs
**Trước:**
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class BaseController : Controller
```

**Sau:**
```csharp
// Loại bỏ using JwtBearer
[Authorize] // Sử dụng default scheme (Cookies)
public abstract class BaseController : Controller
```

### 2. Kiểm tra tất cả controllers
Đảm bảo không có controller nào khác sử dụng JWT Bearer scheme:
```bash
grep -r "JwtBearerDefaults" Controllers/
grep -r "Bearer" Controllers/
```

### 3. Xác nhận Program.cs đúng
```csharp
// Session Authentication
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
```

## Kết quả
- ✅ Tất cả controllers sử dụng Cookie authentication
- ✅ Không còn lỗi Bearer authentication
- ✅ Session-based authentication hoạt động bình thường
- ✅ Protected routes được bảo vệ đúng cách

## Các controller được bảo vệ
- `UsersController` - Quản lý người dùng
- `RolesController` - Quản lý vai trò  
- `ProductsController` - Quản lý sản phẩm
- Tất cả controllers kế thừa từ `BaseController`

## Test
1. **Login** - Tạo session cookie
2. **Truy cập protected routes** - Không còn lỗi Bearer
3. **Logout** - Xóa session cookie
4. **Truy cập protected routes** - Redirect về login

## Lưu ý
- Không cần thay đổi gì ở frontend
- Session cookie tự động được gửi với mỗi request
- `[Authorize]` attribute hoạt động với Cookie scheme
- Claims được lưu trong session cookie
