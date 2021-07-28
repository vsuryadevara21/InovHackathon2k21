using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text.Sequence;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextConsole
{
    public class Program
    {
        private const string DefaultCulture = Culture.English;
        public async static Task Main(string[] args)
        {
            //Creates an instance of a speech config with specified subscription key and service region.
            //Replace with your own subscription key and service region(e.g., "westus").
            var speechConfig = SpeechConfig.FromSubscription("91da690e7e62478e8ddbce540b1a0562", "eastus");
            await ContinuousRecognitionWithMicAndPhraseListsAsync(speechConfig);

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //ShowIntro();

            //while (true)
            //{
            //    // Read the text to recognize
            //    Console.WriteLine("Enter the text to recognize:");
            //    var input = Console.ReadLine()?.Trim();
            //    Console.WriteLine();

            //    if (input?.ToLower(CultureInfo.InvariantCulture) == "exit")
            //    {
            //        // Close application if user types "exit"
            //        break;
            //    }

            //    // Validate input
            //    if (input?.Length > 0)
            //    {
            //        // Retrieve all the parsers and call 'Parse' to recognize all the values from the user input
            //        var results = ParseAll(input, DefaultCulture);

            //        // Write output
            //        Console.WriteLine(results.Any() ? $"I found the following entities ({results.Count():d}):" : "I found no entities.");
            //        results.ToList().ForEach(result => Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented)));
            //        Console.WriteLine();
            //    }
            //}

        }

        //public async static Task FromMic(SpeechConfig speechConfig)
        //{
        //    using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        //    using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        //    Console.WriteLine("Speak into your microphone.");
        //    var result = await recognizer.RecognizeOnceAsync();
        //    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
        //}

        public static async Task ContinuousRecognitionWithMicAndPhraseListsAsync(SpeechConfig speechConfig)
        {
            var stopRecognition = new TaskCompletionSource<int>();

            // Creates a speech recognizer using file as audio input.
            // Replace with your own audio file name.

            using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
            {
                using (var recognizer = new SpeechRecognizer(speechConfig, audioInput))
                {
                    // Subscribes to events.
                    recognizer.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            var number = NumberRecognizer.RecognizeNumber(e.Result.Text,Culture.English);

                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\n    Session started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Before starting recognition, add a phrase list to help recognition.
                    PhraseListGrammar phraseListGrammar = PhraseListGrammar.FromRecognizer(recognizer);
                    phraseListGrammar.AddPhrase("SELECT TOP 1000 * FROM [SalesLT].[Customer]");
                    phraseListGrammar.AddPhrase("SELECT CustomerID FROM SalesLT.Customer WHERE CustomerID = 10");
                    
                    Console.WriteLine("Speak into your microphone.");
                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                    
                }

                
            }
        }

        private static IEnumerable<ModelResult> ParseAll(string query, string culture)
        {
            return MergeResults(new List<ModelResult>[]
                {
                // Number recognizer will find any number from the input
                // E.g "I have two apples" will return "2".
                NumberRecognizer.RecognizeNumber(query, culture),

                // Ordinal number recognizer will find any ordinal number
                // E.g "eleventh" will return "11".
                NumberRecognizer.RecognizeOrdinal(query, culture),

                // Percentage recognizer will find any number presented as percentage
                // E.g "one hundred percents" will return "100%"
                NumberRecognizer.RecognizePercentage(query, culture),

                // Number Range recognizer will find any cardinal or ordinal number range
                // E.g. "between 2 and 5" will return "(2,5)"
                NumberRecognizer.RecognizeNumberRange(query, culture),

                // Age recognizer will find any age number presented
                // E.g "After ninety five years of age, perspectives change" will return "95 Year"
                NumberWithUnitRecognizer.RecognizeAge(query, culture),

                // Currency recognizer will find any currency presented
                // E.g "Interest expense in the 1988 third quarter was $ 75.3 million" will return "75300000 Dollar"
                NumberWithUnitRecognizer.RecognizeCurrency(query, culture),

                // Dimension recognizer will find any dimension presented
                // E.g "The six-mile trip to my airport hotel that had taken 20 minutes earlier in the day took more than three hours." will return "6 Mile"
                NumberWithUnitRecognizer.RecognizeDimension(query, culture),

                // Temperature recognizer will find any temperature presented
                // E.g "Set the temperature to 30 degrees celsius" will return "30 C"
                NumberWithUnitRecognizer.RecognizeTemperature(query, culture),

                // Datetime recognizer This model will find any Date even if its write in colloquial language
                // E.g "I'll go back 8pm today" will return "2017-10-04 20:00:00"
                DateTimeRecognizer.RecognizeDateTime(query, culture),

                // PhoneNumber recognizer will find any phone number presented
                // E.g "My phone number is ( 19 ) 38294427."
                SequenceRecognizer.RecognizePhoneNumber(query, culture),

                // Add IP recognizer - This recognizer will find any Ipv4/Ipv6 presented
                // E.g "My Ip is 8.8.8.8"
                SequenceRecognizer.RecognizeIpAddress(query, culture),

                // Mention recognizer will find all the mention usages
                // E.g "@Cicero"
                SequenceRecognizer.RecognizeMention(query, culture),

                // Hashtag recognizer will find all the hash tag usages
                // E.g "task #123"
                SequenceRecognizer.RecognizeHashtag(query, culture),

                // Email recognizer will find all the emails
                // E.g "a@b.com"
                SequenceRecognizer.RecognizeEmail(query, culture),

                // URL recognizer will find all the urls
                // E.g "bing.com"
                SequenceRecognizer.RecognizeURL(query, culture),

                // GUID recognizer will find all the GUID usages
                // E.g "{123e4567-e89b-12d3-a456-426655440000}"
                SequenceRecognizer.RecognizeGUID(query, culture),

                // Quoted text recognizer
                // E.g "I meant "no""
                SequenceRecognizer.RecognizeQuotedText(query, culture),

                // Add Boolean recognizer - This model will find yes/no like responses, including emoji -
                // E.g "yup, I need that" will return "True"
                ChoiceRecognizer.RecognizeBoolean(query, culture),
                });
        }

        private static IEnumerable<ModelResult> MergeResults(params List<ModelResult>[] results)
        {
            return results.SelectMany(o => o);
        }

        /// <summary>
        /// Introduction.
        /// </summary>
        private static void ShowIntro()
        {
            Console.WriteLine("Welcome to the Recognizers' Sample console application!");
            Console.WriteLine("To try the recognizers enter a phrase and let us show you the different outputs for each recognizer or just type 'exit' to leave the application.");
            Console.WriteLine();
            Console.WriteLine("Here are some examples you could try:");
            Console.WriteLine();
            Console.WriteLine("\" I want twenty meters of cable for tomorrow\"");
            Console.WriteLine("\" I'll be available tomorrow from 11am to 2pm to receive up to 5kg of sugar\"");
            Console.WriteLine("\" I'll be out between 4 and 22 this month\"");
            Console.WriteLine("\" I was the fifth person to finish the 10 km race\"");
            Console.WriteLine("\" The temperature this night will be of 40 deg celsius\"");
            Console.WriteLine("\" The american stock exchange said a seat was sold for down $ 5,000 from the previous sale last friday\"");
            Console.WriteLine("\" It happened when the baby was only ten months old\"");
            Console.WriteLine();
        }
    }
}
