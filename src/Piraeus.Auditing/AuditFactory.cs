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
            if (instance == null)
            {
                instance = new AuditFactory();
            }

            return instance;
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
            else
            {
                return messageAuditor;
            }
        }
    }
}