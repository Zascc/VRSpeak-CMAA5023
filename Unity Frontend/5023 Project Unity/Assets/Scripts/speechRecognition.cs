// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Microsoft.CognitiveServices.Speech;
// using Microsoft.CognitiveServices.Speech.Audio;
// using UnityEngine;

// public class speechRecognition
// {
//     // Start is called before the first frame update

//     static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
//     static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

//     public async void startRecognition(){
//         var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);        
//         speechConfig.SpeechRecognitionLanguage = "zh-CN";

//         using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
//         using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

//         Console.WriteLine("Speak into your microphone.");
//         var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
//         OutputSpeechRecognitionResult(speechRecognitionResult);
//     }

//     static void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
//     {
//         switch (speechRecognitionResult.Reason)
//         {
//             case ResultReason.RecognizedSpeech:
//                 Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
//                 break;
//             case ResultReason.NoMatch:
//                 Console.WriteLine($"NOMATCH: Speech could not be recognized.");
//                 break;
//             case ResultReason.Canceled:
//                 var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
//                 Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

//                 if (cancellation.Reason == CancellationReason.Error)
//                 {
//                     Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
//                     Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
//                     Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
//                 }
//                 break;
//         }
//     }

//     // Update is called once per frame

// }