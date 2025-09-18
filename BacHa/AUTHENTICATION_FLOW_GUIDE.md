# Hướng dẫn Luồng Authentication

## Tổng quan
Hệ thống đã được cập nhật để sử dụng luồng authentication an toàn hơn với Access Token và Refresh Token.

## Luồng Authentication

### 1. Login
- **Endpoint**: `POST /Auth/Login` hoặc `POST /api/AuthApi/login`
- **Request**: Username/Email + Password
- **Response**: 
  ```json
  {
    "success": true,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
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
  - Tạo Access Token (JWT) với thời hạn ngắn (60 phút)
  - Tạo Refresh Token ngẫu nhiên
  - Lưu Refresh Token vào database
  - Set Refresh Token làm HttpOnly Cookie (7 ngày)
  - Trả về Access Token trong JSON response

### 2. API Calls
- **Header**: `Authorization: Bearer <AccessToken>`
- **Storage**: Access Token được lưu trong `sessionStorage` (chỉ trong session hiện tại)
- **Auto-include**: Tất cả API calls tự động gửi kèm Bearer token

### 3. Token Refresh
- **Endpoint**: `POST /api/AuthApi/refresh-token`
- **Trigger**: Khi Access Token hết hạn (401 response)
- **Process**:
  - Browser tự động gửi Refresh Token cookie
  - Server validate Refresh Token trong database
  - Tạo Access Token mới
  - Cập nhật Refresh Token mới trong database và cookie
  - Trả về Access Token mới

### 4. Logout
- **Endpoint**: `POST /api/AuthApi/logout`
- **Process**:
  - Xóa Refresh Token khỏi database
  - Xóa Refresh Token cookie
  - Xóa Access Token khỏi sessionStorage
  - Redirect về trang login

## Bảo mật

### Access Token
- **Storage**: `sessionStorage` (tự động xóa khi đóng tab)
- **Lifetime**: 60 phút (có thể cấu hình)
- **Usage**: Gửi trong header `Authorization: Bearer <token>`

### Refresh Token
- **Storage**: HttpOnly Cookie (không thể truy cập từ JavaScript)
- **Lifetime**: 7 ngày
- **Security**: 
  - `HttpOnly`: Không thể truy cập từ JavaScript
  - `SameSite=Strict`: Chỉ gửi trong same-site requests
  - `Secure`: Nên set `true` trong production với HTTPS

## Frontend Integration

### JavaScript API
```javascript
// Kiểm tra authentication status
window.AuthManager.isAuthenticated()

// Refresh token manually
await window.AuthManager.refreshToken()

// Logout
await window.AuthManager.logout()
```

### Auto Token Management
- Tự động gửi Bearer token trong tất cả requests
- Tự động refresh token khi nhận 401
- Tự động redirect về login khi refresh thất bại

## Cấu hình

### JWT Settings (appsettings.json)
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "BacHa",
    "Audience": "BacHa-Users",
    "ExpiryMinutes": "60"
  }
}
```

### Cookie Settings
- **HttpOnly**: `true` (bảo mật)
- **Secure**: `false` (development), `true` (production)
- **SameSite**: `Strict`
- **Expires**: 7 ngày

## Testing

### Test Login Flow
1. Mở Developer Tools → Application → Cookies
2. Login với user hợp lệ
3. Kiểm tra:
   - Access Token trong sessionStorage
   - Refresh Token cookie (HttpOnly)
   - Navigation menu cập nhật

### Test Token Refresh
1. Đợi Access Token hết hạn (hoặc thay đổi thời hạn trong config)
2. Thực hiện API call
3. Kiểm tra:
   - Tự động refresh token
   - API call thành công với token mới

### Test Logout
1. Click logout button
2. Kiểm tra:
   - Refresh Token cookie bị xóa
   - Access Token bị xóa khỏi sessionStorage
   - Redirect về trang login

## Troubleshooting

### Common Issues
1. **Token không được gửi**: Kiểm tra `credentials: 'include'` trong fetch requests
2. **Refresh token không hoạt động**: Kiểm tra cookie có được set đúng không
3. **Logout không hoàn toàn**: Kiểm tra cả client và server đều clear token

### Debug Tips
- Sử dụng Developer Tools để kiểm tra cookies và sessionStorage
- Kiểm tra Network tab để xem Authorization headers
- Xem Console logs để debug token refresh process
