using ABCRetailers.Services;
using System.Globalization;

namespace ABCRetailers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register HTTP Client Factory for calling Azure Functions
            builder.Services.AddHttpClient("FunctionsAPI", client =>
            {
                var functionsBaseUrl = builder.Configuration["FunctionsApi:BaseUrl"] ?? "http://localhost:7071/api";
                client.BaseAddress = new Uri(functionsBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register Functions API Service (calls Azure Functions via HTTP)
            // This is the PRIMARY service that implements the required architecture
            builder.Services.AddScoped<IFunctionsApiService, FunctionsApiService>();
            
            // Also register direct Azure Storage Service as IAzureStorageService for backward compatibility
            // But the main app will use IFunctionsApiService
            builder.Services.AddScoped<IAzureStorageService>(sp => 
            {
                // Return the Functions API Service wrapped as IAzureStorageService
                var functionsService = sp.GetRequiredService<IFunctionsApiService>();
                return new FunctionsApiServiceAdapter(functionsService);
            });

            // Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            // Set culture for decimal handling (FIXES PRICE ISSUE)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}