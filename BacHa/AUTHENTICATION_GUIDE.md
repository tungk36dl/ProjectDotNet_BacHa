# 🔐 Authentication & Authorization Guide

## Tổng quan

Hệ thống đã được cập nhật với các tính năng authentication và authorization hoàn chỉnh:

### ✨ Tính năng chính

1. **BaseController với Authorization** - Tất cả controller kế thừa BaseController sẽ yêu cầu đăng nhập
2. **RefreshToken** - Tự động gia hạn token để duy trì đăng nhập
3. **Auto Redirect** - Tự động redirect về trang login khi chưa đăng nhập
4. **JWT Authentication** - Sử dụng JWT token cho authentication

## 🏗️ Kiến trúc

```
View ↔ Controller (BaseController) ↔ Service ↔ UnitOfWork ↔ Repository ↔ Database
```

### Các thành phần chính:

- **BaseController**: Controller cơ sở với authorization
- **AuthenticationMiddleware**: Middleware xử lý authentication và auto refresh token
- **JwtService**: Service xử lý JWT token và refresh token
- **AuthController**: Controller xử lý login/logout/register

## 🚀 Cách sử dụng

### 1. Tạo Controller mới (yêu cầu đăng nhập)

```csharp
public class MyController : BaseController
{
    public IActionResult Index()
    {
        // Chỉ user đã đăng nhập mới truy cập được
        ViewBag.CurrentUser = CurrentUser;
        return View();
    }
}
```

### 2. Truy cập thông tin user

```csharp
// Trong controller kế thừa BaseController
var userId = UserId;           // Guid? - ID của user
var userName = CurrentUser?.UserName;  // string? - Tên user
var userRole = UserRole;       // string? - Role của user
var isAuth = IsAuthenticated;  // bool - Có đăng nhập không
```

### 3. Xử lý response từ service

```csharp
public IActionResult Create(MyModel model)
{
    var response = await _myService.CreateAsync(model);
    return HandleServiceResponse(response, "Index");
}
```

## 🔧 Cấu hình

### 1. JWT Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyHereAtLeast32Characters",
    "Issuer": "BacHa",
    "Audience": "BacHaUsers",
    "ExpiryMinutes": "60"
  }
}
```

### 2. Database Migration

Refresh token fields đã được thêm vào User table:
- `RefreshToken` (nvarchar(max))
- `RefreshTokenExpiryTime` (datetime2)

## 📱 Frontend Integration

### 1. Auto Token Refresh

File `wwwroot/js/auth.js` tự động:
- Refresh token mỗi 50 phút
- Kiểm tra token validity mỗi 5 phút
- Redirect về login khi token hết hạn

### 2. AJAX Request Handling

Tự động retry request khi nhận 401 response.

## 🛡️ Security Features

### 1. Token Security
- JWT token có thời hạn 60 phút
- Refresh token có thời hạn 7 ngày
- Tự động xóa refresh token khi logout

### 2. Authorization
- Tất cả controller kế thừa BaseController yêu cầu đăng nhập
- Middleware tự động kiểm tra và refresh token
- Redirect tự động về login khi unauthorized

## 📋 API Endpoints

### Authentication
- `GET /Auth/Login` - Trang đăng nhập
- `POST /Auth/Login` - Xử lý đăng nhập
- `GET /Auth/Register` - Trang đăng ký
- `POST /Auth/Register` - Xử lý đăng ký
- `GET /Auth/Logout` - Đăng xuất
- `POST /Auth/RefreshToken` - Refresh token
- `GET /Auth/ValidateToken` - Validate token

### Protected Areas
- `/Users/*` - Quản lý user (yêu cầu đăng nhập)
- `/Products/*` - Quản lý sản phẩm (yêu cầu đăng nhập)

## 🔄 Luồng hoạt động

### 1. Đăng nhập
1. User nhập thông tin đăng nhập
2. Hệ thống verify password
3. Tạo JWT token và refresh token
4. Lưu refresh token vào database
5. Set cookies và redirect

### 2. Truy cập trang protected
1. Middleware kiểm tra JWT token
2. Nếu token hết hạn, thử refresh token
3. Nếu refresh thành công, tiếp tục
4. Nếu thất bại, redirect về login

### 3. Auto refresh
1. JavaScript tự động refresh token mỗi 50 phút
2. Kiểm tra token validity mỗi 5 phút
3. Tự động logout khi không thể refresh

## 🎯 Best Practices

### 1. Controller Design
- Kế thừa BaseController cho các trang cần đăng nhập
- Sử dụng `HandleServiceResponse()` cho consistency
- Kiểm tra role nếu cần phân quyền chi tiết

### 2. Service Design
- Luôn trả về `DataResponse<T>` để consistency
- Xử lý validation và business logic
- Log errors để debugging

### 3. Frontend
- Sử dụng `AuthManager` API cho authentication
- Handle 401 responses appropriately
- Show loading states khi refresh token

## 🐛 Troubleshooting

### 1. Token không refresh
- Kiểm tra refresh token trong database
- Kiểm tra JWT configuration
- Xem console logs

### 2. Redirect loop
- Kiểm tra middleware order trong Program.cs
- Kiểm tra skip paths trong AuthenticationMiddleware

### 3. 401 errors
- Kiểm tra JWT key configuration
- Kiểm tra token format
- Kiểm tra cookie settings

## 📚 Examples

### Tạo ProductController với authorization

```csharp
public class ProductController : BaseController
{
    private readonly IProductService _productService;
    
    public ProductController(IProductService productService)
    {
        _productService = productService;
    }
    
    public async Task<IActionResult> Index()
    {
        var response = await _productService.GetAllAsync();
        return HandleServiceResponse(response);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        // Chỉ Admin mới được tạo product
        if (UserRole != "Admin")
        {
            TempData["ErrorMessage"] = "Access denied";
            return RedirectToAction("Index");
        }
        
        var response = await _productService.CreateAsync(product);
        return HandleServiceResponse(response, "Index");
    }
}
```

Hệ thống authentication đã sẵn sàng để sử dụng! 🎉
