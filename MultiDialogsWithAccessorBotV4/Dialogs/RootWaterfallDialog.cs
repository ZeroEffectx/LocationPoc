using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MultiDialogsWithAccessorBotV4.BotAccessor;

namespace Bot_Builder_Simplified_Echo_Bot_V4
{
    public class RootWaterfallDialog : WaterfallDialog
    {
        public static string DialogId { get; } = "rootDialog";

        public static RootWaterfallDialog BotInstance { get; } = new RootWaterfallDialog(DialogId, null);

        private static bool hasSeenIntro = false;

        public RootWaterfallDialog(string dialogId, IEnumerable<WaterfallStep> steps)
            : base (dialogId, steps)
        {
            //AddStep(FirstStepAsync);
            AddStep(PromptDialogChoiceStepAsync);
            //AddStep(LoopDialogStepAsync);
        }

        /*private static async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.NextAsync("Data from First Step", cancellationToken);
        }*/

        private static async Task<DialogTurnResult> PromptDialogChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var text = hasSeenIntro ? "Is there anything else I can help you with? " : "Hi, I'm Eddy!  I am here to help you anytime.";
            hasSeenIntro = true;

            return await stepContext.PromptAsync(
                "dialogIntentCollection",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(text),
                },
                cancellationToken);
        }

        /*private async Task<DialogTurnResult> LoopDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(3000);
            return await stepContext.ReplaceDialogAsync(RootWaterfallDialog.DialogId);
        }*/
    }
}
