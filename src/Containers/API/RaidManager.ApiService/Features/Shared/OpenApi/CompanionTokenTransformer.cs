using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RaidManager.ApiService.Features.Shared.Authentication;

namespace RaidManager.ApiService.Features.Shared.OpenApi;

/// <summary>Documents the companion's device token and marks the operations that require it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the OpenAPI document honest about ADR-0030: companion operations show the bearer token they need.
/// </remarks>
internal sealed class CompanionTokenTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    #region Public Methods
    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[CompanionTokenDefaults.Scheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Device token a paired companion received from POST /companion/pairings/token.",
        };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresCompanion = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any(authorize => authorize.Policy == CompanionTokenDefaults.Policy);
        if (requiresCompanion)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(CompanionTokenDefaults.Scheme, context.Document)] = [] },
            ];
        }

        return Task.CompletedTask;
    }
    #endregion Public Methods
}
