using HealthcarePOC.API.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class FilesController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

    [HttpPost("upload")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Doctor)] // Only Admins & Doctors can upload
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty.");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Invalid file type. Allowed: .jpg, .jpeg, .png, .pdf");

        var filePath = Path.Combine("Uploads", file.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok("File uploaded successfully.");
    }
}
