using System.Text;
using System.Threading.Tasks;
using SkunkLab.Storage;

namespace Piraeus.Auditing
{
    public class FileAuditor : IAuditor
    {
        private readonly string path;

        private readonly LocalFileStorage storage;

        public FileAuditor(string path)
        {
            storage = LocalFileStorage.Create();
            this.path = path;
        }

        public async Task UpdateAuditRecordAsync(AuditRecord record)
        {
            byte[] source = Encoding.UTF8.GetBytes(record.ConvertToCsv());
            storage.AppendFileAsync(path, source, 100000).IgnoreException();
            await Task.CompletedTask;
        }

        public async Task WriteAuditRecordAsync(AuditRecord record)
        {
            byte[] source = Encoding.UTF8.GetBytes(record.ConvertToCsv());
            storage.AppendFileAsync(path, source, 100000).IgnoreException();
            await Task.CompletedTask;
        }
    }
}