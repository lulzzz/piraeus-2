using System.Collections.Generic;
using System.Threading.Tasks;
using SkunkLab.Storage;

namespace Piraeus.Auditing
{
    public class AzureTableAuditor : IAuditor
    {
        private readonly TableStorage storage;

        private readonly string tableName;

        public AzureTableAuditor(string connectionString, string tableName, long? maxBufferSize = null,
            int? defaultBufferSize = null)
        {
            if (!maxBufferSize.HasValue)
            {
                storage = TableStorage.CreateSingleton(connectionString);
            }
            else
            {
                storage = TableStorage.CreateSingleton(connectionString, maxBufferSize.Value, defaultBufferSize.Value);
            }

            this.tableName = tableName;
        }

        public async Task UpdateAuditRecordAsync(AuditRecord record)
        {
            if (record is UserAuditRecord userRecord)
            {
                List<UserAuditRecord> list =
                    await storage.ReadAsync<UserAuditRecord>(tableName, record.PartitionKey, record.RowKey);
                if (list?.Count == 1)
                {
                    UserAuditRecord updateRecord = list[0];
                    updateRecord.LogoutTime = userRecord.LogoutTime;
                    storage.WriteAsync(tableName, updateRecord).IgnoreException();
                }
            }
        }

        public async Task WriteAuditRecordAsync(AuditRecord record)
        {
            storage.WriteAsync(tableName, record).IgnoreException();
            await Task.CompletedTask;
        }
    }
}