using System.Diagnostics;
using System.Threading.Tasks;

namespace SkunkLab.Protocols.Coap.Handlers
{
    public class CoapPostHandler : CoapMessageHandler
    {
        public CoapPostHandler(CoapSession session, CoapMessage message, ICoapRequestDispatch dispatcher = null)
            : base(session, message, dispatcher)
        {
            session.EnsureAuthentication(message);
        }

        public override async Task<CoapMessage> ProcessAsync()
        {
            CoapMessage response = null;
            if (!Session.CoapReceiver.IsDup(Message.MessageId))
            {
                response = await Dispatcher.PostAsync(Message)
                    .ContinueWith(ExceptionAction, TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                if (Message.MessageType == CoapMessageType.Confirmable)
                {
                    return await Task.FromResult<CoapMessage>(new CoapResponse(Message.MessageId,
                        ResponseMessageType.Acknowledgement, ResponseCodeType.EmptyMessage));
                }
            }

            if (response != null && !Message.NoResponse.IsNoResponse(Message.Code))
            {
                return response;
            }

            return null;
        }

        private CoapMessage ExceptionAction(Task task)
        {
            Trace.WriteLine(task.Exception.Message);
            Trace.WriteLine(task.Exception.StackTrace);
            return null;
        }
    }
}