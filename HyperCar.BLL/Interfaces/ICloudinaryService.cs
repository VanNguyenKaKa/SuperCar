using Microsoft.AspNetCore.Http;

namespace HyperCar.BLL.Interfaces
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Upload an image file to Cloudinary
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="folder">Cloudinary folder (e.g. "hypercar/cars", "hypercar/brands")</param>
        /// <returns>The secure URL of the uploaded image</returns>
        Task<string> UploadImageAsync(IFormFile file, string folder);

        /// <summary>
        /// Delete an image from Cloudinary by its public ID
        /// </summary>
        /// <param name="publicId">The public ID of the image</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteImageAsync(string publicId);
    }
}
