using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using HealthcarePOC.API.Data;
using HealthcarePOC.API.Models;
using System.Text;

namespace HealthcarePOC.API.Helpers
{
    public static class Helper
    {
        public static async Task<string> GetSecretAsync(string secretName)
        {
            var region = Amazon.RegionEndpoint.USEast1;
            var client = new AmazonSecretsManagerClient(region);  // Use AWS SDK to retrieve the secret
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            try
            {
                var response = await client.GetSecretValueAsync(request);

                if (response.SecretString != null)
                {
                    return response.SecretString;  // Return the secret string if available
                }
                else if (response.SecretBinary != null)
                {
                    // If the secret is in binary format, convert it
                    using (var memoryStream = new MemoryStream())
                    {
                        response.SecretBinary.CopyTo(memoryStream);
                        byte[] data = memoryStream.ToArray();  // Convert stream to byte array
                        return Encoding.UTF8.GetString(data);  // Convert byte array to string
                    }
                }
                else
                {
                    throw new Exception("Secret not found or is empty.");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error retrieving secret: {e.Message}");
            }
        }
        public static void SeedDatabase(AppDbContext context)
        {
            if (!context.Users.Any()) // Check if users already exist
            {
                string adminPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                string doctorPassword = BCrypt.Net.BCrypt.HashPassword("Doctor123!");
                string patientPassword = BCrypt.Net.BCrypt.HashPassword("Patient123!");

                var users = new List<User>
                {
                    new User { UserName = "admin", Email = "admin@healthcare.com", PasswordHash = adminPassword, Role = "Admin" },
                    new User { UserName = "doctor1", Email = "doctor1@healthcare.com", PasswordHash = doctorPassword, Role = "Doctor" },
                    new User { UserName = "patient1", Email = "patient1@healthcare.com", PasswordHash = patientPassword, Role = "Patient" },
                    new User { UserName = "patient2", Email = "patient2@healthcare.com", PasswordHash = patientPassword, Role = "Patient" }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            if (!context.Patients.Any()) // Check if patients already exist
            {
                var patients = new List<Patient>
                {
                    new Patient { FirstName = "John", LastName = "Doe", Email = "patient1@healthcare.com", DateOfBirth = new DateTime(1990, 5, 14), MedicalRecord = "Record1" },
                    new Patient { FirstName = "Jane", LastName = "Smith", Email = "patient2@healthcare.com", DateOfBirth = new DateTime(1985, 3, 22), MedicalRecord = "Record2" }
                };

                context.Patients.AddRange(patients);
                context.SaveChanges();
            }
        }
    }
}
