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

        public ImageUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<(string fileName, string contentType)> SaveProductImageAsync(IFormFile file, string prefix = "product")
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                ext = ".jpg";

            var fileName = $"{prefix}_{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "files");
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName);

            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input);

            if (image.Width > MaxWidth)
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(MaxWidth, 0), Mode = ResizeMode.Max }));

            var encoder = new JpegEncoder { Quality = JpegQuality };
            await image.SaveAsJpegAsync(fullPath, encoder);

            return (fileName, "image/jpeg");
        }
    }
}
