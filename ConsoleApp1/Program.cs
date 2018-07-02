using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Web;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpeechToTextAzure
{
    public class Authentication
    {
        public static readonly string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";
        private string subscriptionKey;
        private string token;
        private Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        public Authentication(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
            this.token = FetchToken(FetchTokenUri, subscriptionKey).Result;

            // renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));
        }

        public string GetAccessToken()
        {
            return this.token;
        }

        private void RenewAccessToken()
        {
            this.token = FetchToken(FetchTokenUri, this.subscriptionKey).Result;
            Console.WriteLine("Renewed token.");
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                return await result.Content.ReadAsStringAsync();
            }
        }
        public Boolean ValidarCertificado(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
    public class Program
    {

        public static void Main(string[] args)
        {
            string[] audios;
            string b;
            Console.WriteLine("Ingresa ruta de la carpeta de audios");
            audios = Directory.GetFiles(Console.ReadLine(),"*.wav");
            Console.WriteLine("Ingresa la ruta de la carpeta a guardar los Txt");
            string txt = Console.ReadLine();
            for (int i = 0; i < audios.Length; i++)
            {
                //Console.WriteLine(audios[i]);
                Authentication authentication = new Authentication("");
                args = new string[2];
                args[0] = " https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?language=es-mx&format=default";//https://speech.platform.bing.com/recognize";
                args[1] = audios[i];
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(authentication.ValidarCertificado);
                if ((args.Length < 2) || (string.IsNullOrWhiteSpace(args[0])))
                {
                    Console.WriteLine("Arg[0]: Specify the endpoint to hit https://speech.platform.bing.com/recognize");
                    Console.WriteLine("Arg[1]: Specify a valid input wav file.");
                    return;
                }
                Authentication auth = new Authentication("105a2b1fc08745a6b22b50ad024a3562");
                string requestUri = args[0];/*.Trim(new char[] { '/', '?' });*/
                string host = @"speech.platform.bing.com";
                string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";
                string audioFile = args[1];
                string responseString;
                FileStream fs = null;
                try
                {
                    var token = auth.GetAccessToken();
                    HttpWebRequest request = null;
                    request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
                    request.SendChunked = true;
                    request.Accept = @"application/json;text/xml";
                    request.Method = "POST";
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.Host = host;
                    request.ContentType = contentType;
                    request.Headers["Authorization"] = "Bearer " + token;
                    using (fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = null;
                        int bytesRead = 0;
                        using (Stream requestStream = request.GetRequestStream())
                        {
                            buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                requestStream.Write(buffer, 0, bytesRead);
                            }
                            requestStream.Flush();
                        }
                        Console.WriteLine("Texto:");
                        using (WebResponse response = request.GetResponse())
                        {
                            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                            {
                                responseString = sr.ReadToEnd();
                                JObject objert = JObject.Parse(responseString);
                                b = (string)objert["DisplayText"];
                            }
                            string name = Path.GetFileName(audioFile);
                            DateTime dt = File.GetLastWriteTime(audioFile);
                            name = name.Remove(name.Length -4);
                            string s = dt.ToString(" dddd,dd - MMMM - yyyy hh.mm.ss tt");
                            using (System.IO.StreamWriter escritor = new System.IO.StreamWriter(txt+@"\"+name+ s +".txt"))
                            {
                                escritor.WriteLine(b);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                    Console.ReadLine();
                }
            }
        }

    }

}