using TransactionApi.Models;

namespace TransactionApi.Services
{
    public interface ISignatureValidationService
    {
        bool ValidateSignature(TransactionRequest request);
        string GenerateSignature(string timestamp, string partnerKey, string partnerRefNo, long totalAmount, string encodedPassword);
    }
}

