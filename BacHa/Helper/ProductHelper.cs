using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BacHa.Helper
{
    public static class ProductHelper
    {
        // Format tiền tệ VNĐ
        public static IHtmlContent FormatVNDForProduct(this IHtmlHelper htmlHelper, decimal? amount)
        {
            if (amount == null)
                return new HtmlString("<span>-</span>");

            var formattedAmount = amount.Value.ToString("N0");
            return new HtmlString($"<span>{formattedAmount} ₫</span>");
        }

        // Hiển thị hình ảnh sản phẩm
        public static IHtmlContent ProductImage(this IHtmlHelper htmlHelper, string? imageUrl, string? altText = null, int? width = null, int? height = null)
        {
            // Nếu imageUrl null hoặc rỗng, sử dụng placeholder
            var finalImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "/images/placeholder.svg" : imageUrl;
            var finalAltText = string.IsNullOrWhiteSpace(altText) ? "Product Image" : altText;

            // Tạo style cho width và height
            var style = "";
            if (width.HasValue || height.HasValue)
            {
                var widthStyle = width.HasValue ? $"width: {width}px;" : "";
                var heightStyle = height.HasValue ? $"height: {height}px;" : "";
                var objectFit = width.HasValue && height.HasValue ? "object-fit: cover;" : "";
                style = $" style=\"{widthStyle}{heightStyle}{objectFit}\"";
            }

            var html = $"<img src=\"{finalImageUrl}\" alt=\"{finalAltText}\" class=\"img-thumbnail\"{style}>";
            return new HtmlString(html);
        }

        // Hiển thị hình ảnh responsive
        public static IHtmlContent ProductImageResponsive(this IHtmlHelper htmlHelper, string? imageUrl, string? altText = null)
        {
            var finalImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "/images/placeholder.svg" : imageUrl;
            var finalAltText = string.IsNullOrWhiteSpace(altText) ? "Product Image" : altText;

            var html = $"<img src=\"{finalImageUrl}\" alt=\"{finalAltText}\" class=\"img-fluid rounded\">";
            return new HtmlString(html);
        }
    }
}
