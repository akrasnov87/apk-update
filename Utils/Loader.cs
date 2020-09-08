using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ApkUpdate.Utils
{
    public class Loader
    {
        private const bool DEBUG = false;
        private static Loader loader;

        public static void CreateInstanse()
        {
            string host = DEBUG ? "http://localhost:3000" : "http://cic.it-serv.ru/dev";
            if (File.Exists("host.txt"))
            {
                host = File.ReadAllText("host.txt");
            }
            
            loader = new Loader(host);
        }

        public static Loader GetInstanse()
        {
            return loader;
        }

        private string token;
        private string host;

        private Loader(string host)
        {
            this.host = host;

            WebRequest request = WebRequest.Create(host + "/auth");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            String authData = "UserName=test&Password=test";
            Stream dataStream = request.GetRequestStream();
            byte[] bytes = Encoding.UTF8.GetBytes(authData);
            dataStream.Write(bytes, 0, bytes.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            // Get the stream containing content returned by the server.  
            // The using block ensures the stream is automatically closed.
            using (dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseFromServer);
                token = data.token;
            }

            // Close the response.  
            response.Close();
        }

        public dynamic RPC(string action, string method, string body)
        {
            WebRequest request = WebRequest.Create(host + "/rpc");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("rpc-authorization", "Token " + token);
            Stream dataStream = request.GetRequestStream();
            byte[] bytes = Encoding.UTF8.GetBytes("[{\"action\":\"" + action + "\", \"method\":\"" + method + "\", \"tid\":1, \"data\":" + body + ", \"type\":\"rpc\"}]");
            dataStream.Write(bytes, 0, bytes.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            // Get the stream containing content returned by the server.  
            // The using block ensures the stream is automatically closed.
            dynamic data;
            using (dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseFromServer);
            }

            // Close the response.  
            response.Close();
            return data;
        }

        public async Task<String> FileUpload(StorageFile file)
        {
            HttpClient httpClient = new HttpClient();
            MultipartFormDataContent form = new MultipartFormDataContent();

            var bytes = await GetBytesAsync(file);
            form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "client", "client");
            System.Net.Http.HttpResponseMessage response = await httpClient.PostAsync(host + "/upload/version", form);

            response.EnsureSuccessStatusCode();
            httpClient.Dispose();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetVersion()
        {
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = httpClient.GetAsync(host + "/upload/version").Result;

            response.EnsureSuccessStatusCode();
            httpClient.Dispose();
            string sd = await response.Content.ReadAsStringAsync();

            dynamic d = Newtonsoft.Json.JsonConvert.DeserializeObject(sd);
            return d.version;
        }

        public static async Task<byte[]> GetBytesAsync(StorageFile file)
        {
            byte[] fileBytes = null;
            if (file == null) return null;
            using (var stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }
    }
}
