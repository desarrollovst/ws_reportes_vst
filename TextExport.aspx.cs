using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

public partial class TextExport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        
        String strName = Request.Params["titulo"].ToString() + ".txt";
        String cad = Request.Params["texto"].ToString(); 
        Encoding encoding = Encoding.Default;

        Response.Clear();
        Response.Buffer = true;
        Response.AddHeader("Content-disposition", "attachment;filename=" + strName);
        Response.Charset = encoding.EncodingName;
        Response.ContentType = "text/plain";
        Response.Write(cad);
        Response.Flush();
        Response.End();
    }
}
