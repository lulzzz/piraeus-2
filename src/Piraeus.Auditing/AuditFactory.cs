namespace Piraeus.Auditing
{
    public class AuditFactory : IAuditFactory
    {
        private static AuditFactory instance;

        private IAuditor messageAuditor;

        private IAuditor userAuditor;

        protected AuditFactory()
        {
        }

        public static IAuditFactory CreateSingleton()
        {
            return instance ??= new AuditFactory();
        }

        public void Add(IAuditor auditor, AuditType type)
        {
            if (type == AuditType.User)
            {
                userAuditor = auditor;
            }
            else
            {
                messageAuditor = auditor;
            }
        }

        public IAuditor GetAuditor(AuditType type)
        {
            if (type == AuditType.User)
            {
                return userAuditor;
            }

            return messageAuditor;
        }
    }
}