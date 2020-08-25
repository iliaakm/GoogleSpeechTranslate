using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Grpc.Auth;
using Grpc.Core;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleSpeech
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int valueWithAdd = INISetting.GetValueWithAdd<int>("Duration, s", 60);
            if (!Directory.Exists(Path.GetTempPath() + "\\speechtext"))
                Directory.CreateDirectory(Path.GetTempPath() + "\\speechtext");
            Task<object> task = Program.StreamingMicRecognizeAsync(valueWithAdd);
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                File.WriteAllText("error.txt", ex.ToString());
            }
        }

        private void RecongnizeFromFile()
        {
            SpeechClient speechClient = SpeechClient.Create();
            RecognitionConfig recognitionConfig = new RecognitionConfig()
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
                SampleRateHertz = 16000,
                LanguageCode = "en-US"
            };
            RecognitionAudio recognitionAudio = RecognitionAudio.FromFile("speech.wav");
            RecognitionConfig config = recognitionConfig;
            RecognitionAudio audio = recognitionAudio;
            foreach (SpeechRecognitionResult result in speechClient.Recognize(config, audio).Results)
            {
                foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                    Console.WriteLine(alternative.Transcript);
            }
        }

        private static async Task<object> StreamingMicRecognizeAsync(int seconds)
        {
            if (WaveIn.DeviceCount < 1)
            {
                File.WriteAllText("error.txt", "No microphone!");
                return (object)-1;
            }
            string lower = INISetting.GetValueWithAdd<string>("CredentialsFilePath", "credentials.json").ToLower();
            Console.WriteLine(lower);
            GoogleCredential googleCredential;
            using (Stream stream = (Stream)new FileStream(lower, FileMode.Open))
                googleCredential = GoogleCredential.FromStream(stream);
            SpeechClient.StreamingRecognizeStream streamingCall = SpeechClient.Create(new Channel(SpeechClient.DefaultEndpoint.Host, googleCredential.ToChannelCredentials())).StreamingRecognize();
            await streamingCall.WriteAsync(new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = 16000,
                        LanguageCode = "ru"
                    },
                    InterimResults = true
                }
            });
            Task printResponses = Task.Run((Func<Task>)(async () =>
           {
               string s = "";
               while (true)
               {
                   if (await streamingCall.ResponseStream.MoveNext(new CancellationToken()))
                   {
                       using (IEnumerator<StreamingRecognitionResult> enumerator1 = streamingCall.ResponseStream.Current.Results.GetEnumerator())
                       {
                           if (enumerator1.MoveNext())
                           {
                               using (IEnumerator<SpeechRecognitionAlternative> enumerator2 = enumerator1.Current.Alternatives.GetEnumerator())
                               {
                                   if (enumerator2.MoveNext())
                                   {
                                       SpeechRecognitionAlternative current = enumerator2.Current;
                                       Console.WriteLine(current.Transcript);
                                       s += current.Transcript;
                                   }
                               }
                           }
                       }
                       File.WriteAllText(Path.GetTempPath() + "\\speechtext\\speechtext.txt", s);
                       s = "";
                   }
                   else
                       break;
               }
           }));
            object writeLock = new object();
            bool writeMore = true;
            WaveInEvent waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable += (EventHandler<WaveInEventArgs>)((sender, args) =>
           {
               lock (writeLock)
               {
                   if (!writeMore)
                       return;
                   streamingCall.WriteAsync(new StreamingRecognizeRequest()
                   {
                       AudioContent = ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                   }).Wait();
               }
           });
            waveIn.StartRecording();
            Console.WriteLine("Speak now " + (object)seconds);
            await Task.Delay(TimeSpan.FromSeconds((double)seconds));
            waveIn.StopRecording();
            lock (writeLock)
                writeMore = false;
            await streamingCall.WriteCompleteAsync();
            await printResponses;
            return (object)0;
        }
    }
}
