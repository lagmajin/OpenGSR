using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    public sealed class NetworkRequestRouter
    {
        private sealed class RouteHandler
        {
            public Func<JToken, CancellationToken, Task<JToken>> Handler;
        }

        private readonly Dictionary<string, RouteHandler> handlersByRoute = new Dictionary<string, RouteHandler>(StringComparer.Ordinal);
        private readonly object syncRoot = new object();

        public void RegisterHandler<TRequest, TResponse>(string route, Func<TRequest, CancellationToken, Task<TResponse>> handler)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("Route is required.", nameof(route));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var routeHandler = new RouteHandler
            {
                Handler = async (payload, ct) =>
                {
                    TRequest request = payload != null ? payload.ToObject<TRequest>() : default;
                    TResponse response = await handler(request, ct);
                    return response != null ? JToken.FromObject(response) : JValue.CreateNull();
                }
            };

            lock (syncRoot)
            {
                handlersByRoute[route] = routeHandler;
            }
        }

        public bool CanHandle(JObject message)
        {
            if (message == null)
            {
                return false;
            }

            string messageType = message["MessageType"]?.ToString();
            if (!string.Equals(messageType, NetworkEnvelopeTypes.RequestMessageType, StringComparison.Ordinal))
            {
                return false;
            }

            string route = message["Route"]?.ToString();
            if (string.IsNullOrWhiteSpace(route))
            {
                return false;
            }

            lock (syncRoot)
            {
                return handlersByRoute.ContainsKey(route);
            }
        }

        public async Task<JObject> HandleAsync(JObject requestEnvelope, CancellationToken cancellationToken = default)
        {
            if (requestEnvelope == null)
            {
                throw new ArgumentNullException(nameof(requestEnvelope));
            }

            string requestId = requestEnvelope["RequestId"]?.ToString() ?? string.Empty;
            string route = requestEnvelope["Route"]?.ToString();

            if (string.IsNullOrWhiteSpace(requestId))
            {
                return CreateErrorResponse(string.Empty, route, "INVALID_REQUEST_ID", "RequestId is required.");
            }

            if (string.IsNullOrWhiteSpace(route))
            {
                return CreateErrorResponse(requestId, route, "INVALID_ROUTE", "Route is required.");
            }

            RouteHandler handler;
            lock (syncRoot)
            {
                handlersByRoute.TryGetValue(route, out handler);
            }

            if (handler == null)
            {
                return CreateErrorResponse(requestId, route, "ROUTE_NOT_FOUND", $"No handler registered for route '{route}'.");
            }

            try
            {
                JToken payload = requestEnvelope["Payload"];
                JToken responsePayload = await handler.Handler(payload, cancellationToken);
                return CreateSuccessResponse(requestId, route, responsePayload);
            }
            catch (NetworkRequestException ex)
            {
                return CreateErrorResponse(requestId, route, ex.ErrorCode ?? "REQUEST_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(requestId, route, "INTERNAL_ERROR", ex.Message);
            }
        }

        public static JObject CreateSuccessResponse(string requestId, string route, JToken payload)
        {
            return new JObject
            {
                ["MessageType"] = NetworkEnvelopeTypes.ResponseMessageType,
                ["RequestId"] = requestId ?? string.Empty,
                ["Route"] = route ?? string.Empty,
                ["Success"] = true,
                ["ErrorCode"] = string.Empty,
                ["ErrorMessage"] = string.Empty,
                ["SentAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["Payload"] = payload ?? JValue.CreateNull()
            };
        }

        public static JObject CreateErrorResponse(string requestId, string route, string errorCode, string errorMessage)
        {
            return new JObject
            {
                ["MessageType"] = NetworkEnvelopeTypes.ResponseMessageType,
                ["RequestId"] = requestId ?? string.Empty,
                ["Route"] = route ?? string.Empty,
                ["Success"] = false,
                ["ErrorCode"] = errorCode ?? "UNKNOWN",
                ["ErrorMessage"] = errorMessage ?? "Unknown error.",
                ["SentAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["Payload"] = JValue.CreateNull()
            };
        }
    }
}
