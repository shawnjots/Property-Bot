using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SearchDialogs;

namespace Microsoft.Bot.Sample.JobListingBot.Dialogs
{
    [Serializable]
    public class IntroDialog : IDialog<IMessageActivity>
    {
        public IntroDialog()
        {

        }

        protected readonly SearchQueryBuilder queryBuilder = new SearchQueryBuilder();
        protected readonly SearchQueryBuilder queryBuilder1 = new SearchQueryBuilder();
        protected readonly SearchQueryBuilder queryBuilder2 = new SearchQueryBuilder();
        protected readonly SearchQueryBuilder queryBuilder3 = new SearchQueryBuilder();
        

        public Task StartAsync(IDialogContext context)
        {
            SearchDialogIndexClient.Schema = new SearchSchema().AddFields(
                new Field[] {
                    new Field() { Name = "Sort", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field() { Name = "Region", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "City", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "RentOrBuy", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Surbub", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Baths", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Price", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Location", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Amount", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Details", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Contact", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Rooms", Type = DataType.String, IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true },
                    new Field { Name = "Tags", Type = DataType.Collection(DataType.String), IsFacetable = true, IsFilterable = true, IsKey = false, IsRetrievable = true, IsSearchable = true, IsSortable = true }
                });
            context.Wait(SelectTitle);
            return Task.CompletedTask;
        }

        public Task SelectTitle(IDialogContext context, IAwaitable<IMessageActivity> input)
        {
            context.Call(new SearchRefineDialog(SearchDialogIndexClient.Schema.Fields["Sort"], queryBuilder, promptStyler: new FacebookQuickRepliesPromptStyler(), prompt: "What sort of property are you looking for?"),
                         SelectProvince);
            return Task.CompletedTask;
        }

        public  Task SelectProvince(IDialogContext context, IAwaitable<FilterExpression> input)
        {
            context.Call(new SearchRefineDialog(SearchDialogIndexClient.Schema.Fields["Region"], queryBuilder1, promptStyler: new FacebookQuickRepliesPromptStyler(), prompt: "Select a Province"), SelectCity);
            return Task.CompletedTask;
        }

        public Task SelectCity(IDialogContext context, IAwaitable<FilterExpression> input)
        {
            context.Call(new SearchRefineDialog(SearchDialogIndexClient.Schema.Fields["City"], queryBuilder2, promptStyler: new FacebookQuickRepliesPromptStyler(), prompt: "In what area in particular?"), RentOrBuy);
            return Task.CompletedTask;
        }

        public Task RentOrBuy(IDialogContext context, IAwaitable<FilterExpression> input)
        {
            context.Call(new SearchRefineDialog(SearchDialogIndexClient.Schema.Fields["RentOrBuy"], queryBuilder3, promptStyler: new FacebookQuickRepliesPromptStyler(), prompt: "Do you seek to rent or purchase?"), StartSearchDialog);
            return Task.CompletedTask;
        }



        public async Task StartSearchDialog(IDialogContext context, IAwaitable<FilterExpression> input)
        {
            await input; // We don't actually use the result from the previous step, it was reflected in the queryBuilder instance we're passing along
            context.Call(new PropertyDialog(this.queryBuilder3), Done);
        }

        public async Task Done(IDialogContext context, IAwaitable<IList<SearchHit>> input)
        {
            var selection = await input;
            string word;
            if (selection == null)
                
            {
                 word ="Alright. What next?";
            }
            else
            {
                //string list = string.Join(", ", selection.Select(s => s.Key));
                word = "Alright. What next?";
            }

            //context.Done<IMessageActivity>(null);

            var begin = context.MakeMessage();
            begin.Text = word;
            if (begin.ChannelId.Equals("facebook", StringComparison.InvariantCultureIgnoreCase))
            {
                var channelData = new FacebookChannelData
                {
                    QuickReplies = new[]
           {
                        new FacebookTextQuickReply("Change Area", "DEFINED_PAYLOAD_FOR_PICKING_AREA"),
                        new FacebookTextQuickReply("Change Province", "DEFINED_PAYLOAD_FOR_PICKING_PROVINCE"),
                        new FacebookTextQuickReply("Change Property", "DEFINED_PAYLOAD_FOR_PICKING_PROPERTY"),
                        new FacebookTextQuickReply("Change Rent/Buy", "DEFINED_PAYLOAD_FOR_PICKING_RENTORBUY"),
                        new FacebookTextQuickReply("Change Area", "DEFINED_PAYLOAD_FOR_PICKING_AREA"),
                        new FacebookTextQuickReply("Change Area", "DEFINED_PAYLOAD_FOR_PICKING_AREA"),
                        new FacebookTextQuickReply("Change Area", "DEFINED_PAYLOAD_FOR_PICKING_AREA")
                    }
                };

                begin.ChannelData = channelData;
            }
            await context.PostAsync(begin);
            context.Wait(MakeChoice);
        }

        protected virtual async Task MakeChoice(IDialogContext context, IAwaitable<IMessageActivity> input)
        {
            var activity = await input;
            var choice = activity.Text;
            switch (choice.ToLowerInvariant())
            {
                case ("change area"):
                    this.queryBuilder2.Reset();
                    await SelectCity(context, null);
                    break;

                case ("change province"):
                    this.queryBuilder1.Reset();
                    await SelectProvince(context, null);
                    break;

                case ("change property"):
                    this.queryBuilder.Reset();
                    await SelectTitle(context, null);
                    break;

                case ("change rent/buy"):
                    this.queryBuilder3.Reset();
                    await RentOrBuy(context, null);
                    break;

                default:
                    this.queryBuilder.Reset();
                    context.Done<IMessageActivity>(null);
                    break;
            }

        }
    }
}
