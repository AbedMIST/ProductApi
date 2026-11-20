using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProductApi.OpenApi;

// Declares the "Bearer" (JWT) scheme once for the whole document -> Swagger UI shows the Authorize button.
public sealed class BearerSchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the token from POST /api/auth/login (without the 'Bearer ' prefix)."
        };
        return Task.CompletedTask;
    }
}

// Marks only the endpoints that actually carry [Authorize] as requiring the scheme, so GET shows no lock.
public sealed class BearerRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }
        return Task.CompletedTask;
    }
}
