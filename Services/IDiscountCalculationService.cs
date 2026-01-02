namespace TransactionApi.Services
{
    public interface IDiscountCalculationService
    {
        /// <summary>
        /// Calculates the discount based on business rules.
        /// </summary>
        /// <param name="totalAmount">Total amount in cents</param>
        /// <returns>Tuple containing total discount percentage, discount amount in cents, and final amount in cents</returns>
        (decimal discountPercentage, long discountAmount, long finalAmount) CalculateDiscount(long totalAmount);
    }
}

