namespace HealthcarePOC.API.DTOs
{
    public class EncryptedPayloadDto
    {
        public string EncryptedData { get; set; }
        public string EncryptedKey { get; set; }
        public string IV { get; set; }
    }
}
