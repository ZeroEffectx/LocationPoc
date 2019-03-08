// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MultiDialogsWithAccessorBotV4.BotAccessor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimplifiedWaterfallDialogBotV4;
using SimplifiedWaterfallDialogBotV4.BotAccessor;

namespace Bot_Builder_Simplified_Echo_Bot_V4
{
    public class MultiDialogWithAccessorBot : IBot
    {
        private readonly DialogSet _dialogSet;
        private readonly DialogBotConversationStateAndUserStateAccessor _dialogBotConversationStateAndUserStateAccessor;

        public DialogBotConversationStateAndUserStateAccessor DialogBotConversationStateAndUserStateAccessor { get; set; }

        public static readonly string QnAMakerKey = "RoyaltyInfo2018";
        private readonly BotServices _services;

        public static string LuisKey { get; } = "LocationPOC";

        public MultiDialogWithAccessorBot(DialogBotConversationStateAndUserStateAccessor accessor, BotServices services)
        {
            _dialogBotConversationStateAndUserStateAccessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _dialogSet = new DialogSet(_dialogBotConversationStateAndUserStateAccessor.ConversationDialogState);
            _dialogSet.Add(RootWaterfallDialog.BotInstance);

            _dialogSet.Add(LocationWaterfallDialog.BotInstance);
            _dialogSet.Add(new TextPrompt("dialogLocationCollection", LocationWaterfallDialog.ValidateLocationAsync));
            _dialogSet.Add(new TextPrompt("dialogIntentCollection"));
            _dialogSet.Add(new TextPrompt("dialogPhoneCollection"));
            _dialogSet.Add(new ChoicePrompt("dialogPhoneSelection"));

            _dialogSet.Add(new ChoicePrompt("dialogChoice"));

            DialogBotConversationStateAndUserStateAccessor = accessor;

            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!_services.QnAServices.ContainsKey(QnAMakerKey))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a QnA service named '{QnAMakerKey}'.");
            }

            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new ArgumentException($"Invalid Configuration.  Please check your .bot file for a Luis service named '{LuisKey}'.");
            }

        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var botState = await DialogBotConversationStateAndUserStateAccessor.TheUserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            // qna
            var myWelcomeUserState = await DialogBotConversationStateAndUserStateAccessor.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState(), cancellationToken);

            turnContext.TurnState.Add("DialogBotConversationStateAndUserStateAccessor", DialogBotConversationStateAndUserStateAccessor);

            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Run the DialogSet - let the framework identify the current state of the dialog from
                // the dialog stack and figure out what (if any) is the active dialog.
                var dialogContext = await _dialogSet.CreateContextAsync(turnContext, cancellationToken);

                if (dialogContext != null)
                {
                    var luisResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                    var topIntent = luisResult?.GetTopScoringIntent();
                    if (topIntent != null && topIntent.HasValue && topIntent.Value.intent == "Cancel")
                    {
                        LocationWaterfallDialog.LocationEntity = null;
                        await dialogContext.ReplaceDialogAsync(RootWaterfallDialog.DialogId, null, cancellationToken);
                    }

                    if (dialogContext.ActiveDialog != null)
                    {
                        if (dialogContext.ActiveDialog.Id == "dialogIntentCollection")
                        {
                            if (luisResult != null && luisResult.Intents != null && luisResult.Intents.Count > 0)
                            {
                                if (luisResult.Entities != null && luisResult.Entities.Count > 1)
                                {
                                    var test = luisResult.Entities["Locations"];
                                    LocationWaterfallDialog.LocationEntity = test.First.First.Value<string>();
                                }

                                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent == "FindLocation")
                                {
                                    await dialogContext.BeginDialogAsync(LocationWaterfallDialog.DialogId, null, cancellationToken);
                                }
                            }
                        }

                        if (dialogContext.ActiveDialog.Id == "dialogLocationCollection")
                        {
                            if (luisResult.Entities != null && luisResult.Entities.Count > 1)
                            {
                                var test = luisResult.Entities["Locations"];
                                LocationWaterfallDialog.LocationEntity = test.First.First.Value<string>();
                            }
                        }

                        if (dialogContext.ActiveDialog.Id == "dialogPhoneCollection")
                        {
                            if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None" && !(topIntent.Value.intent == "Affirmative"))
                            {
                                LocationWaterfallDialog.LocationEntity = null;
                                await dialogContext.ReplaceDialogAsync(RootWaterfallDialog.DialogId, null, cancellationToken);
                            }
                        }
                    }
                }

                if (dialogContext.ActiveDialog == null)
                {
                    await dialogContext.BeginDialogAsync(RootWaterfallDialog.DialogId, null, cancellationToken);
                }
                else
                {
                    await dialogContext.ContinueDialogAsync(cancellationToken);
                }

                await _dialogBotConversationStateAndUserStateAccessor.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await _dialogBotConversationStateAndUserStateAccessor.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
        }
    }
}