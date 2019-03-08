using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MultiDialogsWithAccessorBotV4.BotAccessor;
using Twilio;
using Twilio.Exceptions;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace Bot_Builder_Simplified_Echo_Bot_V4
{
    public class LocationWaterfallDialog : WaterfallDialog
    {
        public static string DialogId { get; } = "locationDialog";

        public static string LocationEntity { get; set; }

        public static string PhoneEntity { get; set; }

        public static LocationWaterfallDialog BotInstance { get; } = new LocationWaterfallDialog(DialogId, null);

        public LocationWaterfallDialog(string dialogId, IEnumerable<WaterfallStep> steps)
            : base(dialogId, steps)
        {
            AddStep(GetLocationEntityAsync);
            AddStep(GetAddressForLocationAsync);
            AddStep(RequestTextAsync);
            AddStep(GetPhoneAsync);
            AddStep(SendTextAsync);
        }

        public async Task<DialogTurnResult> GetLocationEntityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(LocationEntity))
            {
                return await stepContext.PromptAsync(
                    "dialogLocationCollection",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"I'm glad to help.  What location are you traveling to?"),
                        RetryPrompt = MessageFactory.Text($"I'm not sure I recognize that location, could you send me the name again?"),

                    },
                    cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> GetAddressForLocationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can help you with that."));

            string address = PplLocations.ContainsKey(LocationEntity) ? PplLocations[LocationEntity] : string.Empty;
            if (!string.IsNullOrEmpty(address))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{address}"));
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry, I don't seem to have an address for that location, is there anywhere else I can help you find?"));
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        public async Task<DialogTurnResult> RequestTextAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                    "dialogPhoneCollection",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Would you like me to text that information to you?"),
                    },
                    cancellationToken);
        }

        public async Task<DialogTurnResult> GetPhoneAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                return await stepContext.PromptAsync(
                    "dialogPhoneSelection",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Ok! Which phone number would you like me to send that to?"),
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Desk", "Mobile", "Other" }),
                    },
                    cancellationToken);
        }

        public async Task<DialogTurnResult> SendTextAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var phone = PhoneNumbers.ContainsKey((stepContext.Result as FoundChoice)?.Value) ? PhoneNumbers[(stepContext.Result as FoundChoice)?.Value] : "6101234567";

            try
            {
                string accountSID = "AC039559141884d03c96a0f57f91357332";
                string authToken = "1e15e552340d63875be0aecb475867b7";

                // Initialize the TwilioClient.
                TwilioClient.Init(accountSID, authToken);

                // Send message via Twilio
                var message = MessageResource.Create(
                    to: new PhoneNumber($"+1{phone}"),
                    from: new PhoneNumber("+12674777516"),
                    body: $"{PplLocations[LocationEntity]}");
            }
            catch(TwilioException ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry, I unable to send the location to {phone}."));
                LocationEntity = null;
                return await stepContext.ReplaceDialogAsync(RootWaterfallDialog.DialogId, null, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I have successfully sent the location to {phone}."));
            LocationEntity = null;
            return await stepContext.ReplaceDialogAsync(RootWaterfallDialog.DialogId, null, cancellationToken);
        }

        public static async Task<bool> ValidateLocationAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return !string.IsNullOrWhiteSpace(LocationEntity) && PplLocations.ContainsKey(LocationEntity);
        }

        private static Dictionary<string, string> PplLocations { get; } = new Dictionary<string, string>()
        {
            { "Global Office", "2 N 9th St\r\nAllentown, PA 18101" },
            { "Lehigh", "827 Hausman Rd\r\nAllentown, PA 18104" },
            { "Windsor", "7231 Windsor Dr\r\nAllentown, PA 18106" },
            { "Scranton", "600 Larch St\r\nScranton, PA 18509" },
            { "Walbert", "1639 Church Rd\r\nAllentown, PA 18104" },
        };

        private Dictionary<string, string> PhoneNumbers { get; } = new Dictionary<string, string>()
        {
            { "Desk", "6107307598" },
            { "Mobile", "6107307597" },
            { "Other", "6107307596" },
        };
    }
}
