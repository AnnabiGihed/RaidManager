using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RaidManager.ApiService.Features.Shared.Authentication;

namespace RaidManager.ApiService.Features.Shared.OpenApi;

/// <summary>Documents the website key and marks the operations that require it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps the OpenAPI document honest about ADR-0011: website-only operations show the key header they need.
/// </remarks>
internal sealed class WebsiteServiceKeyTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    #region Public Methods
    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[WebsiteServiceDefaults.Scheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = WebsiteServiceDefaults.HeaderName,
            Description = "Shared key of the RaidManager website server. Only the website may call these operations.",
        };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresWebsite = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any(authorize => authorize.Policy == WebsiteServiceDefaults.Policy);
        if (requiresWebsite)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(WebsiteServiceDefaults.Scheme, context.Document)] = [] },
            ];
        }

        return Task.CompletedTask;
    }
    #endregion Public Methods
}
