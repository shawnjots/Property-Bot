using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    class FacebookChannelData
    {
        [Newtonsoft.Json.JsonProperty("quick_replies")]
        public FacebookQuickReply[] QuickReplies { get; set; }
    }
}
