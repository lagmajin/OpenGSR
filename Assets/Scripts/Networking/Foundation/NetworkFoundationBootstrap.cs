using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGS
{
    public static class NetworkFoundationBootstrap
    {
        public static void RegisterDefaultHandlers(NetworkRequestRouter router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            router.RegisterHandler<PingRequest, PingResponse>(NetworkFoundationRoutes.Ping, HandlePingAsync);
        }

        private static Task<PingResponse> HandlePingAsync(PingRequest request, CancellationToken _)
        {
            request ??= new PingRequest();

            var response = new PingResponse
            {
                Nonce = string.IsNullOrWhiteSpace(request.Nonce) ? Guid.NewGuid().ToString("N") : request.Nonce,
                EchoClientSentAtUtc = request.ClientSentAtUtc ?? string.Empty,
                ServerSentAtUtc = DateTime.UtcNow.ToString("O")
            };

            return Task.FromResult(response);
        }
    }
}
