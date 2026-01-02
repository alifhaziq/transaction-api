namespace TransactionApi.Services
{
    public class DiscountCalculationService : IDiscountCalculationService
    {
        public (decimal discountPercentage, long discountAmount, long finalAmount) CalculateDiscount(long totalAmount)
        {
            // Convert cents to MYR for business logic
            decimal amountInMYR = totalAmount / 100m;

            // 1. Calculate Base Discount
            decimal baseDiscount = CalculateBaseDiscount(totalAmount);

            // 2. Calculate Conditional Discounts
            decimal conditionalDiscount = CalculateConditionalDiscounts(totalAmount, amountInMYR);

            // 3. Calculate Total Discount
            decimal totalDiscount = baseDiscount + conditionalDiscount;

            // 4. Apply Cap (maximum 20%)
            if (totalDiscount > 20m)
            {
                totalDiscount = 20m;
            }

            // 5. Calculate discount amount and final amount
            long discountAmount = (long)(totalAmount * totalDiscount / 100m);
            long finalAmount = totalAmount - discountAmount;

            return (totalDiscount, discountAmount, finalAmount);
        }

        /// <summary>
        /// Calculates base discount based on amount ranges per Question 3.
        /// All amounts in cents: MYR 1 = 100 cents
        /// </summary>
        private decimal CalculateBaseDiscount(long totalAmount)
        {
            // Question 3 Discount Tiers (amounts in cents):
            // MYR 0 - 500 = 0 - 50000 cents: 0%
            // MYR 501 - 1,000 = 50100 - 100000 cents: 3%
            // MYR 1,001 - 5,000 = 100100 - 500000 cents: 5%
            // MYR 5,001 - 10,000 = 500100 - 1000000 cents: 7%
            // MYR 10,001 - 50,000 = 1000100 - 5000000 cents: 10%
            // MYR 50,001+ = 5000100+ cents: 15%

            if (totalAmount <= 50000)
            {
                return 0m; // 0 - MYR 500: 0%
            }
            else if (totalAmount >= 50100 && totalAmount <= 100000)
            {
                return 3m; // MYR 501 - 1,000: 3%
            }
            else if (totalAmount >= 100100 && totalAmount <= 500000)
            {
                return 5m; // MYR 1,001 - 5,000: 5%
            }
            else if (totalAmount >= 500100 && totalAmount <= 1000000)
            {
                return 7m; // MYR 5,001 - 10,000: 7%
            }
            else if (totalAmount >= 1000100 && totalAmount <= 5000000)
            {
                return 10m; // MYR 10,001 - 50,000: 10%
            }
            else // totalAmount > 5000000
            {
                return 15m; // MYR 50,001+: 15%
            }
        }

        /// <summary>
        /// Calculates conditional discounts based on special rules.
        /// </summary>
        private decimal CalculateConditionalDiscounts(long totalAmount, decimal amountInMYR)
        {
            decimal conditionalDiscount = 0m;

            // Conditional 1: Prime number above MYR 500
            if (amountInMYR > 500m && IsPrime((long)amountInMYR))
            {
                conditionalDiscount += 8m;
            }

            // Conditional 2: Ends in digit 5 and above MYR 900
            if (amountInMYR > 900m && EndsInDigit5((long)amountInMYR))
            {
                conditionalDiscount += 10m;
            }

            return conditionalDiscount;
        }

        /// <summary>
        /// Checks if a number is prime.
        /// </summary>
        private bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            // Check odd divisors up to sqrt(number)
            long boundary = (long)Math.Sqrt(number);
            for (long i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a number ends in the digit 5.
        /// </summary>
        private bool EndsInDigit5(long number)
        {
            return number % 10 == 5;
        }
    }
}

