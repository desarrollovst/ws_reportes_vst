using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/// <summary>
/// Descripción breve de ServiceCap
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
// [System.Web.Script.Services.ScriptService]
public class ServiceCap : System.Web.Services.WebService
{
    Parametros oP = new Parametros();
    Engine oE = new Engine();
    Funciones func = new Funciones();
    public string cdgEmpresa;
    public string emisoraBanorte;

    public ServiceCap()
    {
        //Elimine la marca de comentario de la línea siguiente si utiliza los componentes diseñados 
        //InitializeComponent(); 
        cdgEmpresa = ConfigurationManager.AppSettings.Get("Empresa");
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE CAPACITACIONES
    [WebMethod]
    public string getRepCpCapacitacionPaso(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT * " +
            "             FROM CP_CAPACITACION_PASO " +
            "            WHERE CDGEM = '" + empresa + "' " +
            "              AND CDGPE = '" + usuario + "'" +
            "         ORDER BY ORDEN ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE INVITACION A CAPACITACION
    [WebMethod]
    public string getRepCpInvitacionPaso(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT * " +
            "             FROM CP_INVITACION_PASO " +
            "            WHERE CDGEM = '" + empresa + "' " +
            "              AND CDGPE = '" + usuario + "'" +
            "         ORDER BY ORDEN ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }
}