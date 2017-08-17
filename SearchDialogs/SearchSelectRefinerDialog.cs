using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    [Serializable]
    public class SearchSelectRefinerDialog : IDialog<SearchField>
    {
        protected readonly SearchQueryBuilder queryBuilder;
        protected readonly IEnumerable<SearchField> refiners;
        protected readonly PromptStyler promptStyler;

        public SearchSelectRefinerDialog(IEnumerable<SearchField> refiners, SearchQueryBuilder queryBuilder = null, PromptStyler promptStyler = null)
        {
            if (refiners == null)
            {
                throw new ArgumentNullException("refiners");
            }

            this.refiners = refiners.ToList(); // make a local copy for serialization
            this.queryBuilder = queryBuilder ?? new SearchQueryBuilder();
            this.promptStyler = promptStyler;
        }

        public Task StartAsync(IDialogContext context)
        {
            PromptOptions<string> promptOptions = new PromptOptions<string>("What do you want to refine by?", options: this.refiners.Select(r => r.Name).ToList(), promptStyler: this.promptStyler);
            PromptDialog.Choice(context, ReturnSelection, promptOptions);
            return Task.CompletedTask;
        }

        protected virtual async Task ReturnSelection(IDialogContext context, IAwaitable<string> input)
        {
            var refiner = await input;
            var field = this.refiners.FirstOrDefault(r => string.Equals(r.Name, refiner, StringComparison.InvariantCultureIgnoreCase));
            context.Done(field);
        }
    }
}
