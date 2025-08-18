<%@ WebHandler Language="C#" Class="Uploader" %>
using System.Web;
using System.Web.UI;
using System.Data;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System;
using System.IO;
using System.Net;

using System.Web.Configuration;

public class Uploader : IHttpHandler 
{
    Engine oE = new Engine();
    Parametros oP = new Parametros();
    
    public void ProcessRequest( HttpContext _context ) 
    {
		  // not very elegant - change to full path of your upload folder (there are no upload folders on my site)
            string uploadDir = ConfigurationManager.AppSettings.Get("upload");
								
    if (_context.Request.Files.Count == 0)
    {
      _context.Response.Write("<result><status>Error</status><message>No files selected</message></result>");
      return;
    }

    foreach(string fileKey in _context.Request.Files)
    {
        HttpPostedFile file = _context.Request.Files[fileKey];
        int iRes = 0;
        try        
        {
            //string fileName = _context.Server.MapPath + "\\" + file.FileName;
            file.SaveAs(_context.Server.MapPath(file.FileName));
            string strFilePath = _context.Server.MapPath(file.FileName);
            //Create a FTP Request Object and Specfiy a Complete Path            
            string strFileName = strFilePath.Substring(strFilePath.LastIndexOf("\\") + 1);            
            FtpWebRequest reqObj = (FtpWebRequest)WebRequest.Create(uploadDir + @"/" + strFileName);            
            //Call A FileUpload Method of FTP Request Object            
            reqObj.Method = WebRequestMethods.Ftp.UploadFile;            
            //If you want to access Resourse Protected You need to give User Name      and PWD            
            reqObj.Credentials = new NetworkCredential(ConfigurationManager.AppSettings.Get("usuario"), ConfigurationManager.AppSettings.Get("password"));            
            //FileStream object read file from Local Drive            
            FileStream streamObj = File.OpenRead(strFilePath);
            //Store File in Buffer            
            byte[] buffer = new byte[streamObj.Length + 1];            
            //Read File from Buffer            
            streamObj.Read(buffer, 0, buffer.Length);            
            //Close FileStream Object Set its Value to nothing            
            streamObj.Close();            
            streamObj = null;            
            //Upload File to ftp://localHost/ set its object to nothing            
            reqObj.GetRequestStream().Write(buffer, 0, buffer.Length);          
            //reqObj.GetRequestStream().Close();          
            reqObj.Abort();
            reqObj = null;
            _context.Response.Write("<result><status>Success</status><message>Upload completed</message></result>");
            File.Delete(strFilePath);
            iRes = oE.myExecuteNonQuery(ConfigurationManager.AppSettings.Get("esquemaLB") + ".dbo.SP_Lineas_B_Import", CommandType.StoredProcedure, ConfigurationManager.AppSettings.Get("sqlConnUp"));  
        }   
        catch (Exception Ex)        
        {            
        string m = Ex.Message;        
        }  
    }         
    }
 
  public bool IsReusable 
		{
    get { return true; }
  }
}