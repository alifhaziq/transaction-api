using System.Globalization;
using TransactionApi.Models;

namespace TransactionApi.Services
{
    public class TransactionValidationService : ITransactionValidationService
    {
        public (bool isValid, string errorMessage) ValidateTransaction(TransactionRequest request)
        {
            // Validate mandatory fields with specific error format
            if (request == null)
                return (false, "Request is Required.");

            if (string.IsNullOrWhiteSpace(request.partnerkey))
                return (false, "partnerkey is Required.");

            if (string.IsNullOrWhiteSpace(request.partnerrefno))
                return (false, "partnerrefno is Required.");

            if (string.IsNullOrWhiteSpace(request.partnerpassword))
                return (false, "partnerpassword is Required.");

            if (string.IsNullOrWhiteSpace(request.timestamp))
                return (false, "timestamp is Required.");

            if (string.IsNullOrWhiteSpace(request.sig))
                return (false, "sig is Required.");

            // Validate totalamount - only allow positive value
            if (request.totalamount <= 0)
                return (false, "totalamount must be a positive value.");

            // Validate timestamp format and expiry (±5 minutes)
            DateTime requestTime;
            if (!DateTime.TryParse(request.timestamp, null, DateTimeStyles.RoundtripKind, out requestTime))
                return (false, "timestamp must be in valid ISO 8601 format.");

            // Convert to UTC for comparison
            if (requestTime.Kind != DateTimeKind.Utc)
            {
                requestTime = requestTime.ToUniversalTime();
            }

            DateTime serverTime = DateTime.UtcNow;
            TimeSpan timeDifference = serverTime - requestTime;
            
            // Check if timestamp is within ±5 minutes
            if (Math.Abs(timeDifference.TotalMinutes) > 5)
                return (false, "Expired.");

            // Validate items if provided
            if (request.items != null && request.items.Count > 0)
            {
                foreach (var item in request.items)
                {
                    var itemValidation = ValidateItem(item);
                    if (!itemValidation.isValid)
                        return itemValidation;
                }

                // Validate total amount matches sum of items (only when items are provided)
                long calculatedTotal = request.items.Sum(item => item.qty * item.unitprice);
                if (calculatedTotal != request.totalamount)
                    return (false, "Invalid Total Amount.");
            }

            return (true, string.Empty);
        }

        private (bool isValid, string errorMessage) ValidateItem(ItemDetail item)
        {
            if (item == null)
                return (false, "Item is Required.");

            // partneritemref cannot be null or empty
            if (string.IsNullOrWhiteSpace(item.partneritemref))
                return (false, "partneritemref is Required.");

            // name cannot be null or empty
            if (string.IsNullOrWhiteSpace(item.name))
                return (false, "name is Required.");

            // qty must be positive and not exceed 5
            if (item.qty <= 0)
                return (false, "qty must be a positive value.");

            if (item.qty > 5)
                return (false, "qty must not exceed 5.");

            // unitprice must be positive
            if (item.unitprice <= 0)
                return (false, "unitprice must be a positive value.");

            // Check for string length limits
            if (item.partneritemref.Length > 50)
                return (false, "partneritemref exceeds maximum length of 50 characters.");

            if (item.name.Length > 100)
                return (false, "name exceeds maximum length of 100 characters.");

            return (true, string.Empty);
        }
    }
}