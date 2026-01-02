using System.Text;
using TransactionApi.Services;

namespace TransactionApi.Middleware
{
    /// <summary>
    /// Middleware to log all HTTP requests and responses to file
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IPasswordEncryptionService _encryptionService;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IPasswordEncryptionService encryptionService)
        {
            _next = next;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate unique request ID for tracking
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;

            // Log incoming request
            await LogRequest(context, requestId);

            // Capture the original response body stream
            var originalResponseBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    // Call the next middleware in the pipeline
                    await _next(context);

                    // Log outgoing response
                    await LogResponse(context, requestId);
                }
                catch (Exception ex)
                {
                    // Log exception
                    _logger.LogError(ex, "[RequestId: {RequestId}] Exception occurred during request processing", requestId);
                    throw;
                }
                finally
                {
                    // Copy response body to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalResponseBodyStream);
                }
            }
        }

        private async Task LogRequest(HttpContext context, string requestId)
        {
            try
            {
                context.Request.EnableBuffering();

                var requestBody = await ReadRequestBody(context.Request);

                // Mask sensitive data before logging
                var maskedBody = _encryptionService.MaskSensitiveData(requestBody);

                var logMessage = new StringBuilder();
                logMessage.AppendLine($"========== INCOMING REQUEST [ID: {requestId}] ==========");
                logMessage.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
                logMessage.AppendLine($"Method: {context.Request.Method}");
                logMessage.AppendLine($"Path: {context.Request.Path}");
                logMessage.AppendLine($"QueryString: {context.Request.QueryString}");
                logMessage.AppendLine($"IP Address: {context.Connection.RemoteIpAddress}");
                logMessage.AppendLine($"User Agent: {context.Request.Headers["User-Agent"]}");
                logMessage.AppendLine($"Content-Type: {context.Request.ContentType}");
                logMessage.AppendLine($"Content-Length: {context.Request.ContentLength}");
                
                // Log headers (excluding sensitive ones)
                logMessage.AppendLine("Headers:");
                foreach (var header in context.Request.Headers)
                {
                    if (!IsSensitiveHeader(header.Key))
                    {
                        logMessage.AppendLine($"  {header.Key}: {header.Value}");
                    }
                    else
                    {
                        logMessage.AppendLine($"  {header.Key}: [MASKED]");
                    }
                }

                // Log request body
                if (!string.IsNullOrWhiteSpace(maskedBody))
                {
                    logMessage.AppendLine("Request Body:");
                    logMessage.AppendLine(maskedBody);
                }

                logMessage.AppendLine("=====================================================");

                _logger.LogInformation(logMessage.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RequestId: {RequestId}] Error logging request", requestId);
            }
        }

        private async Task LogResponse(HttpContext context, string requestId)
        {
            try
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                // Mask sensitive data in response if needed
                var maskedBody = _encryptionService.MaskSensitiveData(responseBody);

                var logMessage = new StringBuilder();
                logMessage.AppendLine($"========== OUTGOING RESPONSE [ID: {requestId}] ==========");
                logMessage.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
                logMessage.AppendLine($"Status Code: {context.Response.StatusCode}");
                logMessage.AppendLine($"Content-Type: {context.Response.ContentType}");
                logMessage.AppendLine($"Content-Length: {context.Response.ContentLength}");

                // Log response headers
                logMessage.AppendLine("Headers:");
                foreach (var header in context.Response.Headers)
                {
                    logMessage.AppendLine($"  {header.Key}: {header.Value}");
                }

                // Log response body
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    logMessage.AppendLine("Response Body:");
                    logMessage.AppendLine(maskedBody);
                }

                logMessage.AppendLine("======================================================");

                _logger.LogInformation(logMessage.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RequestId: {RequestId}] Error logging response", requestId);
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);
                return body;
            }
        }

        private bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[]
            {
                "authorization", "api-key", "x-api-key", "cookie", "set-cookie"
            };

            return sensitiveHeaders.Any(sh => 
                headerName.Equals(sh, StringComparison.OrdinalIgnoreCase));
        }
    }
}

