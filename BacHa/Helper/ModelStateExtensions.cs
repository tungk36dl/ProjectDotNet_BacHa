using BacHa.Models.Service;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BacHa.Helper
{
    public static class ModelStateExtensions
    {
        public static void AddDataResponse(this ModelStateDictionary modelState, DataResponse<object> result)
        {
            if (!string.IsNullOrEmpty(result.Message))
                modelState.AddModelError(string.Empty, result.Message);

            // ErrorDetails may contain serialized field errors: { "FieldName": ["err1","err2"] }
            if (!string.IsNullOrEmpty(result.ErrorDetails))
            {
                try
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(result.ErrorDetails!);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            foreach (var err in kv.Value)
                            {
                                modelState.AddModelError(kv.Key, err);
                            }
                        }
                        return;
                    }
                }
                catch
                {
                    // ignore parse errors and fall back to adding ErrorDetails as general error
                }

                modelState.AddModelError(string.Empty, result.ErrorDetails);
            }
        }
    }

}
