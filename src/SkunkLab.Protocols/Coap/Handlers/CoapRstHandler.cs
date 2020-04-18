using System.Threading.Tasks;

namespace SkunkLab.Protocols.Coap.Handlers
{
    public class CoapRstHandler : CoapMessageHandler
    {
        public CoapRstHandler(CoapSession session, CoapMessage message)
            : base(session, message)
        {
        }

        public override async Task<CoapMessage> ProcessAsync()
        {
            Session.CoapSender.Remove(Message.MessageId);
            return await Task.FromResult(Message);
        }
    }
}