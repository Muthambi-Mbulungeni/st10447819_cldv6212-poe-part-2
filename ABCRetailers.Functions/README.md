# ABCRetailers Azure Functions

## Overview
This project contains Azure Functions that provide HTTP endpoints for the ABCRetailers MVC application to interact with Azure Storage services.

## Architecture
- **MVC App** → HTTP Requests → **Azure Functions** → **Azure Storage**

## Functions Included

### 1. Table Storage Functions (Customer, Product, Order)
- `CustomersFunctions.cs` - CRUD operations for customers
- `ProductsFunctions.cs` - CRUD operations for products
- `OrdersFunctions.cs` - CRUD operations for orders (read/update/delete only)

### 2. Queue Functions
- `QueueProcessorFunctions.cs`
  - **ProcessOrderQueue** (Queue-triggered) - Processes `order-notifications` queue and writes to Orders table
  - **ProcessStockQueue** (Queue-triggered) - Processes `stock-updates` queue and updates product stock
  - SendQueueMessage/ReceiveQueueMessage - HTTP endpoints for queue operations

### 3. Blob Storage Functions
- `BlobFunctions.cs` - Upload/delete/list blobs for product images

### 4. File Share Functions
- `FileShareFunctions.cs` - Upload/download/list files for contracts and documents

## Setup

### Prerequisites
- .NET 8.0 SDK
- Azure Functions Core Tools (`npm install -g azure-functions-core-tools@4`)
- Azure Storage Account

### Configuration
Update `local.settings.json` with your Azure Storage connection string:
```json
{
  "Values": {
    "AzureStorageConnection": "YOUR_CONNECTION_STRING_HERE"
  }
}
```

### Run Locally
```bash
cd ABCRetailers.Functions
func start --port 7071
```

The Functions will be available at: `http://localhost:7071/api/`

## API Endpoints

### Customers
- GET `/api/table/Customers` - Get all customers
- GET `/api/table/Customers/{partitionKey}/{rowKey}` - Get single customer
- POST `/api/table/Customers` - Create customer
- PUT `/api/table/Customers` - Update customer
- DELETE `/api/table/Customers/{partitionKey}/{rowKey}` - Delete customer

### Products
- GET `/api/table/Products` - Get all products
- GET `/api/table/Products/{partitionKey}/{rowKey}` - Get single product
- POST `/api/table/Products` - Create product
- PUT `/api/table/Products` - Update product
- DELETE `/api/table/Products/{partitionKey}/{rowKey}` - Delete product

### Orders
- GET `/api/table/Orders` - Get all orders
- GET `/api/table/Orders/{partitionKey}/{rowKey}` - Get single order
- PUT `/api/table/Orders` - Update order
- DELETE `/api/table/Orders/{partitionKey}/{rowKey}` - Delete order

### Queues
- POST `/api/queue/{queueName}` - Send message to queue
- GET `/api/queue/{queueName}` - Receive message from queue

### Blobs
- POST `/api/blob/{containerName}` - Upload blob (multipart/form-data)
- DELETE `/api/blob/{containerName}/{blobName}` - Delete blob
- GET `/api/blob/{containerName}` - List blobs

### File Shares
- POST `/api/fileshare/{shareName}/{directoryName}` - Upload file (multipart/form-data)
- GET `/api/fileshare/{shareName}/{directoryName}/{fileName}` - Download file
- GET `/api/fileshare/{shareName}/{directoryName}` - List files

## Queue-Triggered Functions

### ProcessOrderQueue
- **Trigger**: `order-notifications` queue
- **Action**: Creates new orders or updates order status in the Orders table
- **Message Format**:
```json
{
  "Action": "Create",
  "OrderId": "guid",
  "CustomerId": "guid",
  "Username": "username",
  "ProductId": "guid",
  "ProductName": "name",
  "OrderDate": "2025-10-06",
  "Quantity": 1,
  "UnitPrice": "99.99",
  "TotalPrice": "99.99",
  "Status": "Submitted"
}
```

### ProcessStockQueue
- **Trigger**: `stock-updates` queue
- **Action**: Decrements product stock when orders are placed
- **Message Format**:
```json
{
  "ProductId": "guid",
  "QuantitySold": 1
}
```

## Deployment

### Deploy to Azure
```bash
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

Then update the MVC app's `appsettings.json`:
```json
{
  "FunctionsApi": {
    "BaseUrl": "https://YOUR_FUNCTION_APP_NAME.azurewebsites.net/api"
  }
}
```


