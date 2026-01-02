using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TransactionApi.Models;

namespace TransactionApi.Services
{
    public class SignatureValidationService : ISignatureValidationService
    {
        public bool ValidateSignature(TransactionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.sig))
                return false;

            try
            {
                // Parse timestamp to extract yyyyMMddHHmmss format
                DateTime dt;
                if (!DateTime.TryParse(request.timestamp, null, DateTimeStyles.RoundtripKind, out dt))
                    return false;

                string timestampForSig = dt.ToString("yyyyMMddHHmmss");

                // Generate signature
                string generatedSig = GenerateSignature(
                    timestampForSig,
                    request.partnerkey,
                    request.partnerrefno,
                    request.totalamount,
                    request.partnerpassword
                );

                // Compare signatures (trim whitespace from both)
                return string.Equals(request.sig.Trim(), generatedSig, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateSignature(string timestamp, string partnerKey, string partnerRefNo, long totalAmount, string encodedPassword)
        {
            // Concatenate parameters in order: timestamp + partnerkey + partnerrefno + totalamount + partnerpassword(encoded)
            string signatureString = $"{timestamp}{partnerKey}{partnerRefNo}{totalAmount}{encodedPassword}";

            // Apply SHA-256 hashing (UTF-8 encoded input, lowercase hexadecimal hash output)
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(signatureString);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                
                // Convert to lowercase hexadecimal string
                StringBuilder hexString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hexString.Append(b.ToString("x2"));
                }
                
                string hexHash = hexString.ToString();

                // Convert the SHA-256 hash to Base64 (UTF-8 encoding)
                byte[] hexHashBytes = Encoding.UTF8.GetBytes(hexHash);
                string base64Signature = Convert.ToBase64String(hexHashBytes);

                return base64Signature;
            }
        }
    }
}

