using Microsoft.WindowsAzure.Storage.Table;

namespace Piraeus.Auditing
{
    public abstract class AuditRecord : TableEntity
    {
        public abstract string ConvertToCsv();

        public abstract string ConvertToJson();
    }
}