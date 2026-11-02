using System;
using System.IO;
using System.Threading.Tasks;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utilities;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Apartmani.Pages.Images
{
    [Authorize(Roles = SD.AdminEndUser)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Apartmani.Models.Image Image { get; set; }

        public IActionResult OnGet()
        {
            LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var files = Request.Form.Files;
            if (Image == null || !await _context.ImageCategory.AnyAsync(c => c.Id == Image.CategoryId))
                ModelState.AddModelError(string.Empty, "Choose a valid category.");
            if (files.Count != 1 || files[0].Length == 0 || files[0].Length > ImageStorage.MaxUploadBytes)
                ModelState.AddModelError(string.Empty, "Choose one JPEG or PNG image up to 10 MB.");
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            byte[] data;
            using (var buffer = new MemoryStream())
            {
                await files[0].CopyToAsync(buffer);
                data = buffer.ToArray();
            }
            var extension = ImageStorage.GetExtension(data);
            if (extension == null)
            {
                ModelState.AddModelError(string.Empty, "The file must be a valid JPEG or PNG image.");
                LoadCategories();
                return Page();
            }

            try
            {
                using var stream = new SKMemoryStream(data);
                using var codec = SKCodec.Create(stream);
                if (codec == null || (extension == ".jpg" && codec.EncodedFormat != SKEncodedImageFormat.Jpeg) ||
                    (extension == ".png" && codec.EncodedFormat != SKEncodedImageFormat.Png))
                    throw new InvalidDataException("Unsupported image format.");

                int width = codec.Info.Width;
                int height = codec.Info.Height;
                if (width < 1 || height < 1 || width > 10000 || height > 10000 || (long)width * height > 24000000)
                    throw new InvalidDataException("Image dimensions are too large.");

                using var decoded = new SKBitmap(codec.Info);
                if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
                    throw new InvalidDataException("The image is incomplete or damaged.");

                var origin = codec.EncodedOrigin;
                bool swapped = origin == SKEncodedOrigin.LeftTop || origin == SKEncodedOrigin.RightTop ||
                    origin == SKEncodedOrigin.RightBottom || origin == SKEncodedOrigin.LeftBottom;
                using var oriented = new SKBitmap(swapped ? height : width, swapped ? width : height);
                using var canvas = new SKCanvas(oriented);
                var matrix = SKMatrix.CreateIdentity();
                switch (origin)
                {
                    case SKEncodedOrigin.TopRight: matrix = new SKMatrix { ScaleX = -1, ScaleY = 1, TransX = width, Persp2 = 1 }; break;
                    case SKEncodedOrigin.BottomRight: matrix = new SKMatrix { ScaleX = -1, ScaleY = -1, TransX = width, TransY = height, Persp2 = 1 }; break;
                    case SKEncodedOrigin.BottomLeft: matrix = new SKMatrix { ScaleX = 1, ScaleY = -1, TransY = height, Persp2 = 1 }; break;
                    case SKEncodedOrigin.LeftTop: matrix = new SKMatrix { SkewX = 1, SkewY = 1, Persp2 = 1 }; break;
                    case SKEncodedOrigin.RightTop: matrix = new SKMatrix { SkewX = -1, SkewY = 1, TransX = height, Persp2 = 1 }; break;
                    case SKEncodedOrigin.RightBottom: matrix = new SKMatrix { SkewX = -1, SkewY = -1, TransX = height, TransY = width, Persp2 = 1 }; break;
                    case SKEncodedOrigin.LeftBottom: matrix = new SKMatrix { SkewX = 1, SkewY = -1, TransY = width, Persp2 = 1 }; break;
                }
                canvas.SetMatrix(matrix);
                canvas.DrawBitmap(decoded, 0, 0, SKSamplingOptions.Default, null);
                canvas.Flush();
                using var image = SKImage.FromBitmap(oriented);
                using var cleanImage = image.Encode(extension == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg, 90);
                if (cleanImage == null) throw new InvalidDataException("Image encoding failed.");
                data = cleanImage.ToArray();
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is ArgumentException || ex is NotSupportedException)
            {
                ModelState.AddModelError(string.Empty, "The image could not be decoded. Choose a valid JPEG or PNG file under 24 megapixels.");
                LoadCategories();
                return Page();
            }

            var relativePath = $"img/uploads/{Guid.NewGuid():N}{extension}";
            var destination = ImageStorage.ResolveImagePath(_environment.WebRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            try
            {
                await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await output.WriteAsync(data);
                }
                _context.Image.Add(new Apartmani.Models.Image { CategoryId = Image.CategoryId, Path = relativePath });
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination);
                throw;
            }
            return RedirectToPage("./Index");
        }

        private void LoadCategories() => ViewData["CategoryId"] = new SelectList(_context.ImageCategory, "Id", "Name");
    }
}
