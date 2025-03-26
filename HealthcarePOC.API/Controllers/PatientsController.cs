using HealthcarePOC.API.Constants;
using HealthcarePOC.API.Data;
using HealthcarePOC.API.Dtos;
using HealthcarePOC.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HealthcarePOC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PatientsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        // GET: api/Patients
        [HttpGet]
        [Authorize(Roles = UserRoles.Doctor)]
        public async Task<IActionResult> GetPatients()
        {
            try
            {
                var patients = await _context.Patients.ToListAsync();
                var jsonPayload = JsonSerializer.Serialize(patients);

                // Hybrid encryption using raw AES key (not encrypted with RSA)
                var (encryptedData, rawAesKey, iv) = EncryptionService.EncryptHybridWithRawKey(jsonPayload);

                return Ok(new
                {
                    EncryptedData = encryptedData,
                    AESKey = rawAesKey,
                    IV = iv
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Error retrieving patients", details = ex.Message });
            }
        }


        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.Patient)]
        public async Task<ActionResult<PatientDto>> GetPatient([FromQuery] string encryptedData, [FromQuery] string encryptedKey, [FromQuery] string iv)
        {
            try
            {
                byte[] aesKey = Convert.FromBase64String(encryptedKey);  // Convert to byte array
                byte[] ivBytes = Convert.FromBase64String(iv);  // Convert to byte array

                // Step 2: Decrypt the patientId using the AES key and IV
                var decryptedPatientId = EncryptionService.DecryptAES(encryptedData, aesKey, ivBytes);


                // Deserialize decrypted patientId
                var patientId = JsonSerializer.Deserialize<Dictionary<string, int>>(decryptedPatientId)?["id"];

                if (!patientId.HasValue)
                {
                    return BadRequest("Invalid patientId.");
                }

                var patient = await _context.Patients.FindAsync(patientId.Value);

                if (patient == null)
                {
                    return NotFound();
                }

                // Map Patient entity to PatientDto
                var patientDto = new PatientDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    DateOfBirth = patient.DateOfBirth,
                    Email = patient.Email
                };

                // Step 2: Encrypt response data (patient details) before sending back
                var responseData = JsonSerializer.Serialize(patientDto);
                var (encryptedResponseData, encryptedResponseKey, responseIV) = EncryptionService.EncryptHybridWithRawKey(responseData);

                // Return the encrypted response
                return Ok(new
                {
                    EncryptedData = encryptedResponseData,
                    AESKey = encryptedResponseKey,
                    IV = responseIV
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Error processing patient details", details = ex.Message });
            }
        }




        // PUT: api/Patients/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPatient(int id, Patient patientUpdate)
        {
            if (id != patientUpdate.Id)
            {
                return BadRequest();
            }

            var existingPatient = await _context.Patients.FindAsync(id);
            if (existingPatient == null)
            {
                return NotFound();
            }

            // Overposting protection: explicitly set fields you allow updates for
            existingPatient.FirstName = patientUpdate.FirstName;
            existingPatient.LastName = patientUpdate.LastName;
            existingPatient.Email = patientUpdate.Email;

            // Secure mode: never allow direct password or sensitive data updates through PUT directly
            var securityMode = _configuration["SecuritySettings:Mode"];
            if (securityMode != "Secure")
            {
                existingPatient.MedicalRecord = patientUpdate.MedicalRecord; // Dangerous in insecure mode
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // POST: api/Patients
        [HttpPost]
        public async Task<ActionResult<Patient>> PostPatient(Patient patient)
        {
            var securityMode = _configuration["SecuritySettings:Mode"];

            if (securityMode == "Secure")
            {
                patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(patient.Password);
                patient.Password = null; // remove plaintext password
            }
            // Else: Insecure mode, stores plaintext password directly (for demo)

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Map Patient to PatientDto clearly
            var patientDto = new PatientDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Email = patient.Email
            };
            if (securityMode != "Secure")
            {
                patientDto.MedicalRecord = patient.MedicalRecord;
            }
            return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patientDto);

        }


        // DELETE: api/Patients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}
