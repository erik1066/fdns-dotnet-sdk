using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class JsonRawOutputFormatter : TextOutputFormatter
    {
        public JsonRawOutputFormatter()
        {
            SupportedMediaTypes.Add("application/json");
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;

            var buffer = new StringBuilder();
            buffer.Append(context.Object.ToString());
            return response.WriteAsync(buffer.ToString());
        }

        protected override bool CanWriteType(Type type)
        {
            return type == typeof(string);
        }
    }
#pragma warning restore 1591
}