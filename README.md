# Transaction API

A REST API middleware for validating and processing transaction information from allowed partners.

## Overview

This API acts as a middleware to validate and process transaction information from authorized partners. Key features include:
- Partner authentication
- Digital signature verification
- Field-level validation with business rules
- Transaction data integrity checks
- **Automated discount calculation** with multiple business rules
- Transaction amount validation
- **Comprehensive logging** with password encryption (log4net)

## Technology Stack

- .NET 8.0
- ASP.NET Core Web API
- Built-in dependency injection
- log4net (Logging framework)
- Docker (Containerization)

## Allowed Partners

| Partner No | Partner Key | Password |
|------------|-------------|----------|
| FG-00001 | FAKEGOOGLE | FAKEPASSWORD1234 |
| FG-00002 | FAKEPEOPLE | FAKEPASSWORD4578 |

## API Endpoint

**POST** `/api/submittrxmessage`

### Request Format

```json
{
  "partnerkey": "FAKEGOOGLE",
  "partnerrefno": "FG-00001",
  "partnerpassword": "RkFLRVBBU1NXT1JEMTIzNA==",
  "totalamount": 1000,
  "items": [
    {
      "partneritemref": "i-00001",
      "name": "Pen",
      "qty": 4,
      "unitprice": 200
    }
  ],
  "timestamp": "2024-08-15T02:11:22.0000000Z",
  "sig": "MDE3ZTBkODg4ZDNhYzU0ZDBlZWRmNmU2NmUyOWRhZWU4Y2M1NzQ1OTIzZGRjYTc1ZGNjOTkwYzg2MWJlMDExMw=="
}
```

### Request Fields

| Field | Type | Size | Required | Description |
|-------|------|------|----------|-------------|
| partnerkey | String | 50 | Yes | The allowed partner's key |
| partnerrefno | String | 50 | Yes | Partner's reference number (e.g., FG-00001) |
| partnerpassword | String | 50 | Yes | Base64 encoded password |
| totalamount | Long | - | Yes | Total amount in cents (e.g., 1000 = MYR 10.00) |
| items | Array | - | No | Array of items purchased |
| timestamp | String | - | Yes | ISO 8601 format (e.g., 2024-08-15T02:11:22.0000000Z) |
| sig | String | - | Yes | Message signature (Base64) |

### Item Details

| Field | Type | Size | Required | Description |
|-------|------|------|----------|-------------|
| partneritemref | String | 50 | Yes | Partner's item reference ID |
| name | String | 100 | Yes | Name of the item |
| qty | Integer | - | Yes | Quantity (1-5) |
| unitprice | Long | - | Yes | Unit price in cents |

### Response Format

**Success:**
```json
{
  "result": 1,
  "totalamount": 100000,
  "totaldiscount": 10000,
  "finalamount": 90000
}
```

**Response Fields**:
- `result`: 1 for success, 0 for failure
- `totalamount`: Original total amount in cents
- `totaldiscount`: Calculated discount amount in cents (based on discount rules)
- `finalamount`: Final amount after discount in cents
- `resultmessage`: Error message (only present when result = 0)

**Failure:**
```json
{
  "result": 0,
  "resultmessage": "Access Denied!"
}
```

## Signature Generation

The signature ensures message integrity and authenticity. Here's how it's generated:

### Steps:

1. **Format timestamp** - Convert ISO 8601 timestamp to `yyyyMMddHHmmss` format
   - Example: `2024-08-15T02:11:22.0000000Z` → `20240815021122`

2. **Concatenate parameters** in this order:
   ```
   timestamp + partnerkey + partnerrefno + totalamount + partnerpassword(encoded)
   ```
   Example: `20240815021122FAKEGOOGLEFG-000011000RkFLRVBBU1NXT1JEMTIzNA==`

3. **Apply SHA-256 hash** (UTF-8 encoding, lowercase hexadecimal output)
   ```
   017e0d888d3ac54d0eedf6e66e29daee8cc5745923ddca75dcc990c861be0113
   ```

4. **Convert to Base64** (UTF-8 encoding)
   ```
   MDE3ZTBkODg4ZDNhYzU0ZDBlZWRmNmU2NmUyOWRhZWU4Y2M1NzQ1OTIzZGRjYTc1ZGNjOTkwYzg2MWJlMDExMw==
   ```

## Business Rules

### Amount Validation
- Only positive values allowed
- Values in cents (100 = MYR 1.00)

### Item Validation
- `partneritemref` and `name` cannot be null or empty
- `qty` must be between 1 and 5
- `unitprice` must be positive

### Authentication
- Partner key and reference number must match
- Password must match after Base64 decoding
- Signature must be valid

### Discount Calculation (Question 3)

The API automatically calculates discounts based on the following rules:

#### Base Discount (Applied to all transactions)
| Amount Range (MYR) | Discount |
|-------------------|----------|
| 0 - 500 | 0% |
| 501 - 1,000 | 3% |
| 1,001 - 5,000 | 5% |
| 5,001 - 10,000 | 7% |
| 10,001 - 50,000 | 10% |
| 50,001+ | 15% |

