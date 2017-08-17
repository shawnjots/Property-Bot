using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;


namespace Microsoft.Bot.Sample.SearchDialogs
{
    public delegate string CanonicalizerDelegate(string propertyName);

    [Serializable]
    public abstract class SearchDialog : IDialog<IList<SearchHit>>
    {
        protected readonly SearchQueryBuilder queryBuilder;
        protected readonly PromptStyler hitStyler;
        protected readonly bool multipleSelection;
        protected readonly CanonicalizerDelegate canonicalizer;
        private readonly List<SearchHit> selected = new List<SearchHit>();

        protected bool firstPrompt = true;
        private List<SearchHit> found;

        public SearchDialog(SearchQueryBuilder queryBuilder = null, PromptStyler searchHitStyler = null, bool multipleSelection = false, CanonicalizerDelegate canonicalizer = null)
        {
            this.queryBuilder = queryBuilder ?? new SearchQueryBuilder();
            this.hitStyler = searchHitStyler ?? new SearchHitStyler();
            this.multipleSelection = multipleSelection;
            this.canonicalizer = canonicalizer;
        }

        public Task StartAsync(IDialogContext context)
        {
            return InitialPrompt(context);
        }

        protected virtual Task InitialPrompt(IDialogContext context)
        {
            string prompt = "Pick a type.";

            if (!this.firstPrompt)
            {
                prompt = "What type would you want?";
                if (this.multipleSelection)
                {
                   
                }
            }
            this.firstPrompt = false;

            var reply = context.MakeMessage();
            reply.Text = prompt;

            if (reply.ChannelId.Equals("facebook", StringComparison.InvariantCultureIgnoreCase))
            {
                var channelData = new FacebookChannelData
                {
                    QuickReplies = new[]
            {
                        new FacebookTextQuickReply("Best Deals", "DEFINED_PAYLOAD_FOR_PICKING_R7"),
                        new FacebookTextQuickReply("Family Home", "DEFINED_PAYLOAD_FOR_PICKING_R6"),
                        new FacebookTextQuickReply("Bachelor Pad", "DEFINED_PAYLOAD_FOR_PICKING_R5"),
                        new FacebookTextQuickReply("Newest Offers", "DEFINED_PAYLOAD_FOR_PICKING_R4"),
                        new FacebookTextQuickReply("Starter Home", "DEFINED_PAYLOAD_FOR_PICKING_R3"),
                        new FacebookTextQuickReply("Luxurious", "DEFINED_PAYLOAD_FOR_PICKING_R2"),
                        new FacebookTextQuickReply("6 Rooms", "DEFINED_PAYLOAD_FOR_PICKING_R8"),
                        new FacebookTextQuickReply("5 Rooms", "DEFINED_PAYLOAD_FOR_PICKING_R9"),
                        new FacebookTextQuickReply("4 Rooms", "DEFINED_PAYLOAD_FOR_PICKING_R10"),
                        new FacebookTextQuickReply("3 Rooms", "DEFINED_PAYLOAD_FOR_PICKING_R11")
                    }
                };
                reply.ChannelData = channelData;
            }

            var promptOptions = new PromptOptions<string>(
            prompt,
            options: new[] { "Best Deals", "Family Home", "Bachelor Pad", "Newest Offers",
                "Starter Home", "Luxurious", "6 Rooms", "5 Rooms", "4 Rooms", "3 Rooms" },
            promptStyler: new FacebookQuickRepliesPromptStyler());

            PromptDialog.Choice(context, Search, promptOptions);
            return Task.CompletedTask;
        }

