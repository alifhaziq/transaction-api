using Microsoft.AspNetCore.Mvc;
using TransactionApi.Models;
using TransactionApi.Services;

namespace TransactionApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class TransactionController : ControllerBase
    {
        private readonly IPartnerAuthenticationService _partnerAuthService;
        private readonly ISignatureValidationService _signatureValidationService;
        private readonly ITransactionValidationService _transactionValidationService;
        private readonly IDiscountCalculationService _discountCalculationService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            IPartnerAuthenticationService partnerAuthService,
            ISignatureValidationService signatureValidationService,
            ITransactionValidationService transactionValidationService,
            IDiscountCalculationService discountCalculationService,
            ILogger<TransactionController> logger)
        {
            _partnerAuthService = partnerAuthService;
            _signatureValidationService = signatureValidationService;
            _transactionValidationService = transactionValidationService;
            _discountCalculationService = discountCalculationService;
            _logger = logger;
        }

        [HttpPost("submittrxmessage")]
        public ActionResult<TransactionResponse> SubmitTransaction([FromBody] TransactionRequest request)
        {
            try
            {
                var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
                
                _logger.LogInformation("[RequestId: {RequestId}] Received transaction request from partner: {PartnerKey}", 
                    requestId, request?.partnerkey);

                // Check for null request
                if (request == null)
                {
                    return Ok(new TransactionResponse
                    {
                        result = 0,
                        resultmessage = "Request is Required."
                    });
                }

                // Step 1: Validate basic structure and business rules
                _logger.LogInformation("[RequestId: {RequestId}] Starting validation for partner: {PartnerKey}, RefNo: {RefNo}, Amount: {Amount} cents", 
                    requestId, request.partnerkey, request.partnerrefno, request.totalamount);
                
                var validationResult = _transactionValidationService.ValidateTransaction(request);
                if (!validationResult.isValid)
                {
                    _logger.LogWarning("[RequestId: {RequestId}] Validation failed: {ErrorMessage}", 
                        requestId, validationResult.errorMessage);
                    return Ok(new TransactionResponse
                    {
                        result = 0,
                        resultmessage = validationResult.errorMessage
                    });
                }

                // Step 2: Validate partner authentication
                if (!_partnerAuthService.ValidatePartner(request.partnerkey, request.partnerrefno, request.partnerpassword))
                {
                    _logger.LogWarning("[RequestId: {RequestId}] Partner authentication failed for: {PartnerKey} - {PartnerRefNo}", 
                        requestId, request.partnerkey, request.partnerrefno);
                    return Ok(new TransactionResponse
                    {
                        result = 0,
                        resultmessage = "Access Denied!"
                    });
                }

                // Step 3: Validate signature
                if (!_signatureValidationService.ValidateSignature(request))
                {
                    _logger.LogWarning("[RequestId: {RequestId}] Signature validation failed for partner: {PartnerKey}", 
                        requestId, request.partnerkey);
                    return Ok(new TransactionResponse
                    {
                        result = 0,
                        resultmessage = "Invalid signature!"
                    });
                }

                // All validations passed - process transaction and calculate discount
                _logger.LogInformation("[RequestId: {RequestId}] Transaction validated successfully for partner: {PartnerKey}", 
                    requestId, request.partnerkey);

                // Calculate discount based on business rules
                var (discountPercentage, discountAmount, finalAmount) = _discountCalculationService.CalculateDiscount(request.totalamount);

                _logger.LogInformation("[RequestId: {RequestId}] Discount calculated for amount {Amount} cents: {DiscountPercentage}% = {DiscountAmount} cents, Final: {FinalAmount} cents", 
                    requestId, request.totalamount, discountPercentage, discountAmount, finalAmount);

                // Log item details if present
                if (request.items != null && request.items.Any())
                {
                    _logger.LogInformation("[RequestId: {RequestId}] Transaction contains {ItemCount} items", 
                        requestId, request.items.Count);
                }

                var response = new TransactionResponse
                {
                    result = 1,
                    totalamount = request.totalamount,
                    totaldiscount = discountAmount,
                    finalamount = finalAmount
                };

                _logger.LogInformation("[RequestId: {RequestId}] Transaction processed successfully. Result: Success", requestId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                var requestId = HttpContext.Items["RequestId"]?.ToString() ?? "UNKNOWN";
                _logger.LogError(ex, "[RequestId: {RequestId}] Error processing transaction request", requestId);
                return Ok(new TransactionResponse
                {
                    result = 0,
                    resultmessage = "Internal server error occurred"
                });
            }
        }
    }
}