#### Conditional Discounts (Additional)
- **Prime Number Bonus**: +8% if amount (in MYR) is prime AND > MYR 500
- **Ends in 5 Bonus**: +10% if amount (in MYR) ends in digit 5 AND > MYR 900

#### Maximum Cap
- Total discount cannot exceed 20% of the original amount

**Examples**:
- MYR 100,000 → 15% base = MYR 15,000 discount → Final: MYR 85,000
- MYR 523 (prime > 500) → 0% base + 8% prime = 8% = MYR 41.84 discount
- MYR 50,005 (ends in 5) → 15% base + 10% bonus = 25% → capped at 20% = MYR 10,001 discount

## Running the API

### Option 1: Local Development (.NET)

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Build the project:**
   ```bash
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Access API:**
   - Swagger UI: `https://localhost:7000/swagger`
   - API Endpoint: `https://localhost:7000/api/submittrxmessage`

### Option 2: Docker (Recommended for Deployment) - Question 5

1. **Quick Start:**
   ```bash
   docker compose up --build -d
   ```

2. **Access API:**
   - API Endpoint: `http://localhost:7000/api/submittrxmessage`
   - Swagger UI: `http://localhost:7000/swagger`

3. **View Logs:**
   ```bash
   docker compose logs -f
   # Or view container logs directly
   docker logs transaction-api
   ```

4. **Stop Services:**
   ```bash
   docker compose down
   ```

**✅ Currently Running**: Container `transaction-api` is active on port 7000!

**Docker Features:**
- ✅ Multi-stage build (optimized ~220MB image)
- ✅ Non-root user for security
- ✅ Persistent logs via volume mounts
- ✅ Easy deployment with single command

For detailed Docker documentation, refer to Docker setup in project root.

## Testing

Use the provided `test-requests.http` file to test various scenarios:
- Valid transaction
- Invalid partner credentials
- Invalid signature
- Negative amounts
- Quantity exceeding limits

## Project Structure

```
TransactionApi/
├── Controllers/
│   └── TransactionController.cs
├── Models/
│   ├── TransactionRequest.cs
│   ├── ItemDetail.cs
│   └── TransactionResponse.cs
├── Services/
│   ├── IPartnerAuthenticationService.cs
│   ├── PartnerAuthenticationService.cs
│   ├── ISignatureValidationService.cs
│   ├── SignatureValidationService.cs
│   ├── ITransactionValidationService.cs
│   └── TransactionValidationService.cs
└── Program.cs
```

## Error Handling

The API returns detailed error messages for various failure scenarios:

### Error Codes and Messages

| No | Error Message | Description |
|----|---------------|-------------|
| 1 | `"Access Denied!"` | Unauthorized partner or signature mismatch |
| 2 | `"Invalid Total Amount."` | When items are provided, the total value in itemDetails array doesn't equal totalamount |
| 3 | `"Expired."` | Provided timestamp exceeds server time ±5 minutes |
| 4 | `"[ParamName] is Required."` | Mandatory parameter is not provided (e.g., "partnerrefno is Required.") |

### Additional Validation Messages

- `"[field] must be a positive value."` - Invalid amount, price, or quantity
- `"qty must not exceed 5."` - Quantity limit exceeded
- `"[field] exceeds maximum length of [X] characters."` - Field length validation failed
- `"timestamp must be in valid ISO 8601 format."` - Invalid timestamp format

### Validation Order

The API validates in the following order:
1. **Field presence and format** - Checks all required fields are present
2. **Timestamp expiry** - Validates timestamp is within ±5 minutes of server time
3. **Item validation** - Validates each item's fields and business rules
4. **Total amount** - Validates sum of items equals totalamount (when items provided)
5. **Partner authentication** - Validates partner credentials
6. **Signature verification** - Validates message signature

## Logging & Audit Trail

The API includes comprehensive logging with log4net:

### Log Files
- `logs/application.log` - General application logs
- `logs/request-response.log` - Complete HTTP request/response logs
- `logs/errors.log` - Error and exception logs

### Security Features
- **Password Encryption**: All passwords encrypted with AES-256 in logs
- **Sensitive Data Masking**: Signatures and tokens automatically masked
- **Request Tracking**: Unique RequestId for each transaction
- **Complete Audit Trail**: Full traceability from request to response

Example log entry:
```
[RequestId: abc123] Transaction validated successfully for partner: FAKEGOOGLE
[RequestId: abc123] Discount calculated: 10% = 10000 cents, Final: 90000 cents
```

For detailed logging documentation, see [LOGGING_GUIDE.md](../LOGGING_GUIDE.md)

## Security Considerations

- All passwords are Base64 encoded in transit
- **Passwords encrypted with AES-256 in log files**
- SHA-256 signature verification prevents tampering
- Comprehensive input validation
- Complete audit trail with encrypted sensitive data
- Automatic sensitive field detection and masking

