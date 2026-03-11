using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    public sealed class NetworkRequestClient
    {
        private sealed class PendingRequest
        {
            public TaskCompletionSource<JObject> CompletionSource;
        }

        private readonly Func<JObject, bool> sendMessage;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, PendingRequest> pendingByRequestId = new Dictionary<string, PendingRequest>();

        public NetworkRequestClient(Func<JObject, bool> sendMessage)
        {
            this.sendMessage = sendMessage ?? throw new ArgumentNullException(nameof(sendMessage));
        }

        public bool HandleIncomingMessage(JObject message)
        {
            if (message == null)
            {
                return false;
            }

            string messageType = message["MessageType"]?.ToString();
            if (!string.Equals(messageType, NetworkEnvelopeTypes.ResponseMessageType, StringComparison.Ordinal))
            {
                return false;
            }

            string requestId = message["RequestId"]?.ToString();
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            PendingRequest pending = null;
            lock (syncRoot)
            {
                if (pendingByRequestId.TryGetValue(requestId, out pending))
                {
                    pendingByRequestId.Remove(requestId);
                }
            }

            if (pending == null)
            {
                return false;
            }

            pending.CompletionSource.TrySetResult(message);
            return true;
        }

        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string route,
            TRequest payload,
            NetworkRequestOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            NetworkRequestOptions effectiveOptions = options ?? NetworkRequestOptions.Default;
            int maxAttempts = Mathf.Max(1, effectiveOptions.RetryCount + 1);
            int timeoutMs = Mathf.Max(100, effectiveOptions.TimeoutMs);

            Exception lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await SendSingleAttemptAsync<TRequest, TResponse>(route, payload, timeoutMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt >= maxAttempts)
                    {
                        break;
                    }
                }
            }

            throw lastException ?? new NetworkRequestException($"Request failed. route={route}");
        }

        private async Task<TResponse> SendSingleAttemptAsync<TRequest, TResponse>(
            string route,
            TRequest payload,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("Route is required.", nameof(route));
            }

            string requestId = Guid.NewGuid().ToString("N");
            var requestEnvelope = new JObject
            {
                ["MessageType"] = NetworkEnvelopeTypes.RequestMessageType,
                ["RequestId"] = requestId,
                ["Route"] = route,
                ["SentAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["Payload"] = payload != null ? JToken.FromObject(payload) : JValue.CreateNull()
            };

            var tcs = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (syncRoot)
            {
                pendingByRequestId[requestId] = new PendingRequest
                {
                    CompletionSource = tcs
                };
            }

            if (!sendMessage(requestEnvelope))
            {
                RemovePending(requestId);
                throw new NetworkRequestException($"Transport send failed. route={route}");
            }

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            using var registration = linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token));

            JObject responseEnvelope;
            try
            {
                responseEnvelope = await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                RemovePending(requestId);
                if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Request timeout. route={route}, timeoutMs={timeoutMs}");
                }

                throw;
            }
            finally
            {
                RemovePending(requestId);
            }

            bool success = responseEnvelope["Success"]?.ToObject<bool>() ?? false;
            if (!success)
            {
                string errorCode = responseEnvelope["ErrorCode"]?.ToString();
                string errorMessage = responseEnvelope["ErrorMessage"]?.ToString() ?? "Unknown error.";
                throw new NetworkRequestException(errorMessage, errorCode);
            }

            JToken payloadToken = responseEnvelope["Payload"];
            if (payloadToken == null || payloadToken.Type == JTokenType.Null)
            {
                return default;
            }

            return payloadToken.ToObject<TResponse>();
        }

        private void RemovePending(string requestId)
        {
            lock (syncRoot)
            {
                pendingByRequestId.Remove(requestId);
            }
        }
    }
}
