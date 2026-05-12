using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Extensions;

public static class ApiBehaviorExtensions
{
    public static IServiceCollection AddEnvelopeModelValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var traceId = context.HttpContext.TraceIdentifier;
                var errors = context.ModelState
                    .Where(x => x.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors
                            .Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                                ? e.Exception?.Message
                                : e.ErrorMessage)
                            .Where(m => !string.IsNullOrEmpty(m))
                            .ToArray());

                var body = ApiResponseFactory.ErrorResponse("Validation failed", errors, traceId);
                return new BadRequestObjectResult(body);
            };
        });

        return services;
    }
}
