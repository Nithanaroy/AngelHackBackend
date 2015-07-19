using Newtonsoft.Json.Linq;
using NReco.VideoConverter;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace AngelHack
{
    class Program
    {
        static void Main(string[] args)
        {
            var f = new FFMpegConverter();

            // Get thumbnail at a specified time in seconds
            Console.WriteLine("Generating image...");
            f.GetVideoThumbnail(@"C:\Users\npasumarthy\Downloads\Test.mp4", @"C:\Users\npasumarthy\Downloads\TestThumbnail.jpg", 3);

            //Extract Audio from Video
            Console.WriteLine("Extracting Audio...");
            f.ConvertMedia(@"C:\Users\npasumarthy\Downloads\Test.mp4", @"C:\Users\npasumarthy\Downloads\Test2.mp3", "mp3");

            // OCR the image
            // OcrFromUrl();
            Console.WriteLine("OCR...");
            String filename = @"C:\Users\npasumarthy\Downloads\TestThumbnail.jpg";
            //filename = @"C:\Users\npasumarthy\Downloads\Demo.jpg";
            //filename = @"C:\Users\npasumarthy\Downloads\Demo2.jpg";
            var textFromImage = OcrFromFile(filename);
            Console.WriteLine(textFromImage);

            // Clean the Text retuned from OCR
            Console.WriteLine("Cleaning the OCR result");
            var cleanedText = textFromImage.Replace(@"\n", "");

            // Save the audio file and OCR file as response to client
            Console.WriteLine("OCR to Audio...");
            String audioUrl = TextToSpeech(textFromImage);
            Console.WriteLine("\n\n" + audioUrl + "\n\n");
            Process.Start("wmplayer.exe", audioUrl);
        }

        public static String TextToSpeech(String data)
        {
            string sURL;
            sURL = "http://tts-api.com/tts.mp3?q=" + data + "&return_url=1";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(sURL);
            // Console.WriteLine(sURL);
            httpWebRequest.Method = "GET";
            var r = new StreamReader(httpWebRequest.GetResponse().GetResponseStream());
            var res = r.ReadToEnd();
            String result = res.ToString();
            return result;
        }

        private static String OcrFromFile(String filename)
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apikey", "83b70b5b-4313-4f84-be19-d269f561af5b");
            var res = HttpUploadFile("https://api.idolondemand.com/1/api/sync/ocrdocument/v1", filename, "file", "image/jpeg", nvc);
            JObject o = JObject.Parse(res);
            return o["text_block"][0]["text"].ToString();
        }

        public static String OcrFromUrl()
        {
            String url = "https://api.idolondemand.com/1/api/sync/ocrdocument/v1?url=https%3A%2F%2Fdl.dropboxusercontent.com%2Fu%2F95923404%2FTestThumbnail.jpg&apikey=83b70b5b-4313-4f84-be19-d269f561af5b&_=1437291557068";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";
            httpWebRequest.Accept = "application/json";

            //Console.WriteLine("Request sent...");

            var r = new StreamReader(httpWebRequest.GetResponse().GetResponseStream());
            var res = r.ReadToEnd();

            JObject o = JObject.Parse(res);
            return o["text_block"][0]["text"].ToString();
        }

        public static String HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
        {
            // credits: http://stackoverflow.com/a/2996904

            String res = null;

            Console.WriteLine(string.Format("Uploading {0} to {1}", file, url));
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            //wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                res = reader2.ReadToEnd();
                //Console.WriteLine(string.Format("File uploaded, server response is: {0}", res));
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error uploading file", ex);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            return res;
        }
    }
}
