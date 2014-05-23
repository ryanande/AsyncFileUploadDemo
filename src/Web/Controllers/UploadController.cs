using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

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
            var streamProvider = new CustomMultipartFormDataStreamProvider(fullPath);

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
                var info = new FileInfo(i.LocalFileName);
                return "File uploaded as " + info.FullName + " (" + info.Length + ")";
            });
            return fileInfo;
        }
    }



    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public CustomMultipartFormDataStreamProvider(string path) : base(path) { }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            string fileName;
            if (!string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName))
            {
                var ext = Path.GetExtension(headers.ContentDisposition.FileName.Replace("\"", string.Empty));
                fileName = Guid.NewGuid() + ext;
            }
            else
            {
                fileName = Guid.NewGuid() + ".data";
            }
            return fileName;
        }
    }


}
