# product-catalog-api

A RESTful **ASP.NET Core Web API** for managing a product catalog, with user registration, JWT authentication, search with pagination, in-memory caching, rate limiting, and request logging.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-SQLite-003B57?logo=sqlite)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens)

---

## Features

- **Product CRUD**: create, list, update, and delete products
- **Search with pagination**: case-insensitive search by name with `page` and `pageSize`
- **Authentication**: register and log in with hashed passwords (`PasswordHasher`) and receive a **JWT**
- **Authorization**: updating and deleting products requires a valid token
- **Caching**: search results are cached for 5 minutes and invalidated automatically through cache **versioning** whenever products change
- **Rate limiting**: fixed window of **100 requests per minute**; over the limit returns `429 Too Many Requests`
- **Request logging**: custom middleware logs each request's method, path, status code, and duration
- **Validation**: data annotations (`[Required]`, `[MaxLength]`, `[Range]`) with automatic `400` responses

## Tech Stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core |
| Database | SQLite |
| Auth | JWT Bearer + ASP.NET Core Identity `PasswordHasher` |
| Caching | `IMemoryCache` behind an `ICacheService` abstraction |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` |

## Architecture

The project uses a layered architecture. Each layer has a single responsibility:

```
HTTP Request
    │
    ▼
Middleware      → logging, authentication, authorization, rate limiting
    │
    ▼
Controllers     → receive requests and return HTTP responses
    │
    ▼
Services        → business logic and caching
    │
    ▼
Repositories    → data access only
    │
    ▼
AppDbContext    → EF Core → SQLite (products.db)
```

All dependencies are wired with ASP.NET Core's built-in **Dependency Injection** in `Program.cs`.

## Project Structure

```
├── Controllers/
│   └── ProductsController.cs      # ProductsController + AuthController
├── Data/
│   └── AppDbContext.cs            # EF Core DbContext (Products, Users)
├── Middleware/
│   └── RequestLoggingMiddleware.cs
├── Migrations/                    # EF Core migrations
├── Models/
│   ├── Auth/                      # LoginRequest, RegisterRequest
│   ├── Products/                  # CreateProductRequest, UpdateProductRequest
│   ├── Product.cs                 # Product entity
│   └── User.cs                    # User entity
├── Repositories/                  # IProductRepository, IUserRepository + implementations
├── Services/
│   ├── Cache/                     # ICacheService, MemoryCacheService
│   ├── AuthService.cs             # register, login, JWT generation
│   └── ProductService.cs          # product logic + search caching
├── Program.cs                     # DI registration and middleware pipeline
└── appsettings.json               # connection string and JWT settings
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- EF Core CLI tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Run locally

```bash
git clone https://github.com/mehrshad2020/product-catalog-api.git
cd product-catalog-api
dotnet restore
dotnet ef database update
dotnet run
```

The API runs at `http://localhost:5133` (or `https://localhost:7028` with the `https` profile).

`dotnet ef database update` creates `products.db` and applies all migrations.

## Configuration

Settings live in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=products.db"
  },
  "Jwt": {
    "Key": "THIS_IS_A_DEVELOPMENT_SECRET_KEY_123456789",
    "Issuer": "YourProject",
    "Audience": "YourProjectUsers"
  }
}
```

> ⚠️ The JWT key in this repository is for **development only**. For any real deployment, use a long random secret and keep it out of source control, for example with `dotnet user-secrets` or environment variables.

## API Endpoints

### Auth: `/api/auth`

| Method | Endpoint | Description | Success | Errors |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | `200` | `400`, `409` username taken |
| `POST` | `/api/auth/login` | Log in and receive a JWT | `200` + token | `400`, `401` |

### Products: `/api/products`

| Method | Endpoint | Description | Auth | Success | Errors |
|---|---|---|---|---|---|
| `POST` | `/api/products` | Create a product | — | `201` | `400` |
| `GET` | `/api/products` | List all products | — | `200` | — |
| `GET` | `/api/products/search?title=&page=&pageSize=` | Search by name (paginated) | — | `200` | `400` |
| `PUT` | `/api/products/{id}` | Update a product | 🔒 | `200` | `400`, `401`, `404` |
| `DELETE` | `/api/products/{id}` | Delete a product | 🔒 | `204` | `401`, `404` |

All endpoints are rate limited to 100 requests per minute (`429` when exceeded).

## Usage Examples

### 1. Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "mehrshad",
  "password": "secret123"
}
```

Username must be 3–50 characters; password must be at least 6 characters.

### 2. Log in

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "mehrshad",
  "password": "secret123"
}
```

Response:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

The token is valid for **1 hour**.

### 3. Create a product

```http
POST /api/products
Content-Type: application/json

{
  "name": "Laptop",
  "description": "14-inch, 16GB RAM",
  "price": 1200
}
```

Response: `201 Created` with a `Location: /api/products/{id}` header and the created product.

### 4. Search products

```http
GET /api/products/search?title=lap&page=1&pageSize=10
```

- `title`: required
- `page`: default `1`, must be ≥ 1
- `pageSize`: default `10`, must be between 1 and 100

### 5. Update a product (requires token)

```http
PUT /api/products/1
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "name": "Laptop Pro",
  "description": "14-inch, 32GB RAM",
  "price": 1500
}
```

### 6. Delete a product (requires token)

```http
DELETE /api/products/1
Authorization: Bearer <your-token>
```

Response: `204 No Content`

## How Search Caching Works

Search results are cached with a key that includes a **version number**:

```
products:v{version}:search:{title}:{page}:{pageSize}
```

Each create, update, or delete increments the `products` version. Later searches use a new key, so they read fresh data from the database instead of stale cache entries. Old entries expire on their own after 5 minutes.

## Database Migrations

After changing an entity or `AppDbContext`:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```
