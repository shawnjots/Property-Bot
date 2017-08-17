using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SearchDialogs;

namespace Microsoft.Bot.Sample.JobListingBot.Dialogs
{
    [Serializable]
    public class PropertyDialog : SearchDialog
    {
        private static readonly string[] TopRefiners = { "Price", "Surbub", "Baths", "Rooms", "Size"};

        private static readonly Dictionary<string, string> map = new Dictionary<string, string> {
                { "bath", "baths"}, { "bathrooms", "baths"},
                {"bedroom", "beds" }, { "bedrooms", "beds" } };

        public PropertyDialog(SearchQueryBuilder queryBuilder) : base(queryBuilder, multipleSelection: true, canonicalizer: (prop) => map[prop])
        {
            SearchDialogIndexClient.Schema.Fields["Baths"].FilterPreference = PreferredFilter.MinValue;
            SearchDialogIndexClient.Schema.Fields["Rooms"].FilterPreference = PreferredFilter.MinValue;
            SearchDialogIndexClient.Schema.Fields["Price"].FilterPreference = PreferredFilter.Range;
            SearchDialogIndexClient.Schema.Fields["Size"].FilterPreference = PreferredFilter.RangeMax;
        }

        


        protected override string[] GetTopRefiners()
        {
            return TopRefiners;
        }

        protected override SearchHit ToSearchHit(SearchResult hit)
        {
            string description = (string)hit.Document["description"];

            return new SearchHit
            {
                Key = (string)hit.Document["id"],
                Title = GetTitleForItem(hit),
                PictureUrl = (string)hit.Document["picture_url"],
                InstagramUrl = (string)hit.Document["instagram_url"],
                Details = (string)hit.Document["Details"],
                Contact = (string)hit.Document["Contact"],
                Description = description.Length > 512 ? description.Substring(0, 512) + "..." : description

            };
        }

        private static string GetTitleForItem(SearchResult result)
        {
            return string.Format("{0} bedrooms, {1} bath in {2}, ${3:#,0}",
                                  result.Document["Beds"],
                                  result.Document["Baths"],
                                  result.Document["Location"],
                                  result.Document["Amount"]);
        }

        [Serializable]
        class JobStyler : PromptStyler
        {
            public override void Apply<T>(ref IMessageActivity message, string prompt, IList<T> options)
            {
                var hits = (IList<SearchHit>)options;
                var actions = hits.Select(h => new CardAction(ActionTypes.ImBack, h.Title, h.PictureUrl, h.Key)).ToList();
                var attachments = new List<Attachment>
                {
                    new HeroCard(text: prompt, buttons: actions).ToAttachment()
                };

                message.AttachmentLayout = AttachmentLayoutTypes.List;
                message.Attachments = attachments;
            }
        }
    }
}