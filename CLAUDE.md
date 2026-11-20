# CLAUDE.md

ASP.NET Core Web API (.NET 10, controllers) for product CRUD with JWT authentication. Uses an EF Core **in-memory** database, so all data, including registered users, is lost on restart.

## Commands

```bash
dotnet build                       # build
dotnet run                         # run with the "http" profile -> http://localhost:5000 (Development)
dotnet run --launch-profile https  # https://localhost:7276 + http://localhost:5000
```

- Swagger UI: `http://localhost:5000/swagger`. OpenAPI JSON: `/openapi/v1.json`. Both are **Development only**.
- `ProductApi.http` has sample requests for VS Code / Rider, including register/login/refresh/logout. The product POST/PUT/DELETE samples have no `Authorization` header, so they return 401 until you add `Authorization: Bearer <token>`.
- There is no test project and no EF migrations. The in-memory provider does not need migrations.

## Architecture

Layering: **Controller → Service (interface) → AppDbContext**. Controllers handle HTTP only. Business and data logic lives in services.

| Folder | Contents |
|---|---|
| `Program.cs` | DI registration, JWT bearer setup, middleware pipeline |
| `Controllers/` | `ProductsController` (`/api/products`), `AuthController` (`/api/auth`) |
| `Services/` | `ProductService`, `AuthService` (scoped); `TokenService` (singleton, signs JWTs) |
| `Data/AppDbContext.cs` | `Products` and `Users` DbSets; constraints in `OnModelCreating` (unique username index, price precision 18,2) |
| `Models/` | EF entities (`Product`, `User`). **Never returned from the API.** |
| `Dtos/` | Request classes with DataAnnotations validation; response records |
| `Options/JwtOptions.cs` | Bound from the `Jwt` config section, validated at startup |
| `OpenApi/` | Transformers that add the Bearer scheme and a lock on `[Authorize]` endpoints in Swagger |

### Endpoints

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/api/products` | – | ordered by Id |
| GET | `/api/products/{id}` | – | route name `GetProductById` |
| POST | `/api/products` | JWT | 201 + Location via `CreatedAtRoute` |
| PUT | `/api/products/{id}` | JWT | full replacement; sets `UpdatedAt` |
| DELETE | `/api/products/{id}` | JWT | 204 / 404 |
| POST | `/api/auth/register` | – | 201, or 409 if the username is taken |
| POST | `/api/auth/login` | – | returns `TokenResponse` (access + refresh token), or 401 |
| POST | `/api/auth/refresh` | – | body `{ refreshToken }`; rotates and returns a new `TokenResponse`, or 401 |
| POST | `/api/auth/logout` | – | body `{ refreshToken }`; revokes it; always 204 |

## Conventions

- **DTO mapping**: services map entities to response DTOs (`ProductService.ToResponse`). Do not expose `Models/` types from controllers.
- **Validation**: put DataAnnotations on request DTOs. `[ApiController]` returns 400 `ValidationProblemDetails` automatically, so controllers have no manual `ModelState` checks. `UpdateProductRequest` inherits from `CreateProductRequest`, so both share the same rules.
- **Errors**: return RFC 7807 ProblemDetails (`AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages`). For custom errors, use `Problem(detail, statusCode: ...)`.
- **Service return values signal outcomes**: `null` means not found or invalid credentials, and `false` means not found or a duplicate. Controllers translate these into status codes. Services do not throw for expected failures.
- Every async method takes a `CancellationToken ct` and passes it through to EF.
- Use primary-constructor DI (`class X(IDep dep)`), file-scoped namespaces, and nullable enabled.
- Use `[ProducesResponseType]` on every action so the OpenAPI doc is accurate.
- Timestamps are stored as `DateTime.UtcNow`.

## Auth details

- Passwords are hashed with `PasswordHasher<User>` (PBKDF2). Usernames are trimmed and lowercased on both register and login.
- Login returns the same `null` for an unknown user and a wrong password, so it does not reveal which usernames exist. Keep it that way.
- JWT claims: `sub` (user id), `unique_name` (username), `jti`. Signed with HS256.
- Validation lives in `Program.cs` and must match `TokenService`: same issuer, audience and key. `MapInboundClaims = false` keeps short claim names, `NameClaimType = "unique_name"`, and `ClockSkew` is 30 s.
- `[Authorize]` is applied per action, not per controller. The GET endpoints stay public.
- **Refresh tokens** (`Models/RefreshToken.cs`): 64 random bytes, base64url. Only the SHA-256 hex hash is stored (`TokenService.HashRefreshToken`). `AuthService` handles persistence; `TokenService` stays a stateless singleton.
  - **Rotation**: every refresh marks the old row `RevokedAt` + `ReplacedByTokenHash` and issues a new pair.
  - **Reuse detection**: presenting an already-revoked token revokes *all* of that user's active refresh tokens. Keep this behavior.
  - Logout only revokes the refresh token; the access token stays valid until it expires (no JWT denylist).
  - Expired/revoked rows are never cleaned up (fine for the in-memory DB).

## Configuration gotchas

- `Jwt:Key` (at least 32 chars) exists **only** in `appsettings.Development.json`. Because `JwtOptions` uses `ValidateOnStart`, running in any other environment without that key (for example through user-secrets or the `Jwt__Key` env var) **fails at startup**.
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` (access token, default 60, range 1–1440) and `Jwt:RefreshTokenExpiryDays` (default 7, range 1–90) are in `appsettings.json`.
- Middleware order matters: `UseAuthentication()` must come before `UseAuthorization()`.
- The project is not a git repository.
