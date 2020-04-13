using System;
using System.Threading.Tasks;

namespace SkunkLab.Protocols.Coap
{
    public interface ICoapRequestDispatch : IDisposable
    {
        Task<CoapMessage> DeleteAsync(CoapMessage message);

        Task<CoapMessage> GetAsync(CoapMessage message);

        Task<CoapMessage> ObserveAsync(CoapMessage message);

        Task<CoapMessage> PostAsync(CoapMessage message);

        Task<CoapMessage> PutAsync(CoapMessage message);
    }
}