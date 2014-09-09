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
                return "File uploaded as " + info.Name;
            });
            return fileInfo;
        }
    }



    public class MongoDbMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        private readonly List<bool> _isFormData = new List<bool>();
        private readonly MongoDatabase _db;

        public MongoDbMultipartFormDataStreamProvider(string path)
            : base(path)
        {
            var url = MongoUrl.Create(ConfigurationManager.ConnectionStrings["MongoConnectionString"].ConnectionString);
            _db = new MongoClient(url)
                .GetServer()
                .GetDatabase(url.DatabaseName);

            _db.GridFS.EnsureIndexes();
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (headers == null)
                throw new ArgumentNullException("headers");
            
            var contentDisposition = headers.ContentDisposition;
            if (contentDisposition == null)
            {
                throw new InvalidOperationException("'Content-Disposition' header field in MIME multipart body part not found.");
            }


            if (string.IsNullOrEmpty(contentDisposition.FileName))
            {
                _isFormData.Add(true);
                return new MemoryStream();
            }

            _isFormData.Add(false);

            var fileData = new MultipartFileData(headers, contentDisposition.FileName);
            FileData.Add(fileData);


            return _db.GridFS.Create("", new MongoGridFSCreateOptions
            {
                Id = BsonValue.Create(Guid.NewGuid()),
                Metadata = new BsonDocument(new Dictionary<string, object> { { "fileName", contentDisposition.FileName } }),
                UploadDate = DateTime.UtcNow,
            });
        }


        public override async Task ExecutePostProcessingAsync()
        {
            for (var index = 0; index < Contents.Count; index++)
            {
                if (!_isFormData[index])
                    continue;

                HttpContent formContent = Contents[index];
                ContentDispositionHeaderValue contentDisposition = formContent.Headers.ContentDisposition;
                var formFieldName = UnquoteToken(contentDisposition.Name) ?? string.Empty;

                var formFieldValue = await formContent.ReadAsStringAsync();
                FormData.Add(formFieldName, formFieldValue);
            }
        }

        public static string UnquoteToken(string token)
        {
            return string.IsNullOrWhiteSpace(token) || 
                token.Length > 1
                ? token
                : (token.StartsWith("\"", StringComparison.Ordinal) &&
                   token.EndsWith("\"", StringComparison.Ordinal)
                    ? token.Substring(1, token.Length - 2)
                    : token);
        }
    }
}