        public async Task Search(IDialogContext context, IAwaitable<string> input)
        {
            string text = input != null ? await input : null;
            if (this.multipleSelection && text != null && text.ToLowerInvariant() == "list")
            {
                await InitialPrompt(context);
            }
            else
            {
                if (text != null)
                {
                    this.queryBuilder.SearchText = text;
                }

                var response = await ExecuteSearch();

                if (response.Results.Count == 0)
                {
                    await NoResultsConfirmRetry(context);
                }
                else
                {
                    var message = context.MakeMessage();
                    this.found = response.Results.Select(r => ToSearchHit(r)).ToList();
                    this.hitStyler.Apply(ref message,
                                         "I have a few options you may like 💡:",
                                         this.found);
                    await context.PostAsync(message);

                    var reply = context.MakeMessage();
                    reply.Text =  this.multipleSelection ? "Would you like to *refine* your options, search *again*, see *more* or are you *done?*" : "You can *refine* these options, search *again* or see *more*";

                    if (reply.ChannelId.Equals("facebook", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var channelData = new FacebookChannelData
                        {
                            QuickReplies = new[]
                    {
                        new FacebookTextQuickReply("Refine", "DEFINED_PAYLOAD_FOR_PICKING_REFIN"),
                        new FacebookTextQuickReply("🔎 Again", "DEFINED_PAYLOAD_FOR_PICKING_AGAI"),
                        new FacebookTextQuickReply("More", "DEFINED_PAYLOAD_FOR_PICKING_MOR"),
                        new FacebookTextQuickReply("Done", "DEFINED_PAYLOAD_FOR_PICKING_DON")
                    }
                        };
                        reply.ChannelData = channelData;
                    }

                    await context.PostAsync(reply);
                    context.Wait(ActOnSearchResults);
                }
            }
        }

        protected virtual Task NoResultsConfirmRetry(IDialogContext context)
        {
            var promptOptions = new PromptOptions<string>(
            "Sorry, I don't have that in particular. Would you like to retry your search?",
            options: new[] { "YES", "NO" },
            promptStyler: new FacebookQuickRepliesPromptStyler()
);

            PromptDialog.Choice(context, this.ShouldRetry, promptOptions);
            return Task.CompletedTask;
        }

        private async Task ShouldRetry(IDialogContext context, IAwaitable<string> input)
        {
            var retry = await input;
            var again = retry.ToLowerInvariant();

            if (again == "yes")
            {
                await InitialPrompt(context);
            }
            else if (again == "no")
            {
                context.Done<IList<SearchHit>>(null);
            }
        }

        private async Task ActOnSearchResults(IDialogContext context, IAwaitable<IMessageActivity> input)
        {
            var activity = await input;
            var choice = activity.Text;

            switch (choice.ToLowerInvariant())
            {
                case "again":
                case "reset":
                    this.queryBuilder.Reset();
                    await InitialPrompt(context);
                    break;

                case "🔎 more":
                    this.queryBuilder.PageNumber++;
                    await Search(context, null);
                    break;

                case "refine":
                    SelectRefiner(context);
                    break;


                case "done":
                    context.Done<IList<SearchHit>>(this.selected);
                    break;

                default:
                     await Details(context, choice);
                    break;
            }
        }


        protected virtual async Task Details(IDialogContext context, string selection)
        {
            SearchHit hit = found.Find(h => h.Key == selection);
            if (hit == null)
            {
                await UnkownActionOnResults(context, selection);
            }
            else
            {
                if (!this.selected.Exists(h => h.Key == hit.Key))
                {
                    if (hit.Description != "")
                    {
                        await context.PostAsync("Description: " + hit.Description);
                    }

                    if (hit.Contact != "")
                    {
                        await context.PostAsync("Contact: " + hit.Contact);
                    }
                }

                if (this.multipleSelection)
                {
                    var reply = context.MakeMessage();
                    reply.Text = "Do you want to continue searching for property in this place?";
                    if (reply.ChannelId.Equals("facebook", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var channelData = new FacebookChannelData
                        {
                            QuickReplies = new[]
                   {
                        new FacebookTextQuickReply("YES", "DEFINED_PAYLOAD_FOR_PICKING_YES"),
                        new FacebookTextQuickReply("NO", "DEFINED_PAYLOAD_FOR_PICKING_NO")
                    }
                        };

                        reply.ChannelData = channelData;
                    }
                    await context.PostAsync(reply);
                    context.Wait(MakeChoice);
                }
                else
                {
                    context.Done(this.selected);
                }
            }
        }

        protected virtual async Task MakeChoice(IDialogContext context, IAwaitable<IMessageActivity> input)
        {
            var activity = await input;
            var choice = activity.Text;
            switch (choice.ToLowerInvariant())
            {
                case ("no"):
                    context.Done(this.selected);
                    break;

                case ("yes"):
                    await InitialPrompt(context);
                    break;

                default:
                    await Details(context, choice);
                    break;
            }

        }

        protected virtual async Task UnkownActionOnResults(IDialogContext context, string action)
        {
            var reply = context.MakeMessage();
            reply.Text = "Hmmm...not sure what you mean. You can search *again*, *refine* or select one of the items above. Or are you *done*?";
            if (reply.ChannelId.Equals("facebook", StringComparison.InvariantCultureIgnoreCase))
            {
                var channelData = new FacebookChannelData
                {
                    QuickReplies = new[]
                   {
                        new FacebookTextQuickReply("Refine", "DEFINED_PAYLOAD_FOR_PICKING_REFINE"),
                        new FacebookTextQuickReply("🔎 Again", "DEFINED_PAYLOAD_FOR_PICKING_AGAIN"),
                        new FacebookTextQuickReply("Done", "DEFINED_PAYLOAD_FOR_PICKING_DONE")
                    }
                };

                reply.ChannelData = channelData;
            }

            await context.PostAsync(reply);


            context.Wait(ActOnSearchResults);
        }

        protected virtual async Task ShouldContinueSearching(IDialogContext context, IAwaitable<bool> input)
        {
            bool shouldContinue = await input;
            if (shouldContinue)
            {
                await InitialPrompt(context);
            }
            else
            {
                context.Done(this.selected);
            }
        }

        protected void SelectRefiner(IDialogContext context)
        { 
            var dialog = new SearchSelectRefinerDialog(GetTopRefiners().Select(r => SearchDialogIndexClient.Schema.Fields[r]), this.queryBuilder);
            context.Call(dialog, Refine);
        }

        protected async Task Refine(IDialogContext context, IAwaitable<SearchField> input)
        {
            SearchField refiner = await input;
            var dialog = new SearchRefineDialog(refiner, this.queryBuilder);
            context.Call(dialog, ResumeFromRefine);
        }

        protected async Task ResumeFromRefine(IDialogContext context, IAwaitable<FilterExpression> input)
        {
            await input; // refiner filter is already applied to the SearchQueryBuilder instance we passed in
            await Search(context, null);
        }

        protected Task<DocumentSearchResult> ExecuteSearch()
        {
            return SearchDialogIndexClient.Client.Documents.SearchAsync(queryBuilder.SearchText, queryBuilder.BuildParameters());
        }

        protected abstract string[] GetTopRefiners();

        protected abstract SearchHit ToSearchHit(SearchResult hit);
    }
}