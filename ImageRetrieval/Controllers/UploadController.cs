using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ImageRetrieval.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageRetrieval.Controllers
{
    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger _logger;

        public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
        {
            _env = env;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> PostFile([FromForm] IFormFile imageFile)
        {
            if (imageFile.Length == 0)
                return BadRequest();

            try
            {
                await using MemoryStream ms = new();
                await imageFile.CopyToAsync(ms);

                var image = System.Drawing.Image.FromStream(ms);
                var ext = image.RawFormat.GetFilenameExtension();

                using var sha1 = SHA1.Create();
                ms.Seek(0, SeekOrigin.Begin);
                
                var fileHash = await sha1.ComputeHashAsync(ms);
                var fileName = string.Concat(fileHash.Select(b => b.ToString("X2"))) + ext;

                var path = Path.Combine(_env.WebRootPath, Common.UploadedFolderName, fileName);
                await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

                var resourcePath = new Uri($"{Request.Scheme}://{Request.Host}/{Common.UploadedFolderName}/{fileName}");
                return Created(resourcePath, resourcePath.AbsolutePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.StackTrace);
                return BadRequest();
            }
        }
    }
}