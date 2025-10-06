# ABC Retailers - Architecture Implementation

## ✅ **Complete Architecture According to Assessment Requirements**

### **System Flow (REQUIRED ARCHITECTURE)**

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER INTERACTION                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      MVC Web Application                         │
│                     (http://localhost:5236)                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                      Controllers                            │ │
│  │  - HomeController                                          │ │
│  │  - CustomerController                                      │ │
│  │  - ProductController                                       │ │
│  │  - OrderController                                         │ │
│  └─────────────────────┬──────────────────────────────────────┘ │
│                        │ Inject IAzureStorageService            │
│                        ▼                                         │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │            FunctionsApiServiceAdapter                      │ │
│  │     (Implements IAzureStorageService interface)            │ │
│  └─────────────────────┬──────────────────────────────────────┘ │
│                        │ Delegates to                            │
│                        ▼                                         │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │            FunctionsApiService                             │ │
│  │          (Makes HTTP calls to Functions)                   │ │
│  └─────────────────────┬──────────────────────────────────────┘ │
└────────────────────────┼────────────────────────────────────────┘
                         │ HTTP Requests
                         │ (GET, POST, PUT, DELETE)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Azure Functions (Serverless)                    │
│                  (http://localhost:7071/api)                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  HTTP-Triggered Functions:                                │  │
│  │  • GET  /api/customers       → CustomersFunctions        │  │
│  │  • POST /api/customers       → CustomersFunctions        │  │
│  │  • GET  /api/products        → ProductsFunctions         │  │
│  │  • POST /api/products        → ProductsFunctions         │  │
│  │  • POST /api/orders          → OrdersFunctions (→ Queue!)│  │
│  │  • POST /api/blobs/upload    → BlobFunctions             │  │
│  │  • POST /api/files/upload    → FileShareFunctions        │  │
│  └──────────────────────┬───────────────────────────────────┘  │
│                         │                                        │
│  ┌──────────────────────▼───────────────────────────────────┐  │
│  │  Queue-Triggered Function:                                │  │
│  │  • ProcessOrderQueue (listens to order-notifications)     │  │
│  │    - Receives order from queue                            │  │
│  │    - Saves to Orders Table Storage                        │  │
│  └──────────────────────┬───────────────────────────────────┘  │
└────────────────────────┼────────────────────────────────────────┘
                         │ Direct Azure SDK Calls
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Storage Account                        │
│                    (abcretailerspoe)                             │
│  ┌────────────┬────────────┬────────────┬────────────────────┐ │
│  │   Tables   │   Blobs    │   Queues   │   File Shares      │ │
│  ├────────────┼────────────┼────────────┼────────────────────┤ │
│  │ Customers  │ product-   │ order-     │ contracts/         │ │
│  │ Products   │ images/    │ notifica-  │ payments/          │ │
│  │ Orders     │ payment-   │ tions      │                    │ │
│  │            │ proofs/    │ stock-     │                    │ │
│  │            │            │ updates    │                    │ │
│  └────────────┴────────────┴────────────┴────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## **Key Implementation Details**

### 1. **Service Layer Architecture** ✅

#### **IAzureStorageService Interface**
- Defines all storage operations
- Used by all controllers

#### **FunctionsApiService** (NEW - Main Implementation)
- Implements `IFunctionsApiService`
- Makes HTTP calls to Azure Functions
- Uses `HttpClient` with `IHttpClientFactory`
- Logs all API calls

#### **FunctionsApiServiceAdapter** (NEW - Adapter Pattern)
- Wraps `FunctionsApiService`
- Implements `IAzureStorageService` interface
- Allows controllers to use dependency injection without changes

#### **AzureLabStorageService** (Original - Kept for Reference)
- Direct Azure Storage SDK calls
- Kept in codebase but NOT used by default
- Can be switched to in configuration if needed

### 2. **Dependency Injection Setup** (Program.cs)

```csharp
// HTTP Client for calling Functions
builder.Services.AddHttpClient("FunctionsAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:7071/api");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Functions API Service
builder.Services.AddScoped<IFunctionsApiService, FunctionsApiService>();

// Register adapter as IAzureStorageService
builder.Services.AddScoped<IAzureStorageService>(sp => 
{
    var functionsService = sp.GetRequiredService<IFunctionsApiService>();
    return new FunctionsApiServiceAdapter(functionsService);
});
```

### 3. **Configuration** (appsettings.json)

```json
{
  "FunctionsApi": {
    "BaseUrl": "http://localhost:7071/api"
  }
}
```

For production, change to Azure Functions URL:
```json
{
  "FunctionsApi": {
    "BaseUrl": "https://your-function-app.azurewebsites.net/api"
  }
}
```

## **How Data Flows Through the System**

### Example: Creating a Customer

1. **User** fills out customer form on website
2. **CustomerController** receives POST request
3. Controller calls `_storageService.AddEntityAsync<Customer>(customer)`
4. **FunctionsApiServiceAdapter** receives call
5. Adapter delegates to **FunctionsApiService**
6. FunctionsApiService makes HTTP POST to `http://localhost:7071/api/customers`
7. **CustomersFunctions** (Azure Function) receives request
8. Function calls Azure Table Storage SDK directly
9. Customer saved to **Customers table**
10. Function returns HTTP 201 Created with customer data
11. Response flows back through service → adapter → controller → view
12. User sees success message

### Example: Creating an Order (Queue-Based) ⭐

1. **User** submits order
2. **OrderController** calls `_storageService.AddEntityAsync<Order>(order)`
3. **FunctionsApiService** makes HTTP POST to `/api/orders`
4. **OrdersFunctions** receives order
5. Function sends message to `order-notifications` **Queue** (not table!)
6. Function returns HTTP 202 Accepted
7. **ProcessOrderQueue** function (queue-triggered) activates
8. Queue function reads message and saves order to **Orders table**
9. This ensures async processing and reliability!

## **Storage Type Usage**

| Storage Type | Function | MVC Flow |
|--------------|----------|----------|
| **Table Storage** | CustomersFunctions, ProductsFunctions, OrdersFunctions | MVC → HTTP → Function → Table |
| **Blob Storage** | ProductsFunctions, BlobFunctions | MVC → HTTP → Function → Blob |
| **Queue Storage** | OrdersFunctions → ProcessOrderQueue | MVC → HTTP → Function → Queue → QueueTrigger → Table |
| **File Share** | FileShareFunctions | MVC → HTTP → Function → FileShare |

## **Assessment Requirements Met** ✅

### Part A Checklist:

- ✅ **4 Functions (one per storage type)**
  - CustomersFunctions (Table)
  - ProductsFunctions (Table + Blob)
  - OrdersFunctions (Queue)
  - BlobFunctions + FileShareFunctions (Blob + File)

- ✅ **1 Queue-triggered function**
  - ProcessOrderQueue (processes orders from queue)

- ✅ **Functions carry out storage functionality**
  - All CRUD operations in Functions
  - Direct Azure SDK usage in Functions only

- ✅ **Services call Functions via HTTP** ⭐
  - FunctionsApiService uses HttpClient
  - All controllers → Functions → Storage
  - NO direct storage calls from MVC

- ✅ **At least one method per service type in function**
  - Table: GetAllCustomers, CreateCustomer, etc.
  - Blob: UploadImage, DeleteBlob
  - Queue: CreateOrder (sends to queue)
  - File: UploadContract, DownloadFile

- ✅ **Orders update queue (not storage directly)**
  - POST /api/orders sends to queue
  - ProcessOrderQueue saves to table
  - Async, reliable processing

- ✅ **Search functionality**
  - Products page: Real-time search
  - Orders page: Search + filters

- ✅ **Order status updates**
  - Dropdown menus
  - Toast notifications
  - Visual feedback

- ✅ **Styling improvements**
  - Modern cards
  - Color-coded badges
  - Responsive design

## **Running the Complete System**

### Step 1: Start Azure Functions
```bash
cd ABCRetailers.Functions
func start
# Functions available at http://localhost:7071
```

### Step 2: Start MVC App
```bash
cd ABCRetailers
dotnet run
# App available at http://localhost:5236
```

### Step 3: Test the Flow
1. Open http://localhost:5236
2. Create a customer → Check Functions console for HTTP POST log
3. Create an order → See queue message processed in Functions console
4. All operations go through Functions!

## **Switching Between Implementations**

### Use Functions API (DEFAULT - Required for Assessment)
Already configured! Controllers call Functions via HTTP.

### Use Direct Storage (Fallback)
Edit `Program.cs`:
```csharp
// Comment out Functions API registration
// builder.Services.AddScoped<IAzureStorageService>(sp => ...);

// Register direct storage instead
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();
```

## **Benefits of This Architecture**

1. **Separation of Concerns**: UI logic separate from storage logic
2. **Scalability**: Functions auto-scale independently
3. **Security**: Storage credentials only in Functions, not MVC app
4. **Monitoring**: Centralized logging in Functions
5. **Flexibility**: Easy to switch storage implementations
6. **Reliability**: Queue-based processing for critical operations
7. **Cost-Effective**: Pay only for function executions

## **Production Deployment**

1. Deploy Functions to Azure Function App
2. Update `appsettings.json` with Azure Functions URL
3. Deploy MVC app to Azure App Service
4. All traffic flows: App Service → Function App → Storage

---

**This implementation fully satisfies the assessment requirement:**
> "Your services files will be used to call the Functions. You will send http requests or messages to the queue to achieve this."

✅ **ALL REQUIREMENTS MET!**


