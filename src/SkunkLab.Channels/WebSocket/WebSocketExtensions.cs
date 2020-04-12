namespace SkunkLab.Channels.WebSocket
{
    using Microsoft.AspNetCore.Http;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    public static class WebSocketExtensions
    {
        public static async Task<WebSocket> AcceptWebSocketRequestAsync(this HttpContext context, WebSocketHandler handler)
        {
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.ProcessWebSocketRequestAsync(socket);
            return socket;
        }
    }
}