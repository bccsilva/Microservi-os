using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunctionApp1.Entitity;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionApp1
{ 

    public static class Function1
    {

        #region Constantes utilizadas pelo serviço Cognitive Facial

        public const string endPoint = "https://westus.api.cognitive.microsoft.com/face/v1.0/detect";

        public const string keyCognitive = "5b53c2c0fdda406c83edc95513d42524";

        public const string parameter = "?returnFaceAttributes=emotion";
        #endregion

        #region Constantes utilizadas pelo serviço GoogleDrive

        private const string ApplicationName = "Durable Functions API";

        private const string ClientSecret = "zan77LMA__mALmyxAMY-PsWE";

        static string[] Scopes = { DriveService.Scope.Drive };

        private static UserCredential credential;

        #endregion 

        #region Microserviços       

        [FunctionName("fnGetImages")]        
        public static IEnumerable<string> GetImages([ActivityTrigger] string name, ILogger log)
        {   
            List<string> lista = new List<string>();

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();

            listRequest.PageSize = 100;            

            listRequest.Fields = "files(id,webViewLink,fileExtension,name,webContentLink)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
        
            if (files != null && files.Count > 0)
            {
                foreach (Google.Apis.Drive.v3.Data.File file in files.Where(x => x.FileExtension == "jpg"))
                {
                    lista.Add(file.WebContentLink);
                }
            } 
            return lista;
        }         

        /// <summary>
        /// Este microserviço realiza a leitura facial da imagem repassada pela url.
        /// </summary>
        /// <param name="url">Link da imagem no driver.</param>
        /// <param name="log"></param>
        /// <returns>Retorna um Json com as caracteristícas obtidas da face.</returns>
        [FunctionName("fnGetFacialSentiments")]
        public static async Task<string> GetFacialSentiments([ActivityTrigger] string url, ILogger log)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", keyCognitive);

            var json = "{\"url\":\"" + $"{url}" + "\"}";

            var resp = await client.PostAsync("https://westus.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceAttributes=emotion", new StringContent(json, Encoding.UTF8, "application/json"));

            var jsonResponse = await resp.Content.ReadAsStringAsync();                    

            return jsonResponse; 
        }
        #endregion 


        #region Métodos utilizados para orquestração dos microserviços.
        [FunctionName("Function1")]
        public static async Task RunOrchestrator(
          [OrchestrationTrigger] DurableOrchestrationContext context)
        {     

            var images =  await context.CallActivityAsync<IEnumerable<string>>("fnGetImages", string.Empty);

            foreach (var url in images) 
            {
                var jsonResponse = await context.CallActivityAsync<string>("fnGetFacialSentiments", url);

                var faces = JsonConvert.DeserializeObject<Face[]>(jsonResponse);
            }

        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
        #endregion 
    }
}