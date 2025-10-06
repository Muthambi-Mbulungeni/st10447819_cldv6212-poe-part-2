using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // Normalize price into PriceString to persist in Table Storage
            if (Request.Form.TryGetValue("Price", out var priceFormValue) &&
                decimal.TryParse(priceFormValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedPrice))
            {
                product.Price = parsedPrice; // sets PriceString internally
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // TEMPORARILY DISABLED - Skip image upload for now
                    // if (imageFile != null && imageFile.Length > 0)
                    // {
                    //     var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                    //     product.ImageUrl = imageUrl;
                    // }
                    
                    // Set a default placeholder image
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        product.ImageUrl = "https://via.placeholder.com/400x300?text=Product+Image";
                    }

                    // Ensure persisted value set
                    product.PriceString = product.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    await _storageService.AddEntityAsync(product);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }

            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            // Manual price parsing for edit too, using invariant culture
            if (Request.Form.TryGetValue("Price", out var priceFormValue) &&
                decimal.TryParse(priceFormValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedPrice))
            {
                product.Price = parsedPrice; // sets PriceString
                _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original product to preserve ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    // Update properties but keep the original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price; // syncs PriceString
                    originalProduct.PriceString = originalProduct.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    originalProduct.StockAvailable = product.StockAvailable;

                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteAjax([FromBody] DeleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.id))
            {
                return Json(new { success = false, message = "Invalid request" });
            }
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", request.id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class DeleteRequest
        {
            public string? id { get; set; }
        }
    }
}