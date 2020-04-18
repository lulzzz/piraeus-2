using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SkunkLab.Channels.WebSocket
{
    public static class WebSocketExtensions
    {
        public static async Task<System.Net.WebSockets.WebSocket> AcceptWebSocketRequestAsync(this HttpContext context,
            WebSocketHandler handler)
        {
            System.Net.WebSockets.WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.ProcessWebSocketRequestAsync(socket);
            return socket;
        }
    }
}