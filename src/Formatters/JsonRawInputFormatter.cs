using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class JsonRawInputFormatter : TextInputFormatter
    {
        public JsonRawInputFormatter() : this(new List<string>() { "application/json" })
        {
        }

        public JsonRawInputFormatter(IEnumerable<string> supportedMediaTypes)
        {
            foreach (var supportedMediaType in supportedMediaTypes)
            {
                SupportedMediaTypes.Add(supportedMediaType);
            }
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
        }
        protected override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            string data = null;
            using (var streamReader = context.ReaderFactory(context.HttpContext.Request.Body, encoding))
            {
                data = await streamReader.ReadToEndAsync();
            }
            return InputFormatterResult.Success(data);
        }
    }
#pragma warning restore 1591
}