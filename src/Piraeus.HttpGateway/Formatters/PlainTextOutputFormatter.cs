using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Piraeus.HttpGateway.Formatters
{
    public class PlainTextOutputFormatter : OutputFormatter
    {
        private const string CONTENT_TYPE = "text/plain";

        public PlainTextOutputFormatter()
        {
            SupportedMediaTypes.Add(CONTENT_TYPE);
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            return context.HttpContext.Request.ContentType == CONTENT_TYPE;
        }

        public override Task WriteAsync(OutputFormatterWriteContext context)
        {
            return base.WriteAsync(context);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            await Task.CompletedTask;
        }
    }
}