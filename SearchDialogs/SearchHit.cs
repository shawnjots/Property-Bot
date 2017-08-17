using System;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    [Serializable]
    public class SearchHit
    {
        public string Key { get; set; }

        public string Title { get; set; }

        public string PictureUrl { get; set; }

        public string Description { get; set; }

        public string PhoneNumber { get; set; }

        public string Details { get; set; }

        public string Contact { get; set; }

        public string Amount { get; set; }

        public string InstagramUrl { get; set; }
    }
}