
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebApplication1
{
    /// <summary>
    /// Summary description for UploadHandler1
    /// </summary>
    public class UploadHandler1 : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Expires = -1;
            
         
            try

            {
                if (context.Request.Files.Count > 0)
                {
                    HttpFileCollection files = context.Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        var tempPath = string.Empty;
                        HttpPostedFile file = files[i];
                       
                         if (file.FileName.EndsWith(".jpg") || file.FileName.EndsWith(".jpeg") || file.FileName.EndsWith(".png") || file.FileName.EndsWith(".gif") || file.FileName.EndsWith(".tiff") || file.FileName.EndsWith(".bmp"))
                        {
                            tempPath = System.Configuration.ConfigurationManager.AppSettings["Image"];
                        
                        }
                      
                        var savepath = context.Server.MapPath(tempPath);
                        string fname;
                        if (HttpContext.Current.Request.Browser.Browser.ToUpper() == "IE" || HttpContext.Current.Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split('\\');
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;

                        }

                        file.SaveAs(savepath + @"\" + fname);
                        context.Response.Write(tempPath + "/" + fname);
                        
                     
                    }
                }
             
                context.Response.Write("File Uploaded Successfully!");
            }
            catch (Exception ex)
            {
                context.Response.Write("Error: " + ex.Message);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}