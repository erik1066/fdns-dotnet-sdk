using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public sealed class JsonRawInputFormatter : TextInputFormatter
    {
        public JsonRawInputFormatter()
        {
            SupportedMediaTypes.Add("application/json");
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
        }

        public JsonRawInputFormatter(IList<string> supportedMediaTypes) : this()
        {
            foreach (string supportedMediaType in supportedMediaTypes)
            {
                SupportedMediaTypes.Add(supportedMediaType);
            }
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