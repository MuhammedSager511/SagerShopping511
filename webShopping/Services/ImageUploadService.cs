using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace webShopping.Services
{
    public interface IImageUploadService
    {
        Task<(string fileName, string contentType)> SaveProductImageAsync(IFormFile file, string prefix = "product");
    }

    public class ImageUploadService : IImageUploadService
    {
        private readonly IWebHostEnvironment _env;
        private const int MaxWidth = 1200;
        private const int JpegQuality = 82;
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly HashSet<string> AllowedExt =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public ImageUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<(string fileName, string contentType)> SaveProductImageAsync(IFormFile file, string prefix = "product")
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Empty image file.");

            if (file.Length > MaxBytes)
                throw new InvalidOperationException("Image is too large. Maximum size is 5 MB.");

            var ext = Path.GetExtension(file.FileName);
            if (!string.IsNullOrEmpty(ext) && !AllowedExt.Contains(ext))
                throw new InvalidOperationException($"Unsupported image type: {ext}");

            var fileName = $"{prefix}_{Guid.NewGuid():N}.jpg";
            var dir = GetUploadDirectory();
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName);

            try
            {
                await using var input = file.OpenReadStream();
                using var image = await Image.LoadAsync(input);

                if (image.Width > MaxWidth)
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxWidth, 0),
                        Mode = ResizeMode.Max
                    }));

                await image.SaveAsJpegAsync(fullPath, new JpegEncoder { Quality = JpegQuality });
            }
            catch (UnknownImageFormatException)
            {
                throw new InvalidOperationException("Invalid or corrupt image file. Use JPG, PNG, WEBP, or GIF.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException("Could not process image. Use a valid JPG, PNG, WEBP, or GIF.", ex);
            }

            return (fileName, "image/jpeg");
        }

        private string GetUploadDirectory()
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, "files");
        }
    }
}
