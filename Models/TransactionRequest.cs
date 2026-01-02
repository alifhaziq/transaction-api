using System.ComponentModel.DataAnnotations;

namespace TransactionApi.Models
{
    public class TransactionRequest
    {
        [Required]
        [StringLength(50)]
        public string partnerkey { get; set; }

        [Required]
        [StringLength(50)]
        public string partnerrefno { get; set; }

        [Required]
        [StringLength(50)]
        public string partnerpassword { get; set; }

        [Required]
        public long totalamount { get; set; }

        public List<ItemDetail>? items { get; set; }

        [Required]
        public string timestamp { get; set; }

        [Required]
        public string sig { get; set; }
    }
}

