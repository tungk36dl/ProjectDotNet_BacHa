using System.Collections.Generic;

namespace BacHa.Models.Service
{
    public class OperationResult
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public Dictionary<string, List<string>> FieldErrors { get; set; } = new Dictionary<string, List<string>>();

        public void AddError(string field, string error)
        {
            Success = false;
            if (!FieldErrors.ContainsKey(field)) FieldErrors[field] = new List<string>();
            FieldErrors[field].Add(error);
        }
    }
}
