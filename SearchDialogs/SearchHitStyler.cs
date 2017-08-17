using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    [Serializable]
    public class SearchHitStyler : PromptStyler
    {
        public override void Apply<T>(ref IMessageActivity message, string prompt, IList<T> options)
        {
            var hits = options as IList<SearchHit>;
            if (hits != null)
            {
                var cards = hits.Select(h => new HeroCard
                {
                    Title = h.Title,
                    Images = new List<CardImage> { new CardImage(h.PictureUrl) },
                    Text = h.Description,
                    Subtitle = "Asking Price: " + "$"+ h.Amount,
                    Buttons = new List<CardAction> {
                        new CardAction { Type = "imBack", Title = "Get Details",Value = h.Key },
                        new CardAction { Type = "openUrl", Title = "View Images",Value = h.InstagramUrl },
                        new CardAction { Type = "call", Title = "Call Agent", Value = h.PhoneNumber } }
                });

                message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                message.Attachments = cards.Select(c => c.ToAttachment()).ToList();
                message.Text = prompt;
            }
            else
            {
                base.Apply<T>(ref message, prompt, options);
            }
        }
    }
}
