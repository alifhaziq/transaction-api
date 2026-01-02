using TransactionApi.Models;

namespace TransactionApi.Services
{
    public interface ITransactionValidationService
    {
        (bool isValid, string errorMessage) ValidateTransaction(TransactionRequest request);
    }
}

