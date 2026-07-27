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
        private static readonly HashSet<string> AllowedExt =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public ImageUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<(string fileName, string contentType)> SaveProductImageAsync(IFormFile file, string prefix = "product")
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Empty image file.");

            var fileName = $"{prefix}_{Guid.NewGuid():N}.jpg";
            var dir = GetUploadDirectory();
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName);

            var ext = Path.GetExtension(file.FileName);
            if (!string.IsNullOrEmpty(ext) && !AllowedExt.Contains(ext))
                throw new InvalidOperationException($"Unsupported image type: {ext}");

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
                // Fallback: store original bytes if ImageSharp cannot process the file.
                await using var input = file.OpenReadStream();
                await using var output = File.Create(fullPath);
                await input.CopyToAsync(output);
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
