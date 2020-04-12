using System.Threading.Tasks;

namespace Piraeus.Auditing
{
    public interface IAuditor
    {
        Task UpdateAuditRecordAsync(AuditRecord record);

        Task WriteAuditRecordAsync(AuditRecord record);
    }
}