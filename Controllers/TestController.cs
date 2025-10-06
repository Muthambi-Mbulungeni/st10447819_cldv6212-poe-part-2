using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class TestController : Controller
    {
        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Config()
        {
            var functionsUrl = _configuration["FunctionsApi:BaseUrl"];
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            return Json(new 
            { 
                FunctionsUrl = functionsUrl,
                Environment = environment,
                AllConfig = _configuration.AsEnumerable().ToDictionary(x => x.Key, x => x.Value)
            });
        }
    }
}
