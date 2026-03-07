using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HyperCar.BLL.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            await using var stream = file.OpenReadStream();

            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto"),
                Overwrite = true,
                UniqueFilename = true
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}
