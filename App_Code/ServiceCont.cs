using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;

/// <summary>
/// Descripción breve de ServiceCap
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
// [System.Web.Script.Services.ScriptService]
public class ServiceCont : System.Web.Services.WebService
{
    ParamsCont oP = new ParamsCont();
    Engine oE = new Engine();
    public string cdgEmpresa;

    public ServiceCont()
    {
        //Elimine la marca de comentario de la línea siguiente si utiliza los componentes diseñados 
        //InitializeComponent(); 
        cdgEmpresa = ConfigurationManager.AppSettings.Get("Empresa");
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE CAPACITACIONES
    [WebMethod]
    public string getPolizaDesembolsos(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_POLIZA_DESEMBOLSO_VST", CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA = '" + fecha + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE CAPACITACIONES
    [WebMethod]
    public string getPolizaDesembolsosFecha(string fecIni, string fecFin, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_POLIZA_DESEMBOLSO_FECHA_VST", CommandType.StoredProcedure, oP.ParamsPolizaFecha(empresa, fecIni, fecFin, usuario, fecFin));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE CAPACITACIONES
    [WebMethod]
    public string getPolizaDesembolsosMensual(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_POLIZA_DESEMBOLSO_VST", CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE TO_CHAR(FPOLIZA,'MM/YYYY') = TO_CHAR(TO_DATE('" + fecha + "'),'MM/YYYY') " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' " +
                       "ORDER BY ORDEN";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE DEVENGOS
    [WebMethod]
    public string getPolizaDevengoFecha(string fecIni, string fecFin, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_POLIZA_DEVENGO_FECHA_VST", CommandType.StoredProcedure, oP.ParamsPolizaFecha(empresa, fecIni, fecFin, usuario, fecFin));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA = '" + fecFin + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS
    [WebMethod]
    public string getPolizaPagos(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "2")
            proc = "SP_POLIZA_NOIDEN_VST";
        else if (tipo == "3")
            proc = "SP_POLIZA_PAGOS_VST";
        else if (tipo == "4")
            proc = "SP_POLIZA_GL_VST";
        else if (tipo == "5")
            proc = "SP_POLIZA_GL_PAGO_VST";
        else if (tipo == "6")
            proc = "SP_POLIZA_PAGOS_IDEN_VST";
        else if (tipo == "7")
            proc = "SP_POLIZA_PAGO_GL_VST";
        else if (tipo == "11")
            proc = "SP_POLIZA_GL_GL_VST";
        else if (tipo == "12")
            proc = "SP_POLIZA_PAGO_PAGO_VST";
        else if (tipo == "13")
            proc = "SP_POLIZA_GL_IDEN_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA = '" + fecha + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS
    [WebMethod]
    public string getPolizaPagosFecha(string fecIni, string fecFin, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "2")
            proc = "SP_POLIZA_NOIDEN_FECHA_VST";
        else if (tipo == "3")
            proc = "SP_POLIZA_PAGOS_FECHA_VST";
        else if (tipo == "4")
            proc = "SP_POLIZA_GL_FECHA_VST";
        else if (tipo == "5")
            proc = "SP_POLIZA_GL_PAGO_FECHA_VST";
        else if (tipo == "6")
            proc = "SP_POLIZA_PAGOS_IDEN_FECHA_VST";
        else if (tipo == "7")
            proc = "SP_POLIZA_PAGO_GL_FECHA_VST";
        else if (tipo == "11")
            proc = "SP_POLIZA_GL_GL_FECHA_VST";
        else if (tipo == "12")
            proc = "SP_POLIZA_PAGO_PAGO_FECHA_VST";
        else if (tipo == "13")
            proc = "SP_POLIZA_GL_IDEN_FECHA_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPolizaFecha(empresa, fecIni, fecFin, usuario, fecFin));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS
    [WebMethod]
    public string getPolizaPagosMensual(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "2")
            proc = "SP_POLIZA_NOIDEN_VST";
        else if (tipo == "3")
            proc = "SP_POLIZA_PAGOS_VST";
        else if (tipo == "4")
            proc = "SP_POLIZA_GL_VST";
        else if (tipo == "5")
            proc = "SP_POLIZA_GL_PAGO_VST";
        else if (tipo == "6")
            proc = "SP_POLIZA_PAGOS_IDEN_VST";
        else if (tipo == "7")
            proc = "SP_POLIZA_PAGO_GL_VST";
        else if (tipo == "11")
            proc = "SP_POLIZA_GL_GL_VST";
        else if (tipo == "12")
            proc = "SP_POLIZA_PAGO_PAGO_VST";
        else if (tipo == "13")
            proc = "SP_POLIZA_GL_IDEN_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE TO_CHAR(FPOLIZA,'MM/YYYY') = TO_CHAR(TO_DATE('" + fecha + "'),'MM/YYYY') " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' " +
                       "ORDER BY ORDEN";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS DE PAYCASH
    [WebMethod]
    public string getPolizaPagosPaycash(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "8")
            proc = "SP_POLIZA_PAGOS_PAYCASH_VST";
        else if (tipo == "9")
            proc = "SP_POLIZA_PAGOS_PAYCASH_C_VST";
        else if (tipo == "10")
            proc = "SP_POLIZA_GL_PAYCASH_C_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA = '" + fecha + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS DE PAYCASH
    [WebMethod]
    public string getPolizaPagosPaycashFecha(string fecIni, string fecFin, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "8")
            proc = "SP_POLIZA_PAGOS_PAYCASH_F_VST";
        else if (tipo == "9")
            proc = "SP_POLIZA_PAGOS_PAYCASH_CF_VST";
        else if (tipo == "10")
            proc = "SP_POLIZA_GL_PAYCASH_C_FEC_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPolizaFecha(empresa, fecIni, fecFin, usuario, fecFin));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE PAGOS DE PAYCASH
    [WebMethod]
    public string getPolizaPagosPaycashMensual(string fecha, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string proc = "";
        string empresa = cdgEmpresa;
        int iRes;

        if (tipo == "8")
            proc = "SP_POLIZA_PAGOS_PAYCASH_VST";
        else if (tipo == "9")
            proc = "SP_POLIZA_PAGOS_PAYCASH_C_VST";
        else if (tipo == "10")
            proc = "SP_POLIZA_GL_PAYCASH_C_VST";

        iRes = oE.myExecuteNonQuery(proc, CommandType.StoredProcedure, oP.ParamsPoliza(empresa, fecha, usuario, fecha));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE TO_CHAR(FPOLIZA,'MM/YYYY') = TO_CHAR(TO_DATE('" + fecha + "'),'MM/YYYY') " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' " +
                       "ORDER BY ORDEN";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE PERMITE EMITIR LAS POLIZAS DE SEGUROS
    [WebMethod]
    public string getPolizaSegurosFecha(string fecIni, string fecFin, string tipo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_POLIZA_SEGUROS_FECHA_VST", CommandType.StoredProcedure, oP.ParamsPolizaFecha(empresa, fecIni, fecFin, usuario, fecFin));

        string query = "SELECT * " +
                       "FROM LAYOUTPOLIZAS " +
                       "WHERE FPOLIZA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                       "AND TIPO = " + tipo + " " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }
}
