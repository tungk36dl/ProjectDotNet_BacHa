using System.Text.Json;

namespace BacHa.Helper
{
    public static class SessionExtension
    {
      
            // Lưu object/list
            public static void SetObjectAsJson(this ISession session, string key, object value)
            {
                session.SetString(key, JsonSerializer.Serialize(value));
            }

            // Lấy object/list
            public static T? GetObjectFromJson<T>(this ISession session, string key)
            {
                var value = session.GetString(key);
                return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
            }
        
    }
}
