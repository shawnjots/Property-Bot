﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    class FacebookTextQuickReply : FacebookQuickReply
    {
        public FacebookTextQuickReply(string title, string payload, string imageUrl = null)
    : base("text")
        {
            this.Title = title;
            this.Payload = payload;
            this.ImageUrl = imageUrl;
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }
    }
}
