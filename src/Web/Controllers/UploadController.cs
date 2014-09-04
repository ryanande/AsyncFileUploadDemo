using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;

namespace Web.Controllers
{
    public class UploadController : ApiController
    {
        [HttpPost]
        [Route("~/api/upload")]
        public async Task<IEnumerable<string>> Post()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "Invalid Request!"));
            }

            string fullPath = HttpContext.Current.Server.MapPath("~/uploads");
            var streamProvider = new MongoDbMultipartFormDataStreamProvider(fullPath);

            try
            {
                await Request.Content.ReadAsMultipartAsync(streamProvider);
            }
            catch (Exception)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            var fileInfo = streamProvider.FileData.Select(i =>
            {
                var info = new FileInfo(i.LocalFileName.Replace("\"", ""));
                return "File uploaded as " + info.FullName;
            });
            return fileInfo;
        }
    }



    public class MongoDbMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        
        private readonly Collection<bool> _isFormData = new Collection<bool>();

        public MongoDbMultipartFormDataStreamProvider(string path) : base(path) { }


        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {

            // Content-Disposition header is required or form data
            ContentDispositionHeaderValue contentDisposition = headers.ContentDisposition;

            if (contentDisposition == null)
            {
                throw new InvalidOperationException("'Content-Disposition' header field in MIME multipart body part not found.");
            }


            if (string.IsNullOrEmpty(contentDisposition.FileName))
            {
                // We will post process this as form data
                _isFormData.Add(true);
                // filename parameter not found in the Content-Disposition header then return a memory stream
                return new MemoryStream();
            }

            // We won't post process files as form data
            _isFormData.Add(false);

            var fileData = new MultipartFileData(headers, contentDisposition.FileName);
            FileData.Add(fileData); //new Collection<MultipartFileData>();

            var url = MongoUrl.Create(ConfigurationManager.ConnectionStrings["MongoConnectionString"].ConnectionString);
            var db = new MongoClient(url)
                .GetServer()
                .GetDatabase(url.DatabaseName);

            db.GridFS.EnsureIndexes();
            MongoGridFSStream mongoStream = db.GridFS.Create("", new MongoGridFSCreateOptions
            {
                Id = BsonValue.Create(Guid.NewGuid()),
                Metadata = new BsonDocument(new Dictionary<string, object>() { { "fileName", contentDisposition.FileName } }),
                UploadDate = DateTime.UtcNow,
            });


            return mongoStream;

        }


        /// <summary>
        /// Read the non-file contents as form data.
        /// </summary>
        /// <returns></returns>
        public override async Task ExecutePostProcessingAsync()
        {
            for (var index = 0; index < Contents.Count; index++)
            {
                if (!_isFormData[index])
                    continue;

                HttpContent formContent = Contents[index];
                // Extract name from Content-Disposition header. We know from earlier that the header is present.
                ContentDispositionHeaderValue contentDisposition = formContent.Headers.ContentDisposition;
                var formFieldName = UnquoteToken(contentDisposition.Name) ?? string.Empty;

                // Read the contents as string data and add to form data
                string formFieldValue = await formContent.ReadAsStringAsync();
                FormData.Add(formFieldName, formFieldValue);
            }
        }


        private static string UnquoteToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }
    }


    public class FileData
    {
        public FileData(HttpContentHeaders headers, string awsFileUrl)
        {
            Headers = headers;
            AwsFileUrl = awsFileUrl;
        }

        public HttpContentHeaders Headers { get; private set; }

        public string AwsFileUrl { get; private set; }
    }
}