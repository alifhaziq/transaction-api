using System.ComponentModel.DataAnnotations;

namespace TransactionApi.Models
{
    public class ItemDetail
    {
        [Required]
        [StringLength(50)]
        public string partneritemref { get; set; }

        [Required]
        [StringLength(100)]
        public string name { get; set; }

        [Required]
        public int qty { get; set; }

        [Required]
        public long unitprice { get; set; }
    }
}

