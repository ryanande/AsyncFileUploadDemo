using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Web
{
    public class FileHandler : HttpTaskAsyncHandler
    {
        public override bool IsReusable { get { return true; } }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            ////after this line, we depart the handling thread and continue work on a threadpool thread.
            //var resultContent = await FileTransfer(context);

            ////we pick up here on another thread to return to the client
            //context.Response.ContentType = "text/plain";
            //using (var writer = new StreamWriter(context.Response.OutputStream))
            //{
            //    writer.Write(resultContent);
            //    writer.Flush();
            //}
        }

        private Task FileTransfer(HttpContext context)
        {
            return Task.Factory.StartNew(() => TransferFile(context));
        }

        private string TransferFile(HttpContext context)
        {
            string tempFilePath = null;

            try
            {
                if (context.Request.ContentType != "application/xml" && context.Request.ContentType != "text/xml")
                    throw new Exception("Content-Type must be either 'application/xml' or 'text/xml'.");

                tempFilePath = String.Format(@"c:outputnewfile{0}.txt", DateTime.Now);
                using (var reader = new StreamReader(context.Request.GetBufferlessInputStream(true)))
                using (var filestream = new FileStream(tempFilePath, FileMode.Create))
                using (var writer = new StreamWriter(filestream))
                    writer.WriteLine(reader.ReadLineAsync().Result);

                return "Success";
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                return "A critical error occurred. Please try your request again: " + ex;
            }
        }
    }
}