using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.Serialization;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Order ID")]
        public string OrderId => RowKey;

        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public string UnitPriceString { get; set; } = string.Empty;

        [IgnoreDataMember]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice
        {
            get
            {
                return decimal.TryParse(UnitPriceString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                    ? result
                    : 0m;
            }
            set
            {
                UnitPriceString = value.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public string TotalPriceString { get; set; } = string.Empty;

        [IgnoreDataMember]
        [Display(Name = "Total Price")]
        public decimal TotalPrice
        {
            get
            {
                return decimal.TryParse(TotalPriceString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                    ? result
                    : 0m;
            }
            set
            {
                TotalPriceString = value.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";
    }

    public enum OrderStatus
    {
        Submitted,    // When order is first created
        Processing,   // When order is being processed by the store
        Completed,    // When order is delivered to customer
        Cancelled     // When order is cancelled
    }
}