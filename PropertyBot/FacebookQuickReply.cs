using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    class FacebookQuickReply
    {
        public FacebookQuickReply(string contentType)
        {
            this.ContentType = contentType;
        }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }
    }
}
