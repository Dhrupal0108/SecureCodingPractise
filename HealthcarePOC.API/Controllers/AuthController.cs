using HealthcarePOC.API.Constants;
using HealthcarePOC.API.Data;
using HealthcarePOC.API.DTOs;
using HealthcarePOC.API.Helpers;
using HealthcarePOC.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HealthcarePOC.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _secretKey = Helper.GetSecretAsync("SCP").Result;
    }

    [HttpGet("get-public-key")]
    public IActionResult GetPublicKey()
    {
        try
        {
            return Ok(new { publicKey = EncryptionService.GetPublicKeyPem() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve public key", details = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (_context.Users.Any(u => u.Email == user.Email))
            return BadRequest("User already exists.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.Role = string.IsNullOrEmpty(user.Role) ? UserRoles.Patient : user.Role; // Default to Patient if not provided
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] EncryptedPayloadDto encryptedDto)
    {
        try
        {
            var encryptedData = encryptedDto.EncryptedData;
            var encryptedKey = encryptedDto.EncryptedKey;
            var iv = encryptedDto.IV;

            // ✅ Step 1: Decrypt the AES key using RSA
            var aesKeyBytes = Convert.FromBase64String(EncryptionService.DecryptAESKey(encryptedKey));
            var ivBytes = Convert.FromBase64String(iv);

            // ✅ Step 2: Validate AES Key & IV sizes
            if (aesKeyBytes.Length != 32)
                throw new Exception($"Invalid AES key length: Expected 32 bytes, got {aesKeyBytes.Length}");

            if (ivBytes.Length != 16)
                throw new Exception($"Invalid IV length: Expected 16 bytes, got {ivBytes.Length}");

            // ✅ Step 3: Decrypt payload using AES
            var decryptedData = EncryptionService.DecryptAES(encryptedData, aesKeyBytes, ivBytes);

            // ✅ Step 4: Deserialize decrypted payload
            var loginDto = JsonSerializer.Deserialize<LoginDto>(decryptedData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest("Invalid login data format.");

            // ✅ Step 5: Authenticate user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            // ✅ Step 6: Generate JWT token
            var token = GenerateJwtToken(user);

            // ✅ Step 7: Encrypt the token using hybrid encryption (AES + RSA)
            var publicKey = EncryptionService.GetPublicKeyPem();
            var (encryptedResponseData, rawAesKey, responseIV) = EncryptionService.EncryptHybridWithRawKey(token);

            return Ok(new
            {
                EncryptedData = encryptedResponseData,
                AESKey = rawAesKey,
                IV = responseIV
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Error processing login request", details = ex.Message });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString()) // Store user ID in token for role-based UI
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
