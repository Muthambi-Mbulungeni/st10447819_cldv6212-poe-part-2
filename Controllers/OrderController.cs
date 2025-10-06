using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();

            // Backfill any legacy orders with missing price values
            foreach (var order in orders.Where(o => o.TotalPrice <= 0 && !string.IsNullOrEmpty(o.ProductId)))
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (product != null && product.Price > 0)
                {
                    order.UnitPrice = product.Price;
                    order.UnitPriceString = order.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    order.TotalPrice = product.Price * order.Quantity;
                    order.TotalPriceString = order.TotalPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    try { await _storageService.UpdateEntityAsync(order); } catch { }
                }
            }

            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Normalize order date to UTC as required by Azure SDK
                    var orderDateUtc = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc);

                    // Create order
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = orderDateUtc,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted" // Always starts as Submitted
                    };

                    // Ensure string-backed properties are populated for storage
                    order.UnitPriceString = order.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    order.TotalPriceString = order.TotalPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    // Send queue message for new order
                    var orderMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = customer.Name + " " + customer.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                    // Send stock update message
                    var stockMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity,
                        NewStock = product.StockAvailable,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Load the current order to preserve a valid ETag
                    var original = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (original == null)
                    {
                        return NotFound();
                    }

                    // Apply edits
                    original.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    original.Quantity = order.Quantity; // if allowed to change, else ignore
                    original.UnitPrice = order.UnitPrice;
                    original.TotalPrice = order.TotalPrice;
                    original.UnitPriceString = original.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    original.TotalPriceString = original.TotalPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    original.Status = order.Status;

                    await _storageService.UpdateEntityAsync(original);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
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
                await _storageService.DeleteEntityAsync<Order>("Order", request.id);
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

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        public class UpdateOrderStatusRequest
        {
            public string? id { get; set; }
            public string? newStatus { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.id) || string.IsNullOrWhiteSpace(request.newStatus))
                {
                    return Json(new { success = false, message = "Invalid request payload" });
                }

                var order = await _storageService.GetEntityAsync<Order>("Order", request.id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = request.newStatus;
                await _storageService.UpdateEntityAsync(order);

                // Send queue message for status update
                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = request.newStatus,
                    UpdateDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {request.newStatus}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}