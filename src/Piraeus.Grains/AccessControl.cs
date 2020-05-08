using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Capl.Authorization;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    [Reentrant]
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class AccessControl : Grain<AccessControlState>, IAccessControl
    {
        public async Task ClearAsync()
        {
            await WriteStateAsync();
        }

        public async Task<AuthorizationPolicy> GetPolicyAsync()
        {
            AuthorizationPolicy policy = null;
            if (State.Policy != null)
            {
                using MemoryStream stream = new MemoryStream(State.Policy);
                using XmlReader reader = XmlReader.Create(stream);
                policy = AuthorizationPolicy.Load(reader);
                reader.Close();
            }

            return await Task.FromResult(policy);
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        public async Task UpsertPolicyAsync(AuthorizationPolicy policy)
        {
            XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true };
            StringBuilder builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder, settings))
            {
                policy.WriteXml(writer);
                writer.Flush();
                writer.Close();
            }

            State.Policy = Encoding.UTF8.GetBytes(builder.ToString());
            await WriteStateAsync();
        }
    }
}