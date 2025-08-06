using BlazorTemplate.DTO.Api;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Text.Json;

namespace BlazorTemplate.Swagger
{
    /// <summary>
    /// Adds example schemas for API response models
    /// </summary>
    public class SwaggerExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(ApiResponse<>))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                    ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Operation completed successfully"),
                    ["data"] = new Microsoft.OpenApi.Any.OpenApiObject(),
                    ["errors"] = new Microsoft.OpenApi.Any.OpenApiArray(),
                    ["timestamp"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O"))
                };
            }
            else if (context.Type == typeof(PagedApiResponse<>))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                    ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Data retrieved successfully"),
                    ["data"] = new Microsoft.OpenApi.Any.OpenApiArray(),
                    ["errors"] = new Microsoft.OpenApi.Any.OpenApiArray(),
                    ["timestamp"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O")),
                    ["page"] = new Microsoft.OpenApi.Any.OpenApiInteger(1),
                    ["pageSize"] = new Microsoft.OpenApi.Any.OpenApiInteger(10),
                    ["totalCount"] = new Microsoft.OpenApi.Any.OpenApiInteger(100),
                    ["totalPages"] = new Microsoft.OpenApi.Any.OpenApiInteger(10)
                };
            }
        }
    }

    /// <summary>
    /// Adds standard HTTP response codes and descriptions to API operations
    /// </summary>
    public class SwaggerDefaultResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Remove default 200 response if it exists
            if (operation.Responses.ContainsKey("200"))
            {
                operation.Responses.Remove("200");
            }

            // Add standard responses
            if (!operation.Responses.ContainsKey("400"))
            {
                operation.Responses.Add("400", new OpenApiResponse
                {
                    Description = "Bad Request - Invalid input data",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("401"))
            {
                operation.Responses.Add("401", new OpenApiResponse
                {
                    Description = "Unauthorized - Authentication required",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("403"))
            {
                operation.Responses.Add("403", new OpenApiResponse
                {
                    Description = "Forbidden - Insufficient permissions",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("404"))
            {
                operation.Responses.Add("404", new OpenApiResponse
                {
                    Description = "Not Found - Resource not found",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("409"))
            {
                operation.Responses.Add("409", new OpenApiResponse
                {
                    Description = "Conflict - Resource already exists",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("422"))
            {
                operation.Responses.Add("422", new OpenApiResponse
                {
                    Description = "Unprocessable Entity - Validation failed",
                    Content = GetErrorResponseContent()
                });
            }

            if (!operation.Responses.ContainsKey("500"))
            {
                operation.Responses.Add("500", new OpenApiResponse
                {
                    Description = "Internal Server Error - An unexpected error occurred",
                    Content = GetErrorResponseContent()
                });
            }

            // Add success responses based on HTTP method
            var httpMethod = context.ApiDescription.HttpMethod?.ToUpper();
            switch (httpMethod)
            {
                case "GET":
                    AddSuccessResponse(operation, "200", "Success - Data retrieved");
                    break;
                case "POST":
                    AddSuccessResponse(operation, "200", "Success - Resource created");
                    break;
                case "PUT":
                    AddSuccessResponse(operation, "200", "Success - Resource updated");
                    break;
                case "DELETE":
                    AddSuccessResponse(operation, "200", "Success - Resource deleted");
                    break;
            }
        }

        private static void AddSuccessResponse(OpenApiOperation operation, string statusCode, string description)
        {
            if (!operation.Responses.ContainsKey(statusCode))
            {
                operation.Responses.Add(statusCode, new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.Schema,
                                    Id = "ApiResponse"
                                }
                            }
                        }
                    }
                });
            }
        }

        private static Dictionary<string, OpenApiMediaType> GetErrorResponseContent()
        {
            return new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = "ApiResponse"
                        }
                    },
                    Example = new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                        ["message"] = new Microsoft.OpenApi.Any.OpenApiString("An error occurred"),
                        ["data"] = new Microsoft.OpenApi.Any.OpenApiNull(),
                        ["errors"] = new Microsoft.OpenApi.Any.OpenApiArray
                        {
                            new Microsoft.OpenApi.Any.OpenApiString("Detailed error message")
                        },
                        ["timestamp"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O"))
                    }
                }
            };
        }
    }

    /// <summary>
    /// Adds authorization information to Swagger operations
    /// </summary>
    public class SwaggerAuthorizationOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authorizeAttributes = context.MethodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true) ?? Array.Empty<object>())
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .ToList();

            var allowAnonymousAttributes = context.MethodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
                .ToList();

            if (allowAnonymousAttributes.Any())
            {
                // Remove security requirements for anonymous endpoints
                operation.Security?.Clear();
                return;
            }

            if (authorizeAttributes.Any())
            {
                var requiredRoles = authorizeAttributes
                    .Where(a => !string.IsNullOrEmpty(a.Roles))
                    .SelectMany(a => a.Roles!.Split(',').Select(r => r.Trim()))
                    .Distinct()
                    .ToList();

                var requiredPolicies = authorizeAttributes
                    .Where(a => !string.IsNullOrEmpty(a.Policy))
                    .Select(a => a.Policy!)
                    .Distinct()
                    .ToList();

                // Add description about required authorization
                var authDescription = new List<string>();
                
                if (requiredRoles.Any())
                {
                    authDescription.Add($"Required roles: {string.Join(", ", requiredRoles)}");
                }

                if (requiredPolicies.Any())
                {
                    authDescription.Add($"Required policies: {string.Join(", ", requiredPolicies)}");
                }

                if (authDescription.Any())
                {
                    operation.Description = (operation.Description ?? "") + 
                        "\n\n**Authorization:** " + string.Join(", ", authDescription);
                }
            }
        }
    }
}