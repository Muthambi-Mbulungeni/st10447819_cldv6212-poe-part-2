using ABCRetailers.Models;
using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.Serialization;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public string PriceString { get; set; } = string.Empty;

        // Computed convenience property for UI binding only – not stored in Table Storage
        [IgnoreDataMember]
        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than $0.00")]
        public decimal Price
        {
            get
            {
                return decimal.TryParse(PriceString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                    ? result
                    : 0m;
            }
            set
            {
                PriceString = value.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}