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

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Service : System.Web.Services.WebService
{
    Parametros oP = new Parametros();
    Engine oE = new Engine();
    Funciones func = new Funciones();
    public string cdgEmpresa;
    public string emisoraBanorte;

    public Service () {
        //Eliminar la marca de comentario de la línea siguiente si utiliza los componentes diseñados 
        //InitializeComponent(); 
        cdgEmpresa = ConfigurationManager.AppSettings.Get("Empresa");
    }

    #region Ocupa flex

    //EXTRAE LA INFORMACION DE LOS REPORTES HISTORICOS DE REPORTE DE CREDITO 
    [WebMethod]
    public string getConsultaRepCreditoHist(string acred, string tipoRep)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CDGCL, " +
                       "TO_CHAR(FCONSULTA,'DD/MM/YYYY') FECHACONS, " +
                       "TO_CHAR(FVIGENCIA,'DD/MM/YYYY') FECHAVIG " +
                       "FROM CONSULTA_REP_CREDITO " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGCL = '" + acred + "' " +
                       "AND INSTCRED = '" + tipoRep + "'";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS AJUSTES MANUALES 
    [WebMethod]
    public string getRepAjustesManuales(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string detAct = string.Empty;
        string detSig = string.Empty;
        Boolean band = true;
        decimal cantidad;
        decimal capital;
        decimal interes;
        decimal totalCant;
        decimal totalCap;
        decimal totalInt;
        int cont;
        int i;

        string query = "SELECT TO_CHAR(FPAGO, 'DD/MM/YYYY') FECHA_VALOR " +
                       //",TO_CHAR(TRUNC(FREGISTRO),'DD/MM/YYYY') FECHA_OPERACION " +
                       ",GL.CDGCLNS CODIGO " +
                       ",NS.NOMBRE GRUPO " +
                       ",CASE WHEN GL.ESTATUS = 'RE' AND GL.CDGCB = '12' THEN GL.CICLO " +
                       "WHEN GL.ESTATUS = 'CG' THEN GL.CICLO " +
                       "ELSE '' " +
                       "END CICLO " +
                       ",GL.REFERENCIA " +
                       ",NVL(GL.CANTIDAD,0) CANTIDAD " +
                       ",NVL(CASE WHEN ESTATUS = 'CP' THEN (SELECT PAGADOCAP FROM MP WHERE CDGEM = '" + empresa + "' AND CLNS = 'G' AND ESTATUS <> 'E' AND TIPO = 'PD' AND CDGCLNS = GL.CDGCLNS AND FREALDEP = GL.FPAGO AND CDGCB = '12') " +
                       "WHEN ESTATUS = 'RE' AND CDGCB = '19' THEN " +
                            "(SELECT PAGADOCAP " +
                            "FROM MP WHERE CDGEM = '" + empresa + "' " +
                            "AND CLNS = 'G' " +
                            "AND ESTATUS <> 'E' " +
                            "AND TIPO IN ('AC') " +
                            "AND CDGCLNS = GL.CDGCLNS " +
                            "AND FREALDEP = GL.FPAGO " +
                            "AND (SELECT COUNT(*) FROM MPR WHERE CDGEM = MP.CDGEM AND CDGNS = MP.CDGCLNS AND CICLO = MP.CICLO AND SECUENCIA = MP.SECUENCIA AND PERIODO = MP.PERIODO AND FECHA = MP.FREALDEP) = 0) " +
                       "ELSE NULL END,0) CAPITAL " +
                       ",NVL(CASE WHEN ESTATUS = 'CP' THEN (SELECT PAGADOINT FROM MP WHERE CDGEM = '" + empresa + "' AND CLNS = 'G' AND ESTATUS <> 'E' AND TIPO = 'PD' AND CDGCLNS = GL.CDGCLNS AND FREALDEP = GL.FPAGO AND CDGCB = '12') " +
                       "WHEN ESTATUS = 'RE' AND CDGCB = '19' THEN (SELECT PAGADOINT FROM MP WHERE CDGEM = '" + empresa + "' AND CLNS = 'G' AND ESTATUS <> 'E' AND TIPO IN ('MI') AND CDGCLNS = GL.CDGCLNS AND FREALDEP = GL.FPAGO " +
                       "AND (SELECT COUNT(*) FROM MPR WHERE CDGEM = MP.CDGEM AND CDGNS = MP.CDGCLNS AND CICLO = MP.CICLO AND SECUENCIA = MP.SECUENCIA AND PERIODO = MP.PERIODO AND FECHA = MP.FREALDEP) = 0) " +
                       "ELSE NULL END,0) INTERES " +
                       ",IB.NOMBRE BANCO " +
                       ",CB.NUMERO CUENTA " +
                       ",GL.CDGPE USUARIO " +
                       ",CASE WHEN GL.ESTATUS = 'RE' AND GL.CDGCB = '19' THEN 'ABONO A GARANTIA POR EXCEDENTE DE CREDITO' " +
                       "WHEN GL.ESTATUS = 'RE' AND GL.CDGCB = '12' AND CDGPE = 'USER' THEN 'ABONO POR TRASPASO DE GARANTIA DE UN CICLO A OTRO (NETEO)' " +
                       "WHEN GL.ESTATUS = 'CP' THEN 'CARGO POR PAGO AL CREDITO DEL CLIENTE' " +
                       "WHEN GL.ESTATUS = 'CG' THEN 'CARGO POR TRASPASO DE GARANTIA DE UN CICLO A OTRO (NETEO)' " +
                       "ELSE CAT.DESCRIPCION " +
                       "END DETALLE " +
                       ",(SELECT CASE WHEN fnCicloGpoXFecha(NS.CDGEM,NS.CODIGO,GL.FPAGO) IS NULL THEN ( " +
                       "SELECT NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       "FROM NS A, PE " +
                       "WHERE A.CDGEM = NS.CDGEM " +
                       "AND A.CODIGO = NS.CODIGO " +
                       "AND PE.CDGEM = A.CDGEM " +
                       "AND PE.CODIGO = A.CDGACPE) " +
                       "WHEN SUBSTR(fnCicloGpoXFecha(NS.CDGEM,NS.CODIGO,GL.FPAGO),1,1) = 'R' THEN ( " +
                       "SELECT NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       "FROM SN, PE " +
                       "WHERE SN.CDGEM = PE.CDGEM " +
                       "AND SN.CDGOCPE = PE.CODIGO " +
                       "AND SN.CDGEM = NS.CDGEM " +
                       "AND SN.CDGNS = NS.CODIGO " +
                       "AND SN.CICLO = fnCicloGpoXFecha(NS.CDGEM,NS.CODIGO,GL.FPAGO)) " +
                       "ELSE " +
                       "(SELECT NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       "FROM PRN, PE " +
                       "WHERE PRN.CDGEM = PE.CDGEM " +
                       "AND PRN.CDGOCPE = PE.CODIGO " +
                       "AND PRN.CDGEM = NS.CDGEM " +
                       "AND PRN.CDGNS = NS.CODIGO " +
                       "AND PRN.CICLO = fnCicloGpoXFecha(NS.CDGEM,NS.CODIGO,GL.FPAGO)) " +
                       "END " +
                       "FROM DUAL " +
                       ") ASESOR " +
                       //--,GL.*, CAT.*
                       "FROM PAG_GAR_SIM GL, CATMOVSGARSIMPLE CAT, NS,CB, IB " +
                       "WHERE  GL.ESTATUS = CAT.CODIGO " +
                       "AND GL.CDGEM = NS.CDGEM " +
                       "AND GL.CDGCLNS = NS.CODIGO " +
                       "AND CB.CDGEM = IB.CDGEM " +
                       "AND CB.CDGIB = IB.CODIGO " +
                       "AND GL.CDGEM = CB.CDGEM " +
                       "AND GL.CDGCB = CB.CODIGO " +
                       "AND GL.CDGEM = '" + empresa + "' " +
                       "AND GL.FPAGO BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
            //--AND GL.CDGCB NOT IN ('02','03') 
                       "AND GL.ESTATUS NOT IN ('CA') " +
                       "AND NOT (GL.ESTATUS =  'RE' AND GL.CDGCB NOT IN ('12','19')) " +
            //--ORDER BY FPAGO, FREGISTRO, CDGCLNS, GL.CICLO
                       "UNION " +
                       "SELECT TO_CHAR(FREALDEP,'DD/MM/YYYY') FECHA_VALOR " +
            //",TO_CHAR(TRUNC(FTRANSAC),'DD/MM/YYYY') FECHA_OPERACION " +
                       ",CDGCLNS CODIGO " +
                       ",NS.NOMBRE GRUPO " +
                       ",MP.CICLO " +
                       ",NULL REFERENCIA " +
                       ",NVL(CANTIDAD,0) CANTIDAD " +
                       ",NVL(PAGADOCAP,0) CAPITAL " +
                       ",NVL(PAGADOINT,0) INTERES " +
                       ",NULL BANCO " +
                       ",NULL CUENTA " +
                       ",MP.ACTUALIZARPE USUARIO " +
                       ",REFERENCIA DETALLE " +
                       ",NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                       "FROM MP, NS, PRN, PE " +
                       "WHERE MP.CDGEM = NS.CDGEM " +
                       "AND MP.CDGCLNS = NS.CODIGO " +
                       "AND MP.CDGEM = PRN.CDGEM " +
                       "AND MP.CDGCLNS = PRN.CDGNS " +
                       "AND MP.CICLO = PRN.CICLO " +
                       "AND PRN.CDGEM = PE.CDGEM " +
                       "AND PRN.CDGOCPE = PE.CODIGO " +
                       "AND MP.CDGEM = '" + empresa + "' " +
                       "AND TIPO IN ('AA','CI','AC','MI') " +
                       "AND MP.FREALDEP BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "ORDER BY 12,1,2,3,5 ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

        if (res == 1)
        {
            cont = dref.Tables[0].Rows.Count;
            dsRepAjustes.dtAjustesDataTable dt = new dsRepAjustes.dtAjustesDataTable();
            try
            {
                cantidad = 0;
                capital = 0;
                interes = 0;
                totalCap = 0;
                totalInt = 0;
                totalCant = 0;

                if (cont > 0)
                {
                    for (i = 0; i < cont; i++)
                    {
                        band = true;
                        DataRow dr = dt.NewRow();
                        dr["FECHA_VALOR"] = dref.Tables[0].Rows[i]["FECHA_VALOR"].ToString();
                        //dr["FECHA_OPERACION"] = dref.Tables[0].Rows[i]["FECHA_OPERACION"].ToString();
                        dr["CDGNS"] = dref.Tables[0].Rows[i]["CODIGO"].ToString();
                        dr["GRUPO"] = dref.Tables[0].Rows[i]["GRUPO"].ToString();
                        dr["CICLO"] = dref.Tables[0].Rows[i]["CICLO"].ToString();
                        dr["REFERENCIA"] = dref.Tables[0].Rows[i]["REFERENCIA"].ToString();
                        dr["CANTIDAD"] = dref.Tables[0].Rows[i]["CANTIDAD"].ToString();
                        dr["CAPITAL"] = dref.Tables[0].Rows[i]["CAPITAL"].ToString();
                        dr["INTERES"] = dref.Tables[0].Rows[i]["INTERES"].ToString();
                        dr["BANCO"] = dref.Tables[0].Rows[i]["BANCO"].ToString();
                        dr["CUENTA"] = dref.Tables[0].Rows[i]["CUENTA"].ToString();
                        dr["USUARIO"] = dref.Tables[0].Rows[i]["USUARIO"].ToString();
                        dr["DETALLE"] = dref.Tables[0].Rows[i]["DETALLE"].ToString();
                        dr["ASESOR"] = dref.Tables[0].Rows[i]["ASESOR"].ToString();

                        cantidad += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTIDAD"].ToString());
                        capital += Convert.ToDecimal(dref.Tables[0].Rows[i]["CAPITAL"].ToString());
                        interes += Convert.ToDecimal(dref.Tables[0].Rows[i]["INTERES"].ToString());
                        totalCant += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTIDAD"].ToString());
                        totalCap += Convert.ToDecimal(dref.Tables[0].Rows[i]["CAPITAL"].ToString());
                        totalInt += Convert.ToDecimal(dref.Tables[0].Rows[i]["INTERES"].ToString());

                        dt.Rows.Add(dr);

                        if (i + 1 < cont)
                        {
                            detSig = dref.Tables[0].Rows[i + 1]["DETALLE"].ToString();
                        }
                        detAct = dref.Tables[0].Rows[i]["DETALLE"].ToString();

                        if (detSig != detAct)
                        {
                            DataRow dfec = dt.NewRow();
                            dfec["FECHA_VALOR"] = "--- SUBTOTAL ---";
                            dfec["CANTIDAD"] = cantidad;
                            dfec["CAPITAL"] = capital;
                            dfec["INTERES"] = interes;
                            dt.Rows.Add(dfec);
                            DataRow drLine = dt.NewRow();
                            dt.Rows.Add(drLine);
                            cantidad = 0;
                            capital = 0;
                            interes = 0;
                            band = false;
                        }
                    }
                    if (i == cont && band == true)
                    {
                        DataRow dfec = dt.NewRow();
                        dfec["FECHA_VALOR"] = "--- SUBTOTAL ---";
                        dfec["CANTIDAD"] = cantidad;
                        dfec["CAPITAL"] = capital;
                        dfec["INTERES"] = interes;
                        dt.Rows.Add(dfec);
                        DataRow drLine = dt.NewRow();
                        dt.Rows.Add(drLine);
                    }
                    DataRow dtot = dt.NewRow();
                    dtot["FECHA_VALOR"] = "--- TOTAL ---";
                    dtot["CANTIDAD"] = totalCant;
                    dtot["CAPITAL"] = totalCap;
                    dtot["INTERES"] = totalInt;
                    dt.Rows.Add(dtot);
                    ds.Tables.Add(dt);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "";
            }
            xml = ds.GetXml();
        }
        return xml;
    }

    //METODO QUE EXTRAE EL ANALISIS DE ALERTAS DE PLD
    [WebMethod]
    public string getRepAnalisisAlertas(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;

        string xml = "";
        int iRes;

        try
        {
            string query = "SELECT " +
                           " TO_CHAR(PA.ALTA,'DD/MM/YYYY') FALTA " +
                           ",AP.DESCRIPCION DESCALERTA " +
                           ",PA.SECUENCIA " +
                           ",DECODE(PA.ESTATUS,'P','PENDIENTE','C','CONCLUIDO') DESCESTATUS " +
                           ",PA.CDGCL " +
                           ",CASE WHEN PA.CDGCL IS NOT NULL THEN " +
                               " NOMBREC(PA.CDGEM,PA.CDGCL,'I','N',NULL,NULL,NULL,NULL) " +
                           " END NOMCL " +
                           ",CASE WHEN CD.COD_ASESOR IS NOT NULL THEN " +
                               "CD.COD_ASESOR " +
                           "ELSE " +
                               "(SELECT CDGOCPE " +
                               "FROM PRN " +
                               "WHERE CDGEM = PA.CDGEM " +
                               "AND CDGNS = PA.CDGCLNS " +
                               "AND CICLO = PA.CICLO) " + 
                           "END COD_ASESOR "  +
                           ",CASE WHEN CD.NOM_ASESOR IS NOT NULL THEN " +
                               "CD.NOM_ASESOR " +
                           "ELSE " +
                               "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                               "FROM PRN, PE " +
                               "WHERE PRN.CDGEM = PA.CDGEM " +
                               "AND PRN.CDGNS = PA.CDGCLNS " +
                               "AND PRN.CICLO = PA.CICLO " +
                               "AND PE.CDGEM = PRN.CDGEM " +
                               "AND PE.CODIGO = PRN.CDGOCPE) " +
                           "END NOM_ASESOR " +
                           ",(CASE WHEN REPORTADO = 'N' " +
                               " THEN 'SI' " +
                               " ELSE '' END) NOREP " +
                           ",(CASE WHEN REPORTADO = 'S'" +
                               " THEN 'SI' " +
                               " ELSE '' END) REP " +
                           ", PA.REGISTROPE " +
                           ", (SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                               " FROM PE WHERE CDGEM = PA.CDGEM AND CODIGO = PA.REGISTROPE) NOMREGPE " +
                           ", PA.CDGCLNS GRUPO " +
                           ", (SELECT NOMBRE FROM NS WHERE CDGEM = PA.CDGEM AND CODIGO = PA.CDGCLNS) NOM_GPO " +
                           ", PA.CICLO " +
                           " FROM PLD_ALERTA PA " +
                           " LEFT JOIN CAT_ALERTA_PLD AP ON " +
                             " AP.CODIGO = PA.CDGAL " +
                           " LEFT JOIN TBL_CIERRE_DIA CD " +
                              " ON  CD.CDGEM = PA.CDGEM " +
                              " AND CD.CDGCLNS = PA.CDGCLNS " +
                              " AND CD.CICLO = PA.CICLO " +
                              " AND CD.CLNS = PA.CLNS " +
                              " AND CD.FECHA_CALC = TRUNC(PA.ALTA) " + 
                          " WHERE PA.CDGEM = '" + empresa + "' " +
                          " AND TRUNC(PA.ALTA) >= '" + fechaIni + "' " +
                          " AND TRUNC(PA.ALTA) <= '" + fechaFin + "' " +
                          " AND PA.CDGCL IS NOT NULL " +
                          " ORDER BY PA.ALTA, PA.CDGCL ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE EL ENCABEZADO DEL ANALISIS DE CREDITO
    [WebMethod]
    public string getRepAnalisis(string codigo, string ciclo, string usuario)
    {
        DataSet dref = new DataSet();
       
        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_ANALISIS", CommandType.StoredProcedure,
              oP.ParamsAnalisis(empresa, codigo, ciclo, usuario));

            string query = "SELECT * " +
                           "FROM REP_ANALISIS " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "'";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE EL ANALISIS DE COBRANZA
    [WebMethod]
    public string getRepAnalisisCobranza(string fecha, string nivel, string tipoCart, string usuario, string coord, string asesor, string supervisor)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepCobranza.dtCobranzaDataTable dt = new dsRepCobranza.dtCobranzaDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_RECUPERA", CommandType.StoredProcedure, oP.ParamsAnalisisCobranza(empresa, Convert.ToDateTime(fecha), Convert.ToInt32(tipoCart), usuario, coord, asesor, supervisor));

            string query = "SELECT RREC.*, " +
                           "ROUND((CASE WHEN RREC.DIFCOMP <= 0 THEN 100 ELSE ((RREC.RECUCOMP/RREC.PAGOCOMP) * 100) END),2) PORCCOMP, " +
                           "ROUND((CASE WHEN RREC.DIFMORA <= 0 THEN 100 ELSE ((RREC.RECUMORA/RREC.MORA) * 100) END),2) PORCMORA, " +
                           "ROUND((CASE WHEN RREC.TOTALMORA <= 0 THEN 100 ELSE ((RREC.TOTALRECU/RREC.TOTALEXIG) * 100) END),2) PORCTOTAL, " +                           "TO_CHAR(sysdate,'DD/MM/YYYY') AS FECHAIMP, " +
                           "TO_CHAR(sysdate,'HH24:MI:SS') AS HORAIMP " +
                           "FROM REP_RECUPERA RREC " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "' " +
                           "ORDER BY NOMPESUP, NOMCO, NOMPE, CDGCLNS";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drCob = dt.NewRow();
                drCob["NOM_SUP"] = dref.Tables[0].Rows[i]["NOMPESUP"];
                drCob["COORDINACION"] = dref.Tables[0].Rows[i]["NOMCO"];
                drCob["NOM_ASESOR"] = dref.Tables[0].Rows[i]["NOMPE"];
                drCob["GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drCob["NOM_GRUPO"] = dref.Tables[0].Rows[i]["NOMCLNS"];
                drCob["CICLO"] = dref.Tables[0].Rows[i]["CICLO"];
                drCob["INICIO"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["INICIO"]).ToString("dd/MM/yyyy");
                drCob["PAGO_COMP"] = dref.Tables[0].Rows[i]["PAGOCOMP"];
                drCob["PAGO_REAL"] = dref.Tables[0].Rows[i]["PAGOREAL"];
                drCob["RECU_COMP"] = dref.Tables[0].Rows[i]["RECUCOMP"];
                drCob["DIF_COMP"] = dref.Tables[0].Rows[i]["DIFCOMP"];
                drCob["PORC_COMP"] = dref.Tables[0].Rows[i]["PORCCOMP"];
                drCob["MORA"] = dref.Tables[0].Rows[i]["MORA"];
                drCob["RECU_MORA"] = dref.Tables[0].Rows[i]["RECUMORA"];
                drCob["DIF_MORA"] = dref.Tables[0].Rows[i]["DIFMORA"];
                drCob["PORC_MORA"] = dref.Tables[0].Rows[i]["PORCMORA"];
                drCob["TOTAL_EXIG"] = dref.Tables[0].Rows[i]["TOTALEXIG"];
                drCob["TOTAL_RECU"] = dref.Tables[0].Rows[i]["TOTALRECU"];
                drCob["TOTAL_MORA"] = dref.Tables[0].Rows[i]["TOTALMORA"];
                drCob["PORC_TOTAL"] = dref.Tables[0].Rows[i]["PORCTOTAL"];
                dt.Rows.Add(drCob);
            }

            if (contFilas > 0)
            {
                DataRow drTot = dt.NewRow();
                drTot["PAGO_COMP"] = contFilas;
                drTot["PORC_COMP"] = Math.Round(((Convert.ToDecimal(dt.Compute("Sum(RECU_COMP)", "")) / Convert.ToDecimal(dt.Compute("Sum(PAGO_COMP)", ""))) * 100),2);
                drTot["PORC_MORA"] = Math.Round(((Convert.ToDecimal(dt.Compute("Sum(RECU_MORA)", "")) / Convert.ToDecimal(dt.Compute("Sum(MORA)", ""))) * 100),2);
                drTot["PORC_TOTAL"] = Math.Round(((Convert.ToDecimal(dt.Compute("Sum(TOTAL_RECU)", "")) / Convert.ToDecimal(dt.Compute("Sum(TOTAL_EXIG)", ""))) * 100),2);

                dt.Rows.Add(drTot);
            }
            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE EL DETALLE DEL ANALISIS DE CREDITO
    [WebMethod]
    public string getRepAnalisisDetalle(string codigo, string ciclo, string usuario)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepAnalisis.dtAnalisisDetalleDataTable dt = new dsRepAnalisis.dtAnalisisDetalleDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_ANALISIS_DETALLE", CommandType.StoredProcedure, 
              oP.ParamsAnalisis(empresa, codigo, ciclo, usuario));

            string query = "SELECT * " +
                           "FROM REP_ANALISIS_DETALLE " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "' " +
                           "ORDER BY NOMCL";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drAnalisis = dt.NewRow();
                drAnalisis["GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drAnalisis["ACREDITADO"] = dref.Tables[0].Rows[i]["CDGCL"];
                drAnalisis["NOM_ACREDITADO"] = dref.Tables[0].Rows[i]["NOMCL"];
                drAnalisis["PUESTO"] = dref.Tables[0].Rows[i]["PUESTO"];
                drAnalisis["CICLOA"] = dref.Tables[0].Rows[i]["CICLOA"];
                drAnalisis["CICLOB"] = dref.Tables[0].Rows[i]["CICLOB"];
                drAnalisis["CICLOC"] = dref.Tables[0].Rows[i]["CICLOC"];
                drAnalisis["CANTENTREA"] = dref.Tables[0].Rows[i]["CANTENTREA"].ToString() != ""? dref.Tables[0].Rows[i]["CANTENTREA"]: 0;
                drAnalisis["CANTENTREB"] = dref.Tables[0].Rows[i]["CANTENTREB"].ToString() != ""? dref.Tables[0].Rows[i]["CANTENTREB"]: 0;
                drAnalisis["CANTENTREC"] = dref.Tables[0].Rows[i]["CANTENTREC"].ToString() != ""? dref.Tables[0].Rows[i]["CANTENTREC"]: 0; 
                drAnalisis["CANTSOLIC"] = dref.Tables[0].Rows[i]["CANTSOLIC"].ToString() != ""? dref.Tables[0].Rows[i]["CANTSOLIC"]: 0;
                drAnalisis["ANALISIS"] = dref.Tables[0].Rows[i]["ANALISIS"];
                dt.Rows.Add(drAnalisis);
            }

            if (contFilas > 0)
            {
                DataRow drTot = dt.NewRow();
                drTot["CANTENTREA"] = Math.Round(Convert.ToDecimal(dt.Compute("Sum(CANTENTREA)", "")), 2);
                drTot["CANTENTREB"] = Math.Round(Convert.ToDecimal(dt.Compute("Sum(CANTENTREB)", "")), 2);
                drTot["CANTENTREC"] = Math.Round(Convert.ToDecimal(dt.Compute("Sum(CANTENTREC)", "")), 2);
                drTot["CANTSOLIC"] = Math.Round(Convert.ToDecimal(dt.Compute("Sum(CANTSOLIC)", "")), 2);

                dt.Rows.Add(drTot);
            }
            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //REPORTES DE ABC, REPORTE DE CONTROL DE MOVIMIENTOS
    [WebMethod]
    public string getRepAnexoA(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaFin = "LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))";
        fecha = "'" + fecha + "'";
        int iRes;

        try
        {
            string query = "SELECT  ROWNUM CONSECUTIVO,  CL.RFC ID_CLIENTE , "
                        + " CONCAT(CONCAT(PF.CDGNS, PF.CICLO), PF.CDGCL) ID_CREDITO,  CL.RFC, "
                        + " NOMBREC(NULL,NULL,NULL,'A',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOM_ACRED, "
                        + " TO_CHAR(PRN.INICIO,'DD/MM/YYYY') INICIO,  "
                        + " TO_CHAR(DECODE(nvl(PRN.periodicidad,''), "
                        + " 'S', PRN.inicio + (7 * nvl(PRN.plazo,0)), "
                        + " 'Q', PRN.inicio + (15 * nvl(PRN.plazo,0)), "
                        + " 'C', PRN.inicio + (14 * nvl(PRN.plazo,0)), "
                        + " 'M', PRN.inicio + (30 * nvl(PRN.plazo,0)),  "
                        + " '', ''),'DD/MM/YYYY')   FECFIN, "
                        + " PRC.CANTENTRE, "
                        + " TRUNC(PF.SDO_CAPITAL,2)SDO_CAPITAL "
                       + "  FROM PRC_FONDEO PF "
                        + " INNER JOIN PRC ON "
                            + " PRC.CDGEM = PF.CDGEM "
                            + " AND PRC.CDGNS = PF.CDGNS "
                            + " AND PRC.CDGCL = PF.CDGCL "
                            + " AND PRC.CICLO = PF.CICLO  "
                        + " INNER JOIN PRN ON "
                            + " PRC.CDGEM= PRN.CDGEM "
                            + " AND PRN.CDGNS = PF.CDGNS "
                            + " AND PRN.CICLO = PF.CICLO "
                        + " INNER JOIN  CL ON  "
                           + "  CL.CDGEM = PF.CDGEM  "
                            + " AND CL.CODIGO = PF.CDGCL "
                        + " WHERE PF.CDGEM = '" + empresa + "' "
                        + " AND PF.CDGORF = '0005' "
                        + " AND PF.FREPSDO = " + fechaFin;

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LA INFORMACION DE LOS ANIVERSARIOS DE LOS ACREDITADOS
    [WebMethod]
    public string getRepAnivAcreditados(string anio, string mes, string region, string sucursal)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string fecha = "01/" + mes + "/" + anio;
        string qrySuc = "";
        string qryReg = "";

        if (region != "000")
        {
            qryReg = "AND RG.CODIGO = " + region;
        }
        if (sucursal != "000")
        {
            qrySuc = "AND CO.CODIGO = " + sucursal;
        }
        try
        {
            string query = "SELECT DISTINCT " +
                           " CO.NOMBRE SUCURSAL, " +
                           " RG.NOMBRE REGION , " +
                           " PRC.CDGCL COD_CTE, " +
                           " NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOM_ACRED, " +
                           " TO_CHAR(CL.NACIMIENTO , 'DD/MM/YYYY') FECHA_NACIMIENTO, " +
                           " PRN.CDGNS COD_GPO, " +
                           " NS.NOMBRE GRUPO, " +
                           " NOMBREC (NULL, NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE)ASESOR " +
                           " FROM PRN,NS,PRC,CL,PE,RG,CO,TBL_CIERRE_DIA CRD " +
                           " WHERE PRN.CDGEM = NS.CDGEM " +
                           "   AND PRN.CDGNS = NS.CODIGO " +
                           "   AND PRN.CDGEM = PRC.CDGEM " +
                           "   AND PRN.CDGNS = PRC.CDGNS " +
                           "   AND PRC.CLNS = 'G'        " +
                           "   AND PRN.CICLO = PRC.CICLO " +
                           "   AND PRC.CDGEM = CL.CDGEM  " +
                           "   AND PRC.CDGCL = CL.CODIGO " +
                           "   AND CO.CDGEM = PRN.CDGEM  " +
                           "   AND CO.CODIGO = PRN.CDGCO " +
                           "   AND RG.CDGEM = CO.CDGEM   " +
                           "   AND RG.CODIGO = CO.CDGRG  " +
                               qryReg +
                               qrySuc +
                           "   AND PRN.CDGEM = PE.CDGEM  " +
                           "   AND PRN.CDGOCPE = PE.CODIGO " +
                           "   AND PRN.CDGEM = CRD.CDGEM " +
                           "   AND PRN.CDGNS = CRD.CDGCLNS " +
                           "   AND PRN.CICLO = CRD.CICLO   " +
                           "   AND PRC.CANTENTRE > 0       " +
                           "   AND PRN.CANTENTRE > 0       " +
                           "   AND PRC.SITUACION <> 'D'    " +
                           "   AND CRD.CDGEM = '" + cdgEmpresa + "'" +
                           "   AND CRD.FECHA_CALC = (TO_DATE('" + fecha + "', 'DD/MM/YYYY') - 1)" +
                           "   AND TO_CHAR(CL.NACIMIENTO,'MM') = LPAD('" + mes + "',2,'0')" +
                           "   AND CRD.CLNS = 'G'          " +
                           "   AND CRD.SITUACION = 'E' " +
                           "   AND (SELECT COUNT(*) FROM PRN_LEGAL WHERE CDGEM = CRD.CDGEM AND CDGCLNS = CRD.CDGCLNS AND CICLO = CRD.CICLO AND CLNS = CRD.CLNS AND ALTA <= CRD.FECHA_CALC AND TIPO IN ('C','R','Z')) = 0 " +
                           "   ORDER BY SUCURSAL, TO_NUMBER (SUBSTR(FECHA_NACIMIENTO , 1 , 2 )) ";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    [WebMethod]
    public string getRepAntSaldos(string fecha, string nivel, string tipoCart, string usuario, string nomUsuario, string region, string coord, string asesor)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepBandas.dtBandasDataTable dt = new dsRepBandas.dtBandasDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_ANT_SALDOS_BANDAS", CommandType.StoredProcedure, 
                        oP.ParamsBandas(empresa, Convert.ToDateTime(fecha), Convert.ToInt32(tipoCart), usuario, region, coord, asesor));

            string query = "SELECT RS.*, " +
                           "TO_CHAR(sysdate,'DD/MM/YYYY') AS FECHAIMP, " +
                           "TO_CHAR(sysdate,'HH24:MI:SS') AS HORAIMP " +
                           "FROM REP_ANT_SALDOS_BANDAS RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CDGPE = '" + usuario + "'";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drBandas = dt.NewRow();
                drBandas["COD_REGIONAL"] = dref.Tables[0].Rows[i]["CDGRG"];
                drBandas["REGIONAL"] = dref.Tables[0].Rows[i]["NOMRG"];
                drBandas["COD_SUCURSAL"] = dref.Tables[0].Rows[i]["CDGCO"];
                drBandas["SUCURSAL"] = dref.Tables[0].Rows[i]["NOMCO"];
                drBandas["COD_ASESOR"] = dref.Tables[0].Rows[i]["CDGOCPE"];
                drBandas["OF_CREDITO"] = dref.Tables[0].Rows[i]["NOMPE"];
                drBandas["COD_GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drBandas["GRUPO"] = dref.Tables[0].Rows[i]["NOMCLNS"];
                drBandas["CICLO"] = dref.Tables[0].Rows[i]["CICLO"];
                drBandas["INICIO"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["INICIO"]).ToString("dd/MM/yyyy");
                drBandas["SALDO_TOTAL"] = dref.Tables[0].Rows[i]["SALDOCAP"];
                drBandas["MORA_TOTAL"] = dref.Tables[0].Rows[i]["MORA_TOTAL"];
                drBandas["MORA_7"] = dref.Tables[0].Rows[i]["MORA1"];
                drBandas["MORA_15"] = dref.Tables[0].Rows[i]["MORA2"];
                drBandas["MORA_30"] = dref.Tables[0].Rows[i]["MORA3"];
                drBandas["MORA_60"] = dref.Tables[0].Rows[i]["MORA4"];
                drBandas["MORA_90"] = dref.Tables[0].Rows[i]["MORA5"];
                drBandas["MORA_120"] = dref.Tables[0].Rows[i]["MORA6"];
                drBandas["MORA_SUP_120"] = dref.Tables[0].Rows[i]["MORA7"];
                drBandas["MORATORIOS"] = dref.Tables[0].Rows[i]["MORATORIOS"];
                drBandas["SALDO_GL"] = dref.Tables[0].Rows[i]["SDOGARANTIA"];
                dt.Rows.Add(drBandas);
            }
            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE OBTIENE LA INFORMACION DE LAS DISPOSICIONES DE CREDITO CORRESPONDIENTES AL FONDEO
    [WebMethod]
    public string getRepAsignaFondeo(string orgFond, string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string strOrgFond = string.Empty;
        string strFecha = string.Empty;
        
        if(orgFond != "0")
            strOrgFond = "AND D.CDGORF = '" + orgFond + "' "; 
        if(fecha != "" && fecha != null)
            strFecha = "AND D.FDISPOSICION <= '" + fecha + "' ";

        string query = "SELECT D.CDGORF " +
                       ",NOMBREC(NULL,NULL,'I','N',ORF.NOMBRE1,ORF.NOMBRE2,ORF.PRIMAPE,ORF.SEGAPE) ORGFOND " +
                       ",D.CDGLC " +
                       ",LC.DESCRIPCION LINEACRED " +
                       ",D.CODIGO CDGDISP " +
                       ",D.DESCRIPCION " +
                       ",D.CANTIDAD " +
                       ",TO_CHAR(D.FDISPOSICION,'DD/MM/YYYY') FDISP " +
                       ",D.PLAZO " +
                       ",D.CONTRATO " +
                       ",D.GRACIACAP " +
                       ",D.GRACIAINT " +
                       ",DECODE(D.PAGOCAP,'M','Mensual' " +
                                        ",'B','Bimestral' " +
                                        ",'T','Trimestral' " +
                                        ",'C','Cuatrimestral' " +
                                        ",'S','Semestral' " +
                                        ",'A','Anual') CAPITAL " +
                       ",DECODE(D.PAGOINT,'M','Mensual' " +
                                        ",'B','Bimestral' " +
                                        ",'T','Trimestral' " +
                                        ",'C','Cuatrimestral' " +
                                        ",'S','Semestral' " +
                                        ",'A','Anual') INTERES " +
                       ",FNMARCADISPOSICION(D.CDGEM,D.CDGORF,D.CDGLC,D.CODIGO) MARCADO " +
                       ",(D.CANTIDAD - FNMARCADISPOSICION(D.CDGEM,D.CDGORF,D.CDGLC,D.CODIGO)) SALDO " + 
                       ",LC.MONTOMAX " +
                       ",VTI.TASA " +
                       "FROM DISPOSICION D, ORF, LC, VTI " +
                       "WHERE D.CDGEM = '" + empresa + "' " +
                       strOrgFond +
                       strFecha +
                       "AND ORF.CDGEM = D.CDGEM " +
                       "AND ORF.CODIGO = D.CDGORF " +
                       "AND LC.CDGEM = D.CDGEM " +
                       "AND LC.CDGORF = D.CDGORF " +
                       "AND LC.CODIGO = D.CDGLC " +
                       "AND VTI.CDGEM = LC.CDGEM " +
                       "AND VTI.CDGTI = LC.CDGTI";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DE LA BITACORA DE ELIMINACIÓN CON SUS DATOS
    [WebMethod]
    public string getRepBitacoraElimina(string codigo)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = "    SELECT BE.CDGEM" +
                           "         , BE.CODIGO " +
                           "         , TO_CHAR(BE.FECHA, 'DD/MM/YYYY') FECHA " +
                           "         , BE.CDGPE " +
                           "         , BE.DESCRIPCION " +
                           "         , BE.CDGIB " +
                           "         , BE.CDGCB " +
                           "         , TO_CHAR(BE.FELIMINA, 'DD/MM/YYYY') FELIMINA " +
                           "         , BED.CDGBITELI " +
                           "         , BED.CDGORF " +
                           "         , BED.CDGLC " +
                           "         , BED.CDGDISP " +
                           "         , BED.CDGCL " +
                           "         , BED.CDGNS " +
                           "         , BED.CICLO " +
                           "         , TO_CHAR(BED.FREPSDO, 'DD/MM/YYYY') FREPSDO " +
                           "         , BED.ESTATUS " +
                           "     FROM BITACORA_ELIMINACION BE " +
                           "LEFT JOIN BITACORA_ELIMINACION_DATOS BED ON BE.CODIGO = BED.CDGBITELI " +
                           "    WHERE BE.CDGEM = '" + empresa + "' ";

            if (codigo != string.Empty)
                query = query + " AND BE.CODIGO = '" + codigo + "' ";

            query = query + " ORDER BY BE.CODIGO ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LA BITACORA DE OPERACIONES
    [WebMethod]
    public string getRepBitacoraOp(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT BO.*, " +
                       "TO_CHAR(BO.FREGISTRO, 'DD/MM/YYYY') FECREG, " +
                       "TO_CHAR(BO.FREGISTRO,'HH24:MI:SS') AS HORAREG, " +
                       "NOMBREC(NULL,NULL,NULL,'A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) USUARIO, " +
                       "(CASE WHEN BO.CDGNS <> '-' THEN " +
                                  "(SELECT NS.NOMBRE " +
                                    "FROM NS " +
                                   "WHERE NS.CDGEM = BO.CDGEM " +
                                     "AND NS.CODIGO = BO.CDGNS) " +
                             "ELSE '' END) GRUPO, " +
                       "(CASE WHEN BO.CDGCL IS NOT NULL THEN " +
                                 "(SELECT NOMBREC(NULL,NULL,NULL,'A',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) " +
                                    "FROM CL " +
                                   "WHERE CL.CDGEM = BO.CDGEM " +
                                     "AND CL.CODIGO = BO.CDGCL) " +
                             "ELSE '' END) ACRED, " +
                       "CO.NOMBRE COORD, " +
                       "AO.DESCRIPCION " +
                       "FROM BITACORA_OPERACION BO, PE, CO, CAT_ACT_OPERACION AO " +
                       "WHERE BO.CDGEM = '" + empresa + "' " +
                       "AND TRUNC(BO.FREGISTRO) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND BO.ESTATUS = 'RE' " +
                       "AND PE.CDGEM = BO.CDGEM " +
                       "AND PE.CODIGO = BO.CDGPE " +
                       "AND CO.CDGEM = BO.CDGEM " +
                       "AND CO.CODIGO = BO.CDGCO " +
                       "AND AO.CODIGO = BO.CDGACT " +
                       "ORDER BY SECUENCIA";
                       
        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE BONOS PARA LOS ASESORES POR VENTA DE MICROSEGUROS
    [WebMethod]
    public string getRepBonosMicroseguros(string mes, string anio)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaInicio = "01-" + mes + "-" + anio;
        string fechaFin = "LAST_DAY(TO_DATE('" + fechaInicio + "', 'DD-MM-YYYY'))";
        int iRes;

        try
        {
            string query = "SELECT TC.NO_NOMINA " +
                           ",TC.NOM_ASESOR " +
                           ",COUNT(*) MSVENDIDOS " +
                           ",COUNT(*) * PM.BONO AS BONO " +
                           " FROM MICROSEGURO MS, TBL_CIERRE_DIA TC, PE, PRODUCTO_MICROSEGURO PM " +
                           " WHERE MS.CDGEM='" + empresa + "' " +
                           " AND MS.INICIO >= '" + fechaInicio + "'" + 
                           " AND MS.ESTATUS <> 'C' " +
                           " AND TC.CDGEM = MS.CDGEM " +
                           " AND TC.CDGCLNS = MS.CDGCLNS " +
                           " AND TC.CICLO = MS.CICLO" +
                           " AND TC.CLNS = MS.CLNS " +
                           " AND TC.INICIO BETWEEN '" + fechaInicio + "' AND " + fechaFin +
                           " AND TC.FECHA_CALC = " + fechaFin +
                           " AND PE.CDGEM = TC.CDGEM " +
                           " AND PE.TELEFONO = TC.NO_NOMINA " +
                           " AND PE.CODIGO = TC.COD_ASESOR " +
                           " AND PE.PUESTO IN ('A','C','D') " +
                           " AND PM.CDGEM = MS.CDGEM " +
                           " AND PM.CODIGO = MS.CDGPMS " +
                           " GROUP BY TC.NO_NOMINA, TC.NOM_ASESOR,  BONO " +
                           " ORDER BY TC.NOM_ASESOR ASC ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE GENERA LA INFORMACIÓN DEL REPORTE DE ESTADOS DE CUENTA
    [WebMethod]
    public string getRepCargaEdoCuenta(string banco, string cuenta, string fecEdo, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string query = " ";
        string xml = "";
        int iRes;

        query = "SELECT TO_CHAR(NVL(FOPERACION,SYSDATE),'DD/MM/YYYY') FECOPE " +
                ",TO_CHAR(FACTUALIZA,'DD/MM/YYYY') FECHA " +
                ",REFERENCIA " +
                ",DESCRIPCION " +
                ",CODTRANSAC CODTRANS " +
                ",SUCURSAL " +
                ",TRUNC(DEPOSITO,2) DEPOSITOS " +
                ",TRUNC(RETIRO,2) RETIROS " +
                ",TRUNC(SALDO,2) SALDO " +
                ",MOVTO MOVIMIENTO " +
                ",DESDDETALLADA DESCDET " +
                ",CONSECUTIVO NUMERO " +
                ",TRUNC(IMPORTE,2) IMPORTE " +
                ",TIPO " +
                ",REFERENCIA2 REF2 " +
                ",COMENTARIO " +
                "FROM REP_EDOCTABCOS " +
                "WHERE CDGEM = '" + empresa + "' " +
                "AND CDGIB = '" + banco + "' " +
                "AND CDGCB = '" + cuenta + "' " +
                "AND FEDOCTA = '" + fecEdo + "' " +
                "AND CDGPE = '" + usuario + "' " +
                "ORDER BY TO_NUMBER(CONSECUTIVO)";
 
        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

        xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE CLIENTES EXCLUIDOS DEL REPORTE DE CIRCULO DE CREDITO
    [WebMethod]
    public string getRepCargaExcCirc(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT EC.* " +
                       ",TO_CHAR(EC.FECHA_CIERRE ,'DD/MM/YYYY') FEC_CIERRE " +
                       "FROM REP_EXC_CIRC EC " +
                       "WHERE EC.CDGEM = '" + empresa + "' " +
                       "AND EC.CDGPE = '" + usuario + "'" +
                       "ORDER BY EC.ORDEN ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DEL PROCESO DE REGISTRO DE METAS MEDIANTE UN ARCHIVO
    [WebMethod]
    public string getRepCargaMetasAsesor(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = " SELECT CDGEM " +
            "                 , CDGRG " +
            "                 , CDGCO " +
            "                 , CDGOCPE " +
            "                 , MES " +
            "                 , ANIO " +
            "                 , TRUNC(META, 2) META " +
            "                 , CDGPE " +
            "                 , ESTATUS " +
            "              FROM REP_METAS_ASESOR " +
            "             WHERE CDGEM = '" + empresa + "' " +
            "               AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LA INFORMACION DE LA CARTERA VENCIDA
    [WebMethod]
    public string getRepCarteraVencida(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            //INCORPORACION DE CARTERA VENCIDA
            string query = "SELECT CD.REGION " +
                           ",CD.COD_SUCURSAL " +
                           ",CD.NOM_SUCURSAL " +
                           ",CD.NOM_ASESOR " +
                           ",(SELECT NOM_ASESOR FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = '01' AND FECHA_CALC = CD.INICIO AND CLNS = CD.CLNS) NOM_ASESOR_ORIGEN " +
                           ",CD.CDGCLNS " +
                           ",CD.NOMBRE " +
                           ",CD.CICLO " +
                           ",TO_CHAR(CD.INICIO,'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR(CD.FIN,'DD/MM/YYYY') FFIN " +
                           ",CD.MONTO_ENTREGADO " +
                           ",CD.TOTAL_PAGAR " +
                           ",CD.TASA " +
                           ",CD.PLAZO " +
                           ",CD.SDO_CAPITAL " +
                           ",((SELECT NVL(SUM(DEV_DIARIO),0) FROM DEVENGO_DIARIO WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CLNS = CD.CLNS AND CICLO = CD.CICLO AND FECHA_CALC <= CD.FECHA_CALC AND ESTATUS <> 'CA') - CD.INTERES_PAGADO) SDO_INT_DEV_NO_COB " +
                           ",CD.SDO_TOTAL " +
                           ",CASE WHEN CD.TIPO_CARTERA = 'R' THEN " +
                                "CD.SDO_CAPITAL " +
                           "ELSE " +
                                "CD.MORA_TOTAL " +
                           "END CARTERA_VENCIDA " +
                           ",CD.DIAS_MORA " +
                           ",CASE WHEN CD.TIPO_CARTERA = 'R' THEN 'RESTRUCTURA' " +
                                 "WHEN CD.CLNS = 'G' THEN 'COMUNAL' " +
                                 "WHEN CD.CLNS = 'I' THEN 'INDIVIDUAL' " +
                           "END TIPO_CREDITO " +
                           "FROM TBL_CIERRE_DIA CD " +
                           "WHERE CD.CDGEM = '" + empresa + "' " +
                           "AND CD.DIAS_MORA > 90 " +
                           "AND CD.FECHA_CALC = '" + fecha + "' " +
                           "AND (SELECT COUNT(*) " +
                                "FROM PRN_LEGAL " +
                                "WHERE CDGEM = CD.CDGEM " + 
                                "AND CDGCLNS = CD.CDGCLNS " +
                                "AND CICLO = CD.CICLO " +
                                "AND CLNS = CD.CLNS " +
                                "AND TIPO IN ('C','Z') " +
                                "AND TRUNC(ALTA) <= CD.FECHA_CALC) = 0";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_TOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_TOTAL)", ""));
                dtot["CARTERA_VENCIDA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CARTERA_VENCIDA)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO QUE OBTIENE INFORMACION PARA EL REPORTE DE CASTIGOS DE FONDEO
    [WebMethod]
    public string getRepCastigosFondeo(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = " SELECT PF.CDGORF, PF.CDGLC, PF.CDGDISP, TO_CHAR(PF.FREPSDO,'DD/MM/YYYY') FECHA, "
                                + " PL.CDGCLNS, PF.CICLO, PF.CDGCL "
                         + " FROM PRC_FONDEO PF "
                         + " INNER JOIN PRN_LEGAL PL "
                            + " ON  PL.CDGEM = PF.CDGEM "
                            + " AND PL.TIPO IN ('C','Z') " 
                            + " AND TRUNC(PL.ALTA) ='" + fecha + "' " 
                            + " AND PL.CDGCLNS = PF.CDGCLNS "
                            + " AND PL.CICLO = PF.CICLO "
                         + " WHERE PF.CDGEM = '" + empresa + "' ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CHEQUES CANCELADOS
    [WebMethod]
    public string getRepChequeCanc(string fecIni, string fecFin, int impreso, int preimp)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            if (impreso == 1)
                queryEstatus = "'IM', 'IG', 'IE', 'ID', 'IC'";
            if (preimp == 1)
                queryEstatus += (queryEstatus != "" ? "," : "") + "'PR'";

            string query = "SELECT TO_CHAR(CC.FCANCELA,'DD/MM/YYYY') AS FECHA, " +
                           "CC.NOCHEQUE, " +
                           "CC.CANTIDAD, " +
                           "DECODE(ESTATUS,'PR','PREIMPRESO','IM','IMPRESO','IG','IMPRESO','IE','IMPRESO','ID','IMPRESO','IC','IMPRESO') AS SITUACION, " +
                           "DECODE(ESTATUS,'IM','DESEMBOLSO','IG','DEV. GARANTIA','IE','DEV. EXCEDENTE','ID','DEFUNCION','IC','DIAGNOSTICO') TIPO, " +
                           "IB.NOMBRE AS BANCO, " +
                           "CC.CDGCB, " +
                           "CB.NUMERO AS CUENTA, " +
                           "CC.CDGCO, " +
                           "CO.NOMBRE AS NOMCO, " +
                           "CC.CDGPE, " +
                           "NOMBREC(NULL,NULL,NULL,'A',A.NOMBRE1,A.NOMBRE2,A.PRIMAPE,A.SEGAPE) AS NOMPE, " +
                           "CASE WHEN CC.TCANC = 'CD' THEN 'DEFINITIVA' WHEN CC.TCANC = 'CS' THEN 'SUSTITUCION' ELSE '' END TCANC " +
                           "FROM CHEQUE_CANCELADO CC, CB, IB, CO, PE A " +
                           "WHERE CC.CDGEM = '" + empresa + "' " +
                           "AND CC.ESTATUS IN (" + queryEstatus + ") " +
                           "AND CC.FCANCELA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND CC.CDGEM = CB.CDGEM " +
                           "AND CC.CDGCB = CB.CODIGO " +
                           "AND CB.CDGEM = IB.CDGEM " +
                           "AND CB.CDGIB = IB.CODIGO " +
                           "AND CO.CDGEM = CC.CDGEM " +
                           "AND CO.CODIGO = CC.CDGCO " +
                           "AND A.CDGEM = CC.CDGEM " +
                           "AND A.CODIGO = CC.CDGPE " +
                           "ORDER BY CC.FCANCELA";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["FECHA"] = "-- TOTAL --";
                dtot["NOCHEQUE"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(NOCHEQUE)", ""));
                if (impreso == 1)
                    dtot["CANTIDAD"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CANTIDAD)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CHEQUES IMPRESOS
    [WebMethod]
    public string getRepChequeImpreso(string fecIni, string fecFin, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            int iRes = oE.myExecuteNonQuery("SP_REP_CHEQUE_IMP", CommandType.StoredProcedure, oP.ParamsChqImp(empresa, fecIni, fecFin, usuario));

            string query = "SELECT TO_CHAR(FMOVIMIENTO, 'DD/MM/YYYY') FECMOV " +
                           ",CDGNS CODGRUPO " +
                           ",NOMNS GRUPO " +
                           ",CICLO " +
                           ",TO_CHAR(FECHA, 'DD/MM/YYYY') FINICIO " +
                           ",TIPO " +
                           ",CDGCO  " +
                           ",NOMCO " +
                           ",CDGCL " +
                           ",NOMCL CLIENTE " +
                           ",NOCHEQUE " +
                           ",CANTIDAD " +
                           ",CDGIB BANCO " +
                           ",CUENTA " +
                           ",CDGOCPE " +
                           ",NOMOCPE " +
                           "FROM REP_CHEQUE_IMP " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "' " +
                           "ORDER BY FMOVIMIENTO, CLIENTE";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["FECMOV"] = "-- TOTAL --";
                dtot["NOCHEQUE"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(NOCHEQUE)", ""));
                dtot["CANTIDAD"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CANTIDAD)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }

            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CHEQUES IMPRESOS
    [WebMethod]
    public string getRepChequesDesembolso(string grupo, string ciclo, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            string query = "SELECT PRC.CDGNS CODGRUPO, NS.NOMBRE GRUPO, PRC.CICLO, PRN.INICIO FINICIO, 'DESEMBOLSO' TIPO, PRN.CDGCO, CO.NOMBRE NOMCO, PRC.CDGCL, " +
                           "NOMBREC(CL.CDGEM,CL.CODIGO,'I','A','','','','') CLIENTE, PRC.NOCHEQUE, " +
                           "PRC.ENTRREAL CANTIDAD, " +
                           "IB.NOMBRE BANCO, " +
                           "CB.NUMERO CUENTA, " +
                           "PRN.CDGOCPE, " +
                           "NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMOCPE " +
                           "FROM PRC, PRN, NS, CO, CL, CB, IB, PE " +
                           "WHERE PRC.CDGEM = PRN.CDGEM " +
                           "AND PRC.CDGNS = PRN.CDGNS " +
                           "AND PRC.CICLO = PRN.CICLO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND PRC.CDGEM = '" + empresa + "' " +
                           "AND PRN.CDGNS = '" + grupo + "' " +
                           "AND PRN.CICLO = '" + ciclo + "' " +
                           "AND NS.CDGEM = PRN.CDGEM " +
                           "AND NS.CODIGO = PRN.CDGNS " +
                           "AND CL.CDGEM = PRC.CDGEM " +
                           "AND CL.CODIGO = PRC.CDGCL " +
                           "AND CB.CDGEM = PRC.CDGEM " +
                           "AND CB.CODIGO = PRC.CDGCB " +
                           "AND IB.CDGEM = CB.CDGEM " +
                           "AND IB.CODIGO = CB.CDGIB " +
                           "AND PE.CDGEM = PRN.CDGEM " +
                           "AND PE.CODIGO = PRN.CDGOCPE";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["CODGRUPO"] = "-- TOTAL --";
                dtot["NOCHEQUE"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(NOCHEQUE)", ""));
                dtot["CANTIDAD"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CANTIDAD)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }

            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION GENERADA DURANTE LA EJECUCION DEL PROCESO DE CIERRE DE DIA
    [WebMethod]
    public string getRepCierreDia(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        string query = "SELECT ROUND(TCD.SDO_CAPITAL,2) SDO_CAPITAL " +
                       ",ROUND(TCD.SDO_INTERES,2) SDO_INTERES " +
                       ",ROUND(TCD.SDO_RECARGOS,2) SDO_RECARGOS " +
                       ",ROUND(TCD.SDO_TOTAL,2) SDO_TOTAL " +
                       ",ROUND(TCD.MORA_CAPITAL,2) MORA_CAPITAL " +
                       ",ROUND(TCD.MORA_INTERES,2) MORA_INTERES " +
                       ",ROUND(TCD.MORA_TOTAL,2) MORA_TOTAL " +
                       ",ROUND(TCD.PAGOS_COMP,2) PAGOS_COMP " +
                       ",ROUND(TCD.PAGOS_REAL,2) PAGOS_REAL " +
                       ",ROUND(TCD.INTERES_GLOBAL,2) INTERES_GLOBAL " +
                       ",ROUND(TCD.MONTO_CUOTA,2) MONTO_CUOTA " +
                       ",ROUND(TCD.TOTAL_PAGAR,2) TOTAL_PAGAR " +
                       ",ROUND(TCD.CAPITAL_PAGADO,2) CAPITAL_PAGADO " +
                       ",ROUND(TCD.INTERES_PAGADO,2) INTERES_PAGADO " +
                       ",ROUND(TCD.MONTO_ENTREGADO,2) MONTO_ENTREGADO " +
                       ",ROUND(TCD.SALDO_GL,2) SALDO_GL " +
                       ",TCD.* " +
                       ",TO_CHAR(TCD.FECHA_CALC,'DD/MM/YYYY') FFECHA_CALC " +
                       ",TO_CHAR(TCD.FECHA_LIQUIDA,'DD/MM/YYYY') FFECHA_LIQUIDA " +
                       ",TO_CHAR(TCD.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",TO_CHAR(TCD.FIN,'DD/MM/YYYY') FFIN " +
                       "FROM TBL_CIERRE_DIA TCD " +
                       "WHERE TCD.CDGEM = '" + empresa + "' " +
                       "AND TCD.FECHA_CALC = '" + fecha + "' " +
                       "AND (SELECT COUNT(*) " +
                            "FROM PRN_LEGAL " +
                            "WHERE CDGEM = TCD.CDGEM " +
                            "AND CDGCLNS = TCD.CDGCLNS " +
                            "AND CICLO = TCD.CICLO " +
                            "AND CLNS = TCD.CLNS " +
                            "AND TIPO IN ('C','Z') " +
                            "AND TRUNC(ALTA) <= TCD.FECHA_CALC) = 0";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CIFRAS CONTROL CONTABLES
    [WebMethod]
    public string getRepCifrasControl(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        string query = "SELECT CASE WHEN TIPO = 1 THEN " +
                                        "'POLIZA DE DESEMBOLSO' " +
                                   "WHEN TIPO = 2 THEN " +
                                        "'POLIZA DE DEVENGO' " +
                                   "WHEN TIPO = 3 THEN " +
                                        "'POLIZA DE CHEQUES' " +
                                   "WHEN TIPO = 4 THEN " +
                                        "'POLIZA DE PAGOS' " +
                                   "WHEN TIPO = 5 THEN " +
                                        "'POLIZA DE CHEQUES CANCELADOS' " +
                                   "WHEN TIPO = 6 THEN " +
                                        "'POLIZA DE DEVOLUCION' " +
                                   "WHEN TIPO = 7 THEN " +
                                        "'POLIZA DE AJUSTES' " +
                                   "END NOMBRE " +
                       ",TIPOPOLIZA " +
                       ",NUMPOLIZA " +
                       ",COUNT(*) REGISTROS " +
                       ",SUM(CARGO) SUM_CARGOS " +
                       ",SUM(ABONO) SUM_ABONOS " +
                       "FROM layoutpolizas a " +
                       "WHERE a.fpoliza = '" + fecha + "' " +
                       "AND A.ORDEN <> 1 " +
                       "AND a.fregistro = (SELECT MAX (fregistro) " +
                                          "FROM layoutpolizas " +
                                          "WHERE fpoliza = '" + fecha + "' " +
                                          "AND tipopoliza = a.tipopoliza " +
                                          "AND tipo = a.tipo) " +
                       "GROUP BY FPOLIZA, TIPOPOLIZA, NUMPOLIZA, TIPO, FREGISTRO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CIFRAS CONTROL OPERATIVAS
    [WebMethod]
    public string getRepCifrasControlOperac(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string status = string.Empty;

        int iRes = oE.myExecuteNonQuery(ref status, "SP_CIFRAS_CONTROL_OP", CommandType.StoredProcedure, oP.ParamsCifrasControl(empresa, fecha, usuario));

        string query = "SELECT CC.* " +
                       "FROM CIFRAS_CONTROL_OP CC " +
                       "WHERE CC.CDGEM = '" + empresa + "' " +
                       "AND CC.FECHA = '" + fecha + "' " +
                       "AND CC.CDGPE = '" + usuario + "'";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CIFRAS CONTROL OPERATIVAS
    [WebMethod]
    public string getRepCirculoCredito(string fecIni, string fecFin, string usuario)
    {
        DataSet dref = new DataSet();
        int cont = 0;
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        int iRes = oE.myExecuteNonQuery(ref status, "SP_REP_CIRC_CRED_VST", CommandType.StoredProcedure, 
                 oP.ParamsCirculoCredito(empresa, fecIni, fecFin, usuario));

        string query = "SELECT * " +
                       "FROM REP_CIRCULO_CRED RCC " +
                       "WHERE RCC.CDGEM = '" + empresa + "' " +
                       "AND RCC.CVE_USUARIO = '" + usuario + "' " +
                       "ORDER BY RCC.FEC_CIERRE_CTA, RCC.CTA_ACTUAL";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        cont = dref.Tables[0].Rows.Count;

        if (cont > 0)
        {
            dref.Tables[0].Rows[0]["TOT_SDOS_ACT"] = Convert.ToInt32(dref.Tables[0].Compute("Sum(SALDO)", ""));
            dref.Tables[0].Rows[0]["TOT_SDOS_VENC"] = Convert.ToInt32(dref.Tables[0].Compute("Sum(SALDO_VENCIDO)", ""));
            dref.Tables[0].Rows[0]["TOT_NOM_REP"] = cont;
            dref.Tables[0].Rows[0]["TOT_DIR_REP"] = cont;
            dref.Tables[0].Rows[0]["TOT_EMP_REP"] = cont;
            dref.Tables[0].Rows[0]["TOT_CTA_REP"] = cont;
            dref.Tables[0].Rows[0]["NOM_OTOR"] = "VENSUMAT";
            dref.Tables[0].Rows[0]["DIR_DEVOL"] = "AV MIGUEL HIDALGO 104 COL TOLUCA DE LERDO CENTRO MUN TOLUCA CP 50000";
        }

        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION GENERAL DE LOS CLIENTES
    [WebMethod]
    public string getRepClientes(string region, string coord, string asesor, string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryAsesor = string.Empty;
        string queryAsesorInd = string.Empty;
        string queryCoord = string.Empty;
        string queryCoordInd = string.Empty;
        string queryRegion = string.Empty;
        string queryRegionInd = string.Empty;

        try
        {
            if (region != "000")
            {
                queryRegion = "AND CO.CDGRG = '" + region + "' ";
            }
            if (coord != "000")
            {
                queryCoord = "AND PRN.CDGCO = '" + coord + "' ";
                queryCoordInd = "AND PRC.CDGCO = '" + coord + "' ";
            }
            if (asesor != "000000")
            {
                queryAsesor = "AND PRN.CDGOCPE = '" + asesor + "' ";
                queryAsesorInd = "AND PRC.CDGOCPE = '" + asesor + "' ";
            }

            string query = "SELECT DISTINCT PRN.CDGCO COD_SUC " +
                           ",CO.NOMBRE SUCURSAL " +
                           ",CO.CDGRG COD_REG " +
                           ",RG.NOMBRE REGION " +
                           ",PRN.CDGNS COD_GPO " +
                           ",NS.NOMBRE GRUPO " +
                           ",PRN.CICLO " +
                           ",PRN.INICIO " +
                           ",PE.CODIGO COD_ASESOR " +
                           ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                           ",PRC.CDGCL COD_CTE " +
                           ",CL.PRIMAPE PATERNO " +
                           ",CL.SEGAPE MATERNO " +
                           ",CL.NOMBRE1 || ' ' || CL.NOMBRE2 NOMBRE_S " +
                           ",TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') FECHA_NACIMIENTO " +
                           ",CL.SEXO " +
                           ",trunc((months_between(sysdate, CL.NACIMIENTO))/12) EDAD " +
                           ",decode(nvl(CL.EDOCIVIL,''), 'S', 'Soltero', " +
                                                        "'C', 'Casado', " +
                                                        "'U', 'Unión Libre', " +
                                                        "'D', 'Divorciado', " +
                                                        "'V', 'Viudo', " +
                                                        "'', '') AS EDO_CIVIL " +
                           ",CL.CALLE " +
                           ",MU.NOMBRE MUNICIPIO " +
                           ",EF.NOMBRE ENTIDAD_F " +
                           ",COL.CDGPOSTAL " +
                           ",CL.TELEFONO " +
                           ",decode(nvl(CL.NIVESCOLAR,''), 'U', 'TECNICO SUPERIOR', " +
                                                          "'R', 'MAESTRIA', " +
                                                          "'P', 'PRIMARIA', " +
                                                          "'B', 'PREPARATORIA', " +
                                                          "'C', 'CARRERA CORTA', " +
                                                          "'T', 'TECNICO', " +
                                                          "'N', 'NINGUNA', " +
                                                          "'S', 'SECUNDARIA', " +
                                                          "'L', 'LICENCIATURA', " +
                                                          "'O', 'DOCTORADO', " +
                                                          "'', 'NINGUNA') AS ESCOLARIDAD " +
                           ",CL.RFC " +
                           ",CL.CURP " +
                           ",decode(nvl(PRC.SITUACION,''), 'L', 'Liquidado', " +
                                                          "'E', 'Entregado', " +
                                                          "'A', 'Aut. Cartera', " +
                                                          "'T', 'Aut. Tesoreria', " +
                                                           "'', '') AS SITUACION " +
                           ",PRC.CANTENTRE CANT_ENTRE " +
                           ",LO.NOMBRE LOCALIDAD " +
                           ",COL.NOMBRE COLONIA " +
                           ",PI.NOMBRE PROYECTO " +
                           ",AE.CDGSE " +
                           ",(SELECT NOMBRE FROM SE WHERE CDGEM = AE.CDGEM AND CODIGO = AE.CDGSE) NOMSE " +
                           ",AE.CDGGI " +
                           ",(SELECT NOMBRE FROM GI WHERE CDGEM = AE.CDGEM AND CDGSE = AE.CDGSE AND CODIGO = AE.CDGGI) NOMGI " +
                           ",AE.CODIGO CDGAE " +
                           ",AE.NOMBRE NOMAE " +
                           ",CL.NODEPEND " +
                           ",FNCICLOACRED(PRC.CDGEM,PRC.CDGCL,CRD.FECHA_CALC) CICLO_ACRED " +
                           ",EF2.NOMBRE NACIOEF " +
                           "FROM PRN, NS, PRC, CL, CO, COL, LO, MU, PI, AE, SC, EF, PE, RG, EF EF2 " +
                           ",TBL_CIERRE_DIA CRD " +
                           "WHERE PRN.CDGEM = NS.CDGEM " +
                           "AND PRN.CDGNS = NS.CODIGO " +
                           "AND PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRC.CLNS = 'G' " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRC.CDGEM = CL.CDGEM " +
                           "AND PRC.CDGCL = CL.CODIGO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND RG.CDGEM = CO.CDGEM " +
                           "AND RG.CODIGO = CO.CDGRG " +
                           "AND CL.CDGPAI = COL.CDGPAI " +
                           "AND CL.CDGEF = COL.CDGEF " +
                           "AND CL.CDGMU = COL.CDGMU " +
                           "AND CL.CDGLO = COL.CDGLO " +
                           "AND CL.CDGCOL = COL.CODIGO " +
                           "AND COL.CDGPAI = LO.CDGPAI " +
                           "AND COL.CDGEF = LO.CDGEF " +
                           "AND COL.CDGMU = LO.CDGMU " +
                           "AND COL.CDGLO = LO.CODIGO " +
                           "AND LO.CDGPAI = MU.CDGPAI " +
                           "AND LO.CDGEF = MU.CDGEF " +
                           "AND LO.CDGMU = MU.CODIGO " +
                           "AND CL.CDGPAI= EF.CDGPAI " +
                           "AND CL.CDGEF = EF.CODIGO " +
                           "AND CL.CDGPAI = EF2.CDGPAI " +
                           "AND CL.NACIOEF = EF2.CODIGO " +
                           "AND PRC.CDGEM = SC.CDGEM " +
                           "AND PRC.CDGNS = SC.CDGNS " +
                           "AND PRC.CLNS = SC.CLNS " +
                           "AND PRC.CICLO = SC.CICLO " +
                           "AND PRC.CDGCL = SC.CDGCL " +
                           "AND PI.CDGEM (+)= SC.CDGEM " +
                           "AND PI.CDGCL (+)= SC.CDGCL " +
                           "AND PI.PROYECTO (+)= SC.CDGPI " +
                           "AND AE.CDGEM (+)= PI.CDGEM " +
                           "AND AE.CDGSE (+)= PI.CDGSE " +
                           "AND AE.CDGGI (+)= PI.CDGGI " +
                           "AND AE.CODIGO (+)= PI.CDGAE " +
                           "AND PRN.CDGEM = PE.CDGEM " +
                           "AND PRN.CDGOCPE = PE.CODIGO " +
                           "AND PRN.CDGEM = CRD.CDGEM " +
                           "AND PRN.CDGNS = CRD.CDGCLNS " +
                           "AND PRN.CICLO = CRD.CICLO " +
                           queryRegion +
                           queryCoord +
                           queryAsesor +
                           "AND PRC.CANTENTRE > 0 " +
                           "AND PRN.CANTENTRE > 0 " +
                           "AND PRC.SITUACION <> 'D' " +
                           "AND CRD.CDGEM = '" + empresa + "' " +
                           "AND CRD.FECHA_CALC = '" + fecha + "' " +
                           "AND CRD.CLNS = 'G' " +
                           "AND CRD.SITUACION = 'E' " +
                    //INFORMACION DE ACREDITADOS QUE DEVOLVIERON SU CREDITO
                           "UNION " +
                           "SELECT DISTINCT " +
                           "PRN.CDGCO COD_SUC, " +
                           "CO.NOMBRE SUCURSAL, " +
                           "CO.CDGRG COD_REG, " +
                           "RG.NOMBRE REGION, " +
                           "PRN.CDGNS COD_GPO, " +
                           "NS.NOMBRE GRUPO, " +
                           "PRN.CICLO, " +
                           "PRN.INICIO, " +
                           "PE.CODIGO COD_ASESOR, " +
                           "NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR, " +
                           "PRC.CDGCL COD_CTE, " +
                           "CL.PRIMAPE PATERNO, " +
                           "CL.SEGAPE MATERNO, " +
                           "CL.NOMBRE1 || ' ' || CL.NOMBRE2 NOMBRE_S, " +
                           "TO_CHAR (CL.NACIMIENTO, 'DD/MM/YYYY') FECHA_NACIMIENTO, " +
                           "CL.SEXO, " +
                           "TRUNC ( (MONTHS_BETWEEN (SYSDATE, CL.NACIMIENTO)) / 12) EDAD, " +
                           "DECODE (NVL (CL.EDOCIVIL, ''), " +
                                "'S', 'Soltero', " +
                                "'C', 'Casado', " +
                                "'U', 'Unión Libre', " +
                                "'D', 'Divorciado', " +
                                "'V', 'Viudo', " +
                                "'', '') " +
                           "AS EDO_CIVIL, " +
                           "CL.CALLE, " +
                           "MU.NOMBRE MUNICIPIO, " +
                           "EF.NOMBRE ENTIDAD_F, " +
                           "COL.CDGPOSTAL, " +
                           "CL.TELEFONO, " +
                           "DECODE (NVL (CL.NIVESCOLAR, ''), " +
                                 "'U', 'TECNICO SUPERIOR', " +
                                 "'R', 'MAESTRIA', " +
                                 "'P', 'PRIMARIA', " +
                                 "'B', 'PREPARATORIA', " +
                                 "'C', 'CARRERA CORTA', " +
                                 "'T', 'TECNICO', " +
                                 "'N', 'NINGUNA', " +
                                 "'S', 'SECUNDARIA', " +
                                 "'L', 'LICENCIATURA', " +
                                 "'O', 'DOCTORADO', " +
                                 "'', 'NINGUNA') " +
                           "AS ESCOLARIDAD, " +
                           "CL.RFC, " +
                           "CL.CURP, " +
                           "DECODE (NVL (PRC.SITUACION, ''), " +
                                 "'L', 'Liquidado', " +
                                 "'E', 'Entregado', " +
                                 "'A', 'Aut. Cartera', " +
                                 "'T', 'Aut. Tesoreria', " +
                                 "'', '') " +
                           "AS SITUACION, " +
                           "PRC.CANTENTRE CANT_ENTRE, " +
                           "LO.NOMBRE LOCALIDAD, " +
                           "COL.NOMBRE COLONIA, " +
                           "PI.NOMBRE PROYECTO, " +
                           "AE.CDGSE, " +
                           "(SELECT NOMBRE " +
                              "FROM SE " +
                              "WHERE CDGEM = AE.CDGEM AND CODIGO = AE.CDGSE) NOMSE, " +
                           "AE.CDGGI, " +
                           "(SELECT NOMBRE " +
                              "FROM GI " +
                              "WHERE CDGEM = AE.CDGEM AND CDGSE = AE.CDGSE AND CODIGO = AE.CDGGI) NOMGI, " +
                           "AE.CODIGO CDGAE, " +
                           "AE.NOMBRE NOMAE, " +
                           "CL.NODEPEND " +
                           ",FNCICLOACRED(PRC.CDGEM,PRC.CDGCL,CRD.FECHA_CALC) CICLO_ACRED " +
                           ",EF2.NOMBRE NACIOEF " +
                           "FROM PRN, " +
                           "NS, " +
                           "PRC, " +
                           "CL, " +
                           "CO, " +
                           "COL, " +
                           "LO, " +
                           "MU, " +
                           "PI, " +
                           "AE, " +
                           "SC, " +
                           "EF, " +
                           "PE, " +
                           "RG, " +
                           "EF EF2, " +
                           "TBL_CIERRE_DIA CRD " +
                           "WHERE PRN.CDGEM = NS.CDGEM " +
                           "AND PRN.CDGNS = NS.CODIGO " +
                           "AND PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRC.CLNS = 'G' " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRC.CDGEM = CL.CDGEM " +
                           "AND PRC.CDGCL = CL.CODIGO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND RG.CDGEM = CO.CDGEM " +
                           "AND RG.CODIGO = CO.CDGRG " +
                           "AND CL.CDGPAI = COL.CDGPAI " +
                           "AND CL.CDGEF = COL.CDGEF " +
                           "AND CL.CDGMU = COL.CDGMU " +
                           "AND CL.CDGLO = COL.CDGLO " +
                           "AND CL.CDGCOL = COL.CODIGO " +
                           "AND COL.CDGPAI = LO.CDGPAI " +
                           "AND COL.CDGEF = LO.CDGEF " +
                           "AND COL.CDGMU = LO.CDGMU " +
                           "AND COL.CDGLO = LO.CODIGO " +
                           "AND LO.CDGPAI = MU.CDGPAI " +
                           "AND LO.CDGEF = MU.CDGEF " +
                           "AND LO.CDGMU = MU.CODIGO " +
                           "AND CL.CDGPAI = EF.CDGPAI " +
                           "AND CL.CDGEF = EF.CODIGO " +
                           "AND CL.CDGPAI = EF2.CDGPAI " +
                           "AND CL.NACIOEF = EF2.CODIGO " +
                           "AND PRC.CDGEM = SC.CDGEM " +
                           "AND PRC.CDGNS = SC.CDGNS " +
                           "AND PRC.CLNS = SC.CLNS " +
                           "AND PRC.CICLO = SC.CICLO " +
                           "AND PRC.CDGCL = SC.CDGCL " +
                           "AND PI.CDGEM(+) = SC.CDGEM " +
                           "AND PI.CDGCL(+) = SC.CDGCL " +
                           "AND PI.PROYECTO(+) = SC.CDGPI " +
                           "AND AE.CDGEM(+) = PI.CDGEM " +
                           "AND AE.CDGSE(+) = PI.CDGSE " +
                           "AND AE.CDGGI(+) = PI.CDGGI " +
                           "AND AE.CODIGO(+) = PI.CDGAE " +
                           "AND PRN.CDGEM = PE.CDGEM " +
                           "AND PRN.CDGOCPE = PE.CODIGO " +
                           "AND PRN.CDGEM = CRD.CDGEM " +
                           "AND PRN.CDGNS = CRD.CDGCLNS " +
                           queryRegion +
                           queryCoord +
                           queryAsesor +
                           "AND (PRN.CICLOD = CRD.CICLO OR PRN.CICLO = CRD.CICLO) " +
                           "AND TRUNC(PRC.ENTREGA) > '" + fecha + "' " +
                           "AND PRC.SITUACION = 'D' " +
                           "AND CRD.CDGEM = '" + empresa + "' " +
                           "AND CRD.FECHA_CALC = '" + fecha + "' " +
                           "AND CRD.CLNS = 'G' " +
                           "AND CRD.SITUACION = 'E' " +
                /******** UNION CON CREDITOS INDIVIDUALES *********/
                           "UNION " +
                            "SELECT DISTINCT PRC.CDGCO COD_SUC " +
                            ",CO.NOMBRE SUCURSAL " +
                            ",CO.CDGRG COD_REG " +
                            ",RG.NOMBRE REGION " +
                            ",NULL COD_GPO " +
                            ",NULL GRUPO " +
                            ",PRC.CICLO " +
                            ",PRC.INICIO " +
                            ",PE.CODIGO COD_ASESOR " +
                            ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                            ",PRC.CDGCL COD_CTE " +
                            ",CL.PRIMAPE PATERNO " +
                            ",CL.SEGAPE MATERNO " +
                            ",CL.NOMBRE1 || ' ' || CL.NOMBRE2 NOMBRE_S " +
                            ",TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') FECHA_NACIMIENTO " +
                            ",CL.SEXO " +
                            ",trunc((months_between(sysdate, CL.NACIMIENTO))/12) EDAD " +
                            ",decode(nvl(CL.EDOCIVIL,''), 'S', 'Soltero', " +
                                                         "'C', 'Casado', " +
                                                         "'U', 'Unión Libre', " +
                                                         "'D', 'Divorciado', " +
                                                         "'V', 'Viudo', " +
                                                         "'', '') AS EDO_CIVIL " +
                            ",CL.CALLE " +
                            ",MU.NOMBRE MUNICIPIO " +
                            ",EF.NOMBRE ENTIDAD_F " +
                            ",COL.CDGPOSTAL " +
                            ",CL.TELEFONO " +
                            ",decode(nvl(CL.NIVESCOLAR,''), 'U', 'TECNICO SUPERIOR', " +
                                                           "'R', 'MAESTRIA', " +
                                                           "'P', 'PRIMARIA', " +
                                                           "'B', 'PREPARATORIA', " +
                                                           "'C', 'CARRERA CORTA', " +
                                                           "'T', 'TECNICO', " +
                                                           "'N', 'NINGUNA', " +
                                                           "'S', 'SECUNDARIA', " +
                                                           "'L', 'LICENCIATURA', " +
                                                           "'O', 'DOCTORADO', " +
                                                           "'', 'NINGUNA') AS ESCOLARIDAD " +
                            ",CL.RFC " +
                            ",CL.CURP " +
                            ",decode(nvl(PRC.SITUACION,''), 'L', 'Liquidado', " +
                                                           "'E', 'Entregado', " +
                                                           "'A', 'Aut. Cartera', " +
                                                           "'T', 'Aut. Tesoreria', " +
                                                            "'', '') AS SITUACION " +
                            ",PRC.CANTENTRE CANT_ENTRE " +
                            ",LO.NOMBRE LOCALIDAD " +
                            ",COL.NOMBRE COLONIA " +
                            ",PI.NOMBRE PROYECTO " +
                            ",AE.CDGSE " +
                            ",(SELECT NOMBRE FROM SE WHERE CDGEM = AE.CDGEM AND CODIGO = AE.CDGSE) NOMSE " +
                            ",AE.CDGGI " +
                            ",(SELECT NOMBRE FROM GI WHERE CDGEM = AE.CDGEM AND CDGSE = AE.CDGSE AND CODIGO = AE.CDGGI) NOMGI " +
                            ",AE.CODIGO CDGAE " +
                            ",AE.NOMBRE NOMAE " +
                            ",CL.NODEPEND " +
                            ",FNCICLOACRED(PRC.CDGEM,PRC.CDGCL,CRD.FECHA_CALC) CICLO_ACRED " +
                            ",EF2.NOMBRE NACIOEF " +
                            "FROM PRC, CL,CO, COL, LO, MU, PI, AE, SC, EF,PE, RG, EF EF2, TBL_CIERRE_DIA CRD " +
                            "WHERE PRC.CDGEM = CL.CDGEM " +
                            "AND PRC.CDGCL = CL.CODIGO " +
                            "AND PRC.CDGEM = CO.CDGEM " +
                            "AND PRC.CDGCO = CO.CODIGO " +
                            "AND RG.CDGEM = CO.CDGEM " +
                            "AND RG.CODIGO = CO.CDGRG " +
                            "AND CL.CDGPAI = COL.CDGPAI " +
                            "AND CL.CDGEF = COL.CDGEF " +
                            "AND CL.CDGMU = COL.CDGMU " +
                            "AND CL.CDGLO = COL.CDGLO " +
                            "AND CL.CDGCOL = COL.CODIGO " +
                            "AND COL.CDGPAI = LO.CDGPAI " +
                            "AND COL.CDGEF = LO.CDGEF " +
                            "AND COL.CDGMU = LO.CDGMU " +
                            "AND COL.CDGLO = LO.CODIGO " +
                            "AND LO.CDGPAI = MU.CDGPAI " +
                            "AND LO.CDGEF = MU.CDGEF " +
                            "AND LO.CDGMU = MU.CODIGO " +
                            "AND CL.CDGPAI= EF.CDGPAI " +
                            "AND CL.CDGEF = EF.CODIGO " +
                            "AND CL.CDGPAI = EF2.CDGPAI " +
                            "AND CL.NACIOEF = EF2.CODIGO " +
                            "AND PRC.CDGEM = SC.CDGEM " +
                            "AND PRC.CDGCL = SC.CDGCL " +
                            "AND PRC.CLNS = SC.CLNS " +
                            "AND PRC.CICLO = SC.CICLO " +
                            "AND PI.CDGEM (+)= SC.CDGEM " +
                            "AND PI.CDGCL (+)= SC.CDGCL " +
                            "AND PI.PROYECTO (+)= SC.CDGPI " +
                            "AND AE.CDGEM (+)= PI.CDGEM " +
                            "AND AE.CDGSE (+)= PI.CDGSE " +
                            "AND AE.CDGGI (+)= PI.CDGGI " +
                            "AND AE.CODIGO (+)= PI.CDGAE " +
                            "AND PRC.CDGEM = PE.CDGEM " +
                            "AND PRC.CDGOCPE = PE.CODIGO " +
                            "AND PRC.CDGEM = CRD.CDGEM " +
                            "AND PRC.CDGCLNS = CRD.CDGCLNS " +
                            "AND PRC.CLNS = CRD.CLNS " +
                            "AND PRC.CICLO = CRD.CICLO " +
                            queryRegion +
                            queryCoordInd +
                            queryAsesorInd +
                            "AND PRC.CANTENTRE > 0 " +
                            "AND PRC.SITUACION <> 'D' " +
                            "AND CRD.CDGEM = '" + empresa + "' " +
                            "AND CRD.FECHA_CALC = '" + fecha + "' " +
                            "AND CRD.CLNS = 'I' " +
                            "AND CRD.SITUACION = 'E' " +
                //INFORMACION DE ACREDITADOS INDIVIDUALES QUE DEVOLVIERON SU CREDITO
                            "UNION " +
                            "SELECT DISTINCT " +
                            "PRC.CDGCO COD_SUC, " +
                            "CO.NOMBRE SUCURSAL, " +
                            "CO.CDGRG COD_REG, " +
                            "RG.NOMBRE REGION, " +
                            "NULL COD_GPO, " +
                            "NULL GRUPO, " +
                            "PRC.CICLO, " +
                            "PRC.INICIO, " +
                            "PE.CODIGO COD_ASESOR, " +
                            "NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR, " +
                            "PRC.CDGCL COD_CTE, " +
                            "CL.PRIMAPE PATERNO, " +
                            "CL.SEGAPE MATERNO, " +
                            "CL.NOMBRE1 || ' ' || CL.NOMBRE2 NOMBRE_S, " +
                            "TO_CHAR (CL.NACIMIENTO, 'DD/MM/YYYY') FECHA_NACIMIENTO, " +
                            "CL.SEXO, " +
                            "TRUNC ( (MONTHS_BETWEEN (SYSDATE, CL.NACIMIENTO)) / 12) EDAD, " +
                            "DECODE (NVL (CL.EDOCIVIL, ''), " +
                                 "'S', 'Soltero', " +
                                 "'C', 'Casado', " +
                                 "'U', 'Unión Libre', " +
                                 "'D', 'Divorciado', " +
                                 "'V', 'Viudo', " +
                                 "'', '') " +
                            "AS EDO_CIVIL, " +
                            "CL.CALLE, " +
                            "MU.NOMBRE MUNICIPIO, " +
                            "EF.NOMBRE ENTIDAD_F, " +
                            "COL.CDGPOSTAL, " +
                            "CL.TELEFONO, " +
                            "DECODE (NVL (CL.NIVESCOLAR, ''), " +
                                  "'U', 'TECNICO SUPERIOR', " +
                                  "'R', 'MAESTRIA', " +
                                  "'P', 'PRIMARIA', " +
                                  "'B', 'PREPARATORIA', " +
                                  "'C', 'CARRERA CORTA', " +
                                  "'T', 'TECNICO', " +
                                  "'N', 'NINGUNA', " +
                                  "'S', 'SECUNDARIA', " +
                                  "'L', 'LICENCIATURA', " +
                                  "'O', 'DOCTORADO', " +
                                  "'', 'NINGUNA') " +
                            "AS ESCOLARIDAD, " +
                            "CL.RFC, " +
                            "CL.CURP, " +
                            "DECODE (NVL (PRC.SITUACION, ''), " +
                                  "'L', 'Liquidado', " +
                                  "'E', 'Entregado', " +
                                  "'A', 'Aut. Cartera', " +
                                  "'T', 'Aut. Tesoreria', " +
                                  "'', '') " +
                            "AS SITUACION, " +
                            "PRC.CANTENTRE CANT_ENTRE, " +
                            "LO.NOMBRE LOCALIDAD, " +
                            "COL.NOMBRE COLONIA, " +
                            "PI.NOMBRE PROYECTO, " +
                            "AE.CDGSE, " +
                            "(SELECT NOMBRE " +
                               "FROM SE " +
                               "WHERE CDGEM = AE.CDGEM AND CODIGO = AE.CDGSE) NOMSE, " +
                            "AE.CDGGI, " +
                            "(SELECT NOMBRE " +
                               "FROM GI " +
                               "WHERE CDGEM = AE.CDGEM AND CDGSE = AE.CDGSE AND CODIGO = AE.CDGGI) NOMGI, " +
                            "AE.CODIGO CDGAE, " +
                            "AE.NOMBRE NOMAE, " +
                            "CL.NODEPEND " +
                            ",FNCICLOACRED(PRC.CDGEM,PRC.CDGCL,CRD.FECHA_CALC) CICLO_ACRED " +
                            ",EF2.NOMBRE NACIOEF " +
                            "FROM PRC, " +
                            "CL, " +
                            "CO, " +
                            "COL, " +
                            "LO, " +
                            "MU, " +
                            "PI, " +
                            "AE, " +
                            "SC, " +
                            "EF, " +
                            "PE, " +
                            "RG, " +
                            "EF EF2, " +
                            "TBL_CIERRE_DIA CRD " +
                            "WHERE PRC.CDGEM = CL.CDGEM " +
                            "AND PRC.CDGCL = CL.CODIGO " +
                            "AND PRC.CDGEM = CO.CDGEM " +
                            "AND PRC.CDGCO = CO.CODIGO " +
                            "AND RG.CDGEM = CO.CDGEM " +
                            "AND RG.CODIGO = CO.CDGRG " +
                            "AND CL.CDGPAI = COL.CDGPAI " +
                            "AND CL.CDGEF = COL.CDGEF " +
                            "AND CL.CDGMU = COL.CDGMU " +
                            "AND CL.CDGLO = COL.CDGLO " +
                            "AND CL.CDGCOL = COL.CODIGO " +
                            "AND COL.CDGPAI = LO.CDGPAI " +
                            "AND COL.CDGEF = LO.CDGEF " +
                            "AND COL.CDGMU = LO.CDGMU " +
                            "AND COL.CDGLO = LO.CODIGO " +
                            "AND LO.CDGPAI = MU.CDGPAI " +
                            "AND LO.CDGEF = MU.CDGEF " +
                            "AND LO.CDGMU = MU.CODIGO " +
                            "AND CL.CDGPAI = EF.CDGPAI " +
                            "AND CL.CDGEF = EF.CODIGO " +
                            "AND CL.CDGPAI = EF2.CDGPAI " +
                            "AND CL.NACIOEF = EF2.CODIGO " +
                            "AND PRC.CDGEM = SC.CDGEM " +
                            "AND PRC.CDGCL = SC.CDGCL " +
                            "AND PRC.CLNS = SC.CLNS " +
                            "AND PRC.CICLO = SC.CICLO " +
                            "AND PI.CDGEM(+) = SC.CDGEM " +
                            "AND PI.CDGCL(+) = SC.CDGCL " +
                            "AND PI.PROYECTO(+) = SC.CDGPI " +
                            "AND AE.CDGEM(+) = PI.CDGEM " +
                            "AND AE.CDGSE(+) = PI.CDGSE " +
                            "AND AE.CDGGI(+) = PI.CDGGI " +
                            "AND AE.CODIGO(+) = PI.CDGAE " +
                            "AND PRC.CDGEM = PE.CDGEM " +
                            "AND PRC.CDGOCPE = PE.CODIGO " +
                            "AND PRC.CDGEM = CRD.CDGEM " +
                            "AND PRC.CDGCLNS = CRD.CDGCLNS " +
                            "AND PRC.CLNS = CRD.CLNS " +
                            queryRegion +
                            queryCoordInd +
                            queryAsesorInd +
                            "AND (PRC.CICLOD = CRD.CICLO OR PRC.CICLO = CRD.CICLO) " +
                            "AND TRUNC(PRC.ENTREGA) > '" + fecha + "' " +
                            "AND PRC.SITUACION = 'D' " +
                            "AND CRD.CDGEM = '" + empresa + "' " +
                            "AND CRD.FECHA_CALC = '" + fecha + "' " +
                            "AND CRD.CLNS = 'I' " +
                            "AND CRD.SITUACION = 'E'";
            //"ORDER BY PRN.CDGCO,PRN.CDGNS,PRN.CICLO,PRC.CDGCL"; 

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE OBTIENE CLIENTES CAPTURADOS DURANTE EL PERIODO INDICADO
    [WebMethod]
    public string getRepClientesCapturados(string mes, string anio)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT A.CODIGO_REGION " +
                          ",A.REGION " +
                          ",A.CODIGO_SUCURSAL" +
                          ",A.SUCURSAL " +
                          ",A.CODIGO " +
                          ",(SELECT NOMBREC(NULL,NULL,'I','N',NOMBRE1,NOMBRE2,PRIMAPE,SEGAPE) FROM PE WHERE CDGEM = A.CDGEM AND CODIGO = A.CODIGO) NOMBRE " +
                          ",A.CANTIDAD " +
                          "FROM (SELECT RG.CODIGO CODIGO_REGION , RG.NOMBRE REGION, CO.CODIGO CODIGO_SUCURSAL , CO.NOMBRE SUCURSAL, PE.CDGEM, PE.CODIGO, COUNT(PE.CODIGO) CANTIDAD FROM SN, SC, PE, CO, RG " +
                          "WHERE SN.CDGEM = '" + empresa + "' " +
                          "AND SN.INICIO BETWEEN TO_DATE('01/" + mes + "/" + anio + "') AND (LAST_DAY(TO_DATE('01/" + mes + "/" + anio + "'))) " +
                          "AND SN.SITUACION IN ('A') " +
                          "AND PE.CDGEM = SN.CDGEM " +
                          "AND PE.CODIGO = SN.ACTUALIZACPE " +
                          "AND CO.CDGEM = SN.CDGEM " +
                          "AND CO.CODIGO = SN.CDGCO " +
                          "AND RG.CDGEM = CO.CDGEM " +
                          "AND RG.CODIGO = CO.CDGRG " +
                          "AND SC.CDGEM = SN.CDGEM " +
                          "AND SC.CDGNS = SN.CDGNS " +
                          "AND SC.CICLO = SN.CICLO " +
                          "AND SC.SOLICITUD = SN.SOLICITUD " +
                          "AND SC.SITUACION IN ('A') " +
                          "GROUP BY PE.CDGEM, PE.CODIGO, CO.NOMBRE, CO.CODIGO , RG.NOMBRE , RG.CODIGO) A";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DE LOS CLIENTES QUE NO RENOVARON
    [WebMethod]
    public string getRepClientesDesercion(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            string query = "SELECT PRN.CDGCO COD_SUC " +
                           ",CO.NOMBRE SUCURSAL " +
                           ",PRN.CDGNS COD_GPO " +
                           ",NS.NOMBRE GRUPO " +
                           ",PRN.CICLO " +
                           ",PRC.CDGCL COD_CTE " +
                           ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) CLIENTE " +
                           ",CASE WHEN (SELECT COUNT(*) " +
                                       "FROM PRC A, PRN B " +
                                       "WHERE A.CDGEM = PRC.CDGEM " +
                                       "AND A.CDGCL = PRC.CDGCL " +
                                       "AND A.CLNS = 'G' " +
                                       "AND A.SITUACION = 'E' " +
                                       "AND A.CANTENTRE > 0 " +
                                       "AND A.CDGNS || A.CICLO NOT IN (SELECT CDGCLNS || CICLO " +
                                                                        "FROM PRN_LEGAL " +
                                                                        "WHERE CDGEM = A.CDGEM " +
                                                                        "AND TIPO IN ('O','R')) " +
                                       "AND B.CDGEM = A.CDGEM " +
                                       "AND B.CDGNS = A.CDGNS " +
                                       "AND B.CICLO = A.CICLO " +
                                       "AND B.INICIO > PRN.INICIO) > 0 THEN " +
                                "(SELECT B.CDGOCPE " +
                                "FROM PRC A, PRN B " +
                                "WHERE A.CDGEM = PRC.CDGEM " +
                                "AND A.CDGCL = PRC.CDGCL " +
                                "AND A.CLNS = 'G' " +
                                "AND A.SITUACION = 'E' " +
                                "AND A.CANTENTRE > 0 " +
                                "AND A.CDGNS || A.CICLO || A.CLNS NOT IN (SELECT CDGCLNS || CICLO || CLNS " +
                                                                          "FROM PRN_LEGAL " +
                                                                          "WHERE CDGEM = A.CDGEM " +
                                                                          "AND TIPO IN ('O','R')) " +
                                "AND B.CDGEM = A.CDGEM " +
                                "AND B.CDGNS = A.CDGNS " +
                                "AND B.CICLO = A.CICLO " +
                                "AND B.INICIO > PRN.INICIO) " +
                           "ELSE " +
                                "PRN.CDGOCPE " +
                           "END COD_ASESOR " +
                           ",CASE WHEN (SELECT COUNT(*) " +
                                       "FROM PRC A, PRN B " +
                                       "WHERE A.CDGEM = PRC.CDGEM " +
                                       "AND A.CDGCL = PRC.CDGCL " +
                                       "AND A.CLNS = 'G' " +
                                       "AND A.SITUACION = 'E' " +
                                       "AND A.CANTENTRE > 0 " +
                                       "AND A.CDGNS || A.CICLO NOT IN (SELECT CDGCLNS || CICLO " +
                                                                        "FROM PRN_LEGAL " +
                                                                        "WHERE CDGEM = A.CDGEM " +
                                                                        "AND TIPO IN ('O','R')) " +
                                       "AND B.CDGEM = A.CDGEM " +
                                       "AND B.CDGNS = A.CDGNS " +
                                       "AND B.CICLO = A.CICLO " +
                                       "AND B.INICIO > PRN.INICIO) > 0 THEN " +
                                 "(SELECT NOMBREC(NULL,NULL,'I','N',C.NOMBRE1,C.NOMBRE2,C.PRIMAPE,C.SEGAPE) " +
                                 "FROM PRC A, PRN B, PE C " +
                                 "WHERE A.CDGEM = PRC.CDGEM " +
                                 "AND A.CDGCL = PRC.CDGCL " +
                                 "AND A.CLNS = 'G' " +
                                 "AND A.SITUACION = 'E' " +
                                 "AND A.CANTENTRE > 0 " +
                                 "AND A.CDGNS || A.CICLO || A.CLNS NOT IN (SELECT CDGCLNS || CICLO || CLNS " +
                                                                           "FROM PRN_LEGAL " +
                                                                           "WHERE CDGEM = A.CDGEM " +
                                                                           "AND TIPO IN ('O','R')) " +
                                 "AND B.CDGEM = A.CDGEM " +
                                 "AND B.CDGNS = A.CDGNS " +
                                 "AND B.CICLO = A.CICLO " +
                                 "AND B.INICIO > PRN.INICIO " +
                                 "AND C.CDGEM = B.CDGEM " +
                                 "AND C.CODIGO = B.CDGOCPE) " +
                           "ELSE " +
                                "NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                           "END ASESOR " +
                           ",CASE WHEN (SELECT COUNT(*) " +
                                       "FROM PRC A, PRN B " +
                                       "WHERE A.CDGEM = PRC.CDGEM " +
                                       "AND A.CDGCL = PRC.CDGCL " +
                                       "AND A.CLNS = 'G' " +
                                       "AND A.SITUACION = 'E' " +
                                       "AND A.CANTENTRE > 0 " +
                                       "AND A.CDGNS || A.CICLO NOT IN (SELECT CDGCLNS || CICLO " +
                                                                        "FROM PRN_LEGAL " +
                                                                        "WHERE CDGEM = A.CDGEM " +
                                                                        "AND TIPO IN ('O','R')) " +
                                       "AND B.CDGEM = A.CDGEM " +
                                       "AND B.CDGNS = A.CDGNS " +
                                       "AND B.CICLO = A.CICLO " +
                                       "AND B.INICIO > PRN.INICIO) > 0 THEN " +
                                 "(SELECT C.TELEFONO " +
                                 "FROM PRC A, PRN B, PE C " +
                                 "WHERE A.CDGEM = PRC.CDGEM " +
                                 "AND A.CDGCL = PRC.CDGCL " +
                                 "AND A.CLNS = 'G' " +
                                 "AND A.SITUACION = 'E' " +
                                 "AND A.CANTENTRE > 0 " +
                                 "AND A.CDGNS || A.CICLO || A.CLNS NOT IN (SELECT CDGCLNS || CICLO || CLNS " +
                                                                           "FROM PRN_LEGAL " +
                                                                           "WHERE CDGEM = A.CDGEM " +
                                                                           "AND TIPO IN ('O','R')) " +
                                 "AND B.CDGEM = A.CDGEM " +
                                 "AND B.CDGNS = A.CDGNS " +
                                 "AND B.CICLO = A.CICLO " +
                                 "AND B.INICIO > PRN.INICIO " +
                                 "AND C.CDGEM = B.CDGEM " +
                                 "AND C.CODIGO = B.CDGOCPE) " +
                           "ELSE " +
                                "PE.TELEFONO " +
                           "END NOMINA " +
                           ",CASE WHEN (SELECT COUNT(*) " +
                                       "FROM PRC A, PRN B " +
                                       "WHERE A.CDGEM = PRC.CDGEM " +
                                       "AND A.CDGCL = PRC.CDGCL " +
                                       "AND A.CLNS = 'G' " +
                                       "AND A.SITUACION = 'E' " +
                                       "AND A.CANTENTRE > 0 " +
                                       "AND B.CDGEM = A.CDGEM " +
                                       "AND B.CDGNS = A.CDGNS " +
                                       "AND B.CICLO = A.CICLO " +
                                       "AND B.INICIO > PRN.INICIO) > 0 THEN 'RENOVO' " +
                           "ELSE 'DESERTO' " +
                           "END ESTATUS " +
                           "FROM PRN, PRC, PE, NS, CO, CL " +
                           "WHERE PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRN.CDGEM = NS.CDGEM " +
                           "AND PRN.CDGNS = NS.CODIGO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND PRC.CDGEM = CL.CDGEM " +
                           "AND PRC.CDGCL = CL.CODIGO " +
                           "AND PRN.CDGEM = PE.CDGEM " +
                           "AND PRN.CDGOCPE = PE.CODIGO " +
                           "AND PRN.CDGEM = '" + empresa + "' " +
                           "AND FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                           "AND FNFECHAFINREAL(PRN.CDGEM,PRN.CDGNS,'G',PRN.CICLO) <= '" + fechaFin + "' " +
                           "AND PRC.CANTENTRE > 0 " +
                           "AND PRN.CDGNS || PRN.CICLO NOT IN (SELECT CDGCLNS || CICLO " +
                                                              "FROM PRN_LEGAL " +
                                                              "WHERE CDGEM = PRN.CDGEM " +
                                                              "AND TIPO IN ('O','R'))";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION GENERAL DE LOS CLIENTES MARCADOS PARA FONDEO
    [WebMethod]
    public string getRepClientesFondeo(string orgFond, string lineaCred, string fecSaldo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryLinea = string.Empty;

        if (lineaCred != "0")
            queryLinea = "AND F.CDGLC = '" + lineaCred + "' ";

        try
        {
            string query = "SELECT '165' ORG_ID " +
                           ",PRC.CDGCL " +
                           ",CL.CURP " +
                           ",'''' || CL.IFE IFE " +
                           ",CL.PRIMAPE " +
                           ",CL.SEGAPE " +
                           ",CL.NOMBRE1 || ' ' || CL.NOMBRE2 NOMBRE " +
                           ", TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') NACIMIENTO " +
                           ",CASE WHEN TO_NUMBER(CL.NACIOEF) > 0 AND TO_NUMBER(CL.NACIOEF) < 33 THEN CL.NACIOEF ELSE '00' END EDO_NACIM " +
                           ",CASE WHEN CL.SEXO = 'F' THEN 'M' ELSE 'H' END SEXO " +
                           ",NULL TELEFONO " + //--,CL.TELEFONO
                           ",decode(nvl(CL.EDOCIVIL,''), 'S', '1', " + //SOLTERO 
                                                        "'C', '2', " + //--CASADO 
                                                        "'U', '5', " + //--UNION LIBRE
                                                        "'D', '4', " + //--DIVORCIADO
                                                        "'V', '3', " + //--VIUDO  
                                                        "'', '1') AS EDO_CIVIL " +
                           ",CL.CDGEF " +
                           ",CL.CDGEF || CL.CDGMU CDGMU " +
                //--,CL.CDGLO
                           ",CL.CDGEF || CL.CDGMU || '0' ||CL.CDGLO AS CDGLO " +
                           ",CL.CALLE " +
                           ", 'NA' NUM_EXT " +
                           ", NULL NUM_INT " +
                           ",COL.NOMBRE COLONIA " +
                           ",COL.CDGPOSTAL " +
                           ",'G' METODOLOGIA " +
                           ",NS.NOMBRE NOMBRE_GPO " +
                           ",decode(nvl(CL.NIVESCOLAR,''), 'U', '3', " + // --TECNICO SUPERIOS "
                                                          "'R', '6', " + //--MAESTRIA
                                                          "'P', '1', " + //-- PRIMARIA
                                                          "'B', '4', " + //--PREPARATORIA
                                                          "'C', '3', " + //--CARRERA CORTA
                                                          "'T', '3', " + //--TECNICO
                                                          "'N', '7', " + //--NINGUNA
                                                          "'S', '2', " + //--SECUNDARIA
                                                          "'L', '5', " + //--LICENCIATURA
                                                          "'O', '6', " + //-- DOCTORADO
                                                          "'', '7') AS ESCOLARIDAD " +
                           ",'5619' ACTIVIDAD " + //--, PI.NOMBRE ACTIVIDAD    --OTROS SERVICIOS DE APOYO A LOS NEGOCIOS 
                           ", TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FEC_INICIO_ACT " +
                           ", '17' UBI_NEG " + //--OTRO SIN LOCAL 
                           ", '0' PERS_TRAB " +
                           ", '1' INGRE_SEM " +
                           ", '4' ROL_HOGAR " + //--OTRO 
                           ", CO.NOMBRE SUCURSAL " +
                           "FROM TBL_CIERRE_DIA CD, PRC, CL, COL, NS,SC,PI, CO, PRN, PRC_FONDEO F " +
                           "WHERE CD.CDGEM = PRC.CDGEM " +
                           "AND CD.CDGCLNS = PRC.CDGNS " +
                           "AND CD.CLNS = PRC.CLNS " +
                           "AND CD.CICLO = PRC.CICLO " +
                           "AND PRC.CDGEM = CL.CDGEM " +
                           "AND PRC.CDGCL = CL.CODIGO " +
                           "AND CL.CDGPAI = COL.CDGPAI " +
                           "AND CL.CDGEF = COL.CDGEF " +
                           "AND CL.CDGMU = COL.CDGMU " +
                           "AND CL.CDGLO = COL.CDGLO " +
                           "AND CL.CDGCOL = COL.CODIGO " +
                           "AND CD.CDGEM = NS.CDGEM " +
                           "AND CD.CDGCLNS = NS.CODIGO " +
                           "AND PRC.CDGEM = SC.CDGEM " +
                           "AND PRC.CDGNS = SC.CDGNS " +
                           "AND PRC.CDGCL = SC.CDGCL " +
                           "AND PRC.CICLO = SC.CICLO " +
                           "AND SC.CANTAUTOR > 0 " +
                           "AND PI.CDGEM (+)=  SC.CDGEM " +
                           "AND PI.CDGCL (+)= SC.CDGCL " +
                           "AND PI.CDGNS (+)= SC.CDGNS " +
                           "AND PI.PROYECTO (+)=  SC.CDGPI " +
                           "AND PRC.CDGEM = PRN.CDGEM " +
                           "AND PRC.CDGNS = PRN.CDGNS " +
                           "AND PRC.CICLO = PRN.CICLO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND PRC.CDGEM = F.CDGEM " +
                           "AND PRC.CDGNS = F.CDGNS " +
                           "AND PRC.CICLO = F.CICLO " +
                           "AND PRC.CDGCL = F.CDGCL " +
                           "AND CD.CDGEM = '" + empresa + "' " +
                           "AND CD.FECHA_CALC = '" + fecSaldo + "' " +
                           "AND CD.CLNS = 'G' " +
                           "AND PRN.INICIO >= TRUNC(TO_DATE('" + fecSaldo + "','DD/MM/YYYY'), 'MM') " +
                           "AND F.CDGEM = CD.CDGEM " +
                           "AND F.CDGORF = '" + orgFond + "' " +
                           queryLinea +
                           "AND F.FREPSDO = '" + fecSaldo + "'";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION DE LOS CLIENTES NUEVOS Y DE RENOVACION
    [WebMethod]
    public string getRepClientesNuevosRenov(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            string query = "SELECT PRN.CDGCO COD_SUC " +
                           ",CO.NOMBRE SUCURSAL " +
                           ",PRN.CDGNS COD_GPO " +
                           ",NS.NOMBRE GRUPO " +
                           ",PRN.CICLO " +
                           ",PRN.CDGOCPE COD_ASE " +
                           ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                           ",PRC.CDGCL COD_CTE " +
                           ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) CLIENTE " +
                           ",CASE WHEN (SELECT COUNT(*) " +
                                       "FROM PRN A, PRC B, CL C " +
                                       "WHERE A.CDGEM = B.CDGEM " + 
                                       "AND A.CDGNS = B.CDGNS " +
                                       "AND A.CICLO = B.CICLO " +
                                       "AND B.CDGEM = C.CDGEM " +
                                       "AND B.CDGCL = C.CODIGO " +
                                       "AND A.CDGEM = '" + empresa + "' " + 
                                       "AND A.INICIO < '" + fechaIni + "' " +
                                       "AND A.SITUACION IN ('E','L') " +
                                       "AND B.SITUACION IN ('E','L') " +
                                       "AND C.CURP = CL.CURP) > 0 THEN 'Renovación' " +
                           "ELSE 'Nuevo' " +
                           "END TIPO " +
                           "FROM PRN, PRC, CL, PE, CO, NS " +
                           "WHERE PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRC.CDGEM = CL.CDGEM " +
                           "AND PRC.CDGCL = CL.CODIGO " +
                           "AND PRN.CDGEM = CO.CDGEM " +
                           "AND PRN.CDGCO = CO.CODIGO " +
                           "AND PRN.CDGEM = PE.CDGEM " +
                           "AND PRN.CDGOCPE = PE.CODIGO " +
                           "AND PRN.CDGEM = NS.CDGEM " +
                           "AND PRN.CDGNS = NS.CODIGO " +
                           "AND PRN.CDGEM = '" + empresa + "' " +
                           "AND PRN.INICIO BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                           "AND PRC.SITUACION = 'E' " +
                           "AND PRN.SITUACION = 'E' " +
                           "AND PRC.CANTENTRE > 0";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CONCILIACION DE GL
    [WebMethod]
    public string getRepConciliacionGL(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_CONCILIACION_GL", CommandType.StoredProcedure, oP.ParamsConciliacionGL(empresa, fecha, usuario));

            string query = "SELECT CDGEM, " +
                           "NOMRG, " +
                           "NOMCO, " +
                           "CDGOCPE, " +
                           "NOMPE, " +
                           "CDGCLNS, " +
                           "NOMNS, " +
                           "SITUACION, " +
                           "ROUND(SDOINI,2) SDO_INI, " +
                           "ROUND(DEPBANCOS,2) DEP_BANCOS, " +
                           "ROUND(DEPEXCEDENTE,2) DEP_EXCEDENTE, " +
                           "ROUND(DEPTRASPASO,2) DEP_TRASPASO, " +
                           "ROUND(DEVEXCEDENTE,2) DEV_EXCEDENTE, " +
                           "ROUND(DEVCHEQUECAN,2) DEV_CHEQUE_CAN, " +
                           "ROUND(DEVSOLICITUD,2) DEV_SOLICITUD, " +
                           "ROUND(DEVRECHAZO,2) DEV_RECHAZO, " +
                           "ROUND(MICROSEG,2) MICROSEG, " +
                           "ROUND(CANTRASPASO,2) CAN_TRASPASO, " +
                           "ROUND(CANDEVGL,2) CAN_DEV_GL, " +
                           "ROUND(CANMICROSEG,2) CAN_MICROSEG, " +
                           "ROUND(PAGOGL,2) PAGO_GL, " +
                           "ROUND(PAGOMORAT,2) PAGO_MORAT, " +
                           "ROUND(PAGOTROGPO,2) PAGO_OTRO_GPO, " +
                           "ROUND(GLOTROGPO,2) GL_OTRO_GPO, " +
                           "ROUND(E_DEPBANCOS,2) E_DEP_BANCOS, " +
                           "ROUND(E_DEPEXCEDENTE,2) E_DEP_EXCEDENTE, " +
                           "ROUND(E_DEPTRASPASO,2) E_DEP_TRASPASO, " +
                           "ROUND(E_DEVEXCEDENTE,2) E_DEV_EXCEDENTE, " +
                           "ROUND(E_DEVCHEQUECAN,2) E_DEV_CHEQUE_CAN, " +
                           "ROUND(E_DEVSOLICITUD,2) E_DEV_SOLICITUD, " +
                           "ROUND(E_DEVRECHAZO,2) E_DEV_RECHAZO, " + 
                           "ROUND(E_MICROSEG,2) E_MICROSEG, " +
                           "ROUND(E_CANTRASPASO,2) E_CAN_TRASPASO, " +
                           "ROUND(E_CANDEVGL,2) E_CAN_DEV_GL, " +
                           "ROUND(E_CANMICROSEG,2) E_CAN_MICROSEG, " +
                           "ROUND(E_PAGOGL,2) E_PAGO_GL, " +
                           "ROUND(E_PAGOMORAT,2) E_PAGO_MORAT, " +
                           "ROUND(E_PAGOTROGPO,2) E_PAGO_OTRO_GPO, " +
                           "ROUND(E_GLOTROGPO,2) E_GL_OTRO_GPO, " +
                           "ROUND(SDOFINAL,2) SDO_FINAL, " +
                           "TO_CHAR(FECHA_OPER,'DD/MM/YYYY') AS FOPER, " +
                           "TO_CHAR(sysdate,'DD/MM/YYYY') AS FECHAIMP, " +
                           "TO_CHAR(sysdate,'HH24:MI:SS') AS HORAIMP " +
                           "FROM REP_CONCILIACION_GL " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "'";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CONCILIACION DE PAGOS DE MICROSEGURO CON GL
    [WebMethod]
    public string getRepConciliacionMicroseguros(string fecIni, string fecFin, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = "SELECT A.* " +
                           ",(A.TOTAL - A.PAGADO) PAGO_PENDIENTE " +
                           "FROM (SELECT PRN.CDGNS GRUPO " +
                           ",(SELECT NOMBRE FROM NS WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGNS) NOMBRE_GRUPO " +
                           ",PRN.CICLO " +
                           ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') INICIO " +
                           ",ABS(NVL((SELECT SUM(CANTIDAD) FROM PAG_GAR_SIM WHERE CDGEM = PRN.CDGEM AND CDGCLNS = PRN.CDGNS AND CICLO = PRN.CICLO AND ESTATUS IN ('MS','CS')),0)) PAGADO " +
                           ",(SELECT SUM(TOTAL) FROM MICROSEGURO WHERE CDGCLNS = PRN.CDGNS AND CICLO = PRN.CICLO AND ESTATUS IN ('V','R')) TOTAL " +
                           ",FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G', '" + fecFin + "') SDO_GARANTIA " +
                           "FROM PRN " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND PAGOMICROSEG = 'P' " +
                           "AND INICIO BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND SITUACION IN ('E','L')) A " +
                           "ORDER BY TO_DATE(A.INICIO)";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE GENERA LA INFORMACIÓN DEL REPORTE DE LAS COMISIONES DISTRIBUIDAS MENSUALES
    [WebMethod]
    public string getRepComisionesDistribuidasMensual(int anio, int mes, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_COMISION_DIST_MN", CommandType.StoredProcedure,
                     oP.ParamsRepComisionesDistribuidasMensual(empresa, anio, mes, usuario));

            string query = " SELECT NOMASE, CDGASE, CDGCO, NOMCO, NO, CANTIDAD, CUENTA, BANCO " +
                "                 , ROUND(((NO/REG_BCO)) * COMISION_BCO, 2) COMISION_IND " +
                "                 , REFERENCIA, DIARIO, SEGMENTO, COMISION_BCO, REG_BCO " +
                "                 , TRUNC(ROUND((((COUNT(*) * 100)/REG_BCO) * COMISION_BCO) / 100),2) CTO_CHQ " +
                "              FROM REP_COMISION_DIST_MN " +
                "             WHERE CDGEM = '" + empresa + "' " +
                "               AND CDGPE = '" + usuario + "' " +
                "          GROUP BY NOMASE, CDGASE, CDGCO, NOMCO, NO, CANTIDAD, CUENTA, CDGIB, BANCO, REFERENCIA " +
                "                 , DIARIO, SEGMENTO, COMISION_BCO, REG_BCO " +
                "          ORDER BY CDGIB, NOMCO, CUENTA, NOMASE";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION DEL REPORTE DE LAS CONSULTAS DE CIRCULO DE CREDITO 
    [WebMethod]
    public string getRepConsCirculoCredito(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CO.NOMBRE NOMCO " +
                       ",RC.CDGNS " +
                       ",NS.NOMBRE NOMNS " +
                       ",RC.CICLO " +
                       ",CL.CODIGO CDGCL " +
                       ",NOMBREC(CL.CDGEM, CL.CODIGO, 'I','N',NULL,NULL,NULL,NULL) NOMCL " +
                       ",TO_CHAR(FCONSULTA,'DD/MM/YYYY') FECHA " +
                       ",FOLIOCONS CONSULTA " +
                       ",CASE WHEN (SELECT COUNT(*) FROM PRC WHERE CDGEM = RC.CDGEM AND CDGNS = RC.CDGNS AND CICLO = RC.CICLO AND CDGCL = RC.CDGCL) > 0 THEN " +
                            "(SELECT NOMBREC(NULL, NULL, 'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PRN, PE WHERE PRN.CDGEM = RC.CDGEM AND PRN.CDGNS = RC.CDGNS AND PRN.CICLO = RC.CICLO AND PE.CDGEM = PRN.CDGEM AND PE.CODIGO = PRN.CDGOCPE) " +
                       "ELSE " +
                            "(SELECT NOMBREC(NULL, NULL, 'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PE WHERE PE.CDGEM = RC.CDGEM AND PE.CODIGO = RC.CDGOCPE) " +
                       "END NOMPE " +
                       "FROM CONSULTA_REP_CREDITO RC " +
                       "LEFT JOIN NS ON RC.CDGEM = NS.CDGEM AND RC.CDGNS = NS.CODIGO " +
                       "LEFT JOIN CO ON NS.CDGEM = CO.CDGEM AND NS.CDGCO = CO.CODIGO " +
                       ",CL " +
                       "WHERE RC.CDGEM = '" + empresa + "' " +
                       "AND TRUNC(RC.FCONSULTA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND CL.CDGEM = RC.CDGEM " + 
                       "AND CL.CODIGO = RC.CDGCL";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL REPORTE DE LAS CONSULTAS DE CREDITO DE ACREDITADOS Y PROSPECTOS 
    [WebMethod]
    public string getRepConsCredito(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CRD.CDGCL CODIGO " +
                       ",(SELECT NOMBREC(CDGEM, CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) FROM CL WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGCL) NOMBRE " +
                       ",'ACREDITADO' TIPO " +
                       ",TO_CHAR(TRUNC(CRD.FCONSULTA), 'DD/MM/YYYY') FECHA_CONSULTA " +
                       ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', NOMBRE1, NOMBRE2, PRIMAPE, SEGAPE) FROM PE WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGPE) USUARIO_CONSULTA " +
                       ",CRD.CDGCLNS GRUPO " +
                       ",(SELECT NOMBRE FROM NS WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGCLNS) NOM_GRUPO " +
                       ",CRD.CICLO " +
                       ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', NOMBRE1, NOMBRE2, PRIMAPE, SEGAPE) FROM PE WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGOCPE) ASESOR " +
                       ",(SELECT NOMBRE FROM CO WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGCO) SUCURSAL " +
                       "FROM CONSULTA_REP_CREDITO CRD " +
                       "WHERE CRD.CDGEM = '" + empresa + "' " +
                       "AND TRUNC(CRD.FCONSULTA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "UNION ALL " +
                       "SELECT CRD.CDGPROSP CODIGO " +
                       ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', NOMBRE1, NOMBRE2, PRIMAPE, SEGAPE) FROM PROSPECTO WHERE CDGEM = CRD.CDGEM AND CODIGO = CRD.CDGPROSP) NOMBRE " +
                       ",'PROSPECTO' TIPO " +
                       ",TO_CHAR(TRUNC(CRD.FCONSULTA), 'DD/MM/YYYY') FECHA_CONSULTA " +
                       ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) FROM PROSPECTO P, PE WHERE P.CDGEM = CRD.CDGEM AND P.CODIGO = CRD.CDGPROSP AND PE.CDGEM = CRD.CDGEM AND PE.CODIGO = CRD.CDGPE) USUARIO_CONSULTA " +
                       ",NULL GRUPO " +
                       ",NULL NOM_GRUPO " +
                       ",NULL CICLO " +
                       ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) FROM PROSPECTO P, PE WHERE P.CDGEM = CRD.CDGEM AND P.CODIGO = CRD.CDGPROSP AND PE.CDGEM = P.CDGEM AND PE.CODIGO = P.CDGOCPE) ASESOR " +
                       ",(SELECT NOMBRE FROM PROSPECTO P, CO WHERE P.CDGEM = CRD.CDGEM AND P.CODIGO = CRD.CDGPROSP AND CO.CDGEM = P.CDGEM AND CO.CODIGO = P.CDGCO) SUCURSAL " +
                       "FROM CONSULTA_REP_CREDITO_P CRD " +
                       "WHERE CRD.CDGEM = '" + empresa + "' " +
                       "AND TRUNC(CRD.FCONSULTA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "ORDER BY FECHA_CONSULTA";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE CIFRAS CONTROL CONTABLES
    [WebMethod]
    public string getRepConsPEP(string fechaIni, string fechaFin, string grupo, string ciclo, string acred)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string strFecha = string.Empty;
        string strAcred = string.Empty;
        string strFromGrupo = string.Empty;
        string strGrupo = string.Empty;

        if(fechaIni != "" && fechaIni != null)
            strFecha = "AND TRUNC(CP.FREGISTRO) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' ";
        if(grupo != "" && grupo != null){
            strFromGrupo = ",PRC ";
            strGrupo = "AND PRC.CDGEM = CP.CDGEM ";
            strGrupo += "AND PRC.CDGCL = CP.CDGCL ";
            strGrupo += "AND PRC.CDGNS = '" + grupo + "' ";
            strGrupo += "AND PRC.CICLO = '" + ciclo + "' ";
            strGrupo += "AND PRC.CANTENTRE > 0 ";
            strGrupo += "AND PRC.SITUACION IN ('A','E','L') ";
        }
        if (acred != "" && acred != null)
            strAcred = "AND CP.CDGCL = '" + acred + "' ";
        
        string query = "SELECT CP.CDGCL " +
                       ",(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = CP.CDGEM AND CODIGO = CP.CDGCL) NOMCL " +
                       ",CP.OBSERVACION " +
                       ",TO_CHAR(CP.FREGISTRO,'DD/MM/YYYY') FECREG " +
                       ",DECODE(CP.NIVEL,'B','BAJO','M','MEDIO','A','ALTO') NIVEL " +
                       ",(SELECT NOMBRE(PE.CDGEM,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PE WHERE CDGEM = CP.CDGEM AND CODIGO = CP.CDGPE) NOMPE " +
                       ",TO_CHAR(CP.FACTUALIZA,'DD/MM/YYYY') FECACT " +
                       ",(SELECT NOMBRE(PE.CDGEM,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PE WHERE CDGEM = CP.CDGEM AND CODIGO = CP.ACTUALIZAPE) NOMPEACT " +
                       "FROM CONSULTA_PEP CP " +
                       strFromGrupo +
                       "WHERE CP.CDGEM = '" + empresa + "' " +
                       strFecha +
                       strAcred +
                       strGrupo +
                       "ORDER BY CP.FREGISTRO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if(res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //REPORTES DE ABC, REPORTE DE CONTROL DE MOVIMIENTOS
    [WebMethod]
    public string getRepControlMovimientos(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaFin = "LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))";
        fecha = "'" + fecha + "'";
        int iRes;

        try
        {   //SUM_SDO_CAP, SUM_SDO_INT
            string query = " SELECT '01' TIPO_ARCH, '01' TIPO_REG, '1000096097' ID_IFNB, '11123' ID_CRED_ABC, '98' PROD_IFNB, "
                           + " COUNT(*) NUM_MOVS, (SUM(SALDO_CAP_VIG) + SUM( CAP_VENCIDO)) SUM_SDO_CAP, (SUM(SDO_INT_VIGENTE) + SUM(SDO_INT_VENCIDO) ) SUM_SDO_INT, "
                            + " TO_NUMBER( " + fechaFin + " - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FMARCADO FROM  "
                            + "(  SELECT PF.CDGCL, "
                             + " ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL - CD.MORA_CAPITAL)),2) SALDO_CAP_VIG, "
                             + "  ROUND(( (PF.CANTIDAD/CD.MONTO_ENTREGADO)* ( CD.MORA_CAPITAL) ),2) CAP_VENCIDO, "


                             + " CASE WHEN (SELECT TCD.DIAS_MORA FROM TBL_CIERRE_DIA TCD WHERE TCD.CDGEM = PRN.CDGEM AND TCD.CDGCLNS = PRN.CDGNS AND TCD.CICLO = PRN.CICLO AND TCD.FECHA_CALC = " + fechaFin + ") = 0 THEN "
                                            + " CASE WHEN ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA') "
                                                       + "  -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + ")) <0  "
                                           + "  THEN 0 "
                                           + "  ELSE "
                                                + " ROUND((PRC.CANTENTRE/PRN.CANTENTRE)*((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA') "
                                                + " -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + ")),2) "
                                            + " END "
                            + " ELSE "
                                            + " CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                   + "  AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                                   + "  AND " + fechaFin + " AND DD.ESTATUS <> 'CA'),0) <= 0 "
                                                + "  THEN 0 "
                                           + "  ELSE "
                                             + " ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ( SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                    + " AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                                    + " AND " + fechaFin + " AND DD. ESTATUS <> 'CA')),2) "
                                           + "  END "
                            + " END SDO_INT_VIGENTE , "


                             + " CASE WHEN (SELECT TCD.DIAS_MORA FROM TBL_CIERRE_DIA TCD WHERE TCD.CDGEM = PRN.CDGEM AND TCD.CDGCLNS = PRN.CDGNS AND TCD.CICLO = PRN.CICLO AND TCD.FECHA_CALC = " + fechaFin + ") = 0  "
                                + " THEN 0 "
                                + " ELSE "
                                            + " CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                + " AND DD. FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                               + " AND " + fechaFin + " AND DD.ESTATUS <> 'CA'),0) <= 0 "
                                             + " THEN  "
                                               + "  ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA')  "
                                               + "  -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + "))) ,2) "
                                            + " ELSE "
                                                + " (ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA')  "
                                                + " -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + "))) ,2) "
                                                   + "  - "
                                             + " ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ( SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                   + "  AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + "- PRN.INICIO)/ 7 )) + 1) "
                                                                   + "  AND " + fechaFin + " AND DD.ESTATUS <> 'CA')),2)) "
                                            + " END "
                              + "  END SDO_INT_VENCIDO "

                            + " FROM PRC_FONDEO PF "

                            + " INNER JOIN  PRN ON "
                                + " PRN.CDGEM = PF.CDGEM "
                                + " AND PRN.CDGNS = PF.CDGNS "
                                + " AND PRN.CICLO = PF.CICLO "
                            + " INNER JOIN PRC ON "
                                + " PRC .CDGEM = PF.CDGEM "
                                + " AND PRC.CDGNS = PF.CDGNS "
                                + " AND PRC.CDGCL = PF.CDGCL "
                                + " AND PRC.CICLO = PF.CICLO "
                            + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                + " CD.CDGEM = PF.CDGEM "
                                + " AND CD.CDGCLNS = PF.CDGNS "
                                + " AND CD.CICLO = PF.CICLO "
                                + " AND CD.FECHA_CALC = PF.FREPSDO "
                            + " WHERE PF.CDGEM = '" + empresa + "' "
                            + " AND PF.CDGORF = '0005' "
                            + " AND PF.FREPSDO = " + fechaFin

                            + " UNION  " //UNION CON DATOS BORRADOS

                            + " SELECT PF.CDGCL, "
                            + " 0 SALDO_CAP_VIG, 0 CAP_VENCIDO,  0 SDO_INT_VIGENTE ,  0 SDO_INT_VENCIDO "
                            + " FROM BITACORA_ELIMINACION BE "
                            + " INNER JOIN  BITACORA_ELIMINACION_DATOS PF ON "
                                + " PF.CDGEM = BE.CDGEM "
                                + " AND PF.CDGORF = '0005' "
                                + " AND PF.ESTATUS = 'PROCESADO' "
                                + " AND PF.CDGBITELI  = BE.CODIGO "
                            + " INNER JOIN  CL ON  "
                                + " CL.CDGEM = PF.CDGEM "
                                + " AND CL.CODIGO = PF.CDGCL "
                            + " INNER JOIN  PRN ON "
                                + " PRN.CDGEM = PF.CDGEM "
                                + " AND PRN.CDGNS = PF.CDGNS "
                                + " AND PRN.CICLO = PF.CICLO "
                            + " INNER JOIN PRC ON "
                                + " PRC .CDGEM = PF.CDGEM "
                                + " AND PRC.CDGNS = PF.CDGNS "
                                + " AND PRC.CDGCL = PF.CDGCL "
                                + " AND PRC.CICLO = PF.CICLO   "
                            + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                + " CD.CDGEM = PF.CDGEM "
                                + " AND CD.CDGCLNS = PF.CDGNS "
                                + " AND CD.CICLO = PF.CICLO "
                                + " AND CD.FECHA_CALC = PF.FREPSDO "
                            + " INNER JOIN CO ON "
                                + " CO.CDGEM = PF.CDGEM "
                                + " AND CO.CODIGO = PRN.CDGCO "
                            + " WHERE BE.CDGEM = '" + empresa + "' "
                            + " AND BE.FELIMINA = " + fechaFin
                            + " AND BE.DESCRIPCION IN ('ELIMINA_FONDEO_ABC_AUTO_L','ELIMINA_FONDEO_ABC_AUTO') )";


            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //REPORTES DE ABC, REPORTE DE CONTROL DE MOVIMIENTOS DE CALIFICACION
    [WebMethod]
    public string getRepControlMovsCalificacion(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaFin = "LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))";
        fecha = "'" + fecha + "'";
        int iRes;

        try
        {
            string query = " SELECT '02' AS TIPO_ARCHIVO,'01' AS TIPO_REG, '1000096097' AS ID_IFNB, "
                         + " '11123' AS IDCREDABC, '98' PRODUCTO_IFNB,  COUNT(*) NUM_MOVS, "
                         + " ROUND(SUM(SDO_BASE_CALIF) ,2) SUM_SDO_BASE_CALIF, "
                         + " ROUND(SUM(IMP_RES_CUB) ,2)   SUM_SDO_RESERV,  "
                         + " TO_NUMBER( " + fechaFin + " - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECHA_CORTE "
                         + " FROM  (SELECT PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS  IDCRED_IFNB, "
                                 + " ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) SDO_BASE_CALIF, "
                                 + " ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  SDO_CUBIERTO, "
                                 + " (ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) - "
                                 + "  ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  ) SDO_DESCUB, "
                                 + "  ES.EPRCACUM IMP_RES_CUB "
                                 + " FROM PRC_FONDEO PF "
                                 + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                    + " CD.CDGEM = PF.CDGEM "
                                    + " AND CD.CDGCLNS = PF.CDGNS "
                                    + " AND CD.CICLO = PF.CICLO "
                                    + " AND CD.FECHA_CALC = PF.FREPSDO "
                                  + " INNER JOIN PRC ON "
                                   + " PRC.CDGEM=PF.CDGEM "
                                   + " AND PRC.CDGNS = PF.CDGNS "
                                   + " AND PRC.CDGCL = PF.CDGCL "
                                   + " AND PRC.CICLO = PF.CICLO "
                                 + " INNER JOIN ESTIMACION ES ON "
                                    + " ES.CDGEM = PF.CDGEM "
                                    + " AND ES.CDGCLNS = PF.CDGCLNS "
                                    + " AND ES.CDGCL = PF.CDGCL "
                                    + " AND ES.CICLO = PF.CICLO "
                                 + " WHERE  "
                                 + " PF.CDGEM='" + empresa + "' "
                                 + " AND PF.CDGORF = '0005' "
                                + " AND PF.FREPSDO = " + fechaFin + ")";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS POR ACREDITADO
    [WebMethod]
    public string getRepControlPagosAcred(string grupo, string ciclo, string acred, string fecha, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strAsesor = string.Empty;

        if (puesto == "A")
            strAsesor = "AND PRN.CDGOCPE = '" + usuario + "' ";
        else
            strAsesor = "AND PRN.CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') ";

        string query = "SELECT CSA.* " +
                       ",TO_CHAR(CSA.FREALPAGO,'DD/MM/YYYY') FPAGO " +
                       ",DECODE(CSA.TIPO,'S','SEMANAL','P','EXTEMPORANEO') TIPOREG " +
                       ",DECODE(CSA.ASISTENCIA, 'A', 'Asistencia', " +
                                          "'R', 'Retardo', " +
                                          "'F', 'Falta', " +
                                          "'P', 'Permiso', " +
                                          "'MP', 'Mandó Pago') ASIST " +
                       ",NS.NOMBRE GRUPO " +
                       ",NOMBREC(CL.CDGEM, CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE_CL " +
                       ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",TO_CHAR(DECODE(nvl(PRN.PERIODICIDAD,''), " +
                                                                "'S', PRN.INICIO + (7 * NVL(PRN.PLAZO,0)), " +
                                                                "'Q', PRN.INICIO + (15 * NVL(PRN.PLAZO,0)), " +
                                                                "'C', PRN.INICIO + (14 * NVL(PRN.PLAZO,0)), " +
                                                                "'M', PRN.INICIO + (30 * NVL(PRN.PLAZO,0)), " +
                                                                "'', ''),'DD/MM/YYYY') AS FFIN " +
                       ",PRC.CANTENTRE " +  
                       ",CO.NOMBRE COORD " +
                       ",NOMBREC(NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR " +
                       "FROM CONTROL_PAGOS_ACRED CSA, PRN, PRC, NS, CL, CO, PE " +
                       "WHERE CSA.CDGEM = '" + empresa + "' " +
                       "AND CSA.CDGNS = '" + grupo + "' " +
                       "AND CSA.CICLO = '" + ciclo + "' " +
                       "AND CSA.CDGCL = '" + acred + "' " +
                       "AND CSA.FREALPAGO <= '" + fecha + "' " +
                       "AND PRN.CDGEM = CSA.CDGEM " +
                       "AND PRN.CDGNS = CSA.CDGNS " +
                       "AND PRN.CICLO = CSA.CICLO " +
                       strAsesor +
                       "AND PRC.CDGEM = CSA.CDGEM " +
                       "AND PRC.CDGNS = CSA.CDGNS " +
                       "AND PRC.CICLO = CSA.CICLO " +
                       "AND PRC.CDGCL = CSA.CDGCL " +
                       "AND NS.CDGEM = CSA.CDGEM " +
                       "AND NS.CODIGO = CSA.CDGNS " +
                       "AND CL.CDGEM = CSA.CDGEM " +
                       "AND CL.CODIGO = CSA.CDGCL " +
                       "AND CO.CDGEM = PRN.CDGEM " +
                       "AND CO.CODIGO = PRN.CDGCO " +
                       "AND PE.CDGEM = PRN.CDGEM " +
                       "AND PE.CODIGO = PRN.CDGOCPE " +
                       "ORDER BY CSA.FREALPAGO, CSA.SECUENCIA";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS ACUMULADO POR ACREDITADO SEGUN LA FECHA DE CONSULTA
    [WebMethod]
    public string getRepControlPagosAcumulado(string grupo, string ciclo, string fecha, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strAsesor = string.Empty;

        if (puesto == "A")
            strAsesor = "AND PRN.CDGOCPE = '" + usuario + "' "; 
        else
            strAsesor = "AND PRN.CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') ";

        string query = "SELECT COORD " +
                            ",CONTRATO " +
                            ",ASESOR " +
                            ",CDGNS " +
                            ",GRUPO " +
                            ",CDGCL " +
                            ",ACRED " +
                            ",CICLO " +
                            ",FINICIO " +
                            ",FFIN " +
                            ",CANTENTRE " +
                            ",SITUA " +
                            ",PAGOCOMP " +
                            ",SALDOTOTAL " +
                            ",DIASMORA " +
                            ",PARCIALIDAD " +
                            ",PAGO_SEM " +
                            ",PAGO_EXT " +
                            ",APORTACRED " +
                            ",(PAGO_SEM + PAGO_EXT + APORTACRED) PAGOREAL " +
                            ",((PAGO_SEM + PAGO_EXT + APORTACRED) - PAGOCOMP) DIFERENCIA " +
                            ",DEVACRED " +
                            ",AHORROACRED " +
                            ",MULTAACRED " +
                            ",(TOTALPAGAR - (PAGO_SEM + PAGO_EXT + APORTACRED)) SALDO " +
                            "FROM (SELECT A.COORD " +                //DEFINE EL ORIGEN DE DATOS AGRUPADO
                            ",A.CONTRATO " +
                            ",A.ASESOR " +
                            ",A.CDGNS " +
                            ",A.GRUPO " +
                            ",A.CDGCL " +
                            ",A.ACRED " +
                            ",A.CICLO " +
                            ",A.FINICIO " +
                            ",A.FFIN " +
                            ",A.CANTENTRE " +
                            ",A.SITUA " +
                            ",A.PAGOCOMP " +
                            ",A.SALDOTOTAL " +
                            ",A.DIASMORA " +
                            ",A.PARCIALIDAD " +
                            ",A.TOTALPAGAR " +
                            ",NVL(SUM(PAGOSEM),0) PAGO_SEM " +              //SUMATORIA DE LOS PAGOS SEMANALES
                            ",NVL(SUM(PAGOEXT),0) PAGO_EXT " +              //SUMATORIA DE LOS PAGOS EXTEMPORANEOS
                            ",NVL(SUM(APORT_ACRED),0) APORTACRED " +        //SUMATORIA DE LAS APORTACIONES DE CREDITO
                            ",SUM(DEV_ACRED) DEVACRED " +
                            ",SUM(AHORRO_ACRED) AHORROACRED " +
                            ",SUM(MULTA_ACRED) MULTAACRED " +
                             "FROM (SELECT CO.NOMBRE COORD " +
                             ",PRN.CDGNS || PRN.CICLO CONTRATO " +
                             ",NOMBREC (NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR " +
                             ",PRN.CDGNS " +
                             ",NS.NOMBRE GRUPO " +
                             ",PRC.CDGCL " +
                             ",NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                             ",PRN.CICLO " +
                             ",TO_CHAR (PRN.INICIO, 'DD/MM/YYYY') FINICIO " +
                             ",TO_CHAR (DECODE (NVL (PRN.periodicidad, ''), " +
                                               "'S', PRN.inicio + (7 * NVL (PRN.plazo, 0)), " +
                                               "'Q', PRN.inicio + (15 * NVL (PRN.plazo, 0)), " +
                                               "'C', PRN.inicio + (14 * NVL (PRN.plazo, 0)), " +
                                               "'M', PRN.inicio + (30 * NVL (PRN.plazo, 0)), " +
                                               "'', ''), 'DD/MM/YYYY') AS FFIN " +
                            ",PRC.CANTENTRE " +
                            ",CASE WHEN CSA.TIPO = 'S' THEN " +
                                "CSA.PAGO_REAL " +
                            "END PAGOSEM " +
                            ",CASE WHEN CSA.TIPO = 'P' THEN " +
                                "CSA.PAGO_REAL " +
                            "END PAGOEXT " +
                            ",CSA.PAGO_REAL " +
                            ",CSA.APORT_ACRED " +
                            ",CSA.DEV_ACRED " +
                            ",CSA.AHORRO_ACRED " +
                            ",CSA.MULTA_ACRED " +
                            ",DECODE(PRN.SITUACION,'E','ENTREGADO','L','LIQUIDADO') SITUA " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * (PagoVencidoCapitalPrN(PrN.CdgEm,  PRN.CdgNs, PrN.Ciclo,PrN.CantEntre, PrN.Tasa, PrN.Plazo,PrN.Periodicidad, PrN.CdgMCI, Prn.Inicio, Prn.DiaJunta,Prn.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,PrN.ModoApliReca,'" + fecha + "',null,'S')),2) PAGOCOMP " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * (SALDOTOTALPRN(PrN.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa,    PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio, PrN.DiaJunta,    PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,    PrN.ModoApliReca, '" + fecha + "')),2) SALDOTOTAL " +
                            ",CASE WHEN PRN.SITUACION = 'E' THEN " +
                                "(SELECT DIAS_MORA FROM TBL_CIERRE_DIA WHERE CDGEM = PRN.CDGEM AND CDGCLNS = PRN.CDGNS AND CLNS = 'G' AND CICLO = PRN.CICLO AND FECHA_CALC = '" + fecha + "') " +
                            "ELSE " +
                                "0 " +
                            "END DIASMORA " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * PARCIALIDADPrN (PrN.CdgEm, PrN.CdgNs, PrN.Ciclo, NVL(PrN.cantentre,PrN.Cantautor), PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,    PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, NULL),3) PARCIALIDAD " +
                            ",round((PRC.CANTENTRE / PRN.CANTENTRE) * (round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                                           "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                                           "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100), " +
                                           "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                                           "'',  ''),2)) ,2) AS TOTALPAGAR " +
                            "FROM PRN, PRC, CL, CO, PE, NS, " +
                            "(SELECT CDGEM, CDGNS, CICLO, CDGCL, TIPO, SUM(PAGOREAL) PAGO_REAL, " +
                              "SUM(APORT) APORT_ACRED, SUM(DEVOLUCION) DEV_ACRED, SUM(AHORRO) AHORRO_ACRED, SUM(MULTA) MULTA_ACRED " +
                              "FROM CONTROL_PAGOS_ACRED " +
                              "WHERE CDGEM = '" + empresa + "' " +
                              "AND CDGNS = '" + grupo + "' " +
                              "AND CICLO = '" + ciclo + "' " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                              "GROUP BY CDGEM, CDGNS, CICLO, CDGCL, TIPO) CSA " +
                            "WHERE PRN.CDGEM = '" + empresa + "' " +
                            "AND PRN.CDGNS = '" + grupo + "' " +
                            "AND PRN.CICLO = '" + ciclo + "' " +
                            "AND PRN.CANTENTRE > 0 " +
                            "AND PRN.SITUACION IN ('E', 'L') " +
                            strAsesor +
                            "AND PRC.CDGEM = PRN.CDGEM " +
                            "AND PRC.CDGNS = PRN.CDGNS " +
                            "AND PRC.CICLO = PRN.CICLO " +
                            "AND PRC.SITUACION IN ('E', 'L') " +
                            "AND PRC.CANTENTRE > 0 " +
                            "AND CSA.CDGEM = PRC.CDGEM " +
                            "AND CSA.CDGNS = PRC.CDGNS " +
                            "AND CSA.CICLO = PRC.CICLO " +
                            "AND CSA.CDGCL = PRC.CDGCL " +
                            "AND CL.CDGEM = PRC.CDGEM " +
                            "AND CL.CODIGO = PRC.CDGCL " +
                            "AND CO.CDGEM = PRN.CDGEM " +
                            "AND CO.CODIGO = PRN.CDGCO " +
                            "AND PE.CDGEM = PRN.CDGEM " +
                            "AND PE.CODIGO = PRN.CDGOCPE " +
                            "AND NS.CDGEM = PRN.CDGEM " +
                            "AND NS.CODIGO = PRN.CDGNS) A " +
                            "GROUP BY A.COORD " +
                            ",A.CONTRATO " +
                            ",A.ASESOR " +
                            ",A.CDGNS " +
                            ",A.GRUPO " +
                            ",A.CDGCL " +
                            ",A.ACRED " +
                            ",A.CICLO " +
                            ",A.FINICIO " +
                            ",A.FFIN " +
                            ",A.CANTENTRE " +
                            ",A.SITUA " +
                            ",A.PAGOCOMP " +
                            ",A.SALDOTOTAL " +
                            ",A.DIASMORA " +
                            ",A.PARCIALIDAD " +
                            ",A.TOTALPAGAR) " +
                            "ORDER BY CDGNS, CICLO";
                            
        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS POR ACREDITADO
    [WebMethod]
    public string getRepControlPagosFechaReg(string region, string coord, string asesor, string fecha, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strAsesor = string.Empty;
        string strReg = string.Empty;
        string strCoord = string.Empty;

        if (region != "000")
            strReg = "AND RG.CODIGO = '" + region + "' ";
        if (coord != "000")
            strCoord = "AND CO.CODIGO = '" + coord + "' ";
        if (asesor != "000000")
        {
            if (puesto == "A")
                strAsesor = "AND PRN.CDGOCPE = '" + usuario + "' ";
            else
                strAsesor = "AND PRN.CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') ";
        }

        string query = "SELECT CP.CDGNS " +
                       ",NS.NOMBRE NOMNS " +
                       ",CO.NOMBRE NOMCO " + 
                       ",CP.CICLO " +
                       ",TO_CHAR(CP.FREALPAGO,'DD/MM/YYYY') FPAGO " +
                       ",TO_CHAR(CP.FREGISTRO,'DD/MM/YYYY') FREG " +
                       ",TO_CHAR(CP.FREGISTRO, 'DAY') DIAREG " +
                       ",TO_CHAR(CP.FREGISTRO, 'HH24:MI:SS') HORAREG " +
                       ",TO_CHAR(FNFECHAREUNION(CP.CDGEM,CP.CDGNS,CP.CICLO,CEIL((TRUNC(CP.FREALPAGO) - PRN.INICIO)/NVL(DECODE(PRN.PERIODICIDAD,'S',7,'C',14,'Q',15,'M',30),0))),'DD/MM/YYYY') FECHAREUNION " +
                       ",DECODE(PRN.NOACUERDO,1,'LUNES',2,'MARTES',3,'MIERCOLES',4,'JUEVES',5,'VIERNES') DIAREUNION " +
                       ",PRN.HORARIO " +
                       ",FLOOR((((TO_DATE(TO_CHAR(CP.FREGISTRO,'DD/MM/YYYY') || TO_CHAR(CP.FREGISTRO, 'HH24:MI:SS'),'DD/MM/YYYY HH24:MI:SS') - TO_DATE(TO_CHAR(FNFECHAREUNION(CP.CDGEM,CP.CDGNS,CP.CICLO,CEIL((TRUNC(CP.FREALPAGO) - PRN.INICIO)/NVL(DECODE(PRN.PERIODICIDAD,'S',7,'C',14,'Q',15,'M',30),0))),'DD/MM/YYYY') || PRN.HORARIO,'DD/MM/YYYY HH24:MI:SS')) * 86400) / 60)/ 60) TIEMPOTRANS " +
                       ",CP.CDGPE " +
                       ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMPE " +
                       "FROM CONTROL_PAGOS CP, PRN, NS, CO, RG, PE " +
                       "WHERE CP.CDGEM = '" + empresa + "' " +
                       "AND CP.TIPO = 'S' " +
                       "AND TRUNC(CP.FREGISTRO) <= '" + fecha + "' " +
                       "AND PRN.CDGEM = CP.CDGEM " +
                       "AND PRN.CDGNS = CP.CDGNS " +
                       "AND PRN.CICLO = CP.CICLO " +
                       "AND PRN.SITUACION = 'E' " +
                       strAsesor +
                       "AND NS.CDGEM = PRN.CDGEM " +
                       "AND NS.CODIGO = PRN.CDGNS " +
                       "AND CO.CDGEM = NS.CDGEM " +
                       "AND CO.CODIGO = NS.CDGCO " +
                       strCoord +  
                       "AND RG.CDGEM = CO.CDGEM " +
                       "AND RG.CODIGO = CO.CDGRG " +
                       strReg +
                       "AND PE.CDGEM = CP.CDGEM " +
                       "AND PE.CODIGO = CP.CDGPE " +
                       "ORDER BY CP.CDGNS, CP.CICLO, CP.FREALPAGO";
                       
        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS SEGUN LA FECHA DE CONSULTA
    [WebMethod]
    public string getRepControlPagosGrupo(string fecha, string region, string coord, string asesor)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strReg = string.Empty;
        string strCoord = string.Empty;
        string strAsesor = string.Empty;

        if (region != "000")
            strReg = "AND RG.CODIGO = '" + region + "' ";
        if (coord != "000")
            strCoord = "AND CO.CODIGO = '" + coord + "' ";
        if(asesor != "000000")
            strAsesor = "AND PRN.CDGOCPE = '" + asesor + "' ";

        string query = "SELECT A.* " +
                       ",(A.PAGOSEM + A.PAGOEXT + A.APORTA) TOTAL " +
                       ",((A.PAGOSEM + A.PAGOEXT + A.APORTA) - A.TOTAL_PAGADO) DIFERENCIA " +
                       "FROM (SELECT RG.NOMBRE REGION, " +
                             "CO.NOMBRE COORD, " +
                             "PRN.CDGNS || PRN.CICLO CONTRATO, " +
                             "NOMBREC (NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR, " +
                             "PRN.CDGNS, " +
                             "NS.NOMBRE GRUPO, " +
                             "PRN.CICLO, " +
                             "TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO, " +
                             "TO_CHAR(DECODE(nvl(PRN.periodicidad,''), " +
                                        "'S', PRN.inicio + (7 * nvl(PRN.plazo,0)), " +
                                        "'Q', PRN.inicio + (15 * nvl(PRN.plazo,0)), " +
                                        "'C', PRN.inicio + (14 * nvl(PRN.plazo,0)), " +
                                        "'M', PRN.inicio + (30 * nvl(PRN.plazo,0)), " +
                                        "'', ''),'DD/MM/YYYY') AS FFIN, " +
                             "PRN.CANTENTRE, " +
                             "NVL((SELECT SUM (PAGOREAL) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                              "AND TIPO = 'S' " +
                              "GROUP BY CDGNS, CICLO),0) PAGOSEM, " +
                              "NVL((SELECT SUM (PAGOREAL) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                              "AND TIPO = 'P' " +
                              "GROUP BY CDGNS, CICLO),0) PAGOEXT, " +
                             "NVL((SELECT SUM (APORT) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) APORTA, " +
                             "NVL((SELECT SUM (DEVOLUCION) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) DEV_GPO, " +
                             "NVL((SELECT SUM (AHORRO) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) AHORRO_GPO, " +
                             "NVL((SELECT SUM (MULTA) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) MULTA_GPO, " +
                             "(PAGADOCAPITALPRN(Prn.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CdgMci, '" + fecha + "','N') + " +
                             "PAGADOINTERESPRN(PrN.CdgEm, PrN.CdgNs, PrN.Ciclo,   '" + fecha + "')) TOTAL_PAGADO, " +
                             "ROUND(PARCIALIDADPrN(PrN.CdgEm, PrN.CdgNs, PrN.Ciclo, NVL(PrN.cantentre,PrN.Cantautor), PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,    PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, NULL),2) PARCIALIDAD, " +
                             "CASE WHEN SaldoVencidoCapitalPrN(PrN.CdgEm,PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, PrN.ModoApliReca,'" + fecha + "', null, 'S' ) >= 0 THEN " +
                                "ROUND(SaldoVencidoCapitalPrN(PrN.CdgEm,PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, PrN.ModoApliReca,'" + fecha + "', null, 'S' ),2) " +
                             "ELSE " +
                                "0 " +
                             "END MORA_TOTAL, " +
                             "SALDOTOTALPRN(PrN.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa,    PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio, PrN.DiaJunta,    PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,    PrN.ModoApliReca, '" + fecha + "') AS SALDOTOTAL, " +
                             "FNSDOGARANTIA(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'G','" + fecha + "') SALDO_GL, " +
                             "(SELECT DIAS_MORA FROM TBL_CIERRE_DIA WHERE CDGEM = PRN.CDGEM AND CDGCLNS = PRN.CDGNS AND CLNS = 'G' AND CICLO = PRN.CICLO AND FECHA_CALC = '" + fecha +"') DIAS_MORA, " +
                             "DECODE (PRN.SITUACION,  'E', 'ENTREGADO',  'L', 'LIQUIDADO') SITUA " + 
                             "FROM PRN, CO, PE, NS, RG " +
                             "WHERE PRN.CDGEM = '" + empresa + "' " +
                             "AND PRN.CANTENTRE > 0 " +
                             "AND PRN.SITUACION = 'E' " +
                             strAsesor +
                             "AND CO.CDGEM = PRN.CDGEM " +
                             "AND CO.CODIGO = PRN.CDGCO " +
                             strCoord +
                             "AND RG.CDGEM = CO.CDGEM " +
                             "AND RG.CODIGO = CO.CDGRG " +
                             strReg +
                             "AND PE.CDGEM = PRN.CDGEM " +
                             "AND PE.CODIGO = PRN.CDGOCPE " +
                             "AND NS.CDGEM = PRN.CDGEM " +
                             "AND NS.CODIGO = PRN.CDGNS " +
                             "UNION " +
                             "SELECT RG.NOMBRE REGION, " +
                             "CO.NOMBRE COORD, " +
                             "PRN.CDGNS || PRN.CICLO CONTRATO, " +
                             "NOMBREC (NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR, " +
                             "PRN.CDGNS, " +
                             "NS.NOMBRE GRUPO, " +
                             "PRN.CICLO, " +
                             "TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO, " +
                             "TO_CHAR(DECODE(nvl(PRN.periodicidad,''), " +
                                        "'S', PRN.inicio + (7 * nvl(PRN.plazo,0)), " +
                                        "'Q', PRN.inicio + (15 * nvl(PRN.plazo,0)), " +
                                        "'C', PRN.inicio + (14 * nvl(PRN.plazo,0)), " +
                                        "'M', PRN.inicio + (30 * nvl(PRN.plazo,0)), " +
                                        "'', ''),'DD/MM/YYYY') AS FFIN, " +
                             "PRN.CANTENTRE, " +
                             "NVL((SELECT SUM (PAGOREAL) " +
                             "FROM CONTROL_PAGOS " +
                             "WHERE CDGEM = PRN.CDGEM " +
                             "AND CDGNS = PRN.CDGNS " +
                             "AND CICLO = PRN.CICLO " +
                             "AND FREALPAGO <= '" + fecha + "' " +
                             "AND TIPO = 'S' " +
                             "GROUP BY CDGNS, CICLO),0) PAGOSEM, " +
                             "NVL((SELECT SUM (PAGOREAL) " +
                             "FROM CONTROL_PAGOS " +
                             "WHERE CDGEM = PRN.CDGEM " +
                             "AND CDGNS = PRN.CDGNS " +
                             "AND CICLO = PRN.CICLO " +
                             "AND FREALPAGO <= '" + fecha + "' " +
                             "AND TIPO = 'P' " +
                             "GROUP BY CDGNS, CICLO),0) PAGOEXT, " +
                             "NVL((SELECT SUM (APORT) " +
                               "FROM CONTROL_PAGOS " +
                               "WHERE CDGEM = PRN.CDGEM " +
                               "AND CDGNS = PRN.CDGNS " +
                               "AND CICLO = PRN.CICLO " +
                               "AND FREALPAGO <= '" + fecha + "' " +
                               "GROUP BY CDGNS, CICLO),0) APORTA, " +
                              "NVL((SELECT SUM (DEVOLUCION) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) DEV_GPO, " +
                             "NVL((SELECT SUM (AHORRO) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) AHORRO_GPO, " +
                             "NVL((SELECT SUM (MULTA) " +
                              "FROM CONTROL_PAGOS " +
                              "WHERE CDGEM = PRN.CDGEM " +
                              "AND CDGNS = PRN.CDGNS " +
                              "AND CICLO = PRN.CICLO " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                             "GROUP BY CDGNS, CICLO),0) MULTA_GPO, " +
                               "(PAGADOCAPITALPRN(Prn.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CdgMci, '" + fecha + "','N') + " +
                               "PAGADOINTERESPRN(PrN.CdgEm, PrN.CdgNs, PrN.Ciclo,   '" + fecha + "')) TOTAL_PAGADO, " +
                              "ROUND(PARCIALIDADPrN(PrN.CdgEm, PrN.CdgNs, PrN.Ciclo, NVL(PrN.cantentre,PrN.Cantautor), PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,    PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, NULL),2) PARCIALIDAD, " +
                              "CASE WHEN SaldoVencidoCapitalPrN(PrN.CdgEm,PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, PrN.ModoApliReca,'" + fecha + "', null, 'S' ) >= 0 THEN "+
                                "ROUND(SaldoVencidoCapitalPrN(PrN.CdgEm,PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, PrN.ModoApliReca,'" + fecha + "', null, 'S' ),2) " +
                              "ELSE " +
                                "0 " +
                              "END MORA_TOTAL, " +
                              "SALDOTOTALPRN(PrN.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa,    PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio, PrN.DiaJunta,    PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,    PrN.ModoApliReca, '" + fecha + "') AS SALDOTOTAL, " +
                              "FNSDOGARANTIA(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'G','" + fecha + "') SALDO_GL, " +
                              "0 DIAS_MORA, " +
                              "DECODE (PRN.SITUACION,  'E', 'ENTREGADO',  'L', 'LIQUIDADO') SITUA " +  
                              "FROM PRN, CO, PE, NS, RG " + 
                              "WHERE PRN.CDGEM = '" + empresa + "' " +
                              "AND PRN.CANTENTRE > 0 " +
                              "AND PRN.SITUACION = 'L' " +
                              strAsesor +
                              "AND FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO) >= '01/' || to_char(add_months('" + fecha + "', -1),'MM') || " +
                                                                                              "'/' || (CASE WHEN to_char(TO_DATE('" + fecha + "'),'MM') = '01' THEN " +
                                                                                                          "to_char(add_months('" + fecha + "', -1),'YYYY') " +
                                                                                                      "ELSE to_char(TO_DATE('" + fecha + "'),'YYYY') END) " + 
                              "AND CO.CDGEM = PRN.CDGEM " +
                              "AND CO.CODIGO = PRN.CDGCO " +
                              strCoord +
                              "AND RG.CDGEM = CO.CDGEM " +
                              "AND RG.CODIGO = CO.CDGRG " +
                              strReg +
                              "AND PE.CDGEM = PRN.CDGEM " +
                              "AND PE.CODIGO = PRN.CDGOCPE " +
                              "AND NS.CDGEM = PRN.CDGEM " +
                              "AND NS.CODIGO = PRN.CDGNS) A"; 

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS SEGUN LA FECHA DE CONSULTA
    [WebMethod]
    public string getRepControlPagosGrupoMod(string fecha, string region, string coord, string asesor, string usuario)
    {
        DataSet dref = new DataSet();
        int iRes;
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strReg = string.Empty;
        string strCoord = string.Empty;
        string strAsesor = string.Empty;

        iRes = oE.myExecuteNonQuery(ref queryEstatus, "SP_REP_SIT_CONTROL_PAGOS", CommandType.StoredProcedure, oP.ParamsRepContPagos(empresa, fecha, region, coord, asesor, usuario));

        string query = "SELECT * " +
                       "FROM REP_SIT_CONTROL_PAGOS " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "'";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL SEMANAL SEGUN LA FECHA DE CONSULTA
    [WebMethod]
    public string getRepControlSemanal(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        string query = "SELECT * " +
                       "FROM (select prn.cdgco cod_suc " +
                              ",co.nombre sucursal " +
                              ", prn.cdgns AS cod_gpo " +
                              ",ns.nombre AS grupo " +
                              ",prn.ciclo " +
                              ",b.codigo AS cod_cte " +
                              ",nombrec (b.cdgem,b.codigo,'I','A',NULL,NULL,NULL,NULL) AS cliente " +
                              ",prc.cantentre " +
                              ",semana " +
                              ",pagoteo " +
                              ",pagoreal " +
                              ",aport " +
                              ",devolucion " +
                              ",ahorro " +
                              ",asistencia " +
                              ",TO_CHAR(fregistro,'DD/MM/YYYY') fecha_captura " +
                              "FROM control_semanal_acred a, cl b, ns, prn, co, prc " +
                              "WHERE a.cdgem = b.cdgem " +
                              "AND a.cdgcl = b.codigo " +
                              "AND a.cdgem = ns.cdgem " +
                              "AND a.cdgns = ns.codigo " +
                              "AND a.cdgem = prn.cdgem " +
                              "AND a.cdgns = prn.cdgns " +
                              "AND a.ciclo = prn.ciclo " +
                              "AND prn.cdgem = co.cdgem " +
                              "AND prn.cdgco = co.codigo " +
                              "AND prn.cdgem = prc.cdgem " +
                              "AND prn.cdgns = prc.cdgns " +
                              "AND prn.ciclo = prc.ciclo " +
                              "AND PRN.CDGEM = '" + empresa + "' " +
                              "AND PRN.SITUACION = 'E' " +
                              "AND a.cdgem = prc.cdgem " +
                              "AND a.cdgns = prc.cdgns " +
                              "AND a.ciclo = prc.ciclo " +
                              "AND a.cdgcl = prc.cdgcl " +
                              //--ORDER BY prn.cdgns, a.cdgcl, a.ciclo, a.semana
                              "UNION " +
                              "SELECT prn.cdgco cod_suc " +
                              ",co.nombre sucursal " +
                              ",prn.cdgns AS cod_gpo " +
                              ",ns.nombre AS grupo " +
                              ",prn.ciclo " +
                              ",b.codigo AS cod_cte " +
                              ",nombrec (b.cdgem,b.codigo,'I','A',NULL,NULL,NULL,NULL) AS cliente " +
                              ",prc.cantentre " +
                              ",semana " +
                              ",pagoteo " +
                              ",pagoreal " +
                              ",aport " +
                              ",devolucion " +
                              ",ahorro " + 
                              ",asistencia " +
                              ",TO_CHAR(fregistro,'DD/MM/YYYY') fecha_captura " + 
                              "FROM control_semanal_acred a, cl b, ns, prn, co, prc " +
                              "WHERE a.cdgem = b.cdgem " +
                              "AND a.cdgcl = b.codigo " +
                              "AND a.cdgem = ns.cdgem " +
                              "AND a.cdgns = ns.codigo " +
                              "AND a.cdgem = prn.cdgem " +
                              "AND a.cdgns = prn.cdgns " +
                              "AND a.ciclo = prn.ciclo " +
                              "AND prn.cdgem = co.cdgem " +
                              "AND prn.cdgco = co.codigo " +
                              "AND prn.cdgem = prc.cdgem " +
                              "AND prn.cdgns = prc.cdgns " +
                              "AND prn.ciclo = prc.ciclo " +
                              "AND PRN.CDGEM = '" + empresa + "' " +
                              "AND PRN.SITUACION = 'L' " +
                              "AND FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO) >=  '01/' || to_char(add_months('" + fecha + "', -1),'MM') || " +
                                                                                              "'/' || (CASE WHEN to_char(TO_DATE('" + fecha + "'),'MM') = '01' THEN " +
                                                                                                          "to_char(add_months('" + fecha + "', -1),'YYYY') " +
                                                                                                      "ELSE to_char(TO_DATE('" + fecha + "'),'YYYY') END) " +
                              "AND a.cdgem = prc.cdgem " +
                              "AND a.cdgns = prc.cdgns " +
                              "AND a.ciclo = prc.ciclo " +
                              "AND a.cdgcl = prc.cdgcl) " +
                              "ORDER BY cod_gpo, COD_CTE, ciclo, semana"; 

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DEL PROCESO DE REGISTRO DE CONVENIOS MEDIANTE UN ARCHIVO
    [WebMethod]
    public string getRepConvenioArchivo(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT * " +
                       ",TO_CHAR(FINICIO,'DD/MM/YYYY') FECINI " +
                       ",TO_CHAR(FFIN,'DD/MM/YYYY') FECFIN " +
                       "FROM REP_CONVENIO " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "'";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LA INFORMACION DE LOS CREDITOS CASTIGADOS
    [WebMethod]
    public string getRepCreditosAutorizados(string fecIni, string fecFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            //INCORPORACION DE CREDITOS CASTIGADOS
            string query = "SELECT CO.NOMBRE SUCURSAL " +
                           ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                           ",SC.CDGNS CODIGO " +
                           ",NS.NOMBRE GRUPO " +
                           ",SC.CICLO " +
                           ",NOMBREC(SC.CDGEM,SC.CDGCL,'I','N',NULL,NULL,NULL,NULL) ACREDITADO " +
                           ",SC.CANTSOLIC MONTO_SOLICITADO " +
                           ",SC.CANTAUTOR MONTO_AUTORIZADO " +
                           ",TO_CHAR(SC.INICIO,'DD/MM/YYYY') FECHA_INICIO " +
                           "FROM SC, SN, NS, CO, PE " +
                           "WHERE SC.CDGEM = '" + empresa + "' " +
                           "AND SC.INICIO BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND SC.CANTSOLIC > 0 " +
                           "AND SN.CDGEM = SC.CDGEM " +
                           "AND SN.CDGNS = SC.CDGNS " +
                           "AND SN.CICLO = SC.CICLO " +
                           "AND NS.CDGEM = SN.CDGEM " +
                           "AND NS.CODIGO = SN.CDGNS " +
                           "AND CO.CDGEM = SN.CDGEM " +
                           "AND CO.CODIGO = SN.CDGCO " +
                           "AND PE.CDGEM = SN.CDGEM " +
                           "AND PE.CODIGO = SN.CDGOCPE";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO QUE CONSULTA LA INFORMACION DE LOS CREDITOS CASTIGADOS
    [WebMethod]
    public string getRepCreditosCastigados(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            //INCORPORACION DE CREDITOS CASTIGADOS
            string query = "SELECT CD.REGION " +
                           ",CD.COD_SUCURSAL " +
                           ",CD.NOM_SUCURSAL " +
                           ",(SELECT NOM_ASESOR " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                             "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC (PL.ALTA)) ASESOR_CASTIGO " +
                           ",CASE WHEN CD.CLNS = 'G' THEN " +
                               "(SELECT NOM_ASESOR " +
                               "FROM TBL_CIERRE_DIA CDC, PRN " +
                               "WHERE CDC.CDGEM = CD.CDGEM " +
                               "AND CDC.CDGCLNS = CD.CDGCLNS " +
                               "AND CDC.CICLO = '01' " +
                               "AND CDC.CLNS = CD.CLNS " +
                               "AND CDC.FECHA_CALC = PRN.INICIO " +
                               "AND PRN.CDGEM = CD.CDGEM " +
                               "AND PRN.CDGNS = CD.CDGCLNS " +
                               "AND PRN.CICLO = CD.CICLO " +
                               "AND PRN.SITUACION IN ('E','L')) " +
                           "WHEN CD.CLNS = 'I' THEN " +
                               "(SELECT NOM_ASESOR " +
                               "FROM TBL_CIERRE_DIA CDC, PRC " +
                               "WHERE CDC.CDGEM = CD.CDGEM " +
                               "AND CDC.CDGCLNS = CD.CDGCLNS " +
                               "AND CDC.CICLO = '01' " +
                               "AND CDC.CLNS = CD.CLNS " +
                               "AND CDC.FECHA_CALC = PRC.INICIO " +
                               "AND PRC.CDGEM = CD.CDGEM " +
                               "AND PRC.CDGCLNS = CD.CDGCLNS " +
                               "AND PRC.CICLO = CD.CICLO " +
                               "AND PRC.CLNS = CD.CLNS " +
                               "AND PRC.SITUACION IN ('E','L')) " +
                           "END ASESOR_ORIGEN " +
                           ",CD.CDGCLNS " +
                           ",CD.NOMBRE " +
                           ",CD.CICLO " +
                           ",TO_CHAR (CD.INICIO, 'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR (CD.FIN, 'DD/MM/YYYY') FFIN " +
                           ",TO_CHAR (CD.FECHA_LIQUIDA) FLIQUIDA " +
                           ",ROUND(CD.MONTO_ENTREGADO,2) MONTO_ENTREGADO " +
                           ",ROUND(CD.TOTAL_PAGAR,2) TOTAL_PAGAR " +
                           ",ROUND(CD.PAGOS_REAL,2) PAGOS_REAL " +
                           ",CD.TASA " +
                           ",CD.PLAZO " +
                           ",ROUND(CD.SDO_CAPITAL,2) SDO_CAPITAL " +
                           ",ROUND(CD.SDO_INTERES,2) SDO_INTERES " +
                           ",ROUND((SELECT SDO_TOTAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)),2) MONTO_CASTIGADO " +
                           ",ROUND(CD.SDO_TOTAL,2) SALDO_ACTUAL " +
                           ",ROUND(NVL((SELECT MP.CANTIDAD " +
                                  "FROM MP " +
                                  "WHERE MP.CDGEM = CD.CDGEM " +
                                  "AND MP.CDGCLNS = CD.CDGCLNS " +
                                  "AND MP.CLNS = CD.CLNS " +
                                  "AND MP.CICLO = CD.CICLO " +
                                  "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                                  "AND MP.TIPO = 'CI' " +
                                  "AND MP.ESTATUS <> 'E'), 0),2) INTERES_CONDONADO " +
                           ",(SELECT DIAS_MORA " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                             "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC (PL.ALTA)) DIAS_MORA_CASTIGO " +
                           ",(SELECT DIAS_MORA " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                            "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC(PL.ALTA)) " +
                             "+ ((SELECT MAX (FECHA_CALC) " +
                                 "FROM TBL_CIERRE_DIA " +
                                 "WHERE CDGEM = CD.CDGEM " +
                                 "AND CDGCLNS = CD.CDGCLNS " +
                                 "AND CICLO = CD.CICLO " +
                                 "AND CLNS = CD.CLNS) - TRUNC (PL.ALTA)) DIAS_MORA " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'DD/MM/YYYY') FECHA_CASTIGO " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'YYYY/MM/DD') FECHA_ORDEN " +
                            ",(SELECT TO_CHAR (PRL.ALTA , 'DD/MM/YYYY') " +
                                 "FROM PRN_LEGAL PRL WHERE PRL.CDGEM = PL.CDGEM " +
                                 "AND PRL.CDGCLNS = PL.CDGCLNS " +
                                 "AND PRL.CLNS = PL.CLNS " +
                                 "AND PRL.CICLO = PL.CICLO " +
                                 "AND PRL.TIPO = 'Z') FECHA_VENTA " +
                            ",(SELECT MPR.OBSERVACIONES " +
                              "FROM MP, MPR " +
                              "WHERE MP.CDGEM = CD.CDGEM " +
                              "AND MP.CDGCLNS = CD.CDGCLNS " +
                              "AND MP.CLNS = CD.CLNS " +
                              "AND MP.CICLO = CD.CICLO " +
                              "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                              "AND MP.TIPO = 'CI' " +
                              "AND MP.ESTATUS <> 'E' " +
                              "AND MPR.CDGEM = MP.CDGEM " +
                              "AND MPR.CDGCLNS = MP.CDGCLNS " +
                              "AND MPR.CICLO = MP.CICLO " +
                              "AND MPR.PERIODO = MP.PERIODO " +
                              "AND MPR.SECUENCIA = MP.SECUENCIA " +
                              "AND MPR.FECHA = MP.FREALDEP) OBSERV " +
                            "FROM TBL_CIERRE_DIA CD, PRN_LEGAL PL " +
                            "WHERE CD.CDGEM = '" + empresa + "' " +
                            "AND CD.FECHA_CALC = '" + fecha + "' " +
                            "AND CD.SITUACION = 'E' " +
                            "AND PL.CDGEM = CD.CDGEM " +
                            "AND PL.CDGCLNS = CD.CDGCLNS " +
                            "AND PL.CLNS = CD.CLNS " +
                            "AND PL.CICLO = CD.CICLO " +
                            "AND PL.TIPO = 'C' " +
                            "AND TRUNC(PL.ALTA) <= CD.FECHA_CALC " +
                            "UNION " +
                            "SELECT CD.REGION " +
                            ",CD.COD_SUCURSAL " +
                            ",CD.NOM_SUCURSAL " +
                            ",(SELECT NOM_ASESOR " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                             "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC (PL.ALTA)) ASESOR_CASTIGO " +
                            ",CASE WHEN CD.CLNS = 'G' THEN " +
                               "(SELECT NOM_ASESOR " +
                               "FROM TBL_CIERRE_DIA CDC, PRN " +
                               "WHERE CDC.CDGEM = CD.CDGEM " +
                               "AND CDC.CDGCLNS = CD.CDGCLNS " +
                               "AND CDC.CICLO = '01' " +
                               "AND CDC.CLNS = CD.CLNS " +
                               "AND CDC.FECHA_CALC = PRN.INICIO " +
                               "AND PRN.CDGEM = CD.CDGEM " +
                               "AND PRN.CDGNS = CD.CDGCLNS " +
                               "AND PRN.CICLO = CD.CICLO " +
                               "AND PRN.SITUACION IN ('E','L')) " +
                            "WHEN CD.CLNS = 'I' THEN " +
                               "(SELECT NOM_ASESOR " +
                               "FROM TBL_CIERRE_DIA CDC, PRC " +
                               "WHERE CDC.CDGEM = CD.CDGEM " +
                               "AND CDC.CDGCLNS = CD.CDGCLNS " +
                               "AND CDC.CICLO = '01' " +
                               "AND CDC.CLNS = CD.CLNS " +
                               "AND CDC.FECHA_CALC = PRC.INICIO " +
                               "AND PRC.CDGEM = CD.CDGEM " +
                               "AND PRC.CDGCLNS = CD.CDGCLNS " +
                               "AND PRC.CICLO = CD.CICLO " +
                               "AND PRC.CLNS = CD.CLNS " +
                               "AND PRC.SITUACION IN ('E','L')) " +
                            "END ASESOR_ORIGEN " +
                            ",CD.CDGCLNS " +
                            ",CD.NOMBRE " +
                            ",CD.CICLO " +
                            ",TO_CHAR (CD.INICIO, 'DD/MM/YYYY') FINICIO " +
                            ",TO_CHAR (CD.FIN, 'DD/MM/YYYY') FFIN " +
                            ",TO_CHAR (CD.FECHA_LIQUIDA) FLIQUIDA " +
                            ",ROUND(CD.MONTO_ENTREGADO,2) MONTO_ENTREGADO " +
                            ",ROUND(CD.TOTAL_PAGAR,2) TOTAL_PAGAR " +
                            ",ROUND(CD.PAGOS_REAL,2) PAGOS_REAL " +
                            ",CD.TASA " +
                            ",CD.PLAZO " +
                            ",ROUND(CD.SDO_CAPITAL,2) SDO_CAPITAL " +
                            ",ROUND(CD.SDO_INTERES,2) SDO_INTERES " +
                            ",ROUND((SELECT SDO_TOTAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)),2) MONTO_CASTIGADO " +
                            ",ROUND(CD.SDO_TOTAL,2) SALDO_ACTUAL " +
                            ",ROUND(NVL((SELECT MP.CANTIDAD " +
                                  "FROM MP " +
                                  "WHERE MP.CDGEM = CD.CDGEM " +
                                  "AND MP.CDGCLNS = CD.CDGCLNS " +
                                  "AND MP.CLNS = CD.CLNS " +
                                  "AND MP.CICLO = CD.CICLO " +
                                  "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                                  "AND MP.TIPO = 'CI' " +
                                  "AND MP.ESTATUS <> 'E'), 0),2) INTERES_CONDONADO " +
                            ",(SELECT DIAS_MORA " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                             "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC (PL.ALTA)) DIAS_MORA_CASTIGO " +
                            ",(SELECT DIAS_MORA " +
                             "FROM TBL_CIERRE_DIA " +
                             "WHERE CDGEM = CD.CDGEM " +
                             "AND CDGCLNS = CD.CDGCLNS " +
                             "AND CICLO = CD.CICLO " +
                             "AND CLNS = CD.CLNS " +
                             "AND FECHA_CALC = TRUNC(PL.ALTA)) " +
                             "+ ((SELECT MAX (FECHA_CALC) " +
                                 "FROM TBL_CIERRE_DIA " +
                                 "WHERE CDGEM = CD.CDGEM " +
                                 "AND CDGCLNS = CD.CDGCLNS " +
                                 "AND CICLO = CD.CICLO " +
                                 "AND CLNS = CD.CLNS) - TRUNC (PL.ALTA)) DIAS_MORA " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'DD/MM/YYYY') FECHA_CASTIGO " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'YYYY/MM/DD') FECHA_ORDEN " +
                            ",(SELECT TO_CHAR (PRL.ALTA , 'DD/MM/YYYY') " +
                                 "FROM PRN_LEGAL PRL WHERE PRL.CDGEM = PL.CDGEM " +
                                 "AND PRL.CDGCLNS = PL.CDGCLNS " +
                                 "AND PRL.CLNS = PL.CLNS " +
                                 "AND PRL.CICLO = PL.CICLO " +
                                 "AND PRL.TIPO = 'Z') FECHA_VENTA " +
                            ",(SELECT MPR.OBSERVACIONES " +
                              "FROM MP, MPR " +
                              "WHERE MP.CDGEM = CD.CDGEM " +
                              "AND MP.CDGCLNS = CD.CDGCLNS " +
                              "AND MP.CLNS = CD.CLNS " +
                              "AND MP.CICLO = CD.CICLO " +
                              "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                              "AND MP.TIPO = 'CI' " +
                              "AND MP.ESTATUS <> 'E' " +
                              "AND MPR.CDGEM = MP.CDGEM " +
                              "AND MPR.CDGCLNS = MP.CDGCLNS " +
                              "AND MPR.CICLO = MP.CICLO " +
                              "AND MPR.PERIODO = MP.PERIODO " +
                              "AND MPR.SECUENCIA = MP.SECUENCIA " +
                              "AND MPR.FECHA = MP.FREALDEP) OBSERV " +
                            "FROM TBL_CIERRE_DIA CD, PRN_LEGAL PL, (SELECT MAX(CD.FECHA_CALC) FEC_CALC, CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS FROM PRN_LEGAL PL, TBL_CIERRE_DIA CD WHERE PL.CDGEM = '" + empresa + "' AND PL.TIPO = 'Z' AND TRUNC(PL.ALTA) <= '" + fecha + "' AND CD.CDGEM = PL.CDGEM AND CD.CDGCLNS = PL.CDGCLNS AND CD.CICLO = PL.CICLO AND CD.CLNS = PL.CLNS GROUP BY CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS) A " +
                            "WHERE CD.CDGEM = A.CDGEM " +
                            "AND CD.CDGCLNS = A.CDGCLNS " +
                            "AND CD.CICLO = A.CICLO " +
                            "AND CD.CLNS = A.CLNS " +
                            "AND CD.FECHA_CALC = A.FEC_CALC " +
                            "AND PL.CDGEM = CD.CDGEM " +
                            "AND PL.CDGCLNS = CD.CDGCLNS " +
                            "AND PL.CLNS = CD.CLNS " +
                            "AND PL.CICLO = CD.CICLO " +
                            "AND PL.TIPO = 'C' " +
                            "AND TRUNC(PL.ALTA) <= CD.FECHA_CALC " +
                            "UNION " +
                            "SELECT CD.REGION " +
                            ",CD.COD_SUCURSAL " +
                            ",CD.NOM_SUCURSAL " +
                            ",(SELECT NOM_ASESOR " +
                              "FROM TBL_CIERRE_DIA " +
                              "WHERE CDGEM = CD.CDGEM " +
                              "AND CDGCLNS = CD.CDGCLNS " +
                              "AND CICLO = CD.CICLO " +
                              "AND CLNS = CD.CLNS " +
                              "AND FECHA_CALC = TRUNC (PL.ALTA)) ASESOR_CASTIGO " +
                            ",CASE WHEN CD.CLNS = 'G' THEN " +
                                "(SELECT NOM_ASESOR " +
                                "FROM TBL_CIERRE_DIA CDC, PRN " +
                                "WHERE CDC.CDGEM = CD.CDGEM " +
                                "AND CDC.CDGCLNS = CD.CDGCLNS " +
                                "AND CDC.CICLO = '01' " +
                                "AND CDC.CLNS = CD.CLNS " +
                                "AND CDC.FECHA_CALC = PRN.INICIO " +
                                "AND PRN.CDGEM = CD.CDGEM " +
                                "AND PRN.CDGNS = CD.CDGCLNS " +
                                "AND PRN.CICLO = CD.CICLO " +
                                "AND PRN.SITUACION IN ('E','L')) " +
                            "WHEN CD.CLNS = 'I' THEN " +
                                "(SELECT NOM_ASESOR " +
                                "FROM TBL_CIERRE_DIA CDC, PRC " +
                                "WHERE CDC.CDGEM = CD.CDGEM " +
                                "AND CDC.CDGCLNS = CD.CDGCLNS " +
                                "AND CDC.CICLO = '01' " +
                                "AND CDC.CLNS = CD.CLNS " +
                                "AND CDC.FECHA_CALC = PRC.INICIO " +
                                "AND PRC.CDGEM = CD.CDGEM " +
                                "AND PRC.CDGCLNS = CD.CDGCLNS " +
                                "AND PRC.CICLO = CD.CICLO " +
                                "AND PRC.CLNS = CD.CLNS " +
                                "AND PRC.SITUACION IN ('E','L')) " +
                            "END ASESOR_ORIGEN " +
                            ",CD.CDGCLNS " +
                            ",CD.NOMBRE " +
                            ",CD.CICLO " +
                            ",TO_CHAR (CD.INICIO, 'DD/MM/YYYY') FINICIO " +
                            ",TO_CHAR (CD.FIN, 'DD/MM/YYYY') FFIN " +
                            ",TO_CHAR (CD.FECHA_LIQUIDA) FLIQUIDA " +
                            ",ROUND(CD.MONTO_ENTREGADO,2) MONTO_ENTREGADO " +
                            ",ROUND(CD.TOTAL_PAGAR,2) TOTAL_PAGAR " +
                            ",ROUND(CD.PAGOS_REAL,2) PAGOS_REAL " +
                            ",CD.TASA " +
                            ",CD.PLAZO " +
                            ",ROUND((SELECT SDO_CAPITAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)),2) SDO_CAPITAL " +
                            ",ROUND((SELECT SDO_INTERES FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)),2) SDO_INTERES " +
                            ",ROUND((SELECT SDO_TOTAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)),2) MONTO_CASTIGADO " +
                            ",ROUND(CD.SDO_TOTAL,2) SALDO_ACTUAL " +
                            ",ROUND(NVL((SELECT MP.CANTIDAD " +
                                   "FROM MP " +
                                   "WHERE MP.CDGEM = CD.CDGEM " +
                                   "AND MP.CDGCLNS = CD.CDGCLNS " +
                                   "AND MP.CLNS = CD.CLNS " +
                                   "AND MP.CICLO = CD.CICLO " +
                                   "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                                   "AND MP.TIPO = 'CI' " +
                                   "AND MP.ESTATUS <> 'E'), 0),2) INTERES_CONDONADO " +
                            ",(SELECT DIAS_MORA " +
                              "FROM TBL_CIERRE_DIA " +
                              "WHERE CDGEM = CD.CDGEM " +
                              "AND CDGCLNS = CD.CDGCLNS " +
                              "AND CICLO = CD.CICLO " +
                              "AND CLNS = CD.CLNS " +
                              "AND FECHA_CALC = TRUNC (PL.ALTA)) DIAS_MORA_CASTIGO " +
                            ",(SELECT DIAS_MORA " +
                              "FROM TBL_CIERRE_DIA " +
                              "WHERE CDGEM = CD.CDGEM " +
                              "AND CDGCLNS = CD.CDGCLNS " +
                              "AND CICLO = CD.CICLO " +
                              "AND CLNS = CD.CLNS " +
                              "AND FECHA_CALC = TRUNC (PL.ALTA)) + ((A.FEC_CALC - 1) - TRUNC (PL.ALTA)) DIAS_MORA " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'DD/MM/YYYY') FECHA_CASTIGO " +
                            ",TO_CHAR(TRUNC(PL.ALTA),'YYYY/MM/DD') FECHA_ORDEN " +
                            ",(SELECT TO_CHAR(PRL.ALTA , 'DD/MM/YYYY') " +
                                 "FROM PRN_LEGAL PRL WHERE PRL.CDGEM = A.CDGEM " +
                                 "AND PRL.CDGCLNS = A.CDGCLNS " +
                                 "AND PRL.CICLO = A.CICLO " +
                                 "AND PRL.CLNS = A.CLNS " +
                                 "AND PRL.TIPO = 'Z') FECHA_VENTA " +
                            ",(SELECT MPR.OBSERVACIONES " +
                              "FROM MP, MPR " +
                              "WHERE MP.CDGEM = CD.CDGEM " +
                              "AND MP.CDGCLNS = CD.CDGCLNS " +
                              "AND MP.CLNS = CD.CLNS " +
                              "AND MP.CICLO = CD.CICLO " +
                              "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                              "AND MP.TIPO = 'CI' " +
                              "AND MP.ESTATUS <> 'E' " +
                              "AND MPR.CDGEM = MP.CDGEM " +
                              "AND MPR.CDGCLNS = MP.CDGCLNS " +
                              "AND MPR.CICLO = MP.CICLO " +
                              "AND MPR.PERIODO = MP.PERIODO " +
                              "AND MPR.SECUENCIA = MP.SECUENCIA " +
                              "AND MPR.FECHA = MP.FREALDEP) OBSERV " +
                            "FROM TBL_CIERRE_DIA CD, PRN_LEGAL PL, (SELECT MAX(CD.FECHA_CALC) FEC_CALC, CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS FROM PRN_LEGAL PL, TBL_CIERRE_DIA CD WHERE PL.CDGEM = '" + empresa + "' AND PL.TIPO = 'C' AND TRUNC(PL.ALTA) <= '" + fecha + "' AND CD.CDGEM = PL.CDGEM AND CD.CDGCLNS = PL.CDGCLNS AND CD.CICLO = PL.CICLO AND CD.CLNS = PL.CLNS AND CD.FECHA_LIQUIDA IS NOT NULL AND CD.FECHA_LIQUIDA <= '" + fecha + "' GROUP BY CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS) A " +
                            "WHERE CD.CDGEM = A.CDGEM " +
                            "AND CD.CDGCLNS = A.CDGCLNS " +
                            "AND CD.CLNS = A.CLNS " +
                            "AND CD.CICLO = A.CICLO " +
                            "AND CD.FECHA_CALC = A.FEC_CALC " +
                            "AND PL.CDGEM = CD.CDGEM " +
                            "AND PL.CDGCLNS = CD.CDGCLNS " +
                            "AND PL.CLNS = CD.CLNS " +
                            "AND PL.CICLO = CD.CICLO " +
                            "AND PL.TIPO = 'C' " +
                            "AND TRUNC (PL.ALTA) <= '" + fecha + "' " +
                            "ORDER BY FECHA_ORDEN";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_CAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_CAPITAL)", ""));
                dtot["MONTO_CASTIGADO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(MONTO_CASTIGADO)", ""));
                dtot["SDO_INTERES"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INTERES)", ""));
                dtot["INTERES_CONDONADO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(INTERES_CONDONADO)", ""));
                dtot["SALDO_ACTUAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SALDO_ACTUAL)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO QUE EXTRAE LA INFORMACION GENERAL DE LOS CREDITOS MARCADOS PARA FONDEO
    [WebMethod]
    public string getRepCreditosFondeo(string orgFond, string lineaCred, string fecSaldo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryLinea = string.Empty;

        if (lineaCred != "0")
            queryLinea = " AND F.CDGLC = '" + lineaCred + "' ";

        try
        {
            string query = " SELECT PRC.CDGCL PERSONA_ID " +
                "                 , '''' || PRN.CDGNS || PRN.CICLO || PRC.CDGCL CREDITO_ID " +
                "                 , D.PAGARE NO_PAGARE " +
                "                 , 6 DESTINO_CREDITO " +
                "                 , PRC.CANTENTRE MONTO_CREDITO " +
                "                 , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FECHA_ENTREGA " +
                "                 , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD, PRN.PLAZO), 'DD/MM/YYYY') FECHA_VENCIMIENTO " +
                "                 , ROUND((PRN.TASA / 100), 4) TASA_MENSUAL " +
                "                 , 'SS' TIPO_TASA " +
                "                 , PRN.PERIODICIDAD FRECUENCIA_PAGOS " +
                "                 , 'G' METODOLOGIA " +
                "                 , NS.NOMBRE NOMBRE_GRUPO " +
                "                 , NVL(TO_NUMBER(CD.COD_SUCURSAL), 0) PUNTOACCESO_ID " +
                "                 , PE.TELEFONO PROMOTOR " +
                "                 , 'S' HA_SOLICITADO_CREDITO " +
                "                 , CASE WHEN CD.CICLO = '01' THEN 'N' " +
                "                        ELSE 'S' " +
                "                         END PREGUNTA_INGRESO " +
                "                 , ROUND((PRC.CANTENTRE / PRN.PLAZO), 2) MONTO_PAGO " +
                "                 , PRN.PLAZO NUMERO_PAGOS " +
                "                 , CASE WHEN (SELECT COUNT(CDGCL) " +
                "                                FROM MICROSEGURO " +
                "                               WHERE CDGEM = PRC.CDGEM " +
                "                                 AND CDGCL = PRC.CDGCL " +
                "                                 AND ESTATUS IN('R', 'V') " +
                "                                 AND INICIO BETWEEN TO_DATE('01/' || TO_CHAR(CD.FECHA_CALC, 'MM/YYYY')) AND CD.FECHA_CALC) > 0 THEN '2' " +
                "                        ELSE '4' " +
                "                         END ACCESORIO_CREDITICIO " +
                "                 , CASE WHEN (SELECT COUNT(CDGCL) " +
                "                                FROM MICROSEGURO " +
                "                               WHERE CDGEM = PRC.CDGEM " +
                "                                 AND CDGCL = PRC.CDGCL " +
                "                                 AND ESTATUS IN('R', 'V') " +
                "                                 AND INICIO BETWEEN TO_DATE('01/' || TO_CHAR(CD.FECHA_CALC, 'MM/YYYY')) AND CD.FECHA_CALC) > 0 THEN (SELECT MAX(TOTAL) " +
                "                                                                                                                                       FROM MICROSEGURO " +
                "                                                                                                                                      WHERE CDGEM = PRC.CDGEM " +
                "                                                                                                                                        AND CDGCL = PRC.CDGCL " +
                "                                                                                                                                        AND ESTATUS IN('R', 'V') " +
                "                                                                                                                                        AND INICIO BETWEEN TO_DATE('01/' || TO_CHAR(CD.FECHA_CALC, 'MM/YYYY')) AND CD.FECHA_CALC) " +
                "                        ELSE 0 " +
                "                         END MONTO_ACCESORIO_CREDITICIO " +
                "                 , CASE WHEN (SELECT COUNT(CDGCL) " +
                "                                FROM MICROSEGURO " +
                "                               WHERE CDGEM = PRC.CDGEM " +
                "                                 AND CDGCL = PRC.CDGCL " +
                "                                 AND ESTATUS IN('R', 'V') " +
                "                                 AND INICIO BETWEEN TO_DATE('01/' || TO_CHAR(CD.FECHA_CALC, 'MM/YYYY')) " +
                "                                 AND CD.FECHA_CALC) > 0 THEN 1 " +
                "                        ELSE 8 " +
                "                         END FORMA_DE_PAGO_AC " +
                "                 , CASE WHEN FNSDOGARANTIA(CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS, TO_DATE('30/06/2018')) > 0 THEN 1 " +
                "                        ELSE 4 " +
                "                         END GARANTIA_AHORRO_PAGO " +
                "                 , FNSDOGARANTIA(CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS, TO_DATE('30/06/2018')) MONTO_GAR_AHOR " +
                "              FROM TBL_CIERRE_DIA CD " +
                "              JOIN PRN ON CD.CDGEM = PRN.CDGEM AND CD.CDGCLNS = PRN.CDGNS AND CD.CICLO = PRN.CICLO " +
                "              JOIN PRC ON PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO " +
                "              JOIN PE ON CD.CDGEM = PE.CDGEM AND CD.COD_ASESOR = PE.CODIGO " +
                "              JOIN PRC_FONDEO F ON PRC.CDGEM = F.CDGEM AND PRC.CDGNS = F.CDGNS AND PRC.CICLO = F.CICLO AND PRC.CDGCL = F.CDGCL " +
                "              JOIN ORF ON F.CDGEM = ORF.CDGEM AND F.CDGORF = ORF.CODIGO " +
                "              JOIN LC ON F.CDGEM = LC.CDGEM AND F.CDGORF = LC.CDGORF AND F.CDGLC = LC.CODIGO " +
                "              JOIN DISPOSICION D ON F.CDGEM = D.CDGEM AND F.CDGORF = D.CDGORF AND F.CDGLC = D.CDGLC AND F.CDGDISP = D.CODIGO " +
                "              JOIN NS ON CD.CDGEM = NS.CDGEM AND CD.CDGCLNS = NS.CODIGO " +
                "             WHERE CD.CDGEM = '" + empresa + "' " +
                "               AND CD.FECHA_CALC = TO_DATE('" + fecSaldo + "') " +
                "               AND PRN.INICIO >= TRUNC(TO_DATE('" + fecSaldo + "', 'DD/MM/YYYY'), 'MM') " +
                "               AND F.CDGEM = '" + empresa + "' " +
                "               AND F.CDGORF = '" + orgFond + "' " +
                "                 " + queryLinea + " " +
                "               AND F.FREPSDO = TO_DATE('" + fecSaldo + "')";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LA INFORMACION DE LOS CREDITOS VENDIDOS
    [WebMethod]
    public string getRepCreditosVendidos(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            //INCORPORACION DE CREDITOS VENDIDOS
            string query = "SELECT CD.REGION " +
                            ",CD.COD_SUCURSAL " +
                            ",CD.NOM_SUCURSAL " +
                            ",(SELECT NOM_ASESOR " +
                              "FROM TBL_CIERRE_DIA " +
                              "WHERE CDGEM = CD.CDGEM " +
                              "AND CDGCLNS = CD.CDGCLNS " +
                              "AND CICLO = CD.CICLO " +
                              "AND CLNS = CD.CLNS " +
                              "AND FECHA_CALC = TRUNC (PL.ALTA)) ASESOR_VENTA " +
                            ",CASE WHEN CD.CLNS = 'G' THEN " +
                                "(SELECT NOM_ASESOR " +
                                "FROM TBL_CIERRE_DIA CDC, PRN " +
                                "WHERE CDC.CDGEM = CD.CDGEM " +
                                "AND CDC.CDGCLNS = CD.CDGCLNS " +
                                "AND CDC.CICLO = '01' " +
                                "AND CDC.CLNS = CD.CLNS " +
                                "AND CDC.FECHA_CALC = PRN.INICIO " +
                                "AND PRN.CDGEM = CD.CDGEM " +
                                "AND PRN.CDGNS = CD.CDGCLNS " +
                                "AND PRN.CICLO = CD.CICLO " +
                                "AND PRN.SITUACION IN ('E','L')) " +
                            "WHEN CD.CLNS = 'I' THEN " +
                                "(SELECT NOM_ASESOR " +
                                "FROM TBL_CIERRE_DIA CDC, PRC " +
                                "WHERE CDC.CDGEM = CD.CDGEM " +
                                "AND CDC.CDGCLNS = CD.CDGCLNS " +
                                "AND CDC.CICLO = '01' " +
                                "AND CDC.CLNS = CD.CLNS " +
                                "AND CDC.FECHA_CALC = PRC.INICIO " +
                                "AND PRC.CDGEM = CD.CDGEM " +
                                "AND PRC.CDGCLNS = CD.CDGCLNS " +
                                "AND PRC.CICLO = CD.CICLO " +
                                "AND PRC.CLNS = CD.CLNS " +
                                "AND PRC.SITUACION IN ('E','L')) " +
                            "END ASESOR_ORIGEN " +
                            ",CD.CDGCLNS " +
                            ",CD.NOMBRE " +
                            ",CD.CICLO " +
                            ",TO_CHAR (CD.INICIO, 'DD/MM/YYYY') FINICIO " +
                            ",TO_CHAR (CD.FIN, 'DD/MM/YYYY') FFIN " +
                            ",TO_CHAR (CD.FECHA_LIQUIDA) FLIQUIDA " +
                            ",CD.MONTO_ENTREGADO " +
                            ",CD.TOTAL_PAGAR " +
                            ",CD.PAGOS_REAL " +
                            ",CD.TASA " +
                            ",CD.PLAZO " +
                            ",(SELECT SDO_CAPITAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)) SDO_CAPITAL " +
                            ",(SELECT SDO_INTERES FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)) SDO_INTERES " +
                            ",(SELECT SDO_TOTAL FROM TBL_CIERRE_DIA WHERE CDGEM = CD.CDGEM AND CDGCLNS = CD.CDGCLNS AND CICLO = CD.CICLO AND CLNS = CD.CLNS AND FECHA_CALC = TRUNC(PL.ALTA)) MONTO_VENTA " +
                            ",CD.SDO_TOTAL SALDO_ACTUAL " +
                            ",NVL ((SELECT MP.CANTIDAD " +
                                   "FROM MP " +
                                   "WHERE MP.CDGEM = CD.CDGEM " +
                                   "AND MP.CDGCLNS = CD.CDGCLNS " +
                                   "AND MP.CLNS = CD.CLNS " +
                                   "AND MP.CICLO = CD.CICLO " +
                                   "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                                   "AND MP.TIPO = 'CI' " +
                                   "AND MP.ESTATUS <> 'E'), 0) INTERES_CONDONADO " +
                            ",(SELECT DIAS_MORA " +
                              "FROM TBL_CIERRE_DIA " +
                              "WHERE CDGEM = CD.CDGEM " +
                              "AND CDGCLNS = CD.CDGCLNS " +
                              "AND CICLO = CD.CICLO " +
                              "AND CLNS = CD.CLNS " +
                              "AND FECHA_CALC = TRUNC (PL.ALTA)) DIAS_MORA_VENTA " +
                            ",TO_CHAR (TRUNC (PL.ALTA), 'DD/MM/YYYY') FECHA_VENTA " +
                            ",(SELECT MPR.OBSERVACIONES " +
                              "FROM MP, MPR " +
                              "WHERE MP.CDGEM = CD.CDGEM " +
                              "AND MP.CDGCLNS = CD.CDGCLNS " +
                              "AND MP.CLNS = CD.CLNS " +
                              "AND MP.CICLO = CD.CICLO " +
                              "AND MP.FREALDEP = TRUNC (PL.ALTA) " +
                              "AND MP.TIPO = 'CI' " +
                              "AND MP.ESTATUS <> 'E' " +
                              "AND MPR.CDGEM = MP.CDGEM " +
                              "AND MPR.CDGNS = MP.CDGCLNS " +
                              "AND MPR.CICLO = MP.CICLO " +
                              "AND MPR.PERIODO = MP.PERIODO " +
                              "AND MPR.SECUENCIA = MP.SECUENCIA " +
                              "AND MPR.FECHA = MP.FREALDEP) OBSERV " +
                            "FROM TBL_CIERRE_DIA CD, PRN_LEGAL PL, (SELECT MAX(CD.FECHA_CALC) FEC_CALC, CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS FROM PRN_LEGAL PL, TBL_CIERRE_DIA CD WHERE PL.CDGEM = '" + empresa + "' AND PL.TIPO = 'Z' AND TRUNC(PL.ALTA) <= '" + fecha + "' AND CD.CDGEM = PL.CDGEM AND CD.CDGCLNS = PL.CDGCLNS AND CD.CICLO = PL.CICLO AND CD.CLNS = PL.CLNS GROUP BY CD.CDGEM, CD.CDGCLNS, CD.CICLO, CD.CLNS) A " +
                            "WHERE CD.CDGEM = A.CDGEM " +
                            "AND CD.CDGCLNS = A.CDGCLNS " +
                            "AND CD.CLNS = A.CLNS " +
                            "AND CD.CICLO = A.CICLO " +
                            "AND CD.FECHA_CALC = A.FEC_CALC " +
                            "AND PL.CDGEM = CD.CDGEM " +
                            "AND PL.CDGCLNS = CD.CDGCLNS " +
                            "AND PL.CLNS = CD.CLNS " +
                            "AND PL.CICLO = CD.CICLO " +
                            "AND PL.TIPO = 'Z' " +
                            "AND TRUNC(PL.ALTA) <= '" + fecha + "'";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_CAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_CAPITAL)", ""));
                dtot["MONTO_VENTA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(MONTO_VENTA)", ""));
                dtot["SDO_INTERES"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INTERES)", ""));
                dtot["INTERES_CONDONADO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(INTERES_CONDONADO)", ""));
                dtot["SALDO_ACTUAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SALDO_ACTUAL)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO DE GENERA INFORMACION SOBRE CRITERIO DE RIESGO
    [WebMethod]
    public string getRepCriterioRiesgo(string factor, string tipoCrit)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string queryRiesgo = string.Empty;
        string queryCriterio = string.Empty;
        int iRes;
        string xml = "";

        string query = "SELECT CFC.DESCRIPCION FACT_RIESGO " +
                       ", CTC.DESCRIPCION TIPO_CRITERIO " +
                       ", CTR.DESCRIPCION CRITERIO_RIESGO " +
                       ", CNC.DESCRIPCION NIVEL_RIESGO " +
                       " FROM  CAT_FACTOR_RIESGO CFC, " +
                       " CAT_TIPO_CRITERIO CTC, " +
                       " CAT_CRITERIO_RIESGO CTR, " +
                       " CAT_NR_CRITERIO CNC " +
                       " WHERE CFC.CODIGO = CTC.CDGFR " +
                       " AND CTC.CODIGO = CTR.CDGTIPOCRIT " +
                       " AND CTR.VALORACFIS = CNC.CODIGO " +
                       " AND CTR.VALORSOFOM = CNC.CODIGO " +
                       " AND CFC.CODIGO = '" + factor + "' " +
                       " AND CTC.CODIGO = '" + tipoCrit + "' ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE DEFUNCIÓN
    [WebMethod]
    public string getRepDefuncion(string mes, string anio)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaInicio = "01/" + mes + "/" + anio;
        string fechaFin = "LAST_DAY(TO_DATE('" + fechaInicio + "', 'DD/MM/YYYY'))";
        int iRes;

        try
        {
            string query = " SELECT CO.NOMBRE SUCURSAL " +
                "                , NOMBREC(NULL,NULL,'I','N',D.NOMBRE1,D.NOMBRE2,D.PRIMAPE,D.SEGAPE) NOMBREPF " +
                "                 , CASE WHEN D.MISDATCL = 'S' THEN 'ACREDITADO' " +
                "                        ELSE ( SELECT DESCRIPCION FROM CAT_PARENTESCO WHERE CODIGO = D.CDGPAREN ) END PFALLECIDA " +
                "                 , D.SALDODEUDOR " +
                "                 , D.CDGCLNS " +
                "                 , D.CICLO " +
                "                 , TO_CHAR( D.DEFUNCION, 'MONTH') MESDEF " +
                "                 , TO_CHAR( D.FREGISTRO, 'MONTH') MESREP " +
                "                 , D.EDAD " +
                "                 , D.CAUSAMUERTE " +
                "                 , ( SELECT DESCRIPCION FROM CAT_TIPO_DEFUN WHERE CODIGO = D.CDGTDEF ) TIPOMUERTE " +
                "                 , CASE WHEN D.PAGADO = 'S' THEN 'SI' " +
                "                        WHEN D.PAGADO = 'N' THEN 'NO' " +
                "                         END PAGADO " +
                "                 , CASE WHEN D.DOCORIG = 'S' THEN 'SI' " +
                "                        WHEN D.DOCORIG = 'N' THEN 'NO' " +
                "                         END DOCRECIB " +
                "                 , NOMBREC( NULL,NULL,'I','N',DB.NOMBRE1,DB.NOMBRE2,DB.PRIMAPE,DB.SEGAPE ) NOMBENEF " +
                "                 , ( SELECT DESCRIPCION FROM CAT_PARENTESCO WHERE CODIGO = DB. CDGPAREN) PARENTESCO " +
                "                 , CAS.NOMBRE ASEGURADORA " +
                "              FROM DEFUNCION D " +
                "              JOIN DEFUNCION_BENEFICIARIO DB ON DB.CDGEM = D.CDGEM " +
                "                                            AND DB.CDGDEFUN = D.CODIGO " +
                "              JOIN CO ON CO.CDGEM = D.CDGEM " +
                "                     AND CO.CODIGO = D.CDGCO " +
                "              JOIN MICROSEGURO M ON D.CDGEM = M.CDGEM " +
                "                                AND D.CDGCL = M.CDGCL " +
                "                                AND D.CDGPMS = M.CDGPMS " +
                "                                AND D.INICIOPMS = M.INICIO " +
                "              JOIN CAT_ASEGURADORA CAS ON M.CDGEM = CAS.CDGEM AND M.CDGASE = CAS.CODIGO " +
                "             WHERE D.CDGEM = '" + empresa + "' " +
                "               AND TRUNC(D.FREGISTRO) BETWEEN '" + fechaInicio + "' AND " + fechaFin;

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION PARA REPORTE DE CARGA DE DESGLOCE (PRONAFIM) DE LOS CREDITOS MARCADOS PARA FONDEO
    /*[WebMethod]
    public string getRepDesglocePronafim(string orgFond, string lineaCred, string fecSaldo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryLinea = string.Empty;

        if (lineaCred != "0")
            queryLinea = " AND PF.CDGLC = '" + lineaCred + "' ";

        try
        {
            string query = " SELECT PF.CDGCL PERSONA_ID " +
                "                 , '''' || PF.CDGNS || PF.CICLO || PF.CDGCL CREDITO_ID " +
                "                 , CASE WHEN (CASE WHEN (SELECT COUNT(*) " +
                "                                           FROM TBL_DIAS_MORA " +
                "                                          WHERE CDGEM = CD.CDGEM " +
                "                                            AND CDGCLNS = CD.CDGCLNS " +
                "                                            AND CICLO = CD.CICLO " +
                "                                            AND CLNS = CD.CLNS " +
                "                                            AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                        FROM TBL_DIAS_MORA " +
                "                                                                                       WHERE CDGEM = CD.CDGEM " +
                "                                                                                         AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                         AND CICLO = CD.CICLO " +
                "                                                                                         AND CLNS = CD.CLNS " +
                "                                                                                         AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END) > 90 THEN 'CVen' " +
                "                        ELSE 'CV' " +
                "                         END STATUS " +
                "                 , GREATEST(ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO) * (CD.SDO_CAPITAL - CD.MORA_CAPITAL)),2),0) SDO_CAP_VIG " +
                "                 , ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO) * ( CD.MORA_CAPITAL) ),2) SDO_CAP_VEN " +
                "                 , CASE WHEN (SELECT COUNT(*) " +
                "                                FROM TBL_DIAS_MORA " +
                "                               WHERE CDGEM = CD.CDGEM " +
                "                                 AND CDGCLNS = CD.CDGCLNS " +
                "                                 AND CICLO = CD.CICLO " +
                "                                 AND CLNS = CD.CLNS " +
                "                                 AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA " +
                "                                                                             FROM TBL_DIAS_MORA " +
                "                                                                            WHERE CDGEM = CD.CDGEM " +
                "                                                                              AND CDGCLNS = CD.CDGCLNS " +
                "                                                                              AND CICLO = CD.CICLO " +
                "                                                                              AND CLNS = CD.CLNS " +
                "                                                                              AND FECHA_CALC = CD.FECHA_CALC) " +
                "                        ELSE CD.DIAS_MORA " +
                "                         END DIAS_MORA " +
                "                 , PF.CDGLC LINEA " +
                "                 , PF.CDGDISP DISPOSICION " +
                "              FROM PRC_FONDEO PF " +
                "              JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM " +
                "                                    AND CD.CDGCLNS = PF.CDGNS " +
                "                                    AND CD.CICLO = PF.CICLO " +
                "                                    AND CD.FECHA_CALC = PF.FREPSDO " +
                "                                    AND CD.CLNS = PF.CLNS " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '" + orgFond + "' " +
                "                 " + queryLinea + " " +
                "               AND PF.FREPSDO = '" + fecSaldo + "' " +
                "          ORDER BY PF.CDGLC, PF.CDGDISP ";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }*/

    //METODO QUE EXTRAE LA INFORMACION PARA REPORTE DE CARGA DE DESGLOCE (PRONAFIM) DE LOS CREDITOS MARCADOS PARA FONDEO
    [WebMethod]
    public string getRepDesglocePronafim(string orgFond, string lineaCred, string fecSaldo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryLinea = string.Empty;

        if (lineaCred != "0")
            queryLinea = " AND PF.CDGLC = '" + lineaCred + "' ";

        try
        {
            string query = " SELECT PF.CDGCL PERSONA_ID " +
                "                 , '''' || PF.CDGNS || PF.CICLO || PF.CDGCL CREDITO_ID " +
                "                 , CASE WHEN NVL(CD.DIAS_MORA,0) > 90 THEN 'CVen' " +
                "                        ELSE 'CV' " +
                "                         END STATUS " +
                "                 , GREATEST(ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO) * (CD.SDO_CAPITAL - CD.MORA_CAPITAL)),2),0) SDO_CAP_VIG " +
                "                 , ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO) * ( CD.MORA_CAPITAL) ),2) SDO_CAP_VEN " +
                "                 , CD.DIAS_MORA " +
                "                 , PF.CDGLC LINEA " +
                "                 , PF.CDGDISP DISPOSICION " +
                "              FROM PRC_FONDEO PF " +
                "              JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM " +
                "                                    AND CD.CDGCLNS = PF.CDGNS " +
                "                                    AND CD.CICLO = PF.CICLO " +
                "                                    AND CD.FECHA_CALC = PF.FREPSDO " +
                "                                    AND CD.CLNS = PF.CLNS " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '" + orgFond + "' " +
                "                 " + queryLinea + " " +
                "               AND PF.FREPSDO = '" + fecSaldo + "' " +
                "          ORDER BY PF.CDGLC, PF.CDGDISP ";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //REPORTE PARA ABC: REPORTE DETALLE DE MOVIMIENTOS
    [WebMethod]
    public string getRepDetalleMovimientos(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = " SELECT '01' AS TIPO_ARCHIVO " +
                "                 , '02' AS TIPO_REG " +
                "                 , '1000096097' AS ID_IFNB " +
                "                 , '11123' AS IDCREDABC " +
                "                 , CL.RFC AS IDCL_IFNB " +
                "                 , PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS IDCRED_IFNB " +
                "                 , NOMBREC(NULL,NULL,NULL,'N',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOM_CLIENTE " +
                "                 , CO.NOMBRE SUCURSAL " +
                "                 , CASE WHEN CL.CDGTIPOPERS = '001' OR CL.CDGTIPOPERS = '002' OR CL.CDGTIPOPERS = '004' THEN '01' " +
                "                        WHEN CL.CDGTIPOPERS = '003' THEN '02' " +
                "                        ELSE  '' END TIPO_PERSONA " +
                "                 , '98' PROD_IFNB " +
                "                 , CASE WHEN EXISTS (SELECT CDGCL " +
                "                                       FROM PRC_FONDEO_FINAL PFF " +
                "                                      WHERE PFF.CDGEM = PF.CDGEM " +
                "                                        AND PFF.CDGORF = PF.CDGORF " +
                "                                        AND PFF.CDGNS = PF.CDGNS " +
                "                                        AND PFF.CICLO = PF.CICLO " +
                "                                        AND PFF.CDGCL = PF.CDGCL " +
                "                                        AND PFF.FREPSDO = TRUNC(TO_DATE('" + fecha + "','DD-MM-YYYY') , 'MONTH')-1) THEN '03' " +
                "                        ELSE '01' " +
                "                         END TIPO_MOV " +
                "                 , '01' REC_UTILI " +
                "                 , TO_NUMBER(PRN.INICIO - TO_DATE('30-12-1899', 'DD-MM-YYYY')) INICIO " +
                "                 , TO_NUMBER(DECODE(NVL(PRN.PERIODICIDAD,'') , 'S', PRN.inicio + (7 * NVL(PRN.plazo,0)) " +
                "                                                             , 'Q', PRN.inicio + (15 * NVL(PRN.plazo,0)) " +
                "                                                             , 'C', PRN.inicio + (14 * NVL(PRN.plazo,0)) " +
                "                                                             , 'M', PRN.inicio + (30 * NVL(PRN.plazo,0)) " +
                "                                                             , '', '')  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECFIN " +
                "                 , CASE WHEN (CASE WHEN (SELECT COUNT(*) " +
                "                                           FROM TBL_DIAS_MORA " +
                "                                          WHERE CDGEM = CD.CDGEM " +
                "                                            AND CDGCLNS = CD.CDGCLNS " +
                "                                            AND CICLO = CD.CICLO " +
                "                                            AND CLNS = CD.CLNS " +
                "                                            AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                        FROM TBL_DIAS_MORA " +
                "                                                                                       WHERE CDGEM = CD.CDGEM " +
                "                                                                                         AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                         AND CICLO = CD.CICLO " +
                "                                                                                         AND CLNS = CD.CLNS " +
                "                                                                                         AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END ) > 0 THEN NVL((PF.FREPSDO - (CASE WHEN (SELECT COUNT(*)  " +
                "                                                                                   FROM TBL_DIAS_MORA " +
                "                                                                                  WHERE CDGEM = CD.CDGEM " +
                "                                                                                    AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                    AND CICLO = CD.CICLO " +
                "                                                                                    AND CLNS = CD.CLNS " +
                "                                                                                    AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                                                                FROM TBL_DIAS_MORA " +
                "                                                                                                                               WHERE CDGEM = CD.CDGEM " +
                "                                                                                                                                 AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                                                                 AND CICLO = CD.CICLO " +
                "                                                                                                                                 AND CLNS = CD.CLNS " +
                "                                                                                                                                 AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                                                           ELSE CD.DIAS_MORA " +
                "                                                                            END )) - TO_DATE('30-12-1899', 'DD-MM-YYYY'),0) " +
                "                        ELSE 0 " +
                "                         END FEC_INCUMP " +
                "                 , CASE WHEN (SELECT COUNT(*)  " +
                "                                FROM TBL_DIAS_MORA " +
                "                               WHERE CDGEM = CD.CDGEM " +
                "                                 AND CDGCLNS = CD.CDGCLNS " +
                "                                 AND CICLO = CD.CICLO " +
                "                                 AND CLNS = CD.CLNS " +
                "                                 AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                             FROM TBL_DIAS_MORA " +
                "                                                                            WHERE CDGEM = CD.CDGEM " +
                "                                                                              AND CDGCLNS = CD.CDGCLNS " +
                "                                                                              AND CICLO = CD.CICLO " +
                "                                                                              AND CLNS = CD.CLNS " +
                "                                                                              AND FECHA_CALC = CD.FECHA_CALC) " +
                "                           ELSE CD.DIAS_MORA " +
                "                            END NUM_DIAS_INCUMP  " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,''), 'S','01','Q','02','C','02','M','03','','') PERIODICIDAD " +
                "                 , PRC.CANTENTRE " +
                "                 , '01' TIPO_TASA " +
                "                 , PRN.TASA/100 TASA " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,'') , 'S', PRN.PLAZO / 4, 'Q', PRN.PLAZO / 2, 'C', PRN.PLAZO /2, 'M', PRN.PLAZO, '', '') PLAZO " +
                "                 , PRN.PLAZO NUM_CUOTAS " +
                "                 , FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA)  CUOTAS_PAGADAS " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,'') , 'S', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 7 ) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                   , 'Q', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 15) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                   , 'C', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 14) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                   , 'M', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 30) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                   , '', '') CUOTAS_VENCIDAS " +
                "                 , ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL - CD.MORA_CAPITAL)),2) SALDO_CAP_VIG " +
                "                 , ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* ( CD.MORA_CAPITAL) ),2) CAP_VENCIDO " +
                "                 , CASE WHEN (CASE WHEN (SELECT COUNT(*)  " +
                "                                           FROM TBL_DIAS_MORA " +
                "                                          WHERE CDGEM = CD.CDGEM " +
                "                                            AND CDGCLNS = CD.CDGCLNS " +
                "                                            AND CICLO = CD.CICLO " +
                "                                            AND CLNS = CD.CLNS " +
                "                                            AND FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                                                        FROM TBL_DIAS_MORA " +
                "                                                                                                                       WHERE CDGEM = CD.CDGEM " +
                "                                                                                                                         AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                                                         AND CICLO = CD.CICLO " +
                "                                                                                                                         AND CLNS = CD.CLNS " +
                "                                                                                                                         AND FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) " +
                "                                   ELSE (SELECT TCD.DIAS_MORA " +
                "                                            FROM TBL_CIERRE_DIA TCD " +
                "                                           WHERE TCD.CDGEM = PRN.CDGEM " +
                "                                             AND TCD.CDGCLNS = PRN.CDGNS " +
                "                                             AND TCD.CICLO = PRN.CICLO " +
                "                                             AND TCD.FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) " +
                "                                    END ) = 0 THEN CASE WHEN ((SELECT SUM(DD.DEV_DIARIO) " +
                "                                                                 FROM DEVENGO_DIARIO DD " +
                "                                                                WHERE DD.CDGEM = PRN.CDGEM " +
                "                                                                  AND DD.CDGCLNS = PRN.CDGNS " +
                "                                                                  AND DD.CICLO = PRN.CICLO " +
                "                                                                  AND DD.FECHA_CALC <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))  " +
                "                                                                  AND DD.ESTATUS <> 'CA') - (SELECT SUM(MP.PAGADOINT) " +
                "                                                                                               FROM MP " +
                "                                                                                              WHERE MP.CDGEM = PRN.CDGEM  " +
                "                                                                                                AND MP.CDGCLNS = PRN.CDGNS " +
                "                                                                                                AND MP.CICLO = PRN.CICLO " +
                "                                                                                                AND MP.TIPO <> 'IN' " +
                "                                                                                                AND MP.FREALDEP <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')))) <0 THEN 0 " +
                "                                                        ELSE ROUND((PRC.CANTENTRE/PRN.CANTENTRE) * ((SELECT SUM(DD.DEV_DIARIO) " +
                "                                                                                                       FROM DEVENGO_DIARIO DD " +
                "                                                                                                      WHERE DD.CDGEM = PRN.CDGEM " +
                "                                                                                                        AND DD.CDGCLNS = PRN.CDGNS " +
                "                                                                                                        AND DD.CICLO = PRN.CICLO " +
                "                                                                                                        AND DD.FECHA_CALC <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "                                                                                                        AND DD.ESTATUS <> 'CA') - (SELECT SUM(MP.PAGADOINT) " +
                "                                                                                                                                     FROM MP " +
                "                                                                                                                                    WHERE MP.CDGEM = PRN.CDGEM " +
                "                                                                                                                                     AND MP.CDGCLNS = PRN.CDGNS " +
                "                                                                                                                                      AND MP.CICLO = PRN.CICLO " +
                "                                                                                                                                      AND MP.TIPO <> 'IN' " +
                "                                                                                                                                      AND MP.FREALDEP <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')))),2) " +
                "                                                         END " +
                "                        ELSE CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO) " +
                "                                              FROM DEVENGO_DIARIO DD " +
                "                                             WHERE DD.CDGEM = PRN.CDGEM " +
                "                                               AND DD.CDGCLNS = PRN.CDGNS " +
                "                                               AND DD.CICLO = PRN.CICLO   " +
                "                                               AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) - PRN.INICIO)/ 7 )) + 1)  " +
                "                                                                     AND LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) AND DD.ESTATUS <> 'CA'),0) <= 0 THEN 0 " +
                "                                  ELSE ROUND(((PRC.CANTENTRE /PRN.CANTENTRE) * ( SELECT SUM(DD.DEV_DIARIO) " +
                "                                                                                   FROM DEVENGO_DIARIO DD " +
                "                                                                                  WHERE DD.CDGEM = PRN.CDGEM " +
                "                                                                                    AND DD.CDGCLNS = PRN.CDGNS " +
                "                                                                                    AND DD.CICLO = PRN.CICLO " +
                "                                                                                    AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) - PRN.INICIO)/ 7 )) + 1)  " +
                "                                                                                                          AND LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) AND DD. ESTATUS <> 'CA')),2)  " +
                "                                   END  " +
                "                         END SDO_INT_VIGENTE " +
                "                 , CASE WHEN (CASE WHEN (SELECT COUNT(*)  " +
                "                                           FROM TBL_DIAS_MORA " +
                "                                          WHERE CDGEM = CD.CDGEM " +
                "                                            AND CDGCLNS = CD.CDGCLNS " +
                "                                            AND CICLO = CD.CICLO " +
                "                                            AND CLNS = CD.CLNS " +
                "                                            AND FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                                                        FROM TBL_DIAS_MORA " +
                "                                                                                                                       WHERE CDGEM = CD.CDGEM " +
                "                                                                                                                         AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                                                         AND CICLO = CD.CICLO " +
                "                                                                                                                         AND CLNS = CD.CLNS " +
                "                                                                                                                         AND FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) " +
                "                                   ELSE (SELECT TCD.DIAS_MORA  " +
                "                                           FROM TBL_CIERRE_DIA TCD " +
                "                                          WHERE TCD.CDGEM = PRN.CDGEM  " +
                "                                            AND TCD.CDGCLNS = PRN.CDGNS  " +
                "                                            AND TCD.CICLO = PRN.CICLO  " +
                "                                            AND TCD.FECHA_CALC = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))) " +
                "                                    END ) = 0 THEN 0 " +
                "                        ELSE CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO)  " +
                "                                             FROM DEVENGO_DIARIO DD  " +
                "                                            WHERE DD.CDGEM = PRN.CDGEM  " +
                "                                              AND DD.CDGCLNS = PRN.CDGNS  " +
                "                                              AND DD.CICLO = PRN.CICLO " +
                "                                              AND DD. FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) - PRN.INICIO)/ 7 )) + 1)  " +
                "                                                                     AND LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "                                              AND DD.ESTATUS <> 'CA'),0) <= 0 THEN ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO)  " +
                "                                                                                                                              FROM DEVENGO_DIARIO DD  " +
                "                                                                                                                             WHERE DD.CDGEM = PRN.CDGEM  " +
                "                                                                                                                               AND DD.CDGCLNS = PRN.CDGNS  " +
                "                                                                                                                               AND DD.CICLO = PRN.CICLO  " +
                "                                                                                                                               AND DD.FECHA_CALC <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "                                                                                                                               AND DD.ESTATUS <> 'CA') - (SELECT SUM(MP.PAGADOINT)  " +
                "                                                                                                                                                            FROM MP  " +
                "                                                                                                                                                           WHERE MP.CDGEM = PRN.CDGEM  " +
                "                                                                                                                                                             AND MP.CDGCLNS = PRN.CDGNS  " +
                "                                                                                                                                                             AND MP.CICLO = PRN.CICLO  " +
                "                                                                                                                                                             AND MP.TIPO <> 'IN' " +
                "                                                                                                                                                             AND MP.FREALDEP <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))))) ,2) " +
                "                                  ELSE (ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO)  " +
                "                                                                                   FROM DEVENGO_DIARIO DD  " +
                "                                                                                  WHERE DD.CDGEM = PRN.CDGEM  " +
                "                                                                                    AND DD.CDGCLNS = PRN.CDGNS  " +
                "                                                                                    AND DD.CICLO = PRN.CICLO  " +
                "                                                                                    AND DD.FECHA_CALC <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "                                                                                    AND DD.ESTATUS <> 'CA') - (SELECT SUM(MP.PAGADOINT)  " +
                "                                                                                                                 FROM MP  " +
                "                                                                                                                WHERE MP.CDGEM = PRN.CDGEM  " +
                "                                                                                                                  AND MP.CDGCLNS = PRN.CDGNS  " +
                "                                                                                                                  AND MP.CICLO = PRN.CICLO  " +
                "                                                                                                                  AND MP.TIPO <> 'IN'  " +
                "                                                                                                                  AND MP.FREALDEP <= LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))))) ,2) - ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ( SELECT SUM(DD.DEV_DIARIO)  " +
                "                                                                                                                                                                                                                                       FROM DEVENGO_DIARIO DD  " +
                "                                                                                                                                                                                                                                      WHERE DD.CDGEM = PRN.CDGEM  " +
                "                                                                                                                                                                                                                                        AND DD.CDGCLNS = PRN.CDGNS  " +
                "                                                                                                                                                                                                                                        AND DD.CICLO = PRN.CICLO " +
                "                                                                                                                                                                                                                                        AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))- PRN.INICIO)/ 7 )) + 1)  " +
                "                                                                                                                                                                                                                                                              AND LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))  " +
                "                                                                                                                                                                                                                                        AND DD.ESTATUS <> 'CA')),2)) " +
                "                                   END " +
                "                        END SDO_INT_VENCIDO " +
                "                 , (SELECT CALIFICACION  " +
                "                      FROM CAT_CALIF_CL_ABC  " +
                "                     WHERE (CASE WHEN (SELECT COUNT(*)  " +
                "                                         FROM TBL_DIAS_MORA " +
                "                                        WHERE CDGEM = CD.CDGEM " +
                "                                          AND CDGCLNS = CD.CDGCLNS " +
                "                                          AND CICLO = CD.CICLO " +
                "                                          AND CLNS = CD.CLNS " +
                "                                          AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                      FROM TBL_DIAS_MORA " +
                "                                                                                     WHERE CDGEM = CD.CDGEM " +
                "                                                                                       AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                       AND CICLO = CD.CICLO " +
                "                                                                                       AND CLNS = CD.CLNS " +
                "                                                                                       AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END) BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_INICIAL " +
                "                 , TO_NUMBER(TRUNC(SYSDATE)- TO_DATE('30-12-1899', 'DD-MM-YYYY') ) FREGISTRO_PORT " +
                "                 , TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECMOV " +
                "              FROM PRC_FONDEO PF " +
                "              JOIN CL ON CL.CDGEM = PF.CDGEM AND CL.CODIGO = PF.CDGCL " +
                "              JOIN PRN ON PRN.CDGEM = PF.CDGEM AND PRN.CDGNS = PF.CDGNS AND PRN.CICLO = PF.CICLO " +
                "              JOIN PRC ON PRC .CDGEM = PF.CDGEM AND PRC.CDGNS = PF.CDGNS AND PRC.CDGCL = PF.CDGCL AND PRC.CICLO = PF.CICLO " +
                "              JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM AND CD.CDGCLNS = PF.CDGNS AND CD.CICLO = PF.CICLO AND CD.FECHA_CALC = PF.FREPSDO " +
                "              JOIN CO ON CO.CDGEM = PF.CDGEM AND CO.CODIGO = PRN.CDGCO " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '0005' " +
                "               AND PF.FREPSDO = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "             UNION " +
                "            SELECT '01' AS TIPO_ARCHIVO " +
                "                 , '02' AS TIPO_REG " +
                "                 , '1000096097' AS ID_IFNB " +
                "                 , '11123' AS IDCREDABC " +
                "                 , CL.RFC AS IDCL_IFNB " +
                "                 , PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS IDCRED_IFNB " +
                "                 , NOMBREC(NULL,NULL,NULL,'A',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOM_CLIENTE " +
                "                 , CO.NOMBRE SUCURSAL " +
                "                 , CASE WHEN CL.CDGTIPOPERS = '001' OR CL.CDGTIPOPERS = '002' OR CL.CDGTIPOPERS = '004' THEN '01' " +
                "                        WHEN CL.CDGTIPOPERS = '003' THEN '02' " +
                "                        ELSE '' END TIPO_PERSONA " +
                "                 , '98' PROD_IFNB " +
                "                 , '02' TIPO_MOV " +
                "                 , '01' REC_UTILI " +
                "                , TO_NUMBER(PRN.INICIO - TO_DATE('30-12-1899', 'DD-MM-YYYY')) INICIO " +
                "                 , TO_NUMBER(DECODE(NVL(PRN.PERIODICIDAD,'') , 'S', PRN.inicio + (7 * NVL(PRN.plazo,0)) " +
                "                                                             , 'Q', PRN.inicio + (15 * NVL(PRN.plazo,0)) " +
                "                                                             , 'C', PRN.inicio + (14 * NVL(PRN.plazo,0)) " +
                "                                                             , 'M', PRN.inicio + (30 * NVL(PRN.plazo,0)) " +
                "                                                             , '', '')  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECFIN " +
                "                 , (SELECT NVL(TRUNC(MIN(FECHA_CALC)) - TO_DATE('30-12-1899', 'DD-MM-YYYY'),0) " +
                "                      FROM TBL_CIERRE_DIA " +
                "                     WHERE CDGEM = PF.CDGEM " +
                "                       AND CDGCLNS = PF.CDGNS " +
                "                       AND CLNS = PF.CLNS " +
                "                       AND CICLO = PF.CICLO " +
                "                       AND FECHA_CALC <= PF.FREPSDO " +
                "                       AND MORA_TOTAL > 0) FEC_INCUMP " +
                "                 , CASE WHEN (SELECT COUNT(*)  " +
                "                                FROM TBL_DIAS_MORA " +
                "                               WHERE CDGEM = CD.CDGEM " +
                "                                 AND CDGCLNS = CD.CDGCLNS " +
                "                                 AND CICLO = CD.CICLO " +
                "                                 AND CLNS = CD.CLNS " +
                "                                 AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                             FROM TBL_DIAS_MORA " +
                "                                                                            WHERE CDGEM = CD.CDGEM " +
                "                                                                              AND CDGCLNS = CD.CDGCLNS " +
                "                                                                              AND CICLO = CD.CICLO " +
                "                                                                              AND CLNS = CD.CLNS " +
                "                                                                              AND FECHA_CALC = CD.FECHA_CALC) " +
                "                           ELSE CD.DIAS_MORA " +
                "                            END NUM_DIAS_INCUMP  " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,''), 'S','01','Q','02','C','02','M','03','','') PERIODICIDAD " +
                "                 , PRC.CANTENTRE " +
                "                 , '01' TIPO_TASA " +
                "                 , PRN.TASA/100 TASA  " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,''), 'S', PRN.PLAZO / 4 " +
                "                                                  , 'Q', PRN.PLAZO / 2 " +
                "                                                  , 'C', PRN.PLAZO /2 " +
                "                                                  , 'M', PRN.PLAZO  " +
                "                                                  , '', '') PLAZO  " +
                "                 , PRN.PLAZO NUM_CUOTAS " +
                "                 , FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) CUOTAS_PAGADAS " +
                "                 , DECODE(NVL(PRN.PERIODICIDAD,''), 'S', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 7 ) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                  , 'Q', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 15) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                  , 'C', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 14) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                  , 'M', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 30) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA) " +
                "                                                  , '', '') CUOTAS_VENCIDAS " +
                "                 , 0 SALDO_CAP_VIG " +
                "                 , 0 CAP_VENCIDO " +
                "                 , 0 SDO_INT_VIGENTE " +
                "                 , 0 SDO_INT_VENCIDO " +
                "                 , (SELECT CALIFICACION  " +
                "                      FROM CAT_CALIF_CL_ABC  " +
                "                     WHERE (CASE WHEN (SELECT COUNT(*)  " +
                "                                         FROM TBL_DIAS_MORA " +
                "                                        WHERE CDGEM = CD.CDGEM " +
                "                                          AND CDGCLNS = CD.CDGCLNS " +
                "                                          AND CICLO = CD.CICLO " +
                "                                          AND CLNS = CD.CLNS " +
                "                                          AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                      FROM TBL_DIAS_MORA " +
                "                                                                                     WHERE CDGEM = CD.CDGEM " +
                "                                                                                       AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                       AND CICLO = CD.CICLO " +
                "                                                                                       AND CLNS = CD.CLNS " +
                "                                                                                       AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END) BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_INICIAL " +
                "                 , TO_NUMBER(TRUNC(SYSDATE)- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FREGISTRO_PORT " +
                "                 , TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECMOV " +
                "              FROM BITACORA_ELIMINACION BE " +
                "              JOIN BITACORA_ELIMINACION_DATOS PF ON PF.CDGEM = BE.CDGEM AND PF.CDGORF = '0005' AND PF.ESTATUS = 'PROCESADO' AND PF.CDGBITELI  = BE.CODIGO " +
                "              JOIN CL ON CL.CDGEM = PF.CDGEM AND CL.CODIGO = PF.CDGCL " +
                "              JOIN PRN ON PRN.CDGEM = PF.CDGEM AND PRN.CDGNS = PF.CDGNS AND PRN.CICLO = PF.CICLO " +
                "              JOIN PRC ON PRC.CDGEM = PF.CDGEM AND PRC.CDGNS = PF.CDGNS AND PRC.CDGCL = PF.CDGCL AND PRC.CICLO = PF.CICLO " +
                "              JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM AND CD.CDGCLNS = PF.CDGNS AND CD.CICLO = PF.CICLO AND CD.FECHA_CALC = PF.FREPSDO " +
                "              JOIN CO ON CO.CDGEM = PF.CDGEM AND CO.CODIGO = PRN.CDGCO " +
                "             WHERE BE.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '0005' " +
                "               AND BE.FELIMINA = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) " +
                "               AND BE.DESCRIPCION IN ('ELIMINA_FONDEO_ABC_AUTO_L','ELIMINA_FONDEO_ABC_AUTO') " +
                "          ORDER BY TIPO_MOV ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    /*//REPORTE PARA ABC: REPORTE DETALLE DE MOVIMIENTOS
    [WebMethod]
    public string getRepDetalleMovimientos(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaFin = " LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) ";
        string fechaMesAnt = " TRUNC(TO_DATE('" + fecha + "','DD-MM-YYYY') , 'MONTH')-1 ";
        fecha = "'" + fecha + "'";
        int iRes;

        try
        {
            string query = "   SELECT '01' AS TIPO_ARCHIVO,'02' AS TIPO_REG, '1000096097' AS ID_IFNB, '11123' AS IDCREDABC, CL.RFC AS IDCL_IFNB, "
                            + " PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS  IDCRED_IFNB, NOMBREC(NULL,NULL,NULL,'N',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOM_CLIENTE, "
                            + " CO.NOMBRE SUCURSAL, "
                               + "  CASE WHEN CL.CDGTIPOPERS = '001' OR CL.CDGTIPOPERS = '002' OR CL.CDGTIPOPERS = '004'  "
                                    + " THEN '01' "
                               + "  WHEN CL.CDGTIPOPERS = '003' "
                                    + " THEN '02' "
                              + "  ELSE  '' END TIPO_PERSONA, "
                               + "  '98' PROD_IFNB, "
                            + " CASE WHEN EXISTS (SELECT CDGCL FROM PRC_FONDEO_FINAL  PFF "
                                                + " WHERE PFF.CDGEM=PF.CDGEM "
                                                + " AND PFF.CDGORF = PF.CDGORF "
                                                + " AND PFF.CDGNS = PF.CDGNS "
                                                + " AND PFF.CICLO = PF.CICLO "
                                                + " AND PFF.CDGCL = PF.CDGCL "
                                                + " AND PFF.FREPSDO = " + fechaMesAnt + ") "
                                 + " THEN '03' "
                                 + " ELSE '01' "
                           + "  END TIPO_MOV, '01' REC_UTILI, "
                               + "  TO_NUMBER( PRN.INICIO  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) INICIO, "
                               + "  TO_NUMBER(  DECODE(nvl(PRN.periodicidad,''), "
                               + "  'S', PRN.inicio + (7 * nvl(PRN.plazo,0)),  "
                               + "  'Q', PRN.inicio + (15 * nvl(PRN.plazo,0)),  "
                               + "  'C', PRN.inicio + (14 * nvl(PRN.plazo,0)),  "
                               + "  'M', PRN.inicio + (30 * nvl(PRN.plazo,0)),  "
                               + "  '', '')  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECFIN, "
                               + "  CASE WHEN CD.DIAS_MORA > 0 THEN "
                               + "  NVL( ( PF.FREPSDO - CD.DIAS_MORA) - TO_DATE('30-12-1899', 'DD-MM-YYYY'),0) "
                               + "  ELSE 0 END FEC_INCUMP, "
                            + " CD.DIAS_MORA AS NUM_DIAS_INCUMP, "
                            + " DECODE(nvl(PRN.periodicidad,''), 'S','01','Q','02','C','02','M','03','','') PERIODICIDAD, "
                            + " PRC.CANTENTRE , '01' TIPO_TASA, PRN.TASA/100 TASA , "
                               + "  DECODE(nvl(PRN.periodicidad,''),  "
                               + "  'S', PRN.PLAZO / 4, "
                               + "  'Q', PRN.PLAZO / 2,  "
                               + "  'C', PRN.PLAZO /2,   "
                               + "  'M', PRN.PLAZO ,   "
                               + "  '', '') PLAZO , "
                           + " PRN.PLAZO NUM_CUOTAS,  FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA)  CUOTAS_PAGADAS, "
                            + " DECODE(nvl(PRN.periodicidad,''),  "
                               + "  'S', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 7 ) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  'Q', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 15) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  'C', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 14) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + " 'M', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 30) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  '', '') CUOTAS_VENCIDAS, "
                             + " ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL - CD.MORA_CAPITAL)),2) SALDO_CAP_VIG, "
                             + "  ROUND(( (PF.CANTIDAD/CD.MONTO_ENTREGADO)* ( CD.MORA_CAPITAL) ),2) CAP_VENCIDO, "


                             + " CASE WHEN (SELECT TCD.DIAS_MORA FROM TBL_CIERRE_DIA TCD WHERE TCD.CDGEM = PRN.CDGEM AND TCD.CDGCLNS = PRN.CDGNS AND TCD.CICLO = PRN.CICLO AND TCD.FECHA_CALC = " + fechaFin + ") = 0 THEN "
                                            + " CASE WHEN ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA') "
                                                       + "  -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + ")) <0  "
                                           + "  THEN 0 "
                                           + "  ELSE "
                                                + " ROUND((PRC.CANTENTRE/PRN.CANTENTRE)*((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA') "
                                                + " -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + ")),2) "
                                            + " END "
                            + " ELSE "
                                            + " CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                   + "  AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                                   + "  AND " + fechaFin + " AND DD.ESTATUS <> 'CA'),0) <= 0 "
                                                + "  THEN 0 "
                                           + "  ELSE "
                                             + " ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ( SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                    + " AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                                    + " AND " + fechaFin + " AND DD. ESTATUS <> 'CA')),2) "
                                           + "  END "
                            + " END SDO_INT_VIGENTE , "


                             + " CASE WHEN (SELECT TCD.DIAS_MORA FROM TBL_CIERRE_DIA TCD WHERE TCD.CDGEM = PRN.CDGEM AND TCD.CDGCLNS = PRN.CDGNS AND TCD.CICLO = PRN.CICLO AND TCD.FECHA_CALC = " + fechaFin + ") = 0  "
                                + " THEN 0 "
                                + " ELSE "
                                            + " CASE WHEN NVL((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                + " AND DD. FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + " - PRN.INICIO)/ 7 )) + 1) "
                                                               + " AND " + fechaFin + " AND DD.ESTATUS <> 'CA'),0) <= 0 "
                                             + " THEN  "
                                               + "  ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA')  "
                                               + "  -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + "))) ,2) "
                                            + " ELSE "
                                                + " (ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ((SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO AND DD.FECHA_CALC <= " + fechaFin + " AND DD.ESTATUS <> 'CA')  "
                                                + " -(SELECT SUM(MP.PAGADOINT) FROM MP WHERE MP.CDGEM = PRN.CDGEM AND MP.CDGCLNS = PRN.CDGNS AND MP.CICLO = PRN.CICLO AND MP.TIPO <> 'IN' AND MP.FREALDEP <= " + fechaFin + "))) ,2) "
                                                   + "  - "
                                             + " ROUND(((PRC.CANTENTRE /PRN.CANTENTRE)* ( SELECT SUM(DD.DEV_DIARIO) FROM DEVENGO_DIARIO DD WHERE DD.CDGEM = PRN.CDGEM AND DD.CDGCLNS = PRN.CDGNS AND DD.CICLO = PRN.CICLO "
                                                                   + "  AND DD.FECHA_CALC BETWEEN (FNFECHAPROXPAGO(PRN.INICIO, PRN.PERIODICIDAD,FLOOR( (" + fechaFin + "- PRN.INICIO)/ 7 )) + 1) "
                                                                   + "  AND " + fechaFin + " AND DD.ESTATUS <> 'CA')),2)) "
                                            + " END "
                              + "  END SDO_INT_VENCIDO, "

                            + "  (SELECT CALIFICACION FROM CAT_CALIF_CL_ABC WHERE CD.DIAS_MORA BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_INICIAL, "
                            + " TO_NUMBER(TRUNC(SYSDATE)- TO_DATE('30-12-1899', 'DD-MM-YYYY') ) FREGISTRO_PORT, "
                            + " TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECMOV "
                            + " FROM PRC_FONDEO PF "
                            + " INNER JOIN  CL ON  "
                                + " CL.CDGEM = PF.CDGEM "
                                + " AND CL.CODIGO = PF.CDGCL "
                            + " INNER JOIN  PRN ON "
                                + " PRN.CDGEM = PF.CDGEM "
                                + " AND PRN.CDGNS = PF.CDGNS "
                                + " AND PRN.CICLO = PF.CICLO "
                            + " INNER JOIN PRC ON "
                                + " PRC .CDGEM = PF.CDGEM "
                                + " AND PRC.CDGNS = PF.CDGNS "
                                + " AND PRC.CDGCL = PF.CDGCL "
                                + " AND PRC.CICLO = PF.CICLO   "
                            + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                + " CD.CDGEM = PF.CDGEM "
                                + " AND CD.CDGCLNS = PF.CDGNS "
                                + " AND CD.CICLO = PF.CICLO "
                                + " AND CD.FECHA_CALC = PF.FREPSDO "
                            + " INNER JOIN CO ON "
                                + " CO.CDGEM = PF.CDGEM "
                                + " AND CO.CODIGO = PRN.CDGCO "
                            + " WHERE PF.CDGEM = '" + empresa + "' "
                            + " AND PF.CDGORF = '0005' "
                            + " AND PF.FREPSDO = " + fechaFin

                      + " UNION " // UNION CON DATOS ELIMINADOS (TABLA: BITACORA_ELIMINACION_DATOS)

                      + "  SELECT '01' AS TIPO_ARCHIVO,'02' AS TIPO_REG, '1000096097' AS ID_IFNB, '11123' AS IDCREDABC, CL.RFC AS IDCL_IFNB, "
                            + "  PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS  IDCRED_IFNB, NOMBREC(NULL,NULL,NULL,'A',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOM_CLIENTE, "
                            + "  CO.NOMBRE SUCURSAL, "
                               + "  CASE WHEN CL.CDGTIPOPERS = '001' OR CL.CDGTIPOPERS = '002' OR CL.CDGTIPOPERS = '004'  "
                                    + " THEN '01' "
                               + "  WHEN CL.CDGTIPOPERS = '003' "
                                    + " THEN '02' "
                              + "  ELSE  '' END TIPO_PERSONA, "
                               + "  '98' PROD_IFNB, "
                               + "  '02' TIPO_MOV, '01' REC_UTILI, "
                               + "  TO_NUMBER( PRN.INICIO  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) INICIO, "
                               + "  TO_NUMBER(  DECODE(nvl(PRN.periodicidad,''), "
                               + "  'S', PRN.inicio + (7 * nvl(PRN.plazo,0)),  "
                               + "  'Q', PRN.inicio + (15 * nvl(PRN.plazo,0)),  "
                               + "  'C', PRN.inicio + (14 * nvl(PRN.plazo,0)),  "
                               + "  'M', PRN.inicio + (30 * nvl(PRN.plazo,0)),  "
                               + "  '', '')  - TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECFIN, "
                                + " (SELECT NVL(TRUNC(MIN(FECHA_CALC)) - TO_DATE('30-12-1899', 'DD-MM-YYYY'),0) "
                                + "  FROM TBL_CIERRE_DIA "
                                + " WHERE  CDGEM = PF.CDGEM "
                                + " AND CDGCLNS = PF.CDGNS "
                                + " AND CLNS = PF.CLNS "
                                + " AND CICLO = PF.CICLO "
                                + " AND FECHA_CALC <= PF.FREPSDO "
                                + " AND MORA_TOTAL > 0) FEC_INCUMP, "
                            + " CD.DIAS_MORA AS NUM_DIAS_INCUMP,  "
                            + " DECODE(nvl(PRN.periodicidad,''), 'S','01','Q','02','C','02','M','03','','') PERIODICIDAD, "
                            + " PRC.CANTENTRE , '01' TIPO_TASA, PRN.TASA/100 TASA , "
                               + "  DECODE(nvl(PRN.periodicidad,''),  "
                               + "  'S', PRN.PLAZO / 4, "
                               + "  'Q', PRN.PLAZO / 2,  "
                               + "  'C', PRN.PLAZO /2,   "
                               + "  'M', PRN.PLAZO ,   "
                               + "  '', '') PLAZO , "
                            + " PRN.PLAZO NUM_CUOTAS,  FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA)  CUOTAS_PAGADAS, "
                            + " DECODE(nvl(PRN.periodicidad,''),  "
                               + "  'S', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 7 ) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  'Q', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 15) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  'C', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 14) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                              + " 'M', FLOOR( (CD.FECHA_CALC- CD.INICIO)/ 30) - FLOOR((CD.CAPITAL_PAGADO + CD.INTERES_PAGADO) / CD.MONTO_CUOTA), "
                               + "  '', '') CUOTAS_VENCIDAS, "
                             + " 0 SALDO_CAP_VIG, "
                             + " 0 CAP_VENCIDO, "
                             + " 0 SDO_INT_VIGENTE , "
                             + " 0 SDO_INT_VENCIDO, "
                            + "  (SELECT CALIFICACION FROM CAT_CALIF_CL_ABC WHERE CD.DIAS_MORA BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_INICIAL, "
                            + " TO_NUMBER(TRUNC(SYSDATE)- TO_DATE('30-12-1899', 'DD-MM-YYYY') ) FREGISTRO_PORT, "
                            + " TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECMOV "
                            + " FROM BITACORA_ELIMINACION BE "
                            + " INNER JOIN  BITACORA_ELIMINACION_DATOS PF ON "
                                + " PF.CDGEM = BE.CDGEM "
                                + " AND PF.CDGORF = '0005' "
                                + " AND PF.ESTATUS = 'PROCESADO' "
                                + " AND PF.CDGBITELI  = BE.CODIGO "
                            + " INNER JOIN  CL ON  "
                                + " CL.CDGEM = PF.CDGEM "
                                + " AND CL.CODIGO = PF.CDGCL "
                            + " INNER JOIN  PRN ON "
                                + " PRN.CDGEM = PF.CDGEM "
                                + " AND PRN.CDGNS = PF.CDGNS "
                                + " AND PRN.CICLO = PF.CICLO "
                            + " INNER JOIN PRC ON "
                                + " PRC .CDGEM = PF.CDGEM "
                                + " AND PRC.CDGNS = PF.CDGNS "
                                + " AND PRC.CDGCL = PF.CDGCL "
                                + " AND PRC.CICLO = PF.CICLO   "
                            + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                + " CD.CDGEM = PF.CDGEM "
                                + " AND CD.CDGCLNS = PF.CDGNS "
                                + " AND CD.CICLO = PF.CICLO "
                                + " AND CD.FECHA_CALC = PF.FREPSDO "
                            + " INNER JOIN CO ON "
                                + " CO.CDGEM = PF.CDGEM "
                                + " AND CO.CODIGO = PRN.CDGCO "
                            + " WHERE BE.CDGEM = '" + empresa + "' "
                            + " AND PF.CDGORF = '0005' "
                            + " AND BE.FELIMINA = " + fechaFin
                            + " AND BE.DESCRIPCION IN ('ELIMINA_FONDEO_ABC_AUTO_L','ELIMINA_FONDEO_ABC_AUTO') "
                            + " ORDER BY TIPO_MOV ";


            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }*/

    //REPORTES DE ABC, REPORTE DE DETALLE DE MOVIMIENTOS DE CALIFICACION
    [WebMethod]
    public string getRepDetalleCalificacion(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = " SELECT '02' AS TIPO_ARCHIVO " +
                "                 , '02' AS TIPO_REG " +
                "                 , '1000096097' AS ID_IFNB " +
                "                 , '11123' AS IDCREDABC " +
                "                 , CL.RFC AS IDCL_IFNB " +
                "                 , PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS IDCRED_IFNB " +
                "                 , '05' TIPO_MOV " +
                "                 , (SELECT CALIFICACION  " +
                "                      FROM CAT_CALIF_CL_ABC  " +
                "                     WHERE (CASE WHEN (SELECT COUNT(*)  " +
                "                                         FROM TBL_DIAS_MORA " +
                "                                        WHERE CDGEM = CD.CDGEM " +
                "                                          AND CDGCLNS = CD.CDGCLNS " +
                "                                          AND CICLO = CD.CICLO " +
                "                                          AND CLNS = CD.CLNS " +
                "                                          AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                      FROM TBL_DIAS_MORA " +
                "                                                                                     WHERE CDGEM = CD.CDGEM " +
                "                                                                                       AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                       AND CICLO = CD.CICLO " +
                "                                                                                       AND CLNS = CD.CLNS " +
                "                                                                                       AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END) BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_DEUDOR " +
                "                 , (SELECT CALIFICACION  " +
                "                      FROM CAT_CALIF_CL_ABC  " +
                "                     WHERE (CASE WHEN (SELECT COUNT(*)  " +
                "                                         FROM TBL_DIAS_MORA " +
                "                                        WHERE CDGEM = CD.CDGEM " +
                "                                          AND CDGCLNS = CD.CDGCLNS " +
                "                                          AND CICLO = CD.CICLO " +
                "                                          AND CLNS = CD.CLNS " +
                "                                          AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                                      FROM TBL_DIAS_MORA " +
                "                                                                                     WHERE CDGEM = CD.CDGEM " +
                "                                                                                       AND CDGCLNS = CD.CDGCLNS " +
                "                                                                                       AND CICLO = CD.CICLO " +
                "                                                                                       AND CLNS = CD.CLNS " +
                "                                                                                       AND FECHA_CALC = CD.FECHA_CALC) " +
                "                                   ELSE CD.DIAS_MORA " +
                "                                    END) BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_OPERAC " +
                "                 , ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) SDO_BASE_CALIF " +
                "                 , ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  SDO_CUBIERTO " +
                "                 , (ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) - ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  ) SDO_DESCUB " +
                "                 , ES.EPRCPRNFM RES_CUBIERTA " +
                "                 , 1 - ES.EPRCPRNFM RES_DESCUBIERTA " +
                "                 , ES.EPRCACUM IMP_RES_CUB " +
                "                 , 0 AS IMP_RES_DESC " +
                "                 , ES.EPRCACUM TOT_RES " +
                "                 , TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECHA_CORTE " +
                "              FROM PRC_FONDEO PF " +
                "              JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM AND CD.CDGCLNS = PF.CDGNS AND CD.CICLO = PF.CICLO AND CD.FECHA_CALC = PF.FREPSDO " +
                "              JOIN PRC ON PRC.CDGEM=PF.CDGEM AND PRC.CDGNS = PF.CDGNS AND PRC.CDGCL = PF.CDGCL AND PRC.CICLO = PF.CICLO " +
                "              JOIN CL ON CL.CDGEM = PF.CDGEM AND CL.CODIGO = PF.CDGCL " +
                "              JOIN ESTIMACION ES ON ES.CDGEM = PF.CDGEM AND ES.CDGCLNS = PF.CDGCLNS AND ES.CDGCL = PF.CDGCL AND ES.CICLO = PF.CICLO " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '0005' " +
                "               AND PF.FREPSDO = LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY')) ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    /*//REPORTES DE ABC, REPORTE DE DETALLE DE MOVIMIENTOS DE CALIFICACION
    [WebMethod]
    public string getRepDetalleCalificacion(string fecha)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaFin = "LAST_DAY(TO_DATE('" + fecha + "', 'DD-MM-YYYY'))";
        fecha = "'" + fecha + "'";
        int iRes;

        try
        {
            string query = " SELECT '02' AS TIPO_ARCHIVO,'02' AS TIPO_REG, '1000096097' AS ID_IFNB,  "
                             + " '11123' AS IDCREDABC, CL.RFC AS IDCL_IFNB, "
                             + " PRC.CDGNS ||PRC.CICLO ||PRC.CDGCL AS  IDCRED_IFNB, '05' TIPO_MOV, "
                             + " (SELECT CALIFICACION FROM CAT_CALIF_CL_ABC WHERE CD.DIAS_MORA BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_DEUDOR, "
                             + " (SELECT CALIFICACION FROM CAT_CALIF_CL_ABC WHERE CD.DIAS_MORA BETWEEN ATRASOMIN AND ATRASOMAX) CALIF_OPERAC, "
                             + " ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) SDO_BASE_CALIF, "
                             + " ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  SDO_CUBIERTO, "
                             + " (ROUND(((PF.CANTIDAD/CD.MONTO_ENTREGADO)* (CD.SDO_CAPITAL)),2) - "
                             + "  ROUND(CD.SALDO_GL*( PRC.CANTENTRE / CD.MONTO_ENTREGADO),2)  ) SDO_DESCUB, "
                             + " ES.EPRCPRNFM RES_CUBIERTA, 1 - ES.EPRCPRNFM RES_DESCUBIERTA, ES.EPRCACUM IMP_RES_CUB, "
                             + " 0 AS IMP_RES_DESC, ES.EPRCACUM TOT_RES, "
                             + " TO_NUMBER(PF.FREPSDO- TO_DATE('30-12-1899', 'DD-MM-YYYY')) FECHA_CORTE "
                             + " FROM PRC_FONDEO PF "
                             + " INNER JOIN TBL_CIERRE_DIA CD ON "
                                + " CD.CDGEM = PF.CDGEM "
                                + " AND CD.CDGCLNS = PF.CDGNS "
                                + " AND CD.CICLO = PF.CICLO "
                                + " AND CD.FECHA_CALC = PF.FREPSDO "
                              + " INNER JOIN PRC ON "
                               + " PRC.CDGEM=PF.CDGEM "
                               + " AND PRC.CDGNS = PF.CDGNS "
                               + " AND PRC.CDGCL = PF.CDGCL "
                               + " AND PRC.CICLO = PF.CICLO "
                             + " INNER JOIN  CL ON  "
                              + " CL.CDGEM = PF.CDGEM  "
                              + " AND CL.CODIGO = PF.CDGCL  "
                             + " INNER JOIN ESTIMACION ES ON "
                                + " ES.CDGEM = PF.CDGEM "
                                + " AND ES.CDGCLNS = PF.CDGCLNS "
                                + " AND ES.CDGCL = PF.CDGCL "
                                + " AND ES.CICLO = PF.CICLO "
                             + " WHERE  "
                             + " PF.CDGEM='" + empresa + "' "
                             + " AND PF.CDGORF = '0005' "
                            + " AND PF.FREPSDO = " + fechaFin;


            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }*/

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE DETALLE DE CLIENTES CAPTURADOS MENSUAL
    [WebMethod]
    public string getRepDetalleClientesNuevos(string fecIni, string fecFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            string query = "SELECT RG.CODIGO CODIGO_REGION, RG.NOMBRE REGION, CO.CODIGO CODIGO_SUCURSAL , CO.NOMBRE SUCURSAL, PRN.CDGNS GRUPO, NS.NOMBRE NOM_GRUPO, PRN.CICLO, PRC.CDGCL ACREDITADO, (SELECT NOMBREC(CL.CDGEM, CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = PRC.CDGEM AND CODIGO = PRC.CDGCL) NOM_ACREDITADO " +
                           "FROM PRC, PRN, NS, CO, RG " +
                           "WHERE PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRN.CDGEM = '" + cdgEmpresa + "' " +
                           "AND PRC.CLNS = 'G' " +
                           "AND PRN.SITUACION IN ('L','E') " +
                           "AND PRC.SITUACION IN ('L','E') " +
                           "AND PRN.INICIO BETWEEN  '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND NS.CDGEM = PRN.CDGEM " +
                           "AND NS.CODIGO = PRN.CDGNS " +
                           "AND CO.CDGEM = PRN.CDGEM " +
                           "AND CO.CODIGO = PRN.CDGCO " +
                           "AND RG.CDGEM = CO.CDGEM " +
                           "AND RG.CODIGO = CO.CDGRG " +
                           "AND PRC.CDGCL || PRN.INICIO IN " +
                           "(SELECT PRC.CDGCL || MIN(PRN.INICIO) " +
                           "FROM PRN, PRC " +
                           "WHERE PRN.CDGEM = PRC.CDGEM " +
                           "AND PRN.CDGNS = PRC.CDGNS " +
                           "AND PRN.CICLO = PRC.CICLO " +
                           "AND PRN.CDGEM = '" + cdgEmpresa + "' " +
                           "AND PRC.CLNS = 'G' " +
                           "AND PRN.SITUACION IN ('L','E') " +
                           "AND PRC.SITUACION IN ('L','E') " +
                           "AND PRN.INICIO BETWEEN  '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND PRC.CDGCL IN (SELECT CODIGO FROM CL WHERE CDGEM = '" + cdgEmpresa + "' AND ALTA BETWEEN '" + fecIni + "' AND '" + fecFin + "') " +
                           "GROUP BY CDGCL)";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //OBTIENE EL REPORTE DE DEVOLUCION DE PRESTAMOS
    [WebMethod]
    public string getRepDevPrn(string tipo, string fechaIni, string fechaFin, string usuario)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string query = string.Empty;
        string xml = "";

        //DEVOLUCION DE PRESTAMO GRUPAL
        if (tipo == "1")
        {
            query = "SELECT DISTINCT TO_CHAR(PRC.ENTREGA, 'DD/MM/YYYY') FECHA, " +
                    "PRN.CDGCO, " +
                    "CO.NOMBRE SUCURSAL, " +
                    "PRN.CDGNS, " +
                    "NS.NOMBRE GRUPO, " +
                    "PRN.CICLO, " +
                    "TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO, " +
                    "(SELECT SUM(CANTAUTOR) " +
                    "FROM PRC " +
                    "WHERE CDGEM = PRN.CDGEM " +
                    "AND CDGNS = PRN.CDGNS " +
                    "AND CICLO = PRN.CICLO " +
                    "AND TRUNC(ENTREGA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                    "AND SITUACION = 'D') DEVOLUCION, " + 
                    "(SELECT SUM(ABS(DEV_DIARIO)) " +
                    "FROM DEVENGO_DIARIO " +
                    "WHERE CDGEM = PRC.CDGEM " +
                    "AND CDGCLNS = PRC.CDGNS " +
                    "AND CLNS = PRC.CLNS " +
                    "AND (CICLO = PRC.CICLO OR CICLO = PRN.CICLOD) " +
                    "AND FECHA_CALC = TRUNC(PRC.ENTREGA) " +
                    "AND ESTATUS = 'DE') DEV_CANCELADO " +
                    "FROM PRC, PRN, CO, NS " +
                    "WHERE PRC.CDGEM = '" + empresa + "' " +
                    "AND TRUNC(PRC.ENTREGA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                    "AND PRC.SITUACION = 'D' " +
                    "AND PRN.CDGEM = PRC.CDGEM " +
                    "AND PRN.CDGNS = PRC.CDGNS " +
                    "AND PRN.CICLO = PRC.CICLO " +
                    "AND CO.CDGEM = PRN.CDGEM " +
                    "AND CO.CODIGO = PRN.CDGCO " +
                    "AND NS.CDGEM = PRN.CDGEM " +
                    "AND NS.CODIGO = PRN.CDGNS " +
                    "ORDER BY FECHA";
        }
        //DEVOLUCION DE PRESTAMO INDIVIDUAL
        else if (tipo == "2")
        {
            query = "SELECT DISTINCT TO_CHAR(PRC.ENTREGA, 'DD/MM/YYYY') FECHA, " +
                    "PRN.CDGCO, " +
                    "CO.NOMBRE SUCURSAL, " +
                    "PRN.CDGNS, " +
                    "NS.NOMBRE GRUPO, " +
                    "PRN.CICLO, " +
                    "TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO, " +
                    "PRC.CDGCL, " +
                    "NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') CLIENTE, " +
                    "PRC.CANTAUTOR CANTIDAD, " +
                    "(SELECT NOCHEQUE FROM CHEQUE_CANCELADO WHERE CDGEM = PRC.CDGEM AND CDGCLNS = PRC.CDGNS AND (CICLO = PRC.CICLO OR CICLO = PRC.CICLOD) AND CDGCL = PRC.CDGCL AND FCANCELA = TRUNC(PRC.ENTREGA) AND TCANC = 'CD') CHEQUE " +
                    "FROM PRC, PRN, CO, NS, CL " +
                    "WHERE PRC.CDGEM = '" + empresa + "' " +
                    "AND TRUNC(PRC.ENTREGA) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                    "AND PRC.SITUACION = 'D' " +
                    "AND PRN.CDGEM = PRC.CDGEM " +
                    "AND PRN.CDGNS = PRC.CDGNS " +
                    "AND PRN.CICLO = PRC.CICLO " +
                    "AND CO.CDGEM = PRN.CDGEM " +
                    "AND CO.CODIGO = PRN.CDGCO " +
                    "AND NS.CDGEM = PRN.CDGEM " +
                    "AND NS.CODIGO = PRN.CDGNS " +
                    "AND CL.CDGEM = PRC.CDGEM " +
                    "AND CL.CODIGO = PRC.CDGCL " +
                    "ORDER BY FECHA";
        }

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE DIAGNOSTICOS
    [WebMethod]
    public string getRepDiagnostico(string mes, string anio)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaInicio = "01/" + mes + "/" + anio;
        string fechaFin = "LAST_DAY(TO_DATE('" + fechaInicio + "', 'DD/MM/YYYY'))";
        int iRes;

        try
        {
            string query = "SELECT CO.NOMBRE SUCURSAL" +
                "                , NOMBREC(NULL,NULL,'I','N',D.NOMBRE1,D.NOMBRE2,D.PRIMAPE,D.SEGAPE) ACREDITADO " +
                "                , D.CDGCLNS " +
                "                , D.CICLO " +
                "                , TO_CHAR( D.FDIAGNOSTICO, 'MONTH' ) MESDIAG " +
                "                , TO_CHAR( D.FREGISTRO, 'MONTH' ) MESREP " +
                "                , D.EDAD " +
                "                , CA.DESCRIPCION TIPOCANCER " +
                "                , CASE WHEN D.PAGADO = 'S' THEN 'SI' ELSE 'NO' END PAGADO " +
                "                , CASE WHEN D.DOCORIG = 'S' THEN 'SI' ELSE 'NO' END DOCRECIB " +
                "                , CASE WHEN DB.ESTATUS = 'V' THEN 'VIGENTE' ELSE 'CANCELADO' END ESTATUS" +
                "                , NOMBREC(NULL,NULL,'I','N',DB.NOMBRE1,DB.NOMBRE2,DB.PRIMAPE,DB.SEGAPE) BENEFICIARIO " +
                "                , CAS.NOMBRE ASEGURADORA " +
                "             FROM DIAGNOSTICO D " +
                "             JOIN DIAGNOSTICO_BENEFICIARIO DB ON D.CDGEM = DB.CDGEM AND D.CODIGO = DB.CDGDIAG " +
                "             JOIN CO ON D.CDGEM = CO.CDGEM AND D.CDGCO = CO.CODIGO " +
                "             JOIN CAT_TIPO_DIAG CA ON D.CDGTDIAG = CA.CODIGO " +
                "             JOIN MICROSEGURO M ON D.CDGEM = M.CDGEM AND D.CDGCL = M.CDGCL AND D.CDGPMS = M.CDGPMS AND D.INICIOPMS = M.INICIO " +
                "             JOIN CAT_ASEGURADORA CAS ON M.CDGEM = CAS.CDGEM AND M.CDGASE = CAS.CODIGO " +
                "            WHERE D.CDGEM = '" + empresa + "' " +
                "              AND D.FREGISTRO BETWEEN '" + fechaInicio + "' AND " + fechaFin;

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE TRANSFERENCIAS
    [WebMethod]
    public string getRepDispersion(string fecIni, string fecFin, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            string query = "SELECT D.CDGCLNS CODGRUPO " +
                           ",(SELECT NOMBRE FROM NS WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGNS) GRUPO " +
                           ",PRN.CICLO " +
                           ",TO_CHAR(FECHA, 'DD/MM/YYYY') FINICIO " +
                           ",DECODE(D.FORMAENTREGA, 'T', 'INDIVIDUAL', 'D', 'GRUPAL TESORERO') TIPO " +
                           ",PRN.CDGCO  " +
                           ",(SELECT NOMBRE FROM CO WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGCO) NOMCO " +
                           ",D.CDGCL " +
                           ",(SELECT NOMBREC(CDGEM, CODIGO, 'I', 'A', NULL, NULL, NULL, NULL) FROM CL WHERE CDGEM = D.CDGEM AND CODIGO = D.CDGCL) CLIENTE " +
                           ",CANTIDAD " +
                           ",NVL(LPAD(TO_CHAR((SELECT COUNT(*) FROM PRC WHERE CDGEM = A.CDGEM AND CDGCL = A.CDGCL AND SITUACION IN ('E', 'L') AND CANTENTRE > 0 AND TRUNC(SOLICITUD) < TRUNC(A.SOLICITUD) GROUP BY CDGCL) + 1), 2, '0'), '01') CICLO_ACRED " +
                           ",(SELECT NOMBRE FROM BANCO WHERE CODIGO = D.CDGBANCO) BANCO " +
                           ",D.CLABE " +
                           ",D.NOCTABANCO CUENTA " +
                           ",PRN.CDGOCPE " +
                           ",(SELECT NOMBREC(NULL, NULL, 'I', 'A', NOMBRE1, NOMBRE2, PRIMAPE, SEGAPE) FROM PE WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGOCPE) NOMOCPE " +
                           "FROM DISPERSION D, PRN, PRC A " +
                           "WHERE D.CDGEM = '" + empresa + "' " +
                           "AND D.FECHA BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                           "AND D.BAJA IS NULL " +
                           "AND PRN.CDGEM = D.CDGEM " +
                           "AND PRN.CDGNS = D.CDGCLNS " +
                           "AND PRN.CICLO = D.CICLO " +
                           "AND A.CDGEM = D.CDGEM " +
                           "AND A.CDGNS = D.CDGCLNS " + 
                           "AND A.CICLO = D.CICLO " +
                           "AND A.CDGCL = D.CDGCL " +
                           "AND A.CLNS = 'G' " +
                           "ORDER BY FINICIO, D.CDGCLNS";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["CODGRUPO"] = "-- TOTAL --";
                dtot["CLIENTE"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(CLIENTE)", ""));
                dtot["CANTIDAD"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CANTIDAD)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }

            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA INFORMACION PARA EL REPORTE DE ENANOS
    [WebMethod]
    public string getRepEnanos(string fecha, string fechaFin, string region, string sucursal, string asesor)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        int iRes;
        string xml = "";
        string queryAse = string.Empty;
        string querySuc = string.Empty;
        string queryReg = string.Empty;

        if (asesor != null && asesor != string.Empty)
        {
            queryAse = " AND SN.CDGOCPE = '" + asesor + "' ";
        }

        if (region != null && region != string.Empty)
        {
            queryReg = " AND RG.CODIGO = '" + region + "' ";
        }

        if (sucursal != null && sucursal != string.Empty)
        {
            querySuc = " AND CO.CODIGO = '" + sucursal + "' ";
        }

        string query = "SELECT NOMBREC(SC.CDGEM, SC.CDGCL, 'I', 'A', NULL, NULL, NULL, NULL) CLIENTE,"
                     + " SC.CDGCL, NS.NOMBRE NOM_GRUPO, SC.CDGNS, SC.CICLO, "
                     + " TO_CHAR(SC.INICIO,'DD/MM/YYYY') INICIO, TO_CHAR( SC.CANTAUTOR, '999,999,999.99') CANTAUTOR, CM.ALTA,  "
                     + " NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR, "
                     + " PE.TELEFONO NUM_NOMINA, CO.NOMBRE SUCURSAL, RG.NOMBRE ZONA "
                     + " FROM CL_MARCA CM "
                     + " INNER JOIN SC "
                        + " ON  SC.CDGEM = CM.CDGEM "
                        + " AND SC.CDGCL = CM.CDGCL "
                        + " AND SC.SOLICITUD = (SELECT MAX(SOLICITUD) FROM SC WHERE CDGEM = CM.CDGEM  "
                                            + " AND TRUNC(SOLICITUD) <= CM.ALTA AND CDGCL = CM.CDGCL ) "
                     + " INNER JOIN SN  "
                        + " ON  SN.CDGEM = SC.CDGEM "
                        + " AND SN.CDGNS = SC.CDGNS "
                        + " AND SN.CICLO = SC.CICLO "
                     + " INNER JOIN PE "
                        + " ON  PE.CDGEM = SC.CDGEM "
                        + " AND SN.CDGOCPE = PE.CODIGO "
                        + queryAse
                     + " INNER JOIN CO "
                        + " ON  CO.CDGEM = SC.CDGEM "
                        + " AND CO.CODIGO = PE.CDGCO  "
                        + querySuc
                      + " INNER JOIN RG "
                        + " ON  RG.CDGEM = SC.CDGEM "
                        + " AND RG.CODIGO = CO.CDGRG "
                        + queryReg
                     + " INNER JOIN NS  "
                        + " ON  NS.CDGEM = SC.CDGEM "
                        + " AND NS.CODIGO = SN.CDGNS "
                     + " WHERE  "
                     + " CM.CDGEM='" + empresa + "' "
                     + " AND CM.TIPOMARCA ='EN' "
                     + " AND CM.ALTA BETWEEN '" + fecha + "' AND '" + fechaFin + "'"
                     + " ORDER BY RG.NOMBRE, CO.NOMBRE , CLIENTE ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE GENERA LA INFORMACIÓN DEL REPORTE DE ESTADOS DE CUENTA
    [WebMethod]
    public string getRepEstadosdeCuenta(string fechai, string fechaf, string cdgib, string cdgcb)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string query = " ";
        string xml = "";
        int iRes;

        try
        {
            if (cdgib == "00") //BANORTE
            {
                query = " SELECT TO_CHAR(ECB.FOPERACION, 'DD/MM/YYYY') FOPERACION " +
                    "          , TO_CHAR(ECB.FAPLICACION, 'DD/MM/YYYY') FECHA " +
                    "          , ECB.REFERENCIA " +
                    "          , ECB.DESCRIPCION " +
                    "          , ECB.CODTRANSAC " +
                    "          , ECB.SUCURSAL " +
                    "          , ECB.DEPOSITO " +
                    "          , ECB.RETIRO " +
                    "          , ECB.SALDO " +
                    "          , ECB.MOVTO " +
                    "          , ECB.DESDDETALLADA " +
                    "          , ECB.REFERENCIA2  " +
                    "          , ECB.IMPORTE " +
                    "          , ECB.TIPO " +
                    "          , ECB.COMENTARIO " +
                    "       FROM EDOCTABCOS ECB,CB,IB " +
                    "      WHERE ECB.CDGEM = IB.CDGEM " +
                    "        AND ECB.CDGIB = IB.CODIGO " +
                    "        AND ECB.CDGEM = CB.CDGEM " +
                    "        AND ECB.CDGIB = CB.CDGIB " +
                    "        AND ECB.CDGCB = CB.CODIGO " +
                    "        AND ECB.CDGEM = '" + empresa + "' " +
                    "        AND ECB.FEDOCTA BETWEEN '" + fechai + "' AND '" + fechaf + "' " +
                    "        AND ECB.CDGIB = '" + cdgib + "' " +
                    "        AND ECB.CDGCB = '" + cdgcb + "' " +
                    "        ORDER BY ECB.FEDOCTA, CONSECUTIVO ";
            }
            else if (cdgib == "01") //BANAMEX
            {
                query = " SELECT TO_CHAR(ECB.FOPERACION, 'DD/MM/YYYY') FOPERACION " +
                    "          , TO_CHAR(ECB.FAPLICACION , 'DD/MM/YYYY') FECHA " +
                    "          , ECB.REFERENCIA " +
                    "          , ECB.DESCRIPCION " +
                    "          , ECB.CODTRANSAC " +
                    "          , ECB.SUCURSAL " +
                    "          , ECB.DEPOSITO " +
                    "          , ECB.RETIRO " +
                    "          , ECB.SALDO " +
                    "          , ECB.MOVTO " +
                    "          , ECB.DESDDETALLADA " +
                    "          , ECB.REFERENCIA2 " +
                    "          , ECB.IMPORTE " +
                    "          , ECB.TIPO " +
                    "          , ECB.COMENTARIO " +
                    "       FROM EDOCTABCOS ECB,CB,IB " +
                    "      WHERE ECB.CDGEM = IB.CDGEM " +
                    "        AND ECB.CDGIB = IB.CODIGO " +
                    "        AND ECB.CDGEM = CB.CDGEM " +
                    "        AND ECB.CDGIB = CB.CDGIB " +
                    "        AND ECB.CDGCB = CB.CODIGO " +
                    "        AND ECB.CDGEM = '" + empresa + "' " +
                    "        AND ECB.FEDOCTA BETWEEN '" + fechai + "' AND '" + fechaf + "' " +
                    "        AND ECB.CDGIB = '" + cdgib + "' " +
                    "        AND ECB.CDGCB = '" + cdgcb + "' " +
                    "        ORDER BY ECB.FEDOCTA, CONSECUTIVO ";
            }
            else if (cdgib == "06") //SCOTIABANK
            {
                query = " SELECT ECB.MOVTO " +
                    "          , TO_CHAR(ECB.FOPERACION, 'DD/MM/YYYY') FOPERACION " +
                    "          , TO_CHAR(ECB.FAPLICACION , 'DD/MM/YYYY') FECHA " +
                    "          , ECB.REFERENCIA " +
                    "          , ECB.IMPORTE " +
                    "          , ECB.TIPO " +
                    "          , ECB.DESCRIPCION TRANSACCION" +
                    "          , ECB.SALDO " +
                    "          , ECB.DESDDETALLADA LEYENDA1" +
                    "          , ECB.REFERENCIA2 LEYENDA2" +
                    "          , ECB.CODTRANSAC " +
                    "          , ECB.SUCURSAL " +
                    "          , ECB.DEPOSITO " +
                    "          , ECB.RETIRO " +
                    "          , ECB.COMENTARIO " +
                    "       FROM EDOCTABCOS ECB,CB,IB " +
                    "      WHERE ECB.CDGEM = IB.CDGEM " +
                    "        AND ECB.CDGIB = IB.CODIGO " +
                    "        AND ECB.CDGEM = CB.CDGEM " +
                    "        AND ECB.CDGIB = CB.CDGIB " +
                    "        AND ECB.CDGCB = CB.CODIGO " +
                    "        AND ECB.CDGEM = '" + empresa + "' " +
                    "        AND ECB.FEDOCTA BETWEEN '" + fechai + "' AND '" + fechaf + "' " +
                    "        AND ECB.CDGIB = '" + cdgib + "' " +
                    "        AND ECB.CDGCB = '" + cdgcb + "' " +
                    "        ORDER BY ECB.FEDOCTA, CONSECUTIVO ";
            }
            else if (cdgib == "07") //BANCOMER
            {
                query = " SELECT ECB.MOVTO " +
                    "          , TO_CHAR(ECB.FOPERACION, 'DD/MM/YYYY') FOPERACION " +
                    "          , TO_CHAR(ECB.FAPLICACION , 'DD/MM/YYYY') FECHA " +
                    "          , ECB.REFERENCIA " +
                    "          , ECB.DESCRIPCION " +
                    "          , ECB.CODTRANSAC " +
                    "          , ECB.SUCURSAL " +
                    "          , ECB.RETIRO CARGO  " +
                    "          , ECB.DEPOSITO ABONO " +
                    "          , ECB.SALDO" +
                    "          , ECB.MOVTO" +
                    "          , ECB.DESDDETALLADA " +
                    "          , ECB.REFERENCIA2  " +
                    "          , ECB.IMPORTE " +
                    "          , ECB.TIPO " +
                    "          , ECB.COMENTARIO " +
                    "       FROM EDOCTABCOS ECB,CB,IB " +
                    "      WHERE ECB.CDGEM = IB.CDGEM " +
                    "        AND ECB.CDGIB = IB.CODIGO " +
                    "        AND ECB.CDGEM = CB.CDGEM " +
                    "        AND ECB.CDGIB = CB.CDGIB " +
                    "        AND ECB.CDGCB = CB.CODIGO " +
                    "        AND ECB.CDGEM = '" + empresa + "' " +
                    "        AND ECB.FEDOCTA BETWEEN '" + fechai + "' AND '" + fechaf + "' " +
                    "        AND ECB.CDGIB = '" + cdgib + "' " +
                    "        AND ECB.CDGCB = '" + cdgcb + "' " +
                    "        ORDER BY ECB.FEDOCTA, CONSECUTIVO ";
            }


            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS CREDITOS APROBADOS CON EXCEPCION
    [WebMethod]
    public string getRepExcepciones(string excepcion, string grupo, string ciclo, string fecIni, string fecFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryExc = string.Empty;
        string queryGrupo = string.Empty;

        if (excepcion != "000")
            queryExc = "AND P.CDGEXC = '" + excepcion + "' ";
        if (grupo != "" && ciclo != "")
            queryGrupo = "AND P.CDGNS = '" + grupo + "' " +
                         "AND P.CICLO = '" + ciclo + "' ";

        string query = "SELECT P.*, " +
                       "CEC.DESCRIPCION EXCEPCION, " +
                       "NS.NOMBRE GRUPO, " +
                       "CO.CODIGO COORD, " +
                       "CO.NOMBRE NOMCO, " +
                       "RG.NOMBRE REGION, " +
                       "PE.CODIGO ASESOR, " +
                       "NOMBREC(NULL,NULL,NULL,'A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMPE, " +
                       "P.OBSERVACION " +
                       "FROM PRN_EXCEPCION P, CAT_EXCEPCION_CRED CEC, PRN, NS, CO, RG, PE " +
                       "WHERE P.CDGEM = '" + empresa + "' " +
                       queryExc +
                       queryGrupo +
                       "AND CEC.CODIGO = P.CDGEXC " +
                       "AND PRN.CDGEM = P.CDGEM " +
                       "AND PRN.CDGNS = P.CDGNS " +
                       "AND PRN.CICLO = P.CICLO " +
                       "AND TRUNC(PRN.FAUTCAR) BETWEEN '" + fecIni + "' AND '" + fecFin + "' " +
                       "AND NS.CDGEM = P.CDGEM " +
                       "AND NS.CODIGO = P.CDGNS " +
                       "AND CO.CDGEM = PRN.CDGEM " +
                       "AND CO.CODIGO = PRN.CDGCO " +
                       "AND RG.CDGEM = CO.CDGEM " +
                       "AND RG.CODIGO = CO.CDGRG " +
                       "AND PE.CDGEM = PRN.CDGEM " +
                       "AND PE.CODIGO = PRN.CDGOCPE";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA AQUELLOS CREDITOS AUTORIZADOS CON EXCEPCION EN SU GL
    [WebMethod]
    public string getRepExcepGL(int tipo, string fecha, string situacion)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string strInicio = string.Empty;

        //tipo = 2 Indica que debe considerarse la fecha de inicio como parametro de consulta
        if (tipo == 2)
            strInicio = "AND PRN.INICIO = '" + fecha + "' ";

        string query = "SELECT PRN.CDGNS, " +
                       "NS.NOMBRE GRUPO, " +
                       "PRN.CICLO, " +
                       "TO_CHAR(INICIO,'DD/MM/YYYY') INICIO, " +
                       "((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                              "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) MONTO, " +
                       "FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G') SALDO_GL, " +
                       "(((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                               "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) - FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G')) FALTANTE, " +
                       "PRN.CDGCO, " +
                       "CO.NOMBRE NOMCO " +
                       "FROM PRN, NS, CO " +
                       "WHERE PRN.CDGEM = '" + empresa + "' " +
                       "AND PRN.SITUACION = '" + situacion + "' " +
                       "AND (((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                                   "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) - FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G')) BETWEEN 1 AND 100 " +
                       strInicio +
                       "AND NS.CDGEM = PRN.CDGEM " +
                       "AND NS.CODIGO = PRN.CDGNS " +
                       "AND CO.CDGEM = PRN.CDGEM " +
                       "AND CO.CODIGO = PRN.CDGCO";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA EL RESULTADO DEL PROCESO DE CARGA DE ARCHIVO DE ASIGNACION DE FONDEO
    [WebMethod]
    public string getRepFondeoArchivo(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CAF.* " +
            "                , CASE WHEN CAF.ESTATUS = 'R' THEN 'REGISTRADO' " +
            "                       WHEN CAF.ESTATUS = 'E' THEN 'EN ESPERA' " +
            "                       WHEN CAF.ESTATUS = 'N' THEN 'NO REGISTRADO' " +
            "                       WHEN CAF.ESTATUS = 'X' THEN 'NO EXISTE' " +
            "                       WHEN CAF.ESTATUS = 'P' THEN 'REGISTRO REPETIDO' " +
            "                       WHEN CAF.ESTATUS = 'B' THEN 'REGISTRO EN BITACORA' " +
            "                       WHEN CAF.ESTATUS = 'F' THEN 'ASIGNADO A OTRO FONDEO' END DESCEST " +
            //"                , DECODE(CAF.ESTATUS,'R','REGISTRADO','N','NO REGISTRADO') DESCEST " +
            "                 , TO_CHAR(FREPSDO,'DD/MM/YYYY') FECSDO " +
            "              FROM CL_ASIGNA_FONDEO CAF " +
            "             WHERE CAF.CDGEM = '" + empresa + "' " +
            "               AND CAF.CDGPE = '" + usuario + "' " +
            "          ORDER BY ORDEN";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE GENERA LA INFORMACIÓN DEL REPORTE DE HISTORICO POR CHEQUE
    [WebMethod]
    public string getRepHistoricoCheque(string nocheque, string cdgib, string cdgcb)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = "SELECT * " +
                "             FROM ( " +
                "                    SELECT PRC.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, PRC.NOCHEQUE " +
                "                         , CASE WHEN PRC.SITUACION = 'E' THEN PRC.CANTENTRE ELSE PRC.CANTAUTOR END IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'DESEMBOLSO' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , PRC.CDGNS, NS.NOMBRE GRUPO, PRC.CICLO, TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "                         , PRN.CDGCO, CO.NOMBRE SUCURSAL, PRC.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE ASESOR " +
                "                      FROM CONCILIACHEQUE CC, PRC,PRN, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = PRC.CDGEM " +
                "                       AND CC.CDGCB = PRC.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(PRC.NOCHEQUE) " +
                "                       AND CC.IMPORTE = PRC.CANTENTRE " +
                "                       AND PRC.CDGEM = PRN.CDGEM " +
                "                       AND PRC.CDGNS = PRN.CDGNS " +
                "                       AND PRC.CICLO = PRN.CICLO " +
                "                       AND NS.CDGEM = PRC.CDGEM " +
                "                       AND NS.CODIGO = PRC.CDGCLNS " +
                "                       AND NS.CDGEM = CO.CDGEM " +
                "                       AND NS.CDGCO = CO.CODIGO " +
                "                       AND CL.CDGEM = PRC.CDGEM " +
                "                       AND CL.CODIGO = PRC.CDGCL " +
                "                       AND CB.CDGEM = PRC.CDGEM  " +
                "                       AND CB.CODIGO = PRC.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = NS.CDGEM " +
                "                       AND PE.CODIGO = NS.CDGACPE " +
                "                       AND CC.CDGEM = '" + empresa + "'   " +
                "                       AND CC.TIPO = 'DE' " +
                "                       AND CC.ESTATUS IN ('CI','CB') " +
                "                       AND TO_NUMBER(PRC.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " +
                "                    SELECT CAN.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, CAN.NOCHEQUE " +
                "                         , CAN.CANTIDAD IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'DESEMBOLSO' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , CAN.CDGCLNS, NS.NOMBRE GRUPO, CAN.CICLO, TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "                         , PRN.CDGCO, CO.NOMBRE SUCURSAL, CAN.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, CHEQUE_CANCELADO CAN, PRC, PRN, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = CAN.CDGEM " +
                "                       AND CC.CDGCB = CAN.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(CAN.NOCHEQUE) " +
                "                       AND CC.IMPORTE = CAN.CANTIDAD " +
                "                       AND PRC.CDGEM = PRN.CDGEM " +
                "                       AND PRC.CDGNS = PRN.CDGNS " +
                "                       AND PRC.CICLO = PRN.CICLO " +
                "                       AND CAN.CDGEM = PRC.CDGEM " +
                "                       AND CAN.CDGCLNS = PRC.CDGCLNS " +
                "                       AND CAN.CLNS = PRC.CLNS " +
                "                       AND CAN.CICLO = PRC.CICLO " +
                "                       AND CAN.CDGCL = PRC.CDGCL " +
                "                       AND CO.CDGEM = PRN.CDGEM " +
                "                       AND CO.CODIGO = PRN.CDGCO " +
                "                       AND NS.CDGEM = PRN.CDGEM " +
                "                       AND NS.CODIGO = PRN.CDGNS " +
                "                       AND CL.CDGEM = CAN.CDGEM " +
                "                       AND CL.CODIGO = CAN.CDGCL " +
                "                       AND CB.CDGEM = CAN.CDGEM " +
                "                       AND CB.CODIGO = CAN.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = PRN.CDGEM " +
                "                       AND PE.CODIGO = PRN.CDGOCPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND CC.TIPO = 'DE' " +
                "                       AND TO_NUMBER(CAN.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " +
                "                    SELECT PGS.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, PGS.NOCHEQUE " +
                "                         , ABS(PGS.CANTIDAD) IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'GARANTIA' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , PGS.CDGCLNS, NS.NOMBRE GRUPO, PGS.CICLO, TO_CHAR(PGS.FPAGO, 'DD/MM/YYYY')INICIO " +
                "                         , NS.CDGCO, CO.NOMBRE SUCURSAL, PGS.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, PAG_GAR_SIM PGS, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = PGS.CDGEM " +
                "                       AND CC.CDGCB = PGS.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(PGS.NOCHEQUE) " +
                "                       AND CC.IMPORTE = ABS(PGS.CANTIDAD) " +
                "                       AND NS.CDGEM = PGS.CDGEM  " +
                "                       AND NS.CODIGO = PGS.CDGCLNS " +
                "                       AND NS.CDGEM = CO.CDGEM " +
                "                       AND NS.CDGCO = CO.CODIGO " +
                "                       AND CL.CDGEM = PGS.CDGEM " +
                "                       AND CL.CODIGO = PGS.CDGCL " +
                "                       AND CB.CDGEM = PGS.CDGEM " +
                "                       AND CB.CODIGO = PGS.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = NS.CDGEM " +
                "                       AND PE.CODIGO = NS.CDGACPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND CC.TIPO = 'GL' " +
                "                       AND TO_NUMBER(PGS.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " +
                "                    SELECT CAN.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, CAN.NOCHEQUE " +
                "                         , CAN.CANTIDAD IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'GARANTIA' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , CAN.CDGCLNS, NS.NOMBRE GRUPO, CAN.CICLO, TO_CHAR(PGS.FPAGO, 'DD/MM/YYYY') INICIO " +
                "                         , NS.CDGCO, CO.NOMBRE SUCURSAL, CAN.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, CHEQUE_CANCELADO CAN, PAG_GAR_SIM PGS, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = CAN.CDGEM " +
                "                       AND CC.CDGCB = CAN.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(CAN.NOCHEQUE) " +
                "                       AND CC.IMPORTE = CAN.CANTIDAD " +
                "                       AND NS.CDGEM = PGS.CDGEM " +
                "                       AND NS.CODIGO = PGS.CDGCLNS " +
                "                       AND NS.CDGEM = CO.CDGEM " +
                "                       AND NS.CDGCO = CO.CODIGO " +
                "                       AND CAN.CDGEM = PGS.CDGEM " +
                "                       AND CAN.CDGCLNS = PGS.CDGCLNS " +
                "                       AND CAN.CLNS = PGS.CLNS " +
                "                       AND CAN.CICLO = PGS.CICLO " +
                "                       AND ABS(CAN.CANTIDAD) = ABS(PGS.CANTIDAD) " +
                "                       AND CL.CDGEM = CAN.CDGEM " +
                "                       AND CL.CODIGO = CAN.CDGCL " +
                "                       AND CB.CDGEM = CAN.CDGEM " +
                "                       AND CB.CODIGO = CAN.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = NS.CDGEM " +
                "                       AND PE.CODIGO = NS.CDGACPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND CC.TIPO = 'GL' " +
                "                       AND TO_NUMBER(CAN.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " +
                "                    SELECT PGS.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, PGS.NOCHEQUE " +
                "                         , ABS(PGS.CANTIDAD) IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'EXCEDENTE' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , PGS.CDGCLNS, NS.NOMBRE GRUPO, PGS.CICLO, TO_CHAR(PGS.FPAGO, 'DD/MM/YYYY') INICIO " +
                "                         , NS.CDGCO, CO.NOMBRE SUCURSAL, PGS.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, PAG_DEV_EXC PGS, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = PGS.CDGEM " +
                "                       AND CC.CDGCB = PGS.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(PGS.NOCHEQUE) " +
                "                       AND CC.IMPORTE = ABS(PGS.CANTIDAD) " +
                "                       AND NS.CDGEM = PGS.CDGEM  " +
                "                       AND NS.CODIGO = PGS.CDGCLNS " +
                "                       AND NS.CDGEM = CO.CDGEM " +
                "                       AND NS.CDGCO = CO.CODIGO " +
                "                       AND CL.CDGEM = PGS.CDGEM " +
                "                       AND CL.CODIGO = PGS.CDGCL " +
                "                       AND CB.CDGEM = PGS.CDGEM " +
                "                       AND CB.CODIGO = PGS.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = NS.CDGEM " +
                "                       AND PE.CODIGO = NS.CDGACPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND CC.TIPO = 'EX' " +
                "                       AND TO_NUMBER(PGS.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " +
                "                    SELECT CAN.CDGEM, TO_CHAR(CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA, IB.CODIGO CDGIB, IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB, CB.NUMERO CUENTA, CAN.NOCHEQUE " +
                "                         , CAN.CANTIDAD IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' ELSE NULL END ESTATUS " +
                "                         , 'EXCEDENTE' TIPO, TO_CHAR(CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION, TO_CHAR(FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(NOCHEQUESUS,7,'0') NOCHEQUESUS, TO_CHAR(FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , CAN.CDGCLNS, NS.NOMBRE GRUPO, CAN.CICLO, TO_CHAR(PGS.FPAGO, 'DD/MM/YYYY') INICIO " +
                "                         , NS.CDGCO, CO.NOMBRE SUCURSAL, CAN.CDGCL " +
                "                         , NOMBREC(CL.CDGEM,CL.CODIGO,'I','N','','','','') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, CHEQUE_CANCELADO CAN, PAG_DEV_EXC PGS, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = CAN.CDGEM " +
                "                       AND CC.CDGCB = CAN.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER(CAN.NOCHEQUE) " +
                "                       AND CC.IMPORTE = CAN.CANTIDAD " +
                "                       AND NS.CDGEM = PGS.CDGEM " +
                "                       AND NS.CODIGO = PGS.CDGCLNS " +
                "                       AND NS.CDGEM = CO.CDGEM " +
                "                       AND NS.CDGCO = CO.CODIGO " +
                "                       AND CAN.CDGEM = PGS.CDGEM " +
                "                       AND CAN.CDGCLNS = PGS.CDGCLNS " +
                "                       AND CAN.CLNS = PGS.CLNS " +
                "                       AND CAN.CICLO = PGS.CICLO " +
                "                       AND ABS(CAN.CANTIDAD) = ABS(PGS.CANTIDAD) " +
                "                       AND CL.CDGEM = CAN.CDGEM " +
                "                       AND CL.CODIGO = CAN.CDGCL " +
                "                       AND CB.CDGEM = CAN.CDGEM " +
                "                       AND CB.CODIGO = CAN.CDGCB " +
               "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = NS.CDGEM " +
                "                       AND PE.CODIGO = NS.CDGACPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND CC.TIPO = 'EX' " +
                "                       AND TO_NUMBER(CAN.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " + // DEFUNCION
                "                    SELECT CC.CDGEM " +
                "                         , TO_CHAR (CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA " +
                "                         , IB.CODIGO " +
                "                         , IB.NOMBRE BANCO " +
                "                         , CB.CODIGO " +
                "                         , CB.NUMERO CUENTA " +
                "                         , CC.NOCHEQUE " +
                "                         , DB.MONTO " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' " +
                "                                ELSE NULL END ESTATUS " +
                "                         , 'DEFUNCION' " +
                "                         , TO_CHAR (CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION " +
                "                         , TO_CHAR (FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(CC.NOCHEQUESUS,7,'0') NOCHEQUESUS " +
                "                         , TO_CHAR (FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , D.CDGCLNS " +
                "                         , NS.NOMBRE NOMNS " +
                "                         , D.CICLO " +
                "                         , TO_CHAR(DB.FPAGO, 'DD/MM/YYYY') INICIO " +
                "                         , CC.CDGCO " +
                "                         , CC.NOMCO SUCURSAL " +
                "                         , CASE WHEN D.MISDATCL = 'N' THEN D.CDGCL ELSE '' END CDGCL  " +
                "                         , NOMBREC(NULL,NULL,'I','N',DB.NOMBRE1,DB.NOMBRE2,DB.PRIMAPE,DB.SEGAPE) " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, DEFUNCION_BENEFICIARIO DB, DEFUNCION D, IB, CB, NS " +
                "                     WHERE CC.CDGEM = DB.CDGEM " +
                "                       AND CC.CDGCB = DB.CDGCB " +
                "                       AND TO_NUMBER(CC.NOCHEQUE) = TO_NUMBER(DB.NOCHEQUE) " +
                "                       AND CC.IMPORTE = DB.MONTO " +
                "                       AND DB.CDGEM = D.CDGEM " +
                "                       AND DB.CDGDEFUN = D.CODIGO " +
                "                       AND CC.CDGEM = IB.CDGEM " +
                "                       AND CC.CDGIB = IB.CODIGO " +
                "                       AND CC.CDGEM = CB.CDGEM " +
                "                       AND CC.CDGCB = CB.CODIGO " +
                "                       AND NS.CDGEM = D.CDGEM " +
                "                       AND NS.CODIGO = D.CDGCLNS " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND TO_NUMBER (CC.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.TIPO = 'DF' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " + // DEFUNCION CANCELADOS
                "                    SELECT CAN.CDGEM " +
                "                         , TO_CHAR (CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA " +
                "                         , IB.CODIGO CDGIB " +
                "                         , IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB " +
                "                         , CB.NUMERO CUENTA " +
                "                         , CAN.NOCHEQUE " +
                "                         , CAN.CANTIDAD IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' " +
                "                                ELSE NULL END ESTATUS " +
                "                         , 'DEFUNCION' TIPO " +
                "                         , TO_CHAR (CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION " +
                "                         , TO_CHAR (FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION " +
                "                                ELSE NULL END DIAS_TRANS " +
                "                         , LPAD (NOCHEQUESUS, 7, '0') NOCHEQUESUS " +
                "                         , TO_CHAR (FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , CAN.CDGCLNS " +
                "                         , NS.NOMBRE GRUPO " +
                "                         , CAN.CICLO " +
                "                         , TO_CHAR (PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "                         , PRN.CDGCO " +
                "                         , CO.NOMBRE SUCURSAL " +
                "                         , CAN.CDGCL " +
                "                         , NOMBREC (CL.CDGEM, CL.CODIGO, 'I', 'N', '', '', '', '') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, CHEQUE_CANCELADO CAN, PRC, PRN, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = CAN.CDGEM " +
                "                       AND CC.CDGCB = CAN.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER (CAN.NOCHEQUE) " +
                "                       AND CC.IMPORTE = CAN.CANTIDAD " +
                "                       AND PRC.CDGEM = PRN.CDGEM " +
                "                       AND PRC.CDGNS = PRN.CDGNS " +
                "                       AND PRC.CICLO = PRN.CICLO " +
                "                       AND CAN.CDGEM = PRC.CDGEM " +
                "                       AND CAN.CDGCLNS = PRC.CDGCLNS " +
                "                       AND CAN.CLNS = PRC.CLNS " +
                "                       AND CAN.CICLO = PRC.CICLO " +
                "                       AND CAN.CDGCL = PRC.CDGCL " +
                "                       AND CO.CDGEM = PRN.CDGEM " +
                "                       AND CO.CODIGO = PRN.CDGCO " +
                "                       AND NS.CDGEM = PRN.CDGEM " +
                "                       AND NS.CODIGO = PRN.CDGNS " +
                "                       AND CL.CDGEM = CAN.CDGEM " +
                "                       AND CL.CODIGO = CAN.CDGCL " +
                "                       AND CB.CDGEM = CAN.CDGEM " +
                "                       AND CB.CODIGO = CAN.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = PRN.CDGEM " +
                "                       AND PE.CODIGO = PRN.CDGOCPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND TO_NUMBER (CAN.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.TIPO = 'DF' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " + // DIAGNOSTICO
                "                    SELECT CC.CDGEM " +
                "                         , TO_CHAR (CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA " +
                "                         , IB.CODIGO " +
                "                         , IB.NOMBRE BANCO " +
                "                         , CB.CODIGO " +
                "                         , CB.NUMERO CUENTA " +
                "                         , CC.NOCHEQUE " +
                "                         , DB.MONTO " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' " +
                "                                ELSE NULL END ESTATUS " +
                "                         , 'DIAGNOSTICO' TIPO " +
                "                         , TO_CHAR (CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION " +
                "                         , TO_CHAR (FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION ELSE NULL END DIAS_TRANS " +
                "                         , LPAD(CC.NOCHEQUESUS,7,'0') NOCHEQUESUS " +
                "                         , TO_CHAR (FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , D.CDGCLNS " +
                "                         , NS.NOMBRE NOMNS " +
                "                         , D.CICLO " +
                "                         , TO_CHAR(DB.FPAGO, 'DD/MM/YYYY') INICIO " +
                "                         , CC.CDGCO " +
                "                         , CC.NOMCO SUCURSAL " +
                "                         , D.CDGCL " +
                "                         , NOMBREC(NULL,NULL,'I','N',DB.NOMBRE1,DB.NOMBRE2,DB.PRIMAPE,DB.SEGAPE) " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, DIAGNOSTICO_BENEFICIARIO DB, DIAGNOSTICO D, IB, CB, NS " +
                "                     WHERE CC.CDGEM = DB.CDGEM " +
                "                       AND CC.CDGCB = DB.CDGCB " +
                "                       AND TO_NUMBER(CC.NOCHEQUE) = TO_NUMBER(DB.NOCHEQUE) " +
                "                       AND CC.IMPORTE = DB.MONTO " +
                "                       AND DB.CDGEM = D.CDGEM " +
                "                       AND DB.CDGDIAG = D.CODIGO " +
                "                       AND CC.CDGEM = IB.CDGEM " +
                "                       AND CC.CDGIB = IB.CODIGO " +
                "                       AND CC.CDGEM = CB.CDGEM " +
                "                       AND CC.CDGCB = CB.CODIGO " +
                "                       AND NS.CDGEM = D.CDGEM " +
                "                       AND NS.CODIGO = D.CDGCLNS " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND TO_NUMBER (CC.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.TIPO = 'DG' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' " +
                "                     UNION " + // DIAGNOSTICO CANCELADOS
                "                    SELECT CAN.CDGEM " +
                "                         , TO_CHAR (CC.FCONCILIA, 'DD/MM/YYYY') FCONCILIA " +
                "                         , IB.CODIGO CDGIB " +
                "                         , IB.NOMBRE BANCO " +
                "                         , CB.CODIGO CDGCB " +
                "                         , CB.NUMERO CUENTA " +
                "                         , CAN.NOCHEQUE " +
                "                         , CAN.CANTIDAD IMPORTE " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN 'CIRCULACION' " +
                "                                WHEN CC.ESTATUS = 'CB' THEN 'COBRADO' " +
                "                                WHEN CC.ESTATUS = 'CA' THEN 'CANCELADO' " +
                "                                WHEN CC.ESTATUS = 'DE' THEN 'DEVUELTO' " +
                "                                WHEN CC.ESTATUS = 'SU' THEN 'SUSTITUIDO' " +
                "                                ELSE NULL END ESTATUS " +
                "                         , 'DIAGNOSTICO' TIPO " +
                "                         , TO_CHAR (CC.FIMPRESION, 'DD/MM/YYYY') FIMPRESION " +
                "                         , TO_CHAR (FCOBROREAL, 'DD/MM/YYYY') FCOBROREAL " +
                "                         , CASE WHEN CC.ESTATUS = 'CI' THEN CC.FCONCILIA - CC.FIMPRESION " +
               "                                ELSE NULL END DIAS_TRANS " +
                "                         , LPAD (NOCHEQUESUS, 7, '0') NOCHEQUESUS " +
                "                         , TO_CHAR (FCANCELACION, 'DD/MM/YYYY') FCANCELACION " +
                "                         , CAN.CDGCLNS " +
                "                         , NS.NOMBRE GRUPO " +
                "                         , CAN.CICLO " +
                "                         , TO_CHAR (PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "                         , PRN.CDGCO " +
                "                         , CO.NOMBRE SUCURSAL " +
                "                         , CAN.CDGCL " +
                "                         , NOMBREC (CL.CDGEM, CL.CODIGO, 'I', 'N', '', '', '', '') NOMBRECL " +
                "                         , CC.CDGOCPE " +
                "                         , CC.NOMOCPE " +
                "                      FROM CONCILIACHEQUE CC, CHEQUE_CANCELADO CAN, PRC, PRN, NS, CO, CL, CB, IB, PE " +
                "                     WHERE CC.CDGEM = CAN.CDGEM " +
                "                       AND CC.CDGCB = CAN.CDGCB " +
                "                       AND CC.NOCHEQUE = TO_NUMBER (CAN.NOCHEQUE) " +
                "                       AND CC.IMPORTE = CAN.CANTIDAD " +
                "                       AND PRC.CDGEM = PRN.CDGEM " +
                "                       AND PRC.CDGNS = PRN.CDGNS " +
                "                       AND PRC.CICLO = PRN.CICLO " +
                "                       AND CAN.CDGEM = PRC.CDGEM " +
                "                       AND CAN.CDGCLNS = PRC.CDGCLNS " +
                "                       AND CAN.CLNS = PRC.CLNS " +
                "                       AND CAN.CICLO = PRC.CICLO " +
                "                       AND CAN.CDGCL = PRC.CDGCL " +
                "                       AND CO.CDGEM = PRN.CDGEM " +
                "                       AND CO.CODIGO = PRN.CDGCO " +
                "                       AND NS.CDGEM = PRN.CDGEM " +
                "                       AND NS.CODIGO = PRN.CDGNS " +
                "                       AND CL.CDGEM = CAN.CDGEM " +
                "                       AND CL.CODIGO = CAN.CDGCL " +
                "                       AND CB.CDGEM = CAN.CDGEM " +
                "                       AND CB.CODIGO = CAN.CDGCB " +
                "                       AND IB.CDGEM = CB.CDGEM " +
                "                       AND IB.CODIGO = CB.CDGIB " +
                "                       AND PE.CDGEM = PRN.CDGEM " +
                "                       AND PE.CODIGO = PRN.CDGOCPE " +
                "                       AND CC.CDGEM = '" + empresa + "' " +
                "                       AND TO_NUMBER (CAN.NOCHEQUE) = '" + int.Parse(nocheque) + "' " +
                "                       AND CC.TIPO = 'DG' " +
                "                       AND CC.CDGIB = '" + cdgib + "' " +
                "                       AND CC.CDGCB = '" + cdgcb + "' )";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PRESTAMOS HISTORICOS POR ACREDITADO
    [WebMethod]
    public string getRepHistPrcAcred(string acred)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;

        query = "SELECT PRN.CDGNS " +
                ",NS.NOMBRE GRUPO " +
                ",PRN.CICLO " +
                ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                ",PRC.CDGCL " +
                ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE_CL " +
                ",PRC.CANTENTRE " +
                ",(SELECT D.CANTENTRE " +
                  "FROM PRN C, PRC D " +
                  "WHERE C.CDGEM = PRN.CDGEM " +
                  "AND C.INICIO = (SELECT MAX(A.INICIO) " +
                                  "FROM PRN A, PRC B " +
                                  "WHERE A.CDGEM = PRN.CDGEM " +
                                  "AND A.INICIO < PRN.INICIO " +
                                  "AND B.CDGEM = A.CDGEM " +
                                  "AND B.CDGNS = A.CDGNS " +
                                  "AND B.CICLO = A.CICLO " +
                                  "AND B.CDGCL = PRC.CDGCL) " +
                  "AND D.CDGEM = C.CDGEM " +
                  "AND D.CDGNS = C.CDGNS " +
                  "AND D.CICLO = C.CICLO " +
                  "AND D.CDGCL = PRC.CDGCL) CANTENTRE_ANT  " +
                ",DECODE(PRN.SITUACION,'E','ENTREGADO','L','LIQUIDADO') SITUACION " +
                ",ROUND(((PRC.CANTENTRE / (SELECT D.CANTENTRE " +
                                        "FROM PRN C, PRC D " +
                                        "WHERE C.CDGEM = PRN.CDGEM " +
                                        "AND C.INICIO = (SELECT MAX(A.INICIO) " +
                                                        "FROM PRN A, PRC B " +
                                                        "WHERE A.CDGEM = PRN.CDGEM " +
                                                        "AND A.INICIO < PRN.INICIO " +
                                                        "AND B.CDGEM = A.CDGEM " +
                                                        "AND B.CDGNS = A.CDGNS " +
                                                        "AND B.CICLO = A.CICLO " +
                                                        "AND B.CDGCL = PRC.CDGCL) " +
                                        "AND D.CDGEM = C.CDGEM " +
                                        "AND D.CDGNS = C.CDGNS " +
                                        "AND D.CICLO = C.CICLO " +
                                        "AND D.SITUACION <> 'D' " +
                                        "AND D.CANTENTRE > 0 " +
                                        "AND D.CDGCL = PRC.CDGCL) - 1) * 100),2) PORC_INC " +
                ",PRN.CDGOCPE ASESOR " +
                "FROM PRN, " +
                "PRC, " +
                "NS, " +
                "CL " +
                "WHERE PRC.CDGEM = '" + empresa + "' " +
                "AND PRC.CDGCL = '" + acred + "' " +
                "AND PRC.CLNS = 'G' " +
                "AND PRC.SITUACION <> 'D' " +
                "AND PRN.CDGEM = PRC.CDGEM " +
                "AND PRN.CDGNS = PRC.CDGNS " +
                "AND PRN.CICLO = PRC.CICLO " +
                "AND NS.CDGEM = PRN.CDGEM " +
                "AND NS.CODIGO = PRN.CDGNS " +
                "AND CL.CDGEM = PRC.CDGEM " +
                "AND CL.CODIGO = PRC.CDGCL " +
                "ORDER BY PRC.CDGCL, " +
                "PRN.INICIO, " +
                "PRN.CDGNS, " +
                "PRN.CICLO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PRESTAMOS HISTORICOS 
    [WebMethod]
    public string getRepHistPrnAcred(string grupo, string ciclo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;

        query = "SELECT PRN.CDGNS " +
                ",NS.NOMBRE GRUPO " +
                ",PRN.CICLO " +
                ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                ",PRC.CDGCL " +
                ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','A',NULL,NULL,NULL,NULL) NOMBRE_CL " +
                ",PRC.CANTENTRE " +
                ",PRC.ENTRREAL " +
                ",(SELECT D.CANTENTRE " +
                  "FROM PRN C, PRC D " +
                  "WHERE C.CDGEM = PRN.CDGEM " +
                  "AND C.INICIO = (SELECT MAX(A.INICIO) " +
                                  "FROM PRN A, PRC B " +
                                  "WHERE A.CDGEM = PRN.CDGEM " +
                                  "AND A.INICIO < PRN.INICIO " +
                                  "AND B.CDGEM = A.CDGEM " +
                                  "AND B.CDGNS = A.CDGNS " +
                                  "AND B.CICLO = A.CICLO " +
                                  "AND B.CDGCL = PRC.CDGCL) " +
                  "AND D.CDGEM = C.CDGEM " +
                  "AND D.CDGNS = C.CDGNS " +
                  "AND D.CICLO = C.CICLO " +
                  "AND D.CDGCL = PRC.CDGCL) CANTENTRE_ANT  " +
                ",ROUND(((PRC.CANTENTRE / (SELECT D.CANTENTRE " +
                                        "FROM PRN C, PRC D " +
                                        "WHERE C.CDGEM = PRN.CDGEM " +
                                        "AND C.INICIO = (SELECT MAX(A.INICIO) " +
                                                        "FROM PRN A, PRC B " +
                                                        "WHERE A.CDGEM = PRN.CDGEM " +
                                                        "AND A.INICIO < PRN.INICIO " +
                                                        "AND B.CDGEM = A.CDGEM " +
                                                        "AND B.CDGNS = A.CDGNS " +
                                                        "AND B.CICLO = A.CICLO " +
                                                        "AND B.CDGCL = PRC.CDGCL) " +
                                        "AND D.CDGEM = C.CDGEM " +
                                        "AND D.CDGNS = C.CDGNS " +
                                        "AND D.CICLO = C.CICLO " +
                                        "AND D.SITUACION <> 'D' " +
                                        "AND D.CANTENTRE > 0 " +
                                        "AND D.CDGCL = PRC.CDGCL) - 1) * 100),2) PORC_INC " +
                ",CASE WHEN PRN.CDGOCPE IS NOT NULL THEN " +
                    "(SELECT NOMBREC(NULL,NULL,'I','N',NOMBRE1,NOMBRE2,PRIMAPE,SEGAPE) FROM PE WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGOCPE) " +
                "ELSE " +
                    "(SELECT NOMBREC(NULL,NULL,'I','N',NOMBRE1,NOMBRE2,PRIMAPE,SEGAPE) FROM PE WHERE CDGEM = A.CDGEM AND CODIGO = A.CDGOCPE) " +
                "END ASESOR " +
                ",DECODE(PRC.SITUACION,'E','ENTREGADO','L','LIQUIDADO','D','DEVUELTO') SITUACION " +
                ",(SELECT NOMBREC(NULL, NULL, 'I', 'N', NOMBRE1, NOMBRE2, PRIMAPE, SEGAPE) FROM PE WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.AUTCARPE) NOMAUT " +
                ",(SELECT REPORTE FROM PRC_ANALISIS WHERE CDGEM = PRC.CDGEM AND CDGNS = PRC.CDGNS AND CICLO = PRC.CICLO AND CDGCL = PRC.CDGCL) COMENTARIOS " +
                ",FNCALDIASATRASO(PRN.CDGEM,PRN.CDGNS,LPAD(TO_CHAR(TO_NUMBER(PRN.CICLO)),2,'0'),'G') DIASATRASO " +
                "FROM PRN, " +
                "PRC, " +
                "NS, " +
                "CL, " +
                "(SELECT SC.CDGEM, SC.CDGCL, SN.CDGOCPE FROM SN, SC WHERE SN.CDGEM = '" + empresa + "' AND SN.CDGNS = '" + grupo + "' AND SN.CICLO = '" + ciclo + "' AND SC.CDGEM = SN.CDGEM AND SC.CDGNS = SN.CDGNS AND SC.CICLO = SN.CICLO AND SC.CLNS = 'G') A " +
                "WHERE PRC.CDGEM = A.CDGEM " +
                "AND PRC.CDGCL = A.CDGCL " +
                "AND PRC.CLNS = 'G' " +
                "AND PRC.SITUACION <> 'D' " +
                "AND PRN.CDGEM = PRC.CDGEM " +
                "AND PRN.CDGNS = PRC.CDGNS " +
                "AND PRN.CICLO = PRC.CICLO " +
                "AND NS.CDGEM = PRN.CDGEM " +
                "AND NS.CODIGO = PRN.CDGNS " +
                "AND CL.CDGEM = PRC.CDGEM " +
                "AND CL.CODIGO = PRC.CDGCL " +
                //"ORDER BY PRC.CDGCL, " +
                "ORDER BY NOMBRE_CL, " +
                "PRN.INICIO, " +
                "PRN.CDGNS, " +
                "PRN.CICLO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA EL CALCULO DE INCENTIVOS DEL PERIODO SELECCIONADO
    [WebMethod]
    public string getRepIncentivo(string mes, string anio, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = string.Empty;
        string query = string.Empty;

        query = "SELECT INC.ESTATUS " +
			    ",INC.SUCURSAL " + 
			    ",INC.NOMINA " +
			    ",INC.ASESOR " +
			    ",INC.PUESTO " +
			    ",TO_CHAR(INC.FINGRESO,'DD/MM/YYYY') FECING " +
			    ",INC.FBAJA " +
			    ",INC.NOMJEFE " +
			    ",INC.ANTMESES " +
			    ",INC.TIPOASESORANT " +
			    ",INC.CARTPROM " +
			    ",INC.TIPOASESORCART " +
			    ",INC.TIPOASESOR " +
			    ",INC.ASIGPAGO " +
			    ",INC.INTCOBRADOPAG " +
			    ",INC.INTCOBRADOGL " +
			    ",INC.TOTALINTERES " +
		        ",INC.INCENTIVOMAX " +
			    ",INC.PONDMETACOL " +
			    ",INC.INCMAXMETACOL " +
			    ",INC.METACOL " +
			    ",INC.COLOCACION " +
			    ",INC.PORCMETACOL " +
			    ",INC.INCCOBMETACOL " +
			    ",INC.TOTALPAGOMETACOL " +
			    ",INC.PONDMETACTES " +
			    ",INC.INCMAXMETACTES " +
			    ",INC.METACTES " +
			    ",INC.CLIENTES " +
			    ",INC.PORCMETACTES " +
			    ",INC.INCCOBMETACTES " +
			    ",INC.TOTALPAGOMETACTES " +
			    ",INC.PONDDESER " +
			    ",INC.INCMAXDESER " +
			    ",INC.CTESRENOVAR " +
			    ",INC.DESERPERM " +
			    ",INC.DESERCION " +
			    ",INC.TOTALPAGODES " +
			    ",INC.PONDMORA " +
			    ",INC.INCMAXMORA " +
			    ",INC.SALDOTOTAL " +
			    ",INC.MORA " +
			    ",INC.PORCMORA " +
			    ",INC.PORCPAGOMORA " +
			    ",INC.TOTALPAGOMORA " +
                ",INC.INCENTIVO " +
			    ",INC.CONVENIOINC " +
			    ",INC.INCPREAPROB " +
			    ",INC.ANTMESES " +
			    ",INC.GRUPOSREQ " +
			    ",INC.GRUPOSACT " +
			    ",INC.CALIFNUMGPOS " +
			    ",INC.METACTES " +
			    ",INC.CLIENTES " +
			    ",INC.CALIFNUMCTES " +
			    ",INC.INCENTIVOFINAL " +
                "FROM INCENTIVO INC " +
                "WHERE INC.CDGEM = '" + empresa + "' " +
                "AND INC.ANIO = " + anio + " " +
                "AND INC.MES = " + mes;

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE OBTIENE LA INFORMACION DE LOS REGISTROS DE CONTROL DE PAGOS QUE PRESENTAN 
    //INCONSISTENCIAS CON RESPECTO A LAS FICHAS DE PAGO
    [WebMethod]
    public string getRepInconsistencias(string fecha, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string strAsesor = string.Empty;
        string status = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery(ref status, "SPCODIGOSUSUARIO", CommandType.StoredProcedure,
               oP.ParamsCodigosUsuario(empresa, usuario));

        if (puesto == "A")
            strAsesor = "AND PRN.CDGOCPE = '" + usuario + "' ";
        else
            strAsesor = "AND PRN.CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') ";

        string query = "SELECT CO.NOMBRE COORD " +
                       ",NOMBREC(NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR " +
                       ",PRN.CDGNS " +
                       ",NS.NOMBRE NOMNS " +
                       ",PRN.CICLO " +
                       ",TO_CHAR(CP.FREALPAGO,'DD/MM/YYYY') FPAGO " +
                       ",(CP.PAGOREAL + CP.APORT) CANTIDAD  " +
                       "FROM CONTROL_PAGOS CP, PRN, NS, CO, PE " +
                       "WHERE CP.CDGEM = '" + empresa + "' " +
                       "AND CP.FREALPAGO <= '" + fecha + "' " +
                       "AND (SELECT COUNT(*) " +
                            "FROM MP " +
                            "WHERE CDGEM = CP.CDGEM " +
                            "AND CDGCLNS = CP.CDGNS " +
                            "AND CICLO = CP.CICLO " +
                            "AND CLNS = 'G' " +
                            "AND FREALDEP = CP.FREALPAGO " +
                            "AND CANTIDAD = CP.PAGOREAL + CP.APORT) = 0 " +
                       "AND PRN.CDGEM = CP.CDGEM " +
                       "AND PRN.CDGNS = CP.CDGNS " +
                       "AND PRN.CICLO = CP.CICLO " +
                       "AND PRN.SITUACION = 'E' " +
                       "AND (SELECT COUNT(*) FROM REG_USUARIO WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGNS AND CLNS = 'G' AND CDGPE = '" + usuario + "') > 0 " +
                       //strAsesor +
                       "AND NS.CDGEM = PRN.CDGEM " +
                       "AND NS.CODIGO = PRN.CDGNS " +
                       "AND CO.CDGEM = PRN.CDGEM " +
                       "AND CO.CODIGO = PRN.CDGCO " +
                       "AND PE.CDGEM = PRN.CDGEM " +
                       "AND PE.CODIGO = PRN.CDGOCPE";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE OBTIENE EL REPORTE DE INDICADORES DE CARTERA DE LA FECHA INDICADA
    [WebMethod]
    public string getRepIndicadores(string fecha/*, string usuario*/)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT CD.COD_SUCURSAL " +
                       ",CD.NOM_SUCURSAL " +
                       ",CASE WHEN A.CODIGO IS NOT NULL THEN CASE WHEN A.CODIGO = 'JHMJ' THEN 'RENE AGUSTIN CAUICH CANCHE' " +
                                                                 "WHEN A.CODIGO = 'GOCA' THEN 'JUDITH NOEMI DOLORES ESQUIVEL' " +
                                                                 "ELSE NOMBREC(NULL,NULL,'I','N',A.NOMBRE1,A.NOMBRE2,A.PRIMAPE,A.SEGAPE) END " +
                       "ELSE " +
                            "CASE WHEN PE.CODIGO = 'GOCA' THEN " +
                                "'JUDITH NOEMI DOLORES ESQUIVEL' " +
                            "ELSE " +
                                "'COORDINACION' " +
                            "END " +
                       "END NOM_COR " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('R','C') THEN 1 ELSE 0 END) NO_GRUPOS " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('R','C') THEN CD.NO_CLIENTES ELSE 0 END) NO_CLIENTES " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.MONTO_ENTREGADO ELSE 0 END) MONTO_ENTREGADO " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_TOTAL ELSE 0 END) SDO_TOTAL " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END) SDO_CAPITAL " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_INTERES ELSE 0 END) SDO_INTERES " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN DD.DEV_DIARIO ELSE 0 END) - SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.INTERES_PAGADO ELSE 0 END) SIDNC " +
                       ",SUM(CASE WHEN CD.DIAS_MORA > 90 AND CD.TIPO_CARTERA <> 'C' THEN CD.SDO_TOTAL ELSE 0 END) CAR_VEN " +
                       ",CASE WHEN (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END) + SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN DD.DEV_DIARIO ELSE 0 END) - SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.INTERES_PAGADO ELSE 0 END)) > 0 THEN ROUND(SUM(CASE WHEN CD.DIAS_MORA > 90 AND CD.TIPO_CARTERA <> 'C' THEN CD.SDO_TOTAL ELSE 0 END) / (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END) + SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN DD.DEV_DIARIO ELSE 0 END) - SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.INTERES_PAGADO ELSE 0 END)),4) ELSE 0 END PORC_CAR_VEN " +
                       ",SUM(CASE WHEN CD.DIAS_MORA > 30 AND CD.TIPO_CARTERA <> 'C' THEN CD.SDO_CAPITAL ELSE 0 END) CR30 " +
                       ",CASE WHEN (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END)) > 0 THEN ROUND(SUM(CASE WHEN CD.DIAS_MORA > 30 AND CD.TIPO_CARTERA <> 'C' THEN CD.SDO_CAPITAL ELSE 0 END) / (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END)),4) ELSE 0 END PORC_CR30 " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.MORA_TOTAL ELSE 0 END) MORA_TOTAL " +
                       ",CASE WHEN (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_TOTAL ELSE 0 END)) > 0 THEN ROUND(SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.MORA_TOTAL ELSE 0 END) / SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_TOTAL ELSE 0 END),4) ELSE 0 END PORC_MORA " +
                       ",SUM(CASE WHEN CD.TIPO_CARTERA = 'C' THEN CASE WHEN CD.CDGCLNS = '002062' AND CD.CICLO = '01' THEN CD.SDO_TOTAL + 916 ELSE CD.SDO_TOTAL END ELSE 0 END) CASTIGOS " +
                       ",CASE WHEN (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END) + SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN DD.DEV_DIARIO ELSE 0 END) - SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.INTERES_PAGADO ELSE 0 END)) > 0 THEN ROUND(SUM(CASE WHEN CD.TIPO_CARTERA = 'C' THEN CASE WHEN CD.CDGCLNS = '002062' AND CD.CICLO = '01' THEN CD.SDO_TOTAL + 916 ELSE CD.SDO_TOTAL END ELSE 0 END) / (SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.SDO_CAPITAL ELSE 0 END) + SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN DD.DEV_DIARIO ELSE 0 END) - SUM(CASE WHEN CD.TIPO_CARTERA NOT IN ('C') THEN CD.INTERES_PAGADO ELSE 0 END)),4) ELSE 0 END PORC_CAR_CAST " +
                       ",SUM(CD.SALDO_GL) SALDO_GL " +
                       "FROM  TBL_CIERRE_DIA CD " +
                       ",(SELECT CDGEM,CDGCLNS,CICLO,CLNS,INICIO,NVL(SUM(DEV_DIARIO),0) DEV_DIARIO FROM DEVENGO_DIARIO WHERE CDGEM = '" + empresa + "' AND FECHA_CALC <= '" + fecha + "' GROUP BY CDGEM,CDGCLNS,CICLO,CLNS,INICIO " +
                       "UNION " +
                       "SELECT CDGEM,CDGCLNS,CICLO,CLNS,INICIO,0 DEV_DIARIO " +
                       "FROM TBL_CIERRE_DIA WHERE CDGEM = '" + empresa + "' AND FECHA_CALC = '" + fecha + "' AND INICIO = '" + fecha + "') DD " +
                       ",PE LEFT JOIN PE A ON PE.CDGEM = A.CDGEM AND PE.CALLE = A.TELEFONO AND A.PUESTO = 'C' " +
                       "WHERE CD.CDGEM = '" + empresa + "' " +
                       "AND CD.CDGEM = PE.CDGEM " +
                       "AND CD.COD_ASESOR = PE.CODIGO " +
                       "AND CD.CDGEM = DD.CDGEM " +
                       "AND CD.CDGCLNS = DD.CDGCLNS " +
                       "AND CD.CICLO = DD.CICLO " +
                       "AND CD.CLNS = DD.CLNS " +
                       "AND CD.INICIO = DD.INICIO " +
                       "AND CD.SITUACION = 'E' " +
                       "AND CD.FECHA_CALC = '" + fecha + "' " +
                       "GROUP BY CD.COD_SUCURSAL " +
                       ",CD.NOM_SUCURSAL " +
                       ",CASE WHEN A.CODIGO IS NOT NULL THEN CASE WHEN A.CODIGO = 'JHMJ' THEN 'RENE AGUSTIN CAUICH CANCHE' " +
                                                                 "WHEN A.CODIGO = 'GOCA' THEN 'JUDITH NOEMI DOLORES ESQUIVEL' " +
                                                                 "ELSE NOMBREC(NULL,NULL,'I','N',A.NOMBRE1,A.NOMBRE2,A.PRIMAPE,A.SEGAPE) END " +
                       "ELSE " +
                            "CASE WHEN PE.CODIGO = 'GOCA' THEN " +
                                "'JUDITH NOEMI DOLORES ESQUIVEL' " +
                            "ELSE " +
                                "'COORDINACION' " +
                            "END " +
                       "END " +
                       "ORDER BY CD.NOM_SUCURSAL, NOM_COR";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL REPORTE DE INTERES DEVENGADO
    [WebMethod]
    public string getRepInteresDevengado(string tipo, string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string query = string.Empty;
        string xml = "";
        int iRes;

        try
        {
            if (tipo == "1")
            {
                query = "SELECT D.CDGCLNS " +
                       ",NS.NOMBRE " +
                       ",D.CLNS " +
                       ",D.CICLO " +
                       ",PRN.CDGOCPE COD_ASESOR " +
                       ",NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                       ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",PRN.CANTENTRE CANTIDAD_ENTREGADA " +
                       ", round((round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                                                                       "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                                                                       "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100), " +
                                                                       "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                                                                       "'',  ''),2)) ,2) TOTAL_A_PAGAR " +
                       ",ROUND(SUM(D.DEV_DIARIO),2) DEV_DEL_MES " +
                       ",ROUND(SUM(D.DEV_DIARIO_SIN_IVA),2) DEV_MES_SIN_IVA " +
                       ",ROUND(SUM(D.IVA_INT),2) IVA_INT_MES " +
                       ",COUNT(*) DIAS_DEV " +
                       ",CASE WHEN PRN.INICIO < TRUNC(TO_DATE('01/01/2014','DD/MM/YYYY')) AND PRN.CDGCO = '001' AND TO_DATE('" + fechaFin + "','DD/MM/YYYY') > TRUNC(TO_DATE('31/12/2013','DD/MM/YYYY')) THEN " +
                       "16 " +
                       "ELSE " +
                       "(SELECT CF.IVA " +
                       "FROM CF " +
                       "WHERE PRN.CDGEM = CF.CDGEM " +
                       "AND PRN.CDGFDI = CF.CDGFDI) " +
                       "END IVA " +
                       "FROM DEVENGO_DIARIO D, NS, PRN, PE " +
                       "WHERE D.CDGEM = PRN.CDGEM " +
                       "AND D.CDGCLNS = PRN.CDGNS " +
                       "AND D.INICIO = PRN.INICIO " +
                       "AND (TRUNC(PRN.ENTREGA) IS NULL OR TRUNC(PRN.ENTREGA) > PRN.INICIO) " +
                       "AND (D.CICLO = PRN.CICLO OR D.CICLO = PRN.CICLOD) " +
                       "AND PRN.CDGEM = NS.CDGEM " +
                       "AND PRN.CDGNS = NS.CODIGO " +
                       "AND PRN.CDGEM = PE.CDGEM " +
                       "AND PRN.CDGOCPE = PE.CODIGO " +
                       "AND D.CDGEM = NS.CDGEM " +
                       "AND D.CDGCLNS = NS.CODIGO " +
                       "AND D.CDGEM = '" + empresa + "' " +
                       "AND D.FECHA_CALC BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND D.CLNS = 'G' " +
                       "AND D.ESTATUS = 'RE' " +
                       "GROUP BY D.CDGCLNS, NS.NOMBRE, D.CLNS, D.CICLO " +
                       ",PRN.CDGEM " +
                       ",PRN.CDGOCPE " +
                       ",NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       ",PRN.INICIO " +
                       ",PRN.CANTENTRE " +
                       ",PRN.CDGCO " +
                       ",PRN.CDGFDI " +
                       ",round((round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                                                                       "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                                                                       "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100),  " +
                                                                       "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                                                                       "'',  ''),2)) ,2) " +
                    //UNION CON CREDITOS INDIVIDUALES
                       "UNION " +
                       "SELECT D.CDGCLNS " +
                       ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE " +
                       ",D.CLNS " +
                       ",D.CICLO " +
                       ",PRC.CDGOCPE COD_ASESOR " +
                       ",NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) ASESOR " +
                       ",TO_CHAR(PRC.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",PRC.CANTENTRE CANTIDAD_ENTREGADA " +
                       ", round((round(decode(nvl(PRC.periodicidad,''), 'S', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(4 * 100), " +
                                                                       "'Q', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0) * 15)/(30 * 100), " +
                                                                       "'C', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(2 * 100), " +
                                                                       "'M', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(100), " +
                                                                       "'',  ''),2)) ,2) TOTAL_A_PAGAR " +
                       ",ROUND(SUM(D.DEV_DIARIO),2) DEV_DEL_MES " +
                       ",ROUND(SUM(D.DEV_DIARIO_SIN_IVA),2) DEV_MES_SIN_IVA " +
                       ",ROUND(SUM(D.IVA_INT),2) IVA_INT_MES " +
                       ",COUNT(*) DIAS_DEV " +
                       ",CASE WHEN PRC.INICIO < TRUNC(TO_DATE('01/01/2014','DD/MM/YYYY')) AND PRC.CDGCO = '001' AND TO_DATE('" + fechaFin + "', 'DD/MM/YYYY') > TRUNC(TO_DATE('31/12/2013','DD/MM/YYYY')) THEN " +
                       "16 " +
                       "ELSE " +
                       "(SELECT CF.IVA " +
                       "FROM CF " +
                       "WHERE PRC.CDGEM = CF.CDGEM " +
                       "AND PRC.CDGFDI = CF.CDGFDI) " +
                       "END IVA " +
                       "FROM DEVENGO_DIARIO D, CL, PRC, PE " +
                       "WHERE D.CDGEM = PRC.CDGEM " +
                       "AND D.CDGCLNS = PRC.CDGCLNS " +
                       "AND D.INICIO = PRC.INICIO " +
                       "AND (TRUNC(PRC.ENTREGA) IS NULL OR TRUNC(PRC.ENTREGA) > PRC.INICIO) " +
                       "AND (D.CICLO = PRC.CICLO OR D.CICLO = PRC.CICLOD) " +
                       "AND PRC.CDGEM = CL.CDGEM " +
                       "AND PRC.CDGCL = CL.CODIGO " +
                       "AND PRC.CDGEM = PE.CDGEM " +
                       "AND PRC.CDGOCPE = PE.CODIGO " +
                       "AND D.CDGEM = CL.CDGEM " +
                       "AND D.CDGCLNS = CL.CODIGO " +
                       "AND D.CDGEM = '" + empresa + "' " +
                       "AND D.FECHA_CALC BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND D.CLNS = 'I' " +
                       "AND D.ESTATUS = 'RE' " +
                       "GROUP BY D.CDGCLNS " +
                       ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) " +
                       ",D.CLNS " +
                       ",D.CICLO " +
                       ",PRC.CDGEM " +
                       ",PRC.CDGOCPE " +
                       ",NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       ",PRC.INICIO " +
                       ",PRC.CANTENTRE " +
                       ",PRC.CDGCO " +
                       ",PRC.CDGFDI " +
                       ",round((round(decode(nvl(PRC.periodicidad,''), 'S', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(4 * 100), " +
                                                                       "'Q', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0) * 15)/(30 * 100), " +
                                                                       "'C', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(2 * 100),  " +
                                                                       "'M', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(100), " +
                                                                       "'',  ''),2)) ,2) " +

                       "ORDER BY CDGCLNS";

                iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

                if (dref.Tables[0].Rows.Count > 0)
                {
                    DataRow dtot = dref.Tables[0].NewRow();
                    dtot["CDGCLNS"] = "-- TOTAL --";
                    dtot["CANTIDAD_ENTREGADA"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(TOTAL_A_PAGAR)", ""));
                    dtot["TOTAL_A_PAGAR"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(TOTAL_A_PAGAR)", ""));
                    dtot["DEV_DEL_MES"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_DEL_MES)", ""));
                    dtot["DEV_MES_SIN_IVA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_MES_SIN_IVA)", ""));
                    dtot["IVA_INT_MES"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(IVA_INT_MES)", ""));
                    dref.Tables[0].Rows.Add(dtot);
                }

            }
            else if (tipo == "2")
            {
                query = "SELECT D.CDGCLNS " +
                       ",NS.NOMBRE " +
                       ",D.CLNS " +
                       ",D.CICLO " +
                       ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",PRN.CANTENTRE CANTIDAD_ENTREGADA " +
                       ",round((round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                       "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                       "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100), " +
                       "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                       "'',  ''),2)) ,2) AS TOTAL_A_PAGAR " +
                       ",SUM(D.DEV_DIARIO) DEV_ACUMULADO, SUM(D.DEV_DIARIO_SIN_IVA) DEV_ACUMULADO_SIN_IVA, SUM(D.IVA_INT) IVA_INT_ACUMULADO, COUNT(*) DIAS_DEV " +
                       ",ROUND(CD.INTERES_PAGADO,2) INT_PAGADO_ACUM " +
                       ",TO_CHAR(CD.FECHA_LIQUIDA,'DD/MM/YYYY') FFIN_REAL " +
                       ",TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO),'DD/MM/YYYY') FIN_TEO " +
                       ",ROUND(PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO),2) INT_NO_DEV_PAG_ANT " +
                       ",ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO)) - ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO))/(((CF.IVA)/100)+1),2),2) IVA_INT_NO_DEV " +
                       ",ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO))/(((CF.IVA)/100)+1),2) INT_NO_DEV_SIN_IVA " +
                       ",PRN.CDGOCPE COD_ASESOR " +
                       ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR " +
                       ",CF.IVA " +
                       "FROM DEVENGO_DIARIO D, NS, PRN, TBL_CIERRE_DIA CD, PE, " +
                       "(SELECT CDGNS, CICLO, CASE WHEN INICIO < TRUNC(TO_DATE('01/01/2014','DD/MM/YYYY')) AND CDGCO = '001' AND TO_DATE('" + fechaFin + "', 'DD/MM/YYYY') > TRUNC(TO_DATE('31/12/2013','DD/MM/YYYY')) THEN " +
                       "16 " +
                       "ELSE " +
                       "(SELECT CF.IVA " +
                       "FROM CF " +
                       "WHERE PRN.CDGEM = CF.CDGEM " +
                       "AND PRN.CDGFDI = CF.CDGFDI) " +
                       "END IVA FROM PRN WHERE CDGEM = PRN.CDGEM AND CDGNS = PRN.CDGNS AND CICLO = PRN.CICLO) CF " +
                       "WHERE D.CDGEM = PRN.CDGEM " +
                       "AND D.CDGCLNS = PRN.CDGNS " +
                       "AND D.CICLO = PRN.CICLO " +
                       "AND PRN.CDGEM = NS.CDGEM " +
                       "AND PRN.CDGNS = NS.CODIGO " +
                       "AND PE.CDGEM = PRN.CDGEM " +
                       "AND PE.CODIGO = PRN.CDGOCPE " +
                       "AND D.CDGEM = NS.CDGEM " +
                       "AND D.CDGCLNS = NS.CODIGO " +
                       "AND D.CDGEM = '" + empresa + "' " +
                       "AND CD.CDGEM = D.CDGEM " +
                       "AND CD.CDGCLNS = D.CDGCLNS " +
                       "AND CD.CICLO = D.CICLO " +
                       "AND CD.CLNS = D.CLNS " +
                       "AND CD.FECHA_LIQUIDA BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND D.CLNS = 'G' " +
                       "AND CD.FECHA_LIQUIDA < FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO) " +
                       "AND TRUNC(D.FECHA_CALC) <= '" + fechaFin + "' " +
                       "AND PRN.SITUACION = 'L' " +
                       "AND CF.CDGNS = PRN.CDGNS " +
                       "AND CF.CICLO = PRN.CICLO " +
                       "GROUP BY D.CDGEM, D.CDGCLNS, NS.NOMBRE, D.CLNS, D.CICLO,PRN.INICIO,PRN.CANTENTRE " +
                       ",round((round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                       "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                       "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100), " +
                       "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                       "'',  ''),2)) ,2) " +
                       ",CD.INTERES_PAGADO " +
                       ",CD.INTERES_GLOBAL " +
                       ",CD.FECHA_LIQUIDA " +
                       ",FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO) " +
                       ",PRN.CDGOCPE " +
                       ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       ",CF.IVA " +
                       //UNION CON CREDITOS INDIVIDUALES
                       "UNION " +
                       "SELECT D.CDGCLNS " +
                       ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE " +
                       ",D.CLNS " +
                       ",D.CICLO " +
                       ",TO_CHAR(PRC.INICIO,'DD/MM/YYYY') FINICIO " +
                       ",PRC.CANTENTRE CANTIDAD_ENTREGADA " +
                       ",round((round(decode(nvl(PRC.periodicidad,''), 'S', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(4 * 100), " +
                       "'Q', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0) * 15)/(30 * 100), " +
                       "'C', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(2 * 100), " +
                       "'M', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(100), " +
                       "'',  ''),2)) ,2) AS TOTAL_A_PAGAR " +
                       ",SUM(D.DEV_DIARIO) DEV_ACUMULADO, SUM(D.DEV_DIARIO_SIN_IVA) DEV_ACUMULADO_SIN_IVA, SUM(D.IVA_INT) IVA_INT_ACUMULADO, COUNT(*) DIAS_DEV " +
                       ",ROUND(CD.INTERES_PAGADO,2) INT_PAGADO_ACUM " +
                       ",TO_CHAR(CD.FECHA_LIQUIDA,'DD/MM/YYYY') FFIN_REAL " +
                       ",TO_CHAR(FNFECHAPROXPAGO(PRC.INICIO,PRC.PERIODICIDAD,PRC.PLAZO),'DD/MM/YYYY') FIN_TEO " +
                       ",ROUND(PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO),2) INT_NO_DEV_PAG_ANT " +
                       ",ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO)) - ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO))/(((CF.IVA)/100)+1),2),2) IVA_INT_NO_DEV " +
                       ",ROUND((PAGADOINTERESTOTAL(D.CDGEM, D.CDGCLNS, D.CICLO, D.CLNS) - SUM(D.DEV_DIARIO))/(((CF.IVA)/100)+1),2) INT_NO_DEV_SIN_IVA " +
                       ",PRC.CDGOCPE COD_ASESOR " +
                       ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR " +
                       ",CF.IVA " +
                       "FROM DEVENGO_DIARIO D, CL, PRC, TBL_CIERRE_DIA CD, PE, " +
                       "(SELECT CDGCLNS, CICLO, CLNS, CASE WHEN INICIO < TRUNC(TO_DATE('01/01/2014','DD/MM/YYYY')) AND CDGCO = '001' AND TO_DATE('" + fechaFin + "', 'DD/MM/YYYY') > TRUNC(TO_DATE('31/12/2013','DD/MM/YYYY')) THEN " +
                       "16 " +
                       "ELSE " +
                       "(SELECT CF.IVA " +
                       "FROM CF " +
                       "WHERE PRC.CDGEM = CF.CDGEM " +
                       "AND PRC.CDGFDI = CF.CDGFDI) " +
                       "END IVA FROM PRC WHERE CDGEM = PRC.CDGEM AND CDGCLNS = PRC.CDGCLNS AND CICLO = PRC.CICLO AND CLNS = PRC.CLNS) CF " +
                       "WHERE D.CDGEM = PRC.CDGEM " +
                       "AND D.CDGCLNS = PRC.CDGCLNS " +
                       "AND D.CICLO = PRC.CICLO " +
                       "AND PRC.CDGEM = CL.CDGEM " +
                       "AND PRC.CDGCL = CL.CODIGO " +
                       "AND PE.CDGEM = PRC.CDGEM " +
                       "AND PE.CODIGO = PRC.CDGOCPE " +
                       "AND D.CDGEM = CL.CDGEM " +
                       "AND D.CDGCLNS = CL.CODIGO " +
                       "AND D.CDGEM = '" + empresa + "' " +
                       "AND CD.CDGEM = D.CDGEM " +
                       "AND CD.CDGCLNS = D.CDGCLNS " +
                       "AND CD.CICLO = D.CICLO " +
                       "AND CD.CLNS = D.CLNS " +
                       "AND CD.FECHA_LIQUIDA BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND CD.FECHA_LIQUIDA < FNFECHAPROXPAGO(PRC.INICIO,PRC.PERIODICIDAD,PRC.PLAZO) " +
                       "AND TRUNC(D.FECHA_CALC) <= '" + fechaFin + "' " +
                       "AND D.CLNS = 'I' " +
                       "AND PRC.SITUACION = 'L' " +
                       "AND CF.CDGCLNS = PRC.CDGCLNS " +
                       "AND CF.CICLO = PRC.CICLO " +
                       "AND CF.CLNS = PRC.CLNS " +
                       "GROUP BY D.CDGEM, D.CDGCLNS, NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL), D.CLNS, D.CICLO,PRC.INICIO,PRC.CANTENTRE " +
                       ", round((round(decode(nvl(PRC.periodicidad,''), 'S', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(4 * 100), " +
                       "'Q', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0) * 15)/(30 * 100), " +
                       "'C', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(2 * 100), " +
                       "'M', nvl(PRC.cantentre,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(PRC.cantentre,0))/(100), " +
                       "'',  ''),2)) ,2) " +
                       ",CD.INTERES_PAGADO " +
                       ",CD.INTERES_GLOBAL " + 
                       ",CD.FECHA_LIQUIDA " +
                       ",FNFECHAPROXPAGO(PRC.INICIO,PRC.PERIODICIDAD,PRC.PLAZO) " +
                       ",PRC.CDGOCPE " +
                       ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                       ",CF.IVA " + 
                       "ORDER BY FFIN_REAL";

                iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

                if (dref.Tables[0].Rows.Count > 0)
                {
                    DataRow dtot = dref.Tables[0].NewRow();
                    dtot["CDGCLNS"] = "-- TOTAL --";
                    dtot["CANTIDAD_ENTREGADA"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(TOTAL_A_PAGAR)", "")); ;
                    dtot["TOTAL_A_PAGAR"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(TOTAL_A_PAGAR)", ""));
                    dtot["DEV_ACUMULADO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_ACUMULADO)", ""));
                    dtot["DEV_ACUMULADO_SIN_IVA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_ACUMULADO_SIN_IVA)", ""));
                    dtot["IVA_INT_ACUMULADO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(IVA_INT_ACUMULADO)", ""));
                    dtot["INT_PAGADO_ACUM"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(INT_PAGADO_ACUM)", ""));
                    dtot["INT_NO_DEV_PAG_ANT"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(INT_NO_DEV_PAG_ANT)", ""));
                    dtot["INT_NO_DEV_SIN_IVA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(INT_NO_DEV_SIN_IVA)", ""));
                    dtot["IVA_INT_NO_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(IVA_INT_NO_DEV)", ""));
                    dref.Tables[0].Rows.Add(dtot);
                }
            }
            //INTERES DEVENGADO ACUMULADO POR FONDEADOR
            else if (tipo == "3")
            {
                query = "SELECT PRN.CDGCO " +
                        ",F.CDGORF " +
                        ",NOMBREC(NULL,NULL,'I','N',ORF.NOMBRE1,ORF.NOMBRE2,ORF.PRIMAPE,ORF.SEGAPE) NOMORF " +
                        ",F.CDGLC " +
                        ",LC.DESCRIPCION DESCLC " +
                        ",F.CDGDISP " +
                        ",DISP.DESCRIPCION DESCDISP " +
                        ",F.CDGNS COD_GPO " +
                        ",NS.NOMBRE GRUPO " +
                        ",F.CICLO " +
                        ",PRN.CDGOCPE ASESOR " +
                        ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR " +
                        ",F.CDGCL " +
                        ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMCL " +
                        ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FINICIO " +
                        ",F.CANTIDAD CANTIDAD_ENTREGADA " +
                        ",round((round(decode(nvl(PRN.periodicidad,''), 'S', nvl(F.CANTIDAD,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(F.CANTIDAD,0))/(4 * 100), " +
                                                 "'Q', nvl(F.CANTIDAD,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(F.CANTIDAD,0) * 15)/(30 * 100), " +
                                                 "'C', nvl(F.CANTIDAD,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(F.CANTIDAD,0))/(2 * 100), " +
                                                 "'M', nvl(F.CANTIDAD,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(F.CANTIDAD,0))/(100), " +
                                                 "'',  ''),2)) ,2) AS TOTAL_A_PAGAR " +
                        ",ROUND ((F.CANTIDAD / PRN.CANTENTRE) * A.DEV_DIARIO_ACUM, 2) DEV_PERIODO " +
                        ",ROUND ((F.CANTIDAD / PRN.CANTENTRE) * A.DEV_DIARIO_SIN_IVA_ACUM, 2) DEV_PERIODO_SIN_IVA " +
                        ",ROUND ((F.CANTIDAD / PRN.CANTENTRE) * A.IVA_INT_ACUM, 2) IVA_INTERES " +
                        ",CF.IVA PORC_IVA " +
                        ",DIAS_DEV " +
                        "FROM PRN, CF,NS,PE,CL,ORF,LC,DISPOSICION DISP,PRC_FONDEO_FINAL F, (SELECT CDGEM " +
                                                                                     ",CDGCLNS " +
                                                                                     ",CICLO " +
                                                                                     ",SUM(DEV_DIARIO) DEV_DIARIO_ACUM " +
                                                                                     ",SUM(DEV_DIARIO_SIN_IVA) DEV_DIARIO_SIN_IVA_ACUM " +
                                                                                     ",SUM(IVA_INT) IVA_INT_ACUM " +
                                                                                     ",MAX(DIAS_DEV) DIAS_DEV " +
                                                                                     "FROM DEVENGO_DIARIO D " +
                                                                                     "WHERE D.CDGEM = '" + empresa + "' " +
                                                                                     "AND D.CDGCLNS || D.CICLO IN (SELECT CDGNS || CICLO FROM PRC_FONDEO_FINAL WHERE CDGEM = '" + empresa + "' AND CLNS = 'G') " +
                                                                                     "AND D.CLNS = 'G' " +
                                                                                     "AND D.FECHA_CALC <= '" + fechaIni + "' " +
                                                                                     "GROUP BY CDGEM,CDGCLNS,CICLO) A " +
                        "WHERE F.CDGEM = PRN.CDGEM " +
                        "AND F.CDGNS = PRN.CDGNS " +
                        "AND F.CICLO = PRN.CICLO " +
                        "AND F.CDGEM = A.CDGEM " +
                        "AND F.CDGNS = A.CDGCLNS " +
                        "AND F.CICLO = A.CICLO " +
                        "AND F.FREPSDO = (SELECT MAX(FREPSDO) FROM PRC_FONDEO_FINAL WHERE CDGEM = F.CDGEM AND CDGNS = F.CDGNS AND CICLO = F.CICLO AND CDGCL = F.CDGCL AND FREPSDO <= '" + fechaIni + "') " +
                        "AND PRN.CDGEM = A.CDGEM " +
                        "AND PRN.CDGNS = A.CDGCLNS " +
                        "AND PRN.CICLO = A.CICLO " +
                        "AND PRN.CDGEM = CF.CDGEM " +
                        "AND PRN.CDGFDI = CF.CDGFDI " +
                        "AND PRN.CDGEM = NS.CDGEM " +
                        "AND PRN.CDGNS = NS.CODIGO " +
                        "AND PRN.CDGEM = PE.CDGEM " +
                        "AND PRN.CDGOCPE = PE.CODIGO " +
                        "AND F.CDGEM = CL.CDGEM " +
                        "AND F.CDGCL = CL.CODIGO " +
                        "AND F.CDGEM = '" + empresa + "' " +
                        "AND ORF.CDGEM = F.CDGEM " +
                        "AND ORF.CODIGO = F.CDGORF " +
                        "AND LC.CDGEM = F.CDGEM " +
                        "AND LC.CDGORF = F.CDGORF " +
                        "AND LC.CODIGO = F.CDGLC " +
                        "AND DISP.CDGEM = F.CDGEM " +
                        "AND DISP.CDGORF = F.CDGORF " +
                        "AND DISP.CDGLC = F.CDGLC " +
                        "AND DISP.CODIGO = F.CDGDISP " +
                    //UNION CON CREDITOS INDIVIDUALES
                        "UNION " +
                        "SELECT PRC.CDGCO " +
                        ",F.CDGORF " +
                        ",NOMBREC(NULL,NULL,'I','N',ORF.NOMBRE1,ORF.NOMBRE2,ORF.PRIMAPE,ORF.SEGAPE) NOMORF " +
                        ",F.CDGLC " +
                        ",LC.DESCRIPCION DESCLC " +
                        ",F.CDGDISP " +
                        ",DISP.DESCRIPCION DESCDISP " +
                        ",NULL COD_GPO " +
                        ",NULL GRUPO " +
                        ",F.CICLO " +
                        ",PRC.CDGOCPE ASESOR " +
                        ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR " +
                        ",F.CDGCL " +
                        ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMCL " +
                        ",TO_CHAR(PRC.INICIO,'DD/MM/YYYY') FINICIO " +
                        ",F.CANTIDAD CANTIDAD_ENTREGADA " +
                        ",round((round(decode(nvl(PRC.periodicidad,''), 'S', nvl(F.CANTIDAD,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(F.CANTIDAD,0))/(4 * 100), " +
                                                 "'Q', nvl(F.CANTIDAD,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(F.CANTIDAD,0) * 15)/(30 * 100), " +
                                                 "'C', nvl(F.CANTIDAD,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(F.CANTIDAD,0))/(2 * 100), " +
                                                 "'M', nvl(F.CANTIDAD,0) + (nvl(PRC.tasa,0) * nvl(PRC.plazo,0) * nvl(F.CANTIDAD,0))/(100), " +
                                                 "'',  ''),2)) ,2) AS TOTAL_A_PAGAR " +
                        ",ROUND ((F.CANTIDAD / PRC.CANTENTRE) * A.DEV_DIARIO_ACUM, 2) DEV_PERIODO " +
                        ",ROUND ((F.CANTIDAD / PRC.CANTENTRE) * A.DEV_DIARIO_SIN_IVA_ACUM, 2) DEV_PERIODO_SIN_IVA " +
                        ",ROUND ((F.CANTIDAD / PRC.CANTENTRE) * A.IVA_INT_ACUM, 2) IVA_INTERES " +
                        ",CF.IVA PORC_IVA " +
                        ",DIAS_DEV " +
                        "FROM PRC, CF,PE,CL,ORF,LC,DISPOSICION DISP,PRC_FONDEO_FINAL F, (SELECT CDGEM " +
                                                                                     ",CDGCLNS " +
                                                                                     ",CICLO " +
                                                                                     ",SUM(DEV_DIARIO) DEV_DIARIO_ACUM " +
                                                                                     ",SUM(DEV_DIARIO_SIN_IVA) DEV_DIARIO_SIN_IVA_ACUM " +
                                                                                     ",SUM(IVA_INT) IVA_INT_ACUM " +
                                                                                     ",MAX(DIAS_DEV) DIAS_DEV " +
                                                                                     "FROM DEVENGO_DIARIO D " +
                                                                                     "WHERE D.CDGEM = '" + empresa + "' " +
                                                                                     "AND D.CDGCLNS || D.CICLO IN (SELECT CDGCLNS || CICLO FROM PRC_FONDEO_FINAL WHERE CDGEM = '" + empresa + "' AND CLNS = 'I') " +
                                                                                     "AND D.CLNS = 'I' " +
                                                                                     "AND D.FECHA_CALC <= '" + fechaIni + "' " +
                                                                                     "GROUP BY CDGEM,CDGCLNS,CICLO) A " +
                        "WHERE F.CDGEM = PRC.CDGEM " +
                        "AND F.CDGCLNS = PRC.CDGCLNS " +
                        "AND F.CICLO = PRC.CICLO " +
                        "AND F.CDGEM = A.CDGEM " +
                        "AND F.CDGCLNS = A.CDGCLNS " +
                        "AND F.CICLO = A.CICLO " +
                        "AND F.FREPSDO = (SELECT MAX(FREPSDO) FROM PRC_FONDEO_FINAL WHERE CDGEM = F.CDGEM AND CDGCLNS = F.CDGCLNS AND CLNS = F.CLNS AND CICLO = F.CICLO AND CDGCL = F.CDGCL AND FREPSDO <= '" + fechaIni + "') " +
                        "AND PRC.CDGEM = A.CDGEM " +
                        "AND PRC.CDGCLNS = A.CDGCLNS " +
                        "AND PRC.CLNS = 'I' " +
                        "AND PRC.CICLO = A.CICLO " +
                        "AND PRC.CDGEM = CF.CDGEM " +
                        "AND PRC.CDGFDI = CF.CDGFDI " +
                        "AND PRC.CDGEM = PE.CDGEM " +
                        "AND PRC.CDGOCPE = PE.CODIGO " +
                        "AND F.CDGEM = CL.CDGEM " +
                        "AND F.CDGCL = CL.CODIGO " +
                        "AND F.CDGEM = '" + empresa + "' " +
                        "AND ORF.CDGEM = F.CDGEM " +
                        "AND ORF.CODIGO = F.CDGORF " +
                        "AND LC.CDGEM = F.CDGEM " +
                        "AND LC.CDGORF = F.CDGORF " +
                        "AND LC.CODIGO = F.CDGLC " +
                        "AND DISP.CDGEM = F.CDGEM " +
                        "AND DISP.CDGORF = F.CDGORF " +
                        "AND DISP.CDGLC = F.CDGLC " +
                        "AND DISP.CODIGO = F.CDGDISP " +
                        "ORDER BY CDGCO";

                iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

                if (dref.Tables[0].Rows.Count > 0)
                {
                    DataRow dtot = dref.Tables[0].NewRow();
                    dtot["CDGORF"] = "-- TOTAL --";
                    dtot["CANTIDAD_ENTREGADA"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(TOTAL_A_PAGAR)", ""));
                    dtot["TOTAL_A_PAGAR"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(TOTAL_A_PAGAR)", ""));
                    dtot["DEV_PERIODO"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_PERIODO)", ""));
                    dtot["DEV_PERIODO_SIN_IVA"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(DEV_PERIODO_SIN_IVA)", ""));
                    dtot["IVA_INTERES"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(IVA_INTERES)", ""));
                    dref.Tables[0].Rows.Add(dtot);
                }
            }

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA DE MANERA GENERAL A LOS KILOMETROS RECORRIDOS DE LOS VEHICULOS ASIGNADOS Y NO ASIGNADOS
    [WebMethod]
    public string getRepKilometraje(string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        try
        {
            string query = "SELECT RG.NOMBRE REGION, " +
                           "CO.NOMBRE SUCURSAL, " +
                           "TRV.MARCA, " +
                           "TRV.MODELO, " +
                           "TRV.SERIE,  " +
                           "(SELECT NOMBREC(NULL, NULL, 'I', 'A', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) FROM PE WHERE PE.CDGEM = TRV.CDGEM AND PE.CODIGO = TRV.CDGPE) ASESOR, " +
                           "PE.TELEFONO NOMINA, " +
                           "(SELECT NVL(MAX(KILOMETRAJE), 0) FROM TBL_SEG_VEHICULO WHERE CDGEM = TRV.CDGEM AND FECHA <= '" + fecha + "' AND CDGVEH = TRV.CODIGO) KILOMETRAJE " +
                           "FROM TBL_REG_VEHICULO TRV " +
                           "LEFT JOIN RG ON TRV.CDGRG = RG.CODIGO  " +
                           "AND TRV.CDGEM = RG.CDGEM " +
                           "LEFT JOIN CO ON TRV.CDGCO = CO.CODIGO  " +
                           "AND TRV.CDGEM = CO.CDGEM " +
                           "LEFT JOIN PE ON TRV.CDGPE = PE.CODIGO  " +
                           "AND TRV.CDGEM = PE.CDGEM " +
                           "WHERE TRV.CDGEM = '" + empresa + "' " +
                           "ORDER BY RG.NOMBRE, CO.NOMBRE, KILOMETRAJE ";

            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO QUE CONSULTA LAS LIQUIDACONES CON GARANTIA
    [WebMethod]
    public string getRepLiquidacionGarantia(string fechaI, string fechaF)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT MP.CDGCLNS " +
                       ",NS.NOMBRE GRUPO " +
                       ",MP.CICLO " +
                       ",TO_CHAR(TCD.FECHA_LIQUIDA, 'DD/MM/YYYY') FECHA_LIQUIDA " +
                       ",MP.CANTIDAD MONTO_LIQUIDA " +
                       ",TCD.SDO_TOTAL " +
                       ",NVL(FNSDOGARANTIA(MP.CDGEM, MP.CDGCLNS, MP.CICLO, MP.CLNS, TO_DATE('" + fechaF + "')), 0) SALDO_GL " +
                       "FROM MP " +
                       "JOIN TBL_CIERRE_DIA TCD ON MP.CDGEM = TCD.CDGEM AND MP.CDGCLNS = TCD.CDGCLNS AND MP.CICLO = TCD.CICLO AND MP.FREALDEP = TCD.FECHA_LIQUIDA " +
                       "JOIN BITACORA_OPERACION BO ON MP.CDGEM = BO.CDGEM AND MP.CDGCLNS = BO.CDGNS AND MP.CICLO = BO.CICLO AND TRUNC(FREGISTRO) = MP.FREALDEP " +
                       "JOIN NS ON MP.CDGEM = NS.CDGEM AND MP.CDGCLNS = NS.CODIGO " +
                       "WHERE MP.CDGEM = '" + empresa + "' " +
                       "AND MP.CDGCB = '12' " +
                       "AND MP.FREALDEP BETWEEN TO_DATE('" + fechaI + "') AND TO_DATE('" + fechaF + "') " +
                       "AND TCD.SITUACION = 'L' " +
                       "AND BO.CDGACT = '019' " +
                       "ORDER BY TCD.FECHA_LIQUIDA ";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE GENERA INFORMACION PARA EL REPORTE DE LISTYA NEGRA
    [WebMethod]
    public string getRepListaNegra(string fecha, string fechaFin, string region, string sucursal, string asesor)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        int iRes;
        string xml = "";
        string queryAse = string.Empty;
        string querySuc = string.Empty;
        string queryReg = string.Empty;

        if (asesor != null && asesor != string.Empty)
        {
            queryAse = " AND SN.CDGOCPE = '" + asesor + "' ";
        }

        if (region != null && region != string.Empty)
        {
            queryReg = " AND RG.CODIGO = '" + region + "' ";
        }

        if (sucursal != null && sucursal != string.Empty)
        {
            querySuc = " AND CO.CODIGO = '" + sucursal + "' ";
        }

        string query = "SELECT NOMBREC(SC.CDGEM, SC.CDGCL, 'I', 'A', NULL, NULL, NULL, NULL) CLIENTE, "
                         + " SC.CDGCL, NS.NOMBRE NOM_GRUPO, SC.CDGNS, SC.CICLO, TO_CHAR(CM.ALTA,'DD/MM/YYYY') ALTA, "
                         + " NOMBREC(NULL,NULL,'I','A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOM_ASESOR, "
                         + " PE.TELEFONO NUM_NOMINA, CO.NOMBRE SUCURSAL, RG.NOMBRE ZONA, UPPER( CAT.DESCRIPCION) DESCRIPCION "
                         + " FROM CL_MARCA CM "
                         + " INNER JOIN SC "
                            + " ON  SC.CDGEM = CM.CDGEM "
                            + " AND SC.CDGCL = CM.CDGCL  "
                            + " AND SC.SOLICITUD = (SELECT MAX(SOLICITUD) FROM SC WHERE CDGEM = CM.CDGEM  "
                                                + " AND TRUNC(SOLICITUD) <= CM.ALTA AND CDGCL = CM.CDGCL ) "
                         + " INNER JOIN SN  "
                            + " ON  SN.CDGEM = SC.CDGEM "
                            + " AND SN.CDGNS = SC.CDGNS "
                            + " AND SN.CICLO = SC.CICLO "
                          + " INNER JOIN PE "
                            + " ON  PE.CDGEM = SC.CDGEM "
                            + " AND SN.CDGOCPE = PE.CODIGO "
                            + queryAse
                         + " INNER JOIN CO "
                            + " ON  CO.CDGEM = SC.CDGEM "
                            + " AND CO.CODIGO = PE.CDGCO  "
                            + querySuc
                          + " INNER JOIN RG "
                            + " ON  RG.CDGEM = SC.CDGEM "
                            + " AND RG.CODIGO = CO.CDGRG "
                            + queryReg
                         + " INNER JOIN NS "
                            + " ON  NS.CDGEM = SC.CDGEM "
                            + " AND NS.CODIGO = SN.CDGNS "
                         + " INNER JOIN CAT_CAUSA_LISTA_NEGRA CAT "
                            + " ON  CAT.CDGEM = SC.CDGEM "
                            + " AND CAT.CODIGO = CM.CAUSA "
                         + " WHERE  "
                            + " CM.CDGEM='" + empresa + "' "
                            + " AND CM.TIPOMARCA ='LN' "
                            + " AND CM.ALTA BETWEEN '" + fecha + "' AND '" + fechaFin + "'"
                            + " ORDER BY RG.NOMBRE, CO.NOMBRE , CLIENTE ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA EL RESULTADO DEL PROCESO DE CARGA DE ARCHIVO DE LISTA NEGRA
    [WebMethod]
    public string getRepMarcaArchivo(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CMA.* " +
                       "FROM CL_MARCA_ARCHIVO CMA " +
                       "WHERE CMA.CDGEM = '" + empresa + "' " +
                       "AND CMA.CDGPE = '" + usuario + "'";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS REPORTES DEL MARCADO DE FINANCIERA NACIONAL
    [WebMethod]
    public string getRepMarcadoBajio(string fecha, string cdgorg, string cdglc, string cdgdisp, int opcion, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        if (opcion == 1) // Acreditados Finales
        {
            query = "SELECT CASE WHEN FF.ID_PERSONA IS NULL THEN 'NUEVO' " +
                "                ELSE 'ACTUALIZACIÓN' END ESTATUS " +
                "         , FF.ID_PERSONA " +
                "         , FF.NO_CRED_BCO " +
                "         , TO_CHAR(FF.FOPERACION, 'DD/MM/YYYY') FOPERACION " +
                "         , 'FISICA' TIPO_PERSONA " +
                "         , CL.CURP " +
                "         , CL.RFC " +
                "         , CASE WHEN CL.NOMBRE2 IS NULL THEN CL.NOMBRE1 " +
                "                ELSE CL.NOMBRE1 || ' ' || CL.NOMBRE2 END NOMBRE " +
                "         , CL.PRIMAPE " +
                "         , CL.SEGAPE " +
                "         , PF.CANTIDAD MONTO_CREDITO " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FOTORGAMIENTO " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FVENCIMIENTO " +
                "         , PF.SDO_CAPITAL " +
                "         , 'CUENTA CORRIENTE' TIPO_CREDITO " +
                "         , N.CDGEF_INEGI " +
                "         , N.CDGMU_INEGI " +
                "         , N.CDGLO_INEGI " +
                "         , 'EFI110315NC8' CVE_DISPERSORA " +
                "         , 'EMPRENDAMOS FIN, S.A. DE C.V., SOFOM, ENR' RAZON_SOCIAL " +
                "         , NVL(ISS.ACT_INEGI, 'Comercio al por menor de otros alimentos') PROD_CULT " +
                "      FROM PRC_FONDEO PF " +
                "      JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "      JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "      JOIN PRC ON PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PF.CDGCL = PRC.CDGCL " +
                "      JOIN SC ON PRC.CDGEM = SC.CDGEM AND PRC.CDGNS = SC.CDGNS AND PRC.CLNS = SC.CLNS AND PRC.CICLO = SC.CICLO AND PRC.CDGCL = SC.CDGCL " +
                " LEFT JOIN PI ON PI.CDGEM = SC.CDGEM AND PI.CDGCL = SC.CDGCL AND PI.PROYECTO = SC.CDGPI " +
                " LEFT JOIN AE ON AE.CDGEM = PI.CDGEM AND AE.CDGSE = PI.CDGSE AND AE.CDGGI = PI.CDGGI AND AE.CODIGO = PI.CDGAE " +
                " LEFT JOIN GI ON GI.CDGEM = AE.CDGEM AND GI.CDGSE = AE.CDGSE AND GI.CODIGO = AE.CDGGI " +
                " LEFT JOIN INEGI_SUBRAMAS ISS ON GI.CDGEM = ISS.CDGEM AND GI.NOMBRE = ISS.ACT_SEPOMEX " +
                "      JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                " LEFT JOIN FONDEO_FIRA_BJ_PASO FF ON CL.CDGEM = FF.CDGEM AND CL.CURP = FF.CURP AND CL.RFC = FF.RFC AND PRN.INICIO = FF.INICIO " +
                "                                 AND PF.CDGORF = FF.CDGORF AND PF.CDGLC = FF.CDGLC AND PF.CDGDISP = FF.CDGDISP " +
                "     WHERE PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "  ORDER BY CURP ";
        }
        else if (opcion == 2) // Relación Contrato
        {
            query = "SELECT ROWNUM NO, ID_PERSONA, CURP, RFC, NOMBRE, PRIMAPE, SEGAPE, CDGLO_INEGI, CDGMU_INEGI, CDGEF_INEGI, NOCONTROL " +
                "         , ESTRATO, PROD_CULT, MONTO_CREDITO, APORTACL, MONTO_TOTAL, FFIRMACONTRATO, FVENCICONTRATO, NOPOLIZA " +
                "      FROM (SELECT FF.ID_PERSONA " +
                "                 , CL.CURP " +
                "                 , CL.RFC " +
                "                 , CASE WHEN CL.NOMBRE2 IS NULL THEN CL.NOMBRE1 " +
                "                        ELSE CL.NOMBRE1 || ' ' || CL.NOMBRE2 END NOMBRE " +
                "                 , CL.PRIMAPE " +
                "                 , CL.SEGAPE " +
                "                 , N.CDGLO_INEGI " +
                "                 , N.CDGMU_INEGI " +
                "                 , N.CDGEF_INEGI " +
                "                 , '' NOCONTROL " +
                "                 , 'PD3' ESTRATO " +
                "                 , NVL(ISS.ACT_INEGI, 'Comercio al por menor de otros alimentos') PROD_CULT " +
                "                 , PF.CANTIDAD MONTO_CREDITO " +
                "                 , 0 APORTACL " +
                "                 , PF.CANTIDAD MONTO_TOTAL " +
                "                 , '' FFIRMACONTRATO " +
                "                 , '' FVENCICONTRATO " +
                "                 , 'NO APLICA' NOPOLIZA " +
                "              FROM PRC_FONDEO PF " +
                "              JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "              JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "              JOIN PRC ON PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PF.CDGCL = PRC.CDGCL " +
                "              JOIN SC ON PRC.CDGEM = SC.CDGEM AND PRC.CDGNS = SC.CDGNS AND PRC.CLNS = SC.CLNS AND PRC.CICLO = SC.CICLO AND PRC.CDGCL = SC.CDGCL " +
                "         LEFT JOIN PI ON PI.CDGEM = SC.CDGEM AND PI.CDGCL = SC.CDGCL AND PI.PROYECTO = SC.CDGPI " +
                "         LEFT JOIN AE ON AE.CDGEM = PI.CDGEM AND AE.CDGSE = PI.CDGSE AND AE.CDGGI = PI.CDGGI AND AE.CODIGO = PI.CDGAE " +
                "         LEFT JOIN GI ON GI.CDGEM = AE.CDGEM AND GI.CDGSE = AE.CDGSE AND GI.CODIGO = AE.CDGGI " +
                "         LEFT JOIN INEGI_SUBRAMAS ISS ON GI.CDGEM = ISS.CDGEM AND GI.NOMBRE = ISS.ACT_SEPOMEX " +
                "              JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                "         LEFT JOIN FONDEO_FIRA_BJ_PASO FF ON CL.CDGEM = FF.CDGEM AND CL.CURP = FF.CURP AND CL.RFC = FF.RFC AND PRN.INICIO = FF.INICIO " +
                "                                         AND PF.CDGORF = FF.CDGORF AND PF.CDGLC = FF.CDGLC AND PF.CDGDISP = FF.CDGDISP " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '" + cdgorg + "' " +
                "               AND PF.CDGLC = '" + cdglc + "' " +
                "               AND PF.CDGDISP = '" + cdgdisp + "' " +
                "               AND PF.FREPSDO = '" + fecha + "' " +
                "          ORDER BY CURP ) ";
        }

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS REPORTES DEL MARCADO DE BANKAOOL
    [WebMethod]
    public string getRepMarcadoBankaool(string fecha, string cdgorg, string cdglc
        , string cdgdisp, int opcion, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        if (opcion == 1) // Registro Microcrédito
        {
            query = "SELECT FF.ID_PERSONA " +
                "         , SUBSTR(CL.CURP, 0, 18) CURP " +
                "         , SUBSTR(CL.RFC, 0, 10) RFC " +
                "         , FF.NO_CRED_BCO " +
                "         , PF.CANTIDAD MONTO_CREDITO " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FOTORGAMIENTO " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FVENCIMIENTO " +
                "         , PF.SDO_CAPITAL " +
                "         , 1 TIPO_CREDITO " +
                "         , N.CDGEF_INEGI " +
                "         , N.CDGMU_INEGI " +
                "         , N.CDGLO_INEGI " +
                "         , 9087284 CVE_DISPERSORA " +
                "         , 'Emprendamos Fin, SA de CV SOFOM ENR' NOM_DISPERSORA " +
                "         , NOMBREC(NULL, NULL, 'I', 'N', CL.NOMBRE1, CL.NOMBRE2, CL.PRIMAPE, CL.SEGAPE) CLIENTE " +
                "      FROM PRC_FONDEO PF " +
                "      JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "      JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "      JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                " LEFT JOIN FONDEO_FIRA_BJ_PASO FF ON CL.CDGEM = FF.CDGEM AND CL.CURP = FF.CURP AND CL.RFC = FF.RFC AND PRN.INICIO = FF.INICIO " +
                "                                 AND PF.CDGORF = FF.CDGORF AND PF.CDGLC = FF.CDGLC AND PF.CDGDISP = FF.CDGDISP " +
                "     WHERE PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "  ORDER BY CURP ";
        }
        else if (opcion == 2) // Base de enedudamiento
        {
            query = "SELECT NOMBREC(NULL, NULL, 'I', 'N', CL.NOMBRE1, CL.NOMBRE2, CL.PRIMAPE, CL.SEGAPE) CLIENTE " +
                "         , PF.CDGCL NO_CONTRATO " +
                "         , 1 TIPO_CONTRATO " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FINICIO " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FVENCIMIENTO " +
                "         , PF.SDO_CAPITAL TOTAL_CXC " +
                "         , SUBSTR(CL.RFC, 0, 10) RFC " +
                "         , SUBSTR(CL.CURP, 0, 18) CURP " +
                "         , PF.SDO_CAPITAL MONTO " +
                "         , 'VIGENTE' ESTATUS " +
                "      FROM PRC_FONDEO PF " +
                "      JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "      JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "     WHERE PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "  ORDER BY CURP ";
        }

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS REPORTES DEL MARCADO DE FINANCIERA NACIONAL
    [WebMethod]
    public string getRepMarcadoBansefi(string fecha, string cdgorg, string cdglc, string cdgdisp, int opcion, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        if (opcion == 1) // Layout Disposición
        {
            query = "SELECT ROWNUM NO, NUM_CL, FECHA_CESION, PRODUCTO, NUM_PAGARE, NOMBRE_CL, TIPO_PERSONA, SUCURSAL, INICIO, FIN, DIAS_ATRASO, MONTO_ORIGINAL " +
                "         , AMORTIZACION_PACT, AMORTIZACION_REAL, AMORTIZACION_PEND, AMORTIZACION_IMPO, TASA, TIPO_TASA, TASA_INT_MOR, FRECU_PAGO " +
                "         , SALDO_FECHA, PAGO_ANT_ACU, MESANIOVENC, TIPO_CARTERA, CLIENTE_SUST " +
                "      FROM ( SELECT PF.CDGCL NUM_CL " +
                "                  , TO_CHAR(FMARCINI, 'DD/MM/YYYY') FECHA_CESION " +
                "                  , CASE WHEN PRN.CDGTPC = '01' THEN 'EMPRENDAMOS CREDITO' " +
                "                         WHEN PRN.CDGTPC = '04' THEN 'CREZCAMOS TU NEGOCIO' " +
                "                         WHEN PRN.CDGTPC = '03' THEN 'OPORTUNO' END PRODUCTO " +
                "                  , PF.CDGCL NUM_PAGARE " +
                "                  , NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) NOMBRE_CL " +
                "                  , 'Física' TIPO_PERSONA " +
                "                  , CD.COD_SUCURSAL || ' ' || CD.NOM_SUCURSAL SUCURSAL " +
                "                  , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "                  , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN " +
                "                  , CASE WHEN (SELECT COUNT(*)  " +
                "                                 FROM TBL_DIAS_MORA " +
                "                                WHERE CDGEM = CD.CDGEM " +
                "                                  AND CDGCLNS = CD.CDGCLNS " +
                "                                  AND CICLO = CD.CICLO " +
                "                                  AND CLNS = CD.CLNS " +
                "                                  AND FECHA_CALC = CD.FECHA_CALC) > 0 THEN (SELECT DIAS_MORA  " +
                "                                                                              FROM TBL_DIAS_MORA " +
                "                                                                             WHERE CDGEM = CD.CDGEM " +
                "                                                                               AND CDGCLNS = CD.CDGCLNS " +
                "                                                                               AND CICLO = CD.CICLO " +
                "                                                                               AND CLNS = CD.CLNS " +
                "                                                                               AND FECHA_CALC = CD.FECHA_CALC) " +
                "                         ELSE CD.DIAS_MORA " +
                "                          END DIAS_ATRASO " +
                "                  , PRC.CANTENTRE MONTO_ORIGINAL " +
                "                  , CD.PLAZO AMORTIZACION_PACT " +
                "                  , FLOOR(CD.PAGOS_REAL / CD.MONTO_CUOTA) AMORTIZACION_REAL " +
                "                  , CD.PLAZO - FLOOR(CD.PAGOS_REAL / CD.MONTO_CUOTA) AMORTIZACION_PEND " +
                "                  , ROUND(((PRC.CANTENTRE / CD.MONTO_ENTREGADO) * CD.MONTO_CUOTA),2) AMORTIZACION_IMPO " +
                "                  , ( SELECT ROUND(PRN.TASA / (1 + (CF.IVA/100)),2) TASA " +
                "                        FROM PRN, CF " +
                "                       WHERE PRN.CDGEM = PF.CDGEM " +
                "                         AND PRN.CDGNS = PF.CDGCLNS " +
                "                         AND PRN.CICLO = PF.CICLO " +
                "                         AND CF.CDGEM = PRN.CDGEM " +
                "                         AND CF.CDGFDI = PRN.CDGFDI ) TASA " +
                "                  , 'Mensual' TIPO_TASA " +
                "                  , 0 TASA_INT_MOR " +
                "                  , CASE WHEN PRN.PERIODICIDAD = 'S' THEN 'SEMANAL' " +
                "                         WHEN PRN.PERIODICIDAD = 'Q' THEN 'QUINCENAL' " +
                "                         WHEN PRN.PERIODICIDAD = 'C' THEN 'BISEMANAL' " +
                "                         WHEN PRN.PERIODICIDAD = 'M' THEN 'MENSUAL' " +
                "                          END FRECU_PAGO " +
                "                  , ROUND(PF.SDO_CAPITAL) SALDO_FECHA " +
                "                  , ROUND(FNPAGOANTACU(PRC.CANTENTRE, CD.MONTO_ENTREGADO, CD.PLAZO, CD.PAGOS_REAL, CD.MONTO_CUOTA, CD.SDO_CAPITAL)) PAGO_ANT_ACU " +
                "                  , LOWER(TO_CHAR('''' || TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'Mon-YY','nls_date_language=spanish'))) MESANIOVENC " +
                "                  , 'SOLIDARIA' TIPO_CARTERA " +
                "                  , CASE WHEN FREPSDO <= FMARCADO THEN 'NUEVO' ELSE 'ACTUALIZACION' END CLIENTE_SUST " +
                "               FROM PRC_FONDEO PF " +
                "               JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "               JOIN PRC ON PF.CDGEM = PRC.CDGEM AND PF.CDGNS = PRC.CDGCLNS AND PF.CICLO = PRC.CICLO AND PF.CDGCL = PRC.CDGCL " +
                "               JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "               JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM AND CD.CDGCLNS = PF.CDGNS AND CD.CICLO = PF.CICLO AND CD.FECHA_CALC = PF.FREPSDO " +
                "     WHERE PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "           ORDER BY PF.CDGCL ) ";
        }
        else if (opcion == 2) // Anexo A
        {
            query = "SELECT ROWNUM NO, NOMBRE_CL, PRODUCTO, MONTO_CREDITO, VALOR_GARANTIA, SALDO_FECHA, NUM_CL, FIN " +
                "      FROM ( SELECT NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) NOMBRE_CL " +
                "                  , CASE WHEN PRN.CDGTPC = '01' THEN 'EMPRENDAMOS CREDITO' " +
                "                         WHEN PRN.CDGTPC = '04' THEN 'CREZCAMOS TU NEGOCIO' " +
                "                         WHEN PRN.CDGTPC = '03' THEN 'OPORTUNO' END PRODUCTO " +
                "                  , PRC.CANTENTRE MONTO_CREDITO " +
                "                  , ROUND(PF.SDO_CAPITAL) VALOR_GARANTIA " +
                "                  , ROUND(PF.SDO_CAPITAL) SALDO_FECHA " +
                "                  , PF.CDGCL NUM_CL " +
                "                  , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN " +
                "               FROM PRC_FONDEO PF " +
                "               JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "               JOIN PRC ON PF.CDGEM = PRC.CDGEM AND PF.CDGNS = PRC.CDGCLNS AND PF.CICLO = PRC.CICLO AND PF.CDGCL = PRC.CDGCL " +
                "               JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "               JOIN TBL_CIERRE_DIA CD ON CD.CDGEM = PF.CDGEM AND CD.CDGCLNS = PF.CDGNS AND CD.CICLO = PF.CICLO AND CD.FECHA_CALC = PF.FREPSDO " +
                "             WHERE PF.CDGEM = '" + empresa + "' " +
                "               AND PF.CDGORF = '" + cdgorg + "' " +
                "               AND PF.CDGLC = '" + cdglc + "' " +
                "               AND PF.CDGDISP = '" + cdgdisp + "' " +
                "               AND PF.FREPSDO = '" + fecha + "' " +
                "           ORDER BY PF.CDGCL ) ";
        }

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS REPORTES DEL MARCADO DE FINANCIERA NACIONAL
    [WebMethod]
    public string getRepMarcadoFN(string fecha, string fechaInicio, string fechaFin, string cdgorg, string cdglc, 
                                  string cdgdisp, int opcion, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        if (opcion == 1) // ClientesDescripcion
        {
            query = "SELECT CL.RFC " +
                "         , CL.CODIGO CDGCL " +
                "         , CASE WHEN CL.NOMBRE2 IS NULL THEN UPPER(CL.NOMBRE1) ELSE UPPER(CL.NOMBRE1 || ' ' || CL.NOMBRE2 ) END NOMBRE " +
                "         , UPPER(CL.PRIMAPE) PRIMAPE " +
                "         , NVL(CL.SEGAPE, 0) SEGAPE " +
                "         , CL.CURP " +
                "         , 'Física' TIPOPER " +
                "         , 1 NOSOCIOS " +
                "         , CASE WHEN CL.SEXO = 'M' THEN 'Hombre' " +
                "                WHEN CL.SEXO = 'F' THEN 'Mujer' " +
                "                ELSE 'No aplica' END SEXO " +
                "         , CASE WHEN UPPER(EF.NOM_ACEN) = 'DISTRITO FEDERAL' THEN 'CIUDAD DE MÉXICO' " +
                "                ELSE UPPER(EF.NOM_ACEN) END EFNAC " +
                "         , TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') FECNAC " +
                "         , CASE WHEN UPPER(ACT.NOM_ENT) = 'DISTRITO FEDERAL' THEN 'CIUDAD DE MÉXICO' " +
                "                ELSE UPPER(ACT.NOM_ENT) END ESTADO " +
                "         , TRANSLATE(UPPER(ACT.NOM_MUN), 'áéíóúÁÉÍÓÚ', 'aeiouAEIOU') MUNICIPIO " +
                "         , TRANSLATE(UPPER(ACT.NOM_LOC), 'áéíóúÁÉÍÓÚ', 'aeiouAEIOU') LOCALIDAD " +
                "         , NVL(CL.TELEFONO, 0) TELEFONO " +
                "         , 0 CELULAR " +
                "         , 'clientes@emprendamosfin.com' EMAIL " +
                "         , UPPER(CL.CALLE) CALLE " +
                "         , 0 NOEXT " +
                "         , N.CDGPOSTAL CP " +
                "         , CASE WHEN ( SELECT COUNT(*) CICLOS " +
                "                         FROM PRC, PRN " +
                "                        WHERE PRC.SITUACION IN( 'E', 'L') " +
                "                          AND PRC.CDGCL = CL.CODIGO " +
                "                          AND PRN.INICIO <= PF.FREPSDO " +
                "                          AND PRC.CDGEM = PRN.CDGEM " +
                "                          AND PRC.CDGNS = PRN.CDGNS " +
                "                          AND PRC.CICLO = PRN.CICLO ) > 1 THEN 'NO' " +
                "                ELSE 'SI' END CREPRI " +
                "      FROM PRC_FONDEO PF " +
                "      JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "      JOIN EF ON CL.CDGPAI = EF.CDGPAI AND CL.NACIOEF = EF.CODIGO " +
                "      JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "      JOIN NEGOCIO N ON CL.CODIGO = N.CDGCL " +
                "      JOIN INEGI_ACT ACT ON N.CDGEF_INEGI = ACT.CVE_ENT AND CDGMU_INEGI = ACT.CVE_MUN AND CDGLO_INEGI = ACT.CVE_LOC " +
                "     WHERE PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "       AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "  ORDER BY PF.CDGCL";
        }
        else if (opcion == 2) // CargaCreditos
        {
            query = "SELECT '''' || D.CONTRATO NO_DISP " +
                "         , CL.RFC " +
                "         , CL.CODIGO NO_CONTRATO " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN_CONTRATO " +
                "         , 'SIMPLE' TIPO_CRED " +
                "         , (SELECT ISS.SUBRAMA " +
                "              FROM GI " +
                "              JOIN INEGI_SUBRAMAS ISS ON GI.NOMBRE = ISS.ACT_SEPOMEX " +
                "             WHERE GI.CDGEM = AE.CDGEM " +
                "               AND GI.CDGSE = AE.CDGSE " +
                "               AND GI.CODIGO = AE.CDGGI) SUBRAMAS " +
                "         , (SELECT ISS.ACT_INEGI " +
                "              FROM GI " +
                "              JOIN INEGI_SUBRAMAS ISS ON GI.NOMBRE = ISS.ACT_SEPOMEX " +
                "             WHERE GI.CDGEM = AE.CDGEM " +
                "               AND GI.CDGSE = AE.CDGSE " +
                "               AND GI.CODIGO = AE.CDGGI) PROD_CULT " +
                "         , 'Diversos' DESTINO " +
                "         , 'Otros' TIPO_UNIDAD " +
                "         , 1 UNIDADES " +
                "         , 'No aplica' CICLO_AGRI " +
                "         , 'No aplica' RIE_TEMP " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FEC_APERTURA " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN_CREDITO " +
                "         , PF.CANTIDAD MONTO_CRE " +
                "         , PF.CANTIDAD MONTO_TOT " +
                "         , (PF.CANTIDAD * .80) MONTO_OTO " +
                "         , 1 NOPAGARE " +
                "         , PF.CANTIDAD MONTO_PAG " +
                "         , CASE WHEN PRN.PERIODICIDAD = 'S' THEN 'Semanal' " +
                "                WHEN PRN.PERIODICIDAD = 'Q' THEN 'Quincenal' " +
                "                WHEN PRN.PERIODICIDAD = 'C' THEN 'Catorcenal' " +
                "                WHEN PRN.PERIODICIDAD = 'M' THEN 'Mensual' END PERIODICIDAD " +
                "         , 'Pesos' TIPO_MN " +
                "         , 'Generación de Cartera' DEST_LINEA " +
                "         , 'Fija' TIPO_TASA " +
                "         , 'No Aplica' BASE_REF " +
                "         , '' PTS_ADIC " +
                "         , 'Mensual' PERIODICIDAD_TASA " +
                "         , ( SELECT ROUND(PRN.TASA / (1 + (CF.IVA/100)),2) TASA " +
                "               FROM PRN, CF " +
                "              WHERE PRN.CDGEM = PF.CDGEM " +
                "                AND PRN.CDGNS = PF.CDGCLNS " +
                "                AND PRN.CICLO = PF.CICLO " +
                "                AND CF.CDGEM = PRN.CDGEM " +
                "                AND CF.CDGFDI = PRN.CDGFDI ) TASA " +
                "         , 'Sin apoyo' APOYO_OTRO_PROG " +
                "         , 1 NO_MINISTRACION " +
                "         , 'Líquida' TIPO_GAR " +
                "         , 0.00 VALOR_GAR_PREN " +
                "         , 0.00 VALOR_GAR_HIPO " +
                "         , 0.00 VALOR_GAR_FIDU " +
                "         , 0.00 VALOR_GAR_NATU " +
                "         , (PF.CANTIDAD * .10) VALOR_GAR_LIQ " +
                "         , 0.00 VALOR_GAR_AVAL " +
                "         , 0.00 VALOR_GAR_CESION " +
                "         , 0.00 VALOR_GAR_USUF " +
                "         , 0.00 VALOR_GAR_OBLI " +
                "         , (PF.CANTIDAD * .10) VALOR_GAR " +
                "         , 'No aplica' REG_UNI_GAR " +
                "         , 'No aplica' DATOS_INS " +
                "         , 'No aplica' NOM_OBLI_SOL " +
                "      FROM PRC_FONDEO PF, CL, PRN, PRC, SC, PI, AE, DISPOSICION D " +
                "     WHERE PF.CDGEM = CL.CDGEM " +
                "       AND PF.CDGCL = CL.CODIGO " +
                "       AND PF.CDGEM = PRN.CDGEM " +
                "       AND PF.CDGCLNS = PRN.CDGNS " +
                "       AND PF.CICLO = PRN.CICLO " +
                "       AND PRN.CDGEM = PRC.CDGEM " +
                "       AND PRN.CDGNS = PRC.CDGNS " +
                "       AND PRN.CICLO = PRC.CICLO " +
                "       AND PF.CDGCL = PRC.CDGCL " +
                "       AND PRC.CDGEM = SC.CDGEM " +
                "       AND PRC.CDGNS = SC.CDGNS " +
                "       AND PRC.CLNS = SC.CLNS " +
                "       AND PRC.CICLO = SC.CICLO " +
                "       AND PRC.CDGCL = SC.CDGCL " +
                "       AND PI.CDGEM (+)= SC.CDGEM " +
                "       AND PI.CDGCL (+)= SC.CDGCL " +
                "       AND PI.PROYECTO (+)= SC.CDGPI " +
                "       AND AE.CDGEM (+)= PI.CDGEM " +
                "       AND AE.CDGSE (+)= PI.CDGSE " +
                "       AND AE.CDGGI (+)= PI.CDGGI " +
                "       AND AE.CODIGO (+)= PI.CDGAE " +
                "       AND PF.CDGEM = D.CDGEM " +
                "       AND PF.CDGORF = D.CDGORF " +
                "       AND PF.CDGLC = D.CDGLC " +
                "       AND PF.CDGDISP = D.CODIGO " +
                "       AND PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "       AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "  ORDER BY PF.CDGCL";
        }
        else if (opcion == 3) // RelacionesDescripcion
        {
            query = "SELECT 'Personas relacionadas' TIPO_REL" +
                "         , NULL RFC_ACRED " +
                "         , NULL RFC_SOC " +
                "         , NULL RFC_PER_REL " +
                "         , NULL RFC_SOC_REL " +
                "         , PF.CDGCL " +
                "         , 'Ninguna' RELACION " +
                "      FROM PRC_FONDEO PF, PRN " +
                "     WHERE PF.CDGEM = PRN.CDGEM " +
                "       AND PF.CDGNS = PRN.CDGNS " +
                "       AND PF.CICLO = PRN.CICLO " +
                "       AND PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "       AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "  ORDER BY PF.CDGCL";
        }
        else if (opcion == 4) // Anexo2
        {
            query = "SELECT NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                "         , 'SIMPLE' TIPO_CRED " +
                "         , '''' || D.CONTRATO NO_CONTRATO " +
                "         , PF.CANTIDAD MONTO_CONT " +
                "         , 'NA' NO_PAGARE " +
                "         , PF.CANTIDAD MONTO_PAG " +
                "         , (PF.CANTIDAD * .20) MONTO_OTRAS " +
                "         , PF.CANTIDAD TOTAL_PROY " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN " +
                "      FROM PRC_FONDEO PF, CL, PRN, DISPOSICION D " +
                "     WHERE PF.CDGEM = CL.CDGEM " +
                "       AND PF.CDGCL = CL.CODIGO " +
                "       AND PF.CDGEM = PRN.CDGEM " +
                "       AND PF.CDGCLNS = PRN.CDGNS " +
                "       AND PF.CICLO = PRN.CICLO " +
                "       AND PF.CDGEM = D.CDGEM " +
                "       AND PF.CDGORF = D.CDGORF " +
                "       AND PF.CDGLC = D.CDGLC " +
                "       AND PF.CDGDISP = D.CODIGO " +
                "       AND PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "       AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "  ORDER BY PF.CDGCL ";
        }
        else if (opcion == 5) // AnexoAnexo2
        {
            query = "SELECT ROWNUM NO, T1.* " +
                "      FROM ( SELECT ROWNUM NO " +
                "                  , NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                "                  , 'SIMPLE' TIPO_CRED " +
                "                  , 'NA' NO_CONTRATO " +
                "                  , PF.CANTIDAD MONTO_CONT " +
                "                  , 'NA' NO_PAGARE " +
                "                  , PF.CANTIDAD MONTO_PAG " +
                "                  , (PF.CANTIDAD * .80) MONTO_RECURSO_FR " +
                "                  , (PF.CANTIDAD * .20) MONTO_OTRAS " +
                "                  , PF.CANTIDAD TOTAL_PROY " +
                "                  , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN " +
                "                  , ACT.NOM_LOC LOCALIDAD " +
                "                  , ACT.NOM_MUN MUNICIPIO " +
                "                  , CASE WHEN ACT.NOM_ENT = 'Distrito Federal' THEN 'Ciudad de México' " +
                "                         ELSE ACT.NOM_ENT END ESTADO " +
                "                  , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "               FROM PRC_FONDEO PF " +
                "               JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "               JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "               JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                "          LEFT JOIN INEGI_ACT ACT ON N.CDGEF_INEGI = ACT.CVE_ENT AND N.CDGMU_INEGI = ACT.CVE_MUN AND N.CDGLO_INEGI = ACT.CVE_LOC " +
                "              WHERE PF.CDGEM = '" + empresa + "' " +
                "                AND PF.CDGORF = '" + cdgorg + "' " +
                "                AND PF.CDGLC = '" + cdglc + "' " +
                "                AND PF.CDGDISP = '" + cdgdisp + "' " +
                "                AND PF.FREPSDO = '" + fecha + "' " +
                "                AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "           ORDER BY PF.CDGCL ) T1";
        }
        else if (opcion == 6) // Certificado
        {
            query = "SELECT NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                "         , CL.CALLE || ', ' || COL.NOMBRE || ', ' ||  IA.NOM_LOC || ', ' ||  IA.NOM_MUN || ', ' ||  N.CDGPOSTAL DIRECCION " +
                "         , PF.CDGCL " +
                "         , 'N/A' NO_CONTRATO " +
                "         , PF.CANTIDAD MONTO_CONTRATO " +
                "         , 'N/A' NO_PAGARE " +
                "         , PF.CANTIDAD MONTO_PAGARE " +
                "         , (PF.CANTIDAD * .20) MONTO_OTRAS " +
                "         , ( SELECT ROUND(PRN.TASA / (1 + (CF.IVA/100)),2) TASA " +
                "               FROM PRN, CF " +
                "              WHERE PRN.CDGEM = PF.CDGEM " +
                "                AND PRN.CDGNS = PF.CDGCLNS " +
                "                AND PRN.CICLO = PF.CICLO " +
                "                AND CF.CDGEM = PRN.CDGEM " +
                "                AND CF.CDGFDI = PRN.CDGFDI ) TASA " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') VENCIMIENTO " +
                "         , 'LIQUIDA' GARANTIA " +
                "         , '10%' VALOR_GAR_LIQ " +
                "         , 'N/A' DATOS_INS " +
                "         , '''' || D.CONTRATO NO_DISPOSICION " +
                "         , TO_CHAR(D.FREGISTRO, 'DD/MM/YYYY') FEC_DIS " +
                "         , 'En Administración' SITUACION_PAG " +
                "         , 0.00 LIBERADO " +
                "         , PF.CANTIDAD ADMINISTRACION " +
                "         , CASE WHEN ( SELECT COUNT(*) CICLOS " +
                "                         FROM PRC, PRN " +
                "                        WHERE PRC.SITUACION IN( 'E', 'L') " +
                "                          AND PRC.CDGCL = CL.CODIGO " +
                "                          AND PRN.INICIO <= PF.FREPSDO " +
                "                          AND PRC.CDGEM = PRN.CDGEM " +
                "                          AND PRC.CDGNS = PRN.CDGNS " +
                "                          AND PRC.CICLO = PRN.CICLO ) > 1 THEN 'Renovación' " +
                "                ELSE 'Inicial' END CREPRI " +
                "         , NULL OBSERVACIONES " +
                "      FROM PRC_FONDEO PF " +
                "      JOIN DISPOSICION D ON PF.CDGEM = D.CDGEM AND PF.CDGORF = D.CDGORF AND PF.CDGLC = D.CDGLC AND PF.CDGDISP = D.CODIGO " +
                "      JOIN PRN ON PF.CDGEM = PRN.CDGEM AND PF.CDGCLNS = PRN.CDGNS AND PF.CICLO = PRN.CICLO " +
                "      JOIN CL ON PF.CDGEM = CL.CDGEM AND PF.CDGCL = CL.CODIGO " +
                "      JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                "      JOIN INEGI_ACT IA ON N.CDGEF_INEGI = IA.CVE_ENT AND N.CDGMU_INEGI = IA.CVE_MUN AND N.CDGLO_INEGI = IA.CVE_LOC " +
                "      JOIN COL ON N.CDGEF = COL.CDGEF AND N.CDGMU = COL.CDGMU AND N.CDGLO = COL.CDGLO AND N.CDGCOL = COL.CODIGO " +
                "     WHERE PF.CDGEM = 'EMPFIN' " +
                "       AND PF.CDGEM = '" + empresa + "' " +
                "       AND PF.CDGORF = '" + cdgorg + "' " +
                "       AND PF.CDGLC = '" + cdglc + "' " +
                "       AND PF.CDGDISP = '" + cdgdisp + "' " +
                "       AND PF.FREPSDO = '" + fecha + "' " +
                "       AND PRN.INICIO BETWEEN '" + fechaInicio + "' AND '" + fechaFin + "' " +
                "  ORDER BY PF.CDGCL";
        }
        else if (opcion == 7) // Trimestral
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_ACRED", CommandType.StoredProcedure,
                   oP.ParamsCierreAcred(empresa, fecha, usuario));

            query = "SELECT '''' || 524100000278 NO_CLIENTE " +
                "         , 'Emprendamos Fin' NOM_CLIENTE " +
                "         , NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                "         , SA.CDGCL " +
                "         , 'Comercial' SEC_ECO " +
                "         , 'Otros' LIN_APOY " +
                "         , NVL(ISS.SUBRAMA, 'COMERCIAL') SUBRAMAS " +
                "         , NVL(ISS.ACT_INEGI, 'Comercio al por menor de otros alimentos') PROD_CULT " +
                "         , 'Diversos' DESTINO " +
                "         , IA.CVE_ENT ENT_FED " +
                "         , IA.CVE_MUN MUNICIPIO " +
                "         , IA.CVE_LOC LOCALIDAD " +
                "         , 'Simple' TIPOCRED " +
                "         , SA.CANTENTRE MONTO_ENT " +
                "         , 'M.N.' MONEDA " +
                "         , '3= Año Natural' CICLOPROD " +
                "         , 'Inversión en capital de trabajo' DESTINO_R " +
                "         , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') INICIO " +
                "         , 'Otro' PERIODPAG " +
                "         , TO_CHAR(FNFECHAPROXPAGO(PRN.INICIO,PRN.PERIODICIDAD,PRN.PLAZO), 'DD/MM/YYYY') FIN " +
                "         , CASE WHEN SA.DIAS_MORA > 90 THEN NULL " +
                "                ELSE SDO_CAPITAL END CAP_VIG " +
                "         , CASE WHEN SA.DIAS_MORA > 90 THEN NULL " +
                "                ELSE SDO_INT_DEV_NO_COB END INT_VIG " +
                "         , CASE WHEN SA.DIAS_MORA > 90 THEN SDO_CAPITAL " +
                "                ELSE NULL END CAP_VEN " +
                "         , CASE WHEN SA.DIAS_MORA > 90 THEN SDO_INT_DEV_NO_COB " +
                "                ELSE NULL END INT_VEN " +
                "         , (SDO_CAPITAL + SDO_INT_DEV_NO_COB )SDO_TOTAL " +
                "         , 'Vigente' ESTATUS " +
                "         , SA.DIAS_MORA " +
                "         , 'Segmento 2' SEGMENTO " +
                "      FROM REP_SALDO_CIERRE_ACRED SA " +
                "      JOIN CL ON SA.CDGEM = CL.CDGEM AND SA.CDGCL = CL.CODIGO " +
                "      JOIN PRN ON SA.CDGEM = PRN.CDGEM AND SA.CDGCLNS = PRN.CDGNS AND SA.CICLO = PRN.CICLO " +
                "      JOIN PRC ON PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND SA.CDGCL = PRC.CDGCL " +
                "      JOIN SC ON PRC.CDGEM = SC.CDGEM AND PRC.CDGNS = SC.CDGNS AND PRC.CLNS = SC.CLNS AND PRC.CICLO = SC.CICLO AND PRC.CDGCL = SC.CDGCL " +
                " LEFT JOIN PI ON PI.CDGEM = SC.CDGEM AND PI.CDGCL = SC.CDGCL AND PI.PROYECTO = SC.CDGPI " +
                " LEFT JOIN AE ON AE.CDGEM = PI.CDGEM AND AE.CDGSE = PI.CDGSE AND AE.CDGGI = PI.CDGGI AND AE.CODIGO = PI.CDGAE " +
                " LEFT JOIN GI ON GI.CDGEM = AE.CDGEM AND GI.CDGSE = AE.CDGSE AND GI.CODIGO = AE.CDGGI " +
                " LEFT JOIN INEGI_SUBRAMAS ISS ON GI.CDGEM = ISS.CDGEM AND GI.NOMBRE = ISS.ACT_SEPOMEX " +
                "      JOIN NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                "      JOIN INEGI_ACT IA ON N.CDGEF_INEGI = IA.CVE_ENT AND N.CDGMU_INEGI = IA.CVE_MUN AND N.CDGLO_INEGI = IA.CVE_LOC " +
                "     WHERE SA.CDGEM = '" + empresa + "' " +
                "       AND SA.CDGPE = '" + usuario + "' " +
                "       AND SA.CDGORF = '" + cdgorg + "' " +
                "  ORDER BY CL.CODIGO";
        }
        else if (opcion == 8)// Mensual
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_ACRED", CommandType.StoredProcedure,
                   oP.ParamsCierreAcred(empresa, fecha, usuario));

            query = "SELECT '''' || 524700003340000 NOCONTRATO " +
                "         , '''' || D.PAGARE NODISPOSICION " +
                "         , 1 TIPOPERSONA " +
                "         , CL.RFC " +
                "         , CL.CURP " +
                "         , 'N/A' RAZONSOCIAL " +
                "         , CASE WHEN CL.NOMBRE2 IS NULL THEN CL.NOMBRE1 " +
                "                ELSE CL.NOMBRE1 || ' ' || CL.NOMBRE2 " +
                "                 END NOMBRE " +
                "         , CL.PRIMAPE " +
                "         , CL.SEGAPE " +
                "         , CASE WHEN CL.SEXO = 'F' THEN 1 " +
                "                WHEN CL.SEXO = 'M' THEN 2 " +
                "                ELSE 0 " +
                "                 END SEXO " +
                "         , IA.CVE_ENT ESTADO " +
                "         , IA.CVE_MUN MUNICIPIO " +
                "         , IA.CVE_LOC LOCALIDAD " +
                "         , 5 TIPOVIALIDAD " +
                "         , N.COLONIA " +
                "         , N.CALLE " +
                "         , CASE WHEN NVL(IA.POBLACION, 0) < 50000 THEN 1 " +
                "                ELSE 2 " +
                "                 END TIPOAMBITO " +
                "         , CASE WHEN N.NOEXT IS NULL THEN 'N/A' " +
                "                ELSE N.NOEXT " +
                "                  END NOEXT " +
                "         , 9 TIPOASENTAMIENTO " +
                "         , CASE WHEN N.NOINT IS NULL THEN 'N/A' " +
                "                ELSE N.NOINT " +
                "                 END NOINT " +
                "         , N.CDGPOSTAL CP " +
                "         , CL.TELEFONO " +
                "         , 0 CELULAR " +
                "         , 'clientes@emprendamosfin.com' EMAIL " +
                "         , 12 TIPOCREDITO " +
                "         , CL.CODIGO NOCLIENTE " +
                "         , SA.INICIO OTORGAMIENTO " +
                "         , SA.INICIO FDISPERSION " +
                "         , SA.FIN FVENCIMIENTO " +
                "         , 1 PLAZOSEM " +
                "         , ROUND(MONTHS_BETWEEN(SA.FIN, SA.INICIO)) PLAZOUNI " +
                "         , 10 RAMA " +
                "         , '''' || 110930000090 DESTINO " +
                "         , '''' || 110561990000 PRODUCTO " +
                "         , 0 TIPOUNIDAD " +
                "         , 0 UNIDADES " +
                "         , 0 CICLOAGRI " +
                "         , 0 RIEGO " +
                "         , SA.CANTENTRE MONTOTOT " +
                "         , SA.CANTENTRE MONTOAUTO " +
                "         , (SA.CANTENTRE *.80) MONTOOTOR " +
                "         , (SA.CANTENTRE * .20) APORTACION " +
                "         , 0 APOYO " +
                "         , 1 NOPAGARE " +
                "         , SA.CANTENTRE MONTOPAGARE " +
                "         , 1 PERIODICIDAD " +
                "         , 1 TIPOMONEDA " +
                "         , CASE WHEN (SELECT COUNT(*) " +
                "                        FROM PRC " +
                "                       WHERE CDGEM = SA.CDGEM " +
                "                         AND CDGCL = SA.CDGCL " +
                "                         AND CANTENTRE > 0 " +
                "                         AND SITUACION IN ('L','E') " +
                "                         AND CLNS = 'G') > 1 THEN 2 " +
                "                ELSE 1 " +
                "                 END EIF " +
                "         , CASE WHEN (SELECT COUNT(*) " +
                "                        FROM PRC " +
                "                       WHERE CDGEM = SA.CDGEM " +
               "                         AND CDGCL = SA.CDGCL " +
                "                         AND CANTENTRE > 0 " +
                "                         AND SITUACION IN ('L','E') " +
                "                         AND CLNS = 'G') > 1 THEN 2 " +
                "                ELSE 1 " +
                "                 END CREDITFORMAL " +
                "         , 1 TIPOTASA " +
                "         , 0 BASETIIE " +
                "         , 0 PUNTOSAD " +
                "         , ( SELECT ROUND(PRN.TASA / (1 + (CF.IVA/100)),2) TASA " +
                "               FROM PRN, CF " +
                "              WHERE PRN.CDGEM = SA.CDGEM " +
                "                AND PRN.CDGNS = SA.CDGCLNS " +
                "                AND PRN.CICLO = SA.CICLO " +
                "                AND CF.CDGEM = PRN.CDGEM " +
                "                AND CF.CDGFDI = PRN.CDGFDI ) TASA " +
                "         , 0 GARANTIA " +
                "         , 0 PRENDARIA " +
                "         , 0 NOFACTURA " +
                "         , NULL FECHA " +
                "         , 0 DESCRGAR " +
                "         , 0 DATOSINSCRI " +
                "         , 0 HIPOTECARIA " +
                "         , 0 NOESCRITURA " +
                "         , 0 GRAVAMEN " +
                "         , 0 DESCRGAR2 " +
                "         , 0 FAVALUO " +
                "         , 0 FINSCRIPCION " +
                "         , 0 FIDUCIARIA " +
                "         , 0 NATURAL " +
                "         , 0 DATOSINSCR " +
                "         , (SA.CANTENTRE * .10) LIQUIDA " +
                "         , 0 OTRASGAR " +
                "         , 0 OBLIGADOSOL " +
                "         , 'N/A' NOMOBLIGADOSOL " +
                "         , 0 CREDITORELACIONADO " +
                "         , 0 RIESGOCOMUN " +
                "         , 0 RESPONSABILIDADES " +
                "         , CASE WHEN CLNS = 'G' THEN 2 " +
                "                ELSE 1 " +
                "                 END TIPOCREDITO " +
                "         , SA.NOMNS NOMGRUPO " +
                "         , ROUND(SDO_CAPITAL) CAPITALVIG " +
                "         , ROUND(SDO_INT_DEV_NO_COB) INTERESVIG " +
                "         , NULL CAPITALVEN " +
                "         , NULL INTERESVEN " +
                "         , ROUND((SDO_CAPITAL + SDO_INT_DEV_NO_COB)) SALDOTOTAL " +
                "         , SA.DIAS_MORA NODIASATRASO " +
                "         , 1 ESTATUS " +
                "         , SA.FECHA_CALC FULTCALIF " +
                "         , 'N/A' RESULTCALIF " +
                "         , 'N/A' RESPENCALIF " +
                "         , 0 IMPORTEULTCALIF " +
                "         , 0 PORCRESERVA " +
                "         , 2 SEGGARNAT " +
                "         , 2 SERGARADI " +
                "         , 0 IMPORTEASEGURADO " +
                "         , 1 SUPERVISADO " +
                "         , TO_CHAR(TO_DATE('29/06/2018'), 'DD/MM/YYYY') FSUPERVISION " +
                "         , 1 RESSUPERVISION " +
                "      FROM REP_SALDO_CIERRE_ACRED SA " +
                "      JOIN DISPOSICION D ON D.CDGEM = SA.CDGEM AND D.CDGORF = SA.CDGORF AND D.CDGLC = SA.CDGLC AND D.CODIGO = SA.CDGDISP " +
                "      JOIN CL ON CL.CDGEM = SA.CDGEM AND CL.CODIGO = SA.CDGCL " +
                "      JOIN CL_NEGOCIO N ON CL.CDGEM = N.CDGEM AND CL.CODIGO = N.CDGCL " +
                "      JOIN INEGI_ACT IA ON N.CDGEF_INEGI = IA.CVE_ENT AND N.CDGMU_INEGI = IA.CVE_MUN AND N.CDGLO_INEGI = IA.CVE_LOC " +
                "     WHERE SA.CDGEM = '" + empresa + "' " +
                "       AND SA.CDGPE = '" + usuario + "' " +
                "       AND SA.CDGORF = '" + cdgorg + "' " +
                "  ORDER BY SA.CDGCL ";
        }

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DEL PROCESO DE REGISTRO DE METAS MEDIANTE UN ARCHIVO
    [WebMethod]
    public string getRepMetaArchivo(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT * " +
                       "FROM REP_META " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "'";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE EL RESULTADO DEL PROCESO DE REGISTRO DE METAS MEDIANTE UN ARCHIVO
    [WebMethod]
    public string getRepMetasAsesor(string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT CDGEM " +
                       ",CDGRG " +
                       ",CDGCO " +
                       ",CDGOCPE " +
                       ",MES " +
                       ",ANIO " +
                       ",TRUNC(META, 2) META " +
                       ",CDGPE " +
                       ",ESTATUS " +
                       "FROM REP_METAS_ASESOR " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "' ";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    /*//METODO QUE EXTRAE EL RESULTADO DE LA CARGA DE METAS POR ASESOR
    [WebMethod]
    public string getRepMetasAsesor(string usuario, string mes, string anio)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;

        string query = "SELECT RG.NOMBRE REGION , " +
                       "CO.NOMBRE SUCURSAL , " +
                       "MRS.CDGOCPE CODIGO , " +
                       "MRS.META , " +
                       "MRS.ESTATUS " +
                       "FROM METAS_REP_ASESOR MRS   " +
                       "LEFT JOIN RG ON RG.CODIGO = MRS.CDGRG " +
                       "LEFT JOIN CO ON CO.CODIGO = MRS.CDGCO " +
                       "WHERE MRS.INICIO = '01/" + mes + "/" + anio + "'" +
                       "AND MRS.CDGEM = '" + empresa + "' " +
                       "AND MRS.CDGPE = '" + usuario + "' " +
                       "ORDER BY REGION , SUCURSAL ,CODIGO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }*/

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE MICROSEGUROS
    [WebMethod]
    public string getRepMicroseguros(string mes, string anio)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        string fechaInicio = "01-" + mes + "-" + anio;
        string fechaFin = "LAST_DAY(TO_DATE('" + fechaInicio + "', 'DD-MM-YYYY'))";
        int iRes;

        try
        {
            string query = "SELECT PRN.CDGCO " +
                           ",CO.NOMBRE NOMCO " +
                           ",M.CDGCL " +
                           ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMCL " +
                           ",TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') FECNAC " +
                           ",DECODE(CL.SEXO,'M','MASCULINO','F','FEMENINO') GENERO " +
                           ",CL.CURP " +
                           ",TO_CHAR(PRN.INICIO,'DD/MM/YYYY') FECINI " +
                           ",TO_CHAR(DECODE(NVL(PRN.PERIODICIDAD,''), " +
                                        "'S', PRN.INICIO + (7 * NVL(PRN.PLAZO,0)), " +
                                        "'Q', PRN.INICIO + (15 * NVL(PRN.PLAZO,0)), " +
                                        "'C', PRN.INICIO + (14 * NVL(PRN.PLAZO,0)), " +
                                        "'M', PRN.INICIO + (30 * NVL(PRN.PLAZO,0)), " +
                                        "'', ''),'DD/MM/YYYY') FECFIN " +
                           ",PRC.CDGNS || PRC.CICLO || PRC.CDGCL CUENTA " +
                           ",PRC.CANTENTRE " +
                           ",ROUND(FNCALTOTAL(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G') * (PRC.CANTENTRE/PRN.CANTENTRE),2) TOTAL_PAGAR " +
                           ",(SELECT ROUND((SDO_TOTAL * (PRC.CANTENTRE/PRN.CANTENTRE)),2) FROM TBL_CIERRE_DIA WHERE CDGEM = M.CDGEM AND CDGCLNS = M.CDGCLNS AND CICLO = M.CICLO AND CLNS = M.CLNS AND FECHA_CALC = LAST_DAY(TO_DATE('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + ",'DD/MM/YYYY'))) SALDO " +
                           ",PRN.CDGNS " +
                           ",(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PE WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGOCPE) NOMPE " +
                           ",(SELECT NOMBRE FROM TPC WHERE CDGEM = PRN.CDGEM AND CODIGO = PRN.CDGTPC) TIPOPROD " +
                           ",(SELECT COUNT(*) CICLOS FROM PRC PC,PRN PN WHERE PC.SITUACION IN( 'E', 'L') AND PC.CDGCL = PRC.CDGCL AND PN.INICIO <= " + fechaFin +
                           " AND PC.CDGEM = PN.CDGEM AND PC.CDGNS = PN.CDGNS AND PC.CICLO = PN.CICLO) CICLOS " +
                           ",DECODE(M.ESTATUS,'V','SI','R','NO') COBRADO " +
                           ",TO_CHAR(M.INICIO,'DD/MM/YYYY') INICIO_SEGURO " +
                           //",DECODE(M.CDGPMS,'001','VIDA','002','CANCER') TIPO_PRODUCTO " +
                           ",'VIDA' TIPO_PRODUCTO " +
                           ",DECODE(M.FORMA_PAGO, 'F', 'FINANCIADO', 'P', 'DE CONTADO') FORMA_PAGO " +
                           ",DECODE(M.TIPOSEGURO, 'I', 'SEGURO INDIVIDUAL', 'F', 'SEGURO FAMILIAR', 'SEGURO INDIVIDUAL') TIPO_SEGURO " +
                           ",ROUND(M.COSTO,2) COSTO " +
                           "FROM MICROSEGURO M, PRN, PRC, CL, CO " +
                           "WHERE M.CDGEM = '" + empresa + "' " +
                           "AND M.CLNS = 'G' " +
                           "AND M.ESTATUS IN ('R','V') " +
                           "AND PRN.CDGEM = M.CDGEM " +
                           "AND PRN.CDGNS = M.CDGCLNS " +
                           "AND PRN.CICLO = M.CICLO " +
                           "AND TO_NUMBER(TO_CHAR(PRN.INICIO,'MM')) = " + mes + " " +
                           "AND TO_NUMBER(TO_CHAR(PRN.INICIO,'YYYY')) = " + anio + " " +
                           "AND PRN.SITUACION IN ('E','L') " +
                           "AND PRN.CANTENTRE > 0 " +
                           "AND PRC.CDGEM = PRN.CDGEM " +
                           "AND PRC.CDGCLNS = PRN.CDGNS " +
                           "AND PRC.CICLO = PRN.CICLO " +
                           "AND PRC.CDGCL = M.CDGCL " +
                           "AND PRC.CLNS = M.CLNS " +
                           "AND PRC.SITUACION IN ('E','L') " +
                           "AND PRC.CANTENTRE > 0 " +
                           "AND CL.CDGEM = M.CDGEM " +
                           "AND CL.CODIGO = M.CDGCL " +
                           "AND CO.CDGEM = PRN.CDGEM " +
                           "AND CO.CODIGO = PRN.CDGCO " +
                //UNION DE CREDITOS INDIVIDUALES
                           "UNION " +
                           "SELECT PRC.CDGCO " +
                           ",CO.NOMBRE NOMCO " +
                           ",M.CDGCL " +
                           ",NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMCL " +
                           ",TO_CHAR(CL.NACIMIENTO,'DD/MM/YYYY') FECNAC " +
                           ",DECODE(CL.SEXO,'M','MASCULINO','F','FEMENINO') GENERO " +
                           ",CL.CURP " +
                           ",TO_CHAR(PRC.INICIO,'DD/MM/YYYY') FECINI " +
                           ",TO_CHAR(DECODE(NVL(PRC.PERIODICIDAD,''), " +
                                        "'S', PRC.INICIO + (7 * NVL(PRC.PLAZO,0)), " +
                                        "'Q', PRC.INICIO + (15 * NVL(PRC.PLAZO,0)), " +
                                        "'C', PRC.INICIO + (14 * NVL(PRC.PLAZO,0)), " +
                                        "'M', PRC.INICIO + (30 * NVL(PRC.PLAZO,0)), " +
                                        "'', ''),'DD/MM/YYYY') FECFIN " +
                           ",PRC.CDGNS || PRC.CICLO || PRC.CDGCL CUENTA " +
                           ",PRC.CANTENTRE " +
                           ",FNCALTOTAL(PRC.CDGEM, PRC.CDGCLNS, PRC.CICLO, PRC.CLNS) TOTAL_PAGAR " +
                           ",(SELECT SDO_TOTAL FROM TBL_CIERRE_DIA WHERE CDGEM = M.CDGEM AND CDGCLNS = M.CDGCLNS AND CICLO = M.CICLO AND CLNS = M.CLNS AND FECHA_CALC = LAST_DAY(TO_DATE('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + ",'DD/MM/YYYY'))) SALDO " +
                           ",NULL " +
                           ",(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PE WHERE CDGEM = PRC.CDGEM AND CODIGO = PRC.CDGOCPE) NOMPE " +
                           ",(SELECT NOMBRE FROM TPC WHERE CDGEM = PRC.CDGEM AND CODIGO = PRC.CDGTPC) TIPOPROD " +
                           ",(SELECT COUNT(*) CICLOS FROM PRC PC,PRN PN WHERE PC.SITUACION IN( 'E', 'L') AND PC.CDGCL = PRC.CDGCL AND PN.INICIO <= " + fechaFin +
                           " AND PC.CDGEM = PN.CDGEM AND PC.CDGNS = PN.CDGNS AND PC.CICLO = PN.CICLO) CICLOS " +
                           ",DECODE(M.ESTATUS,'V','SI','R','NO') COBRADO " +
                           ",TO_CHAR(M.INICIO,'DD/MM/YYYY') INICIO_SEGURO " +
                           //",DECODE(M.CDGPMS,'001','VIDA','002','CANCER') TIPO_PRODUCTO " +
                           ",'VIDA' TIPO_PRODUCTO " +
                           ",DECODE(M.FORMA_PAGO, 'F', 'FINANCIADO', 'P', 'DE CONTADO') FORMA_PAGO " +
                           ",DECODE(M.TIPOSEGURO, 'I', 'SEGURO INDIVIDUAL', 'F', 'SEGURO FAMILIAR', 'SEGURO INDIVIDUAL') TIPO_SEGURO " +
                           ",ROUND(M.COSTO,2) COSTO " +
                           "FROM MICROSEGURO M, PRC, CL, CO " +
                           "WHERE M.CDGEM = '" + empresa + "' " +
                           "AND M.CLNS = 'I' " +
                           "AND M.ESTATUS IN ('R','V') " +
                           "AND PRC.CDGEM = M.CDGEM " +
                           "AND PRC.CDGCLNS = M.CDGCLNS " +
                           "AND PRC.CICLO = M.CICLO " +
                           "AND TO_NUMBER(TO_CHAR(PRC.INICIO,'MM')) = " + mes + " " +
                           "AND TO_NUMBER(TO_CHAR(PRC.INICIO,'YYYY')) = " + anio + " " +
                           "AND PRC.SITUACION IN ('E','L') " +
                           "AND PRC.CANTENTRE > 0 " +
                           "AND PRC.CLNS = M.CLNS " +
                           "AND CL.CDGEM = M.CDGEM " +
                           "AND CL.CODIGO = M.CDGCL " +
                           "AND CO.CDGEM = PRC.CDGEM " +
                           "AND CO.CODIGO = PRC.CDGCO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE SITUACION DE CARTERA
    [WebMethod]
    public string getRepMora(string fecha, string nivel, int cartVig, int cartVenc, int cartRest, int cartCast, string usuario,
                                string nomUsuario, string region, string sucursal, string coord, string asesor, string tipoProd,
                                string nivelMora)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepMora.dtMoraDataTable dt = new dsRepMora.dtMoraDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        DateTime fec;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_ANT_SALDOS", CommandType.StoredProcedure,
                          oP.ParamsMora(empresa, Convert.ToDateTime(fecha), Convert.ToInt32(nivel), cartVig, cartVenc, cartRest,
                                        cartCast, usuario, region, sucursal, coord, asesor, tipoProd, nivelMora));

            string query = "SELECT RS.*, " +
                           "TO_CHAR(SYSDATE,'DD/MM/YYYY') FECHAIMP, " +
                           "TO_CHAR(SYSDATE,'HH24:MI:SS') HORAIMP " +
                           "FROM REP_ANT_SALDOS RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CDGPE = '" + usuario + "' " +
                           "ORDER BY CDGCLNS"; 

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drMora = dt.NewRow();
                drMora["COD_REGIONAL"] = dref.Tables[0].Rows[i]["CDGRG"];
                drMora["REGIONAL"] = dref.Tables[0].Rows[i]["NOMRG"];
                drMora["COD_SUCURSAL"] = dref.Tables[0].Rows[i]["CDGCO"];
                drMora["SUCURSAL"] = dref.Tables[0].Rows[i]["NOMCO"];
                drMora["COD_ASESOR"] = dref.Tables[0].Rows[i]["CDGOCPE"];
                drMora["OF_CREDITO"] = dref.Tables[0].Rows[i]["NOMPE"];
                drMora["COD_GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drMora["GRUPO"] = dref.Tables[0].Rows[i]["NOMCLNS"];
                drMora["CICLO"] = dref.Tables[0].Rows[i]["CICLO"];
                drMora["INICIO"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["INICIO"]).ToString("dd/MM/yyyy");
                drMora["FEC_FIN"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["FECHA_FIN"]).ToString("dd/MM/yyyy");
                drMora["TASA"] = dref.Tables[0].Rows[i]["TASA"];
                drMora["CANT_ENTRE"] = dref.Tables[0].Rows[i]["CARTERAVIG"];
                drMora["TOTAL_PAGAR"] = dref.Tables[0].Rows[i]["TOTAL_PAGAR"];
                drMora["SALDO_CAP"] = dref.Tables[0].Rows[i]["SDO_CAP"];    
                drMora["SALDO_TOTAL"] = dref.Tables[0].Rows[i]["MORA3"];
                drMora["PARCIALIDAD"] = dref.Tables[0].Rows[i]["PARCIALIDAD"];
                drMora["PERIO_TRANS"] = dref.Tables[0].Rows[i]["PERIO_TRANS"];
                drMora["FEC_ULT_PAGO"] = DateTime.TryParse(dref.Tables[0].Rows[i]["FECHA_ULT_PAGO"].ToString(), out fec) ? fec.ToString("dd/MM/yyyy") : null;
                drMora["MONTO_ULT_PAGO"] = dref.Tables[0].Rows[i]["MONTO_ULT_PAGO"];
                drMora["PAGOS_COMP"] = dref.Tables[0].Rows[i]["MORA1"];
                drMora["PAGOS_EFECT"] = dref.Tables[0].Rows[i]["MORA2"];
                drMora["MORA_CAP"] = dref.Tables[0].Rows[i]["MORA_CAP"];
                drMora["MORA_TOTAL"] = dref.Tables[0].Rows[i]["SALDO"];
                drMora["DIAS_MORA"] = dref.Tables[0].Rows[i]["DIAS_MORA"];
                drMora["NUM_INTEG"] = dref.Tables[0].Rows[i]["MORA4"];
                drMora["SALDO_GL"] = dref.Tables[0].Rows[i]["MORA5"];
                drMora["MORATORIOS"] = dref.Tables[0].Rows[i]["MORA6"];
                drMora["SALDO_GL"] = dref.Tables[0].Rows[i]["MORA5"];
                drMora["MORATORIOS"] = dref.Tables[0].Rows[i]["MORA6"];
                drMora["COD_COORD"] = dref.Tables[0].Rows[i]["CDGCOPE"];
                drMora["COORDINADOR"] = dref.Tables[0].Rows[i]["NOMCOPE"];
                drMora["TIPO_CART"] = dref.Tables[0].Rows[i]["TIPO_CART"];
                drMora["TIPOPROD"] = dref.Tables[0].Rows[i]["NOMTIPOPROD"];
                drMora["DIAS_ATRASO"] = dref.Tables[0].Rows[i]["DIAS_ATRASO"];

                dt.Rows.Add(drMora);
            }
            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION DEL REPORTE DE OPERACIONES FRACCIOANDAS
    [WebMethod]
    public string getRepOperacionesFrac(int mes, int anio, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_REP_OPERACIONES_FRAC", CommandType.StoredProcedure,
               oP.ParamsOperacionesFrac(empresa, mes, anio, usuario));

        query = "SELECT CDGEM " +
                ",COD_SUCURSAL " +
                ",NOM_SUCURSAL " +
                ",COD_GRUPO " +
                ",NOM_GRUPO " +
                ",CICLO " +
                ",CDGCL " +
                ",NOM_CL " +
                ",TO_CHAR(FREALDEP, 'DD/MM/YYYY') FREALDEP " +
                ",CANTIDAD " +
                ",NO_CLIENTES " +
                ",TRUNC(PRORRATEO,2) PRORRATEO " +
                ",TRUNC(VALOR,2) VALOR " +
                ",TRUNC(VALORUSD,2) VALORUSD " +
                ",CDGPE " +
                "FROM REP_OPERACIONES_FRAC " +
                "WHERE CDGEM = '" + empresa + "' " +
                "AND CDGPE = '" + usuario + "' " +
                "ORDER BY NOM_SUCURSAL, NOM_GRUPO, CICLO, FREALDEP ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION DEL REPORTE DE OPERACIONES FRACCIOANDAS
    [WebMethod]
    public string getRepOperacIndMonto(int mes, int anio, decimal monto, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_REP_OPERAC_IND_MONTO", CommandType.StoredProcedure,
               oP.ParamsOperacIndMonto(empresa, mes, anio, monto, usuario));

        query = "SELECT CDGEM " +
                ",COD_SUCURSAL " +
                ",NOM_SUCURSAL " +
                ",COD_GRUPO " +
                ",NOM_GRUPO " +
                ",CICLO " +
                ",CDGCL " +
                ",NOM_CL " +
                ",TO_CHAR(FREALDEP, 'DD/MM/YYYY') FREALDEP " +
                ",CANTIDAD " +
                ",NO_CLIENTES " +
                ",TRUNC(PRORRATEO,2) PRORRATEO " +
                ",TRUNC(VALOR,2) VALOR " +
                ",TRUNC(VALORUSD,2) VALORUSD " +
                ",CDGPE " +
                "FROM REP_OPERACIONES_FRAC " +
                "WHERE CDGEM = '" + empresa + "' " +
                "AND CDGPE = '" + usuario + "' " +
                "ORDER BY NOM_SUCURSAL, NOM_GRUPO, CICLO, FREALDEP ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS ESPERADOS
    [WebMethod]
    public string getRepPagosEsperados(string fecha, string region, string sucursal, string coord, string asesor)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string strRegion = string.Empty;
        string strSucursal = string.Empty;
        string strCoord = string.Empty;
        string strAsesor = string.Empty;
        string xml = "";
        int iRes;
        try
        {
            if(region != "000")
            {
                strRegion = "AND CD.REGION = (SELECT NOMBRE FROM RG WHERE CDGEM = '" + empresa + "' AND CODIGO = '" + region + "') ";
            }

            if(sucursal != "000")
            {
                strSucursal = "AND CD.COD_SUCURSAL = '" + sucursal + "' ";
            }

            if (coord != "000000" && coord != "111111") 
            {
                strCoord = "AND CD.COD_COOR = '" + coord + "' "; 
            }

            if(asesor != "000000")
            {
                strAsesor = "AND CD.COD_ASESOR = '" + asesor + "' ";
            }

            string query = "SELECT TO_CHAR(CD.FECHA_CALC,'DD/MM/YYYY') FCALC " +
                           ",CD.REGION " +
                           ",CD.NOM_SUCURSAL SUCURSAL " +
                           ",NOM_COOR NOMBRE_COORDINADOR " +
                           ",CD.NOM_ASESOR NOMBRE_ASESOR " +
                           ",CD.PLAZO " +
                           ",'SEMANAL' PERIODICIDAD " +
                           ",CD.CDGCLNS COD_GPO " +
                           ",NS.NOMBRE NOMBRE_GRUPO " +
                           ",CD.CICLO " +
                           ",FLOOR((CD.FECHA_CALC  - CD.INICIO)/7) NO_PAGO " +
                           ",CASE WHEN CD.MORA_TOTAL > 0 THEN ROUND((CD.MORA_TOTAL/CD.MONTO_CUOTA),2) " +
                                  "ELSE 0 END PAGOS_ATRASADOS " +
                           ",CD.MONTO_CUOTA AS PARCIALIDAD " +
                           ",CASE WHEN CD.MORA_TOTAL = 0 THEN CD.MONTO_CUOTA " +
                                  "ELSE " +
                                     "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                     "FROM MP " +
                                     "WHERE CDGEM = PRN.CDGEM " +
                                     "AND CDGCLNS = PRN.CDGNS " +
                                     "AND CICLO = PRN.CICLO " +
                                     "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                     "AND CLNS = 'G' " +
                                     "AND TIPO = 'PD' " +
                                     "AND ESTATUS <> 'E') " +
                           "END AS PAGO_REAL_PERIODO " +
                           ",CASE WHEN CD.MORA_TOTAL = 0 THEN 0 " +
                                  "WHEN (CD.MONTO_CUOTA - " +
                                         "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                         "FROM MP " +
                                         "WHERE CDGEM = PRN.CDGEM " +
                                         "AND CDGCLNS = PRN.CDGNS " +
                                         "AND CICLO = PRN.CICLO " + 
                                         "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                         "AND CLNS = 'G' " +
                                         "AND TIPO = 'PD' " +
                                         "AND ESTATUS <> 'E')) < 0 THEN 0 " +
                                  "ELSE " +
                                    "(CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) " +
                                  "END DIFERENCIA " +
                            ",CD.MONTO_ENTREGADO " +
                            ",CD.MORA_CAPITAL " +
                            ",CD.MORA_INTERES " +
                            ",CD.MORA_TOTAL " +
                            ",CD.DIAS_MORA " +
                            ",CD.SDO_CAPITAL " +
                            ",CD.SDO_INTERES " +
                            ",CD.SDO_TOTAL " +
                            ",CD.CAPITAL_PAGADO + CD.INTERES_PAGADO TOTAL_PAGADO " +
                            ",CD.SALDO_GL " +
                            ",TO_CHAR(CD.INICIO,'DD/MM/YYYY') FINICIO " +
                            ",TO_CHAR(CD.FIN,'DD/MM/YYYY') FFIN " +
                            ",CASE WHEN CD.MORA_TOTAL = 0 THEN 'COMPLETO' " +
                                  "WHEN (CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) <= 0 THEN 'COMPLETO' " +
                                  "WHEN " +
                                    "(CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) = CD.MONTO_CUOTA THEN 'NO PAGO' " +
                                  "ELSE 'INCOMPLETO' " +
                                  "END ESTATUS " +
                            "FROM TBL_CIERRE_DIA CD, NS, PRN " +
                            "WHERE CD. CDGEM = PRN.CDGEM " +
                            "AND CD.CDGCLNS = PRN.CDGNS " +
                            "AND CD.CICLO = PRN.CICLO " +
                            "AND CD.CDGEM = NS.CDGEM " +
                            "AND CD.CDGCLNS = NS.CODIGO " +
                            "AND CD.CDGEM = '" + empresa + "' " +
                            "AND CD.FECHA_CALC = '" + fecha + "' " +
                            "AND CD.SITUACION = 'E' " +
                            "AND CD.INICIO < CD.FECHA_CALC " +
                            strRegion +
                            strSucursal +
                            strCoord +
                            strAsesor +
                            "AND PRN.PERIODICIDAD = 'S' " +
                            "AND FNFECHAPROXPAGO(CD.INICIO,PRN.PERIODICIDAD,FLOOR((CD.FECHA_CALC - CD.INICIO)/7)) = '" + fecha + "' " +
                            "AND CD.PLAZO >= FLOOR((CD.FECHA_CALC  - CD.INICIO)/7) " +
                            "UNION " +
                            "SELECT TO_CHAR(CD.FECHA_CALC,'DD/MM/YYYY') FCALC " +
                            ",CD.REGION " +
                            ",CD.NOM_SUCURSAL SUCURSAL " +
                            ",NOM_COOR NOMBRE_COORDINADOR " +
                            ",CD.NOM_ASESOR NOMBRE_ASESOR " +
                            ",CD.PLAZO " +
                            ",'CATORCENAL' PERIODICIDAD " +
                            ",CD.CDGCLNS COD_GPO " +
                            ",NS.NOMBRE " +
                            ",CD.CICLO " +
                            ",FLOOR((CD.FECHA_CALC  - CD.INICIO)/14) NO_PAGO " +
                            ",CASE WHEN CD.MORA_TOTAL > 0 THEN ROUND((CD.MORA_TOTAL/CD.MONTO_CUOTA),2) " +
                                  "ELSE 0 END PAGOS_ATRASADOS " +
                            ",CD.MONTO_CUOTA PARCIALIDAD " +
                            ",CASE WHEN CD.MORA_TOTAL = 0 THEN CD.MONTO_CUOTA " +
                                  "ELSE " +
                                     "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                     "FROM MP " +
                                     "WHERE CDGEM = PRN.CDGEM " +
                                     "AND CDGCLNS = PRN.CDGNS " +
                                     "AND CICLO = PRN.CICLO " +
                                     "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                     "AND CLNS = 'G' " +
                                     "AND TIPO = 'PD' " +
                                     "AND ESTATUS <> 'E') " +
                                  "END PAGO_REAL_PERIODO " +
                            ",CASE WHEN CD.MORA_TOTAL = 0 THEN 0 " +
                                  "WHEN (CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) < 0 THEN 0 " +
                                  "ELSE " +
                                    "(CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) " +
                                  "END DIFERENCIA " +
                            ",CD.MONTO_ENTREGADO " +
                            ",CD.MORA_CAPITAL " +
                            ",CD.MORA_INTERES " +
                            ",CD.MORA_TOTAL " +
                            ",CD.DIAS_MORA " +
                            ",CD.SDO_CAPITAL " +
                            ",CD.SDO_INTERES " +
                            ",CD.SDO_TOTAL " +
                            ",CD.CAPITAL_PAGADO + CD.INTERES_PAGADO TOTAL_PAGADO " +
                            ",CD.SALDO_GL " +
                            ",TO_CHAR(CD.INICIO,'DD/MM/YYYY') FINICIO " +
                            ",TO_CHAR(CD.FIN,'DD/MM/YYYY') FFIN " +
                            ",CASE WHEN CD.MORA_TOTAL = 0 THEN 'COMPLETO' " +
                                  "WHEN (CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) <= 0 THEN 'COMPLETO' " +
                                  "WHEN " +
                                    "(CD.MONTO_CUOTA - " +
                                             "(SELECT NVL (SUM (CANTIDAD), 0) " +
                                             "FROM MP " +
                                             "WHERE CDGEM = PRN.CDGEM " +
                                             "AND CDGCLNS = PRN.CDGNS " +
                                             "AND CICLO = PRN.CICLO " +
                                             "AND FREALDEP BETWEEN FNFECANTHABIL(CD.FECHA_CALC,2) AND CD.FECHA_CALC " +
                                             "AND CLNS = 'G' " +
                                             "AND TIPO = 'PD' " +
                                             "AND ESTATUS <> 'E')) = CD.MONTO_CUOTA THEN 'NO PAGO' " +
                                  "ELSE 'INCOMPLETO' " +
                                  "END ESTATUS " +
                            "FROM TBL_CIERRE_DIA CD, NS, PRN " +
                            "WHERE CD. CDGEM = PRN.CDGEM " +
                            "AND CD.CDGCLNS = PRN.CDGNS " +
                            "AND CD.CICLO = PRN.CICLO " +
                            "AND CD.CDGEM = NS.CDGEM " +
                            "AND CD.CDGCLNS = NS.CODIGO " +
                            "AND CD.CDGEM = '" + empresa + "' " +
                            "AND CD.FECHA_CALC = '" + fecha + "' " +
                            "AND CD.SITUACION = 'E' " +
                            "AND CD.INICIO < CD.FECHA_CALC " +
                            strRegion +
                            strSucursal +
                            strCoord +
                            strAsesor +
                            "AND PRN.PERIODICIDAD = 'C' " +
                            "AND FNFECHAPROXPAGO(CD.INICIO,PRN.PERIODICIDAD,FLOOR((CD.FECHA_CALC - CD.INICIO)/14)) = '" + fecha + "' " +
                            "AND CD.PLAZO >= FLOOR((CD.FECHA_CALC - CD.INICIO)/14)";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS ESPERADOS CON DIFERENCIA
    [WebMethod]
    public string getRepPagosEsperadosDif(string fecha, string fecha1, string region, string sucursal, string coord, string asesor, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string strRegion = string.Empty;
        string strSucursal = string.Empty;
        string strCoord = string.Empty;
        string strAsesor = string.Empty;
        string xml = "";
        int iRes;
        try
        {
            string query = "SELECT * " +
                           "FROM REP_CONC_ACUERDO " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "CVE_USUARIO = '" + usuario + "'";
            
            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    // Genera reporte de pagos semanales esperados
    [WebMethod]
    public string getRepPagosEsperadosSemanales(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string status = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery(ref status, "SP_REP_PAGOS_TEORICOS", CommandType.StoredProcedure,
               oP.ParamsRepPagosSemanalesEsperados(empresa, fecha, usuario));

        string query = "SELECT NOMRG REGION " +
                       ",NOMCO SUCURSAL " +
                       ",NOMPE ASESOR " +
                       ",CDGCLNS GRUPO " +
                       ",NOMCLNS NOM_GRUPO " +
                       ",CICLO " +
                       ",TO_CHAR(INICIO,'DD/MM/YYYY') INICIO " +
                       ",TO_CHAR(FIN,'DD/MM/YYYY') FIN " +
                       ",LUNES " +
                       ",MARTES " +
                       ",MIERCOLES " +
                       ",JUEVES " +
                       ",VIERNES " +
                       ",DECODE(PERIODICIDAD,'S','SEMANAL','C','CATORCENAL','Q','QUINCENAL','M','MENSUAL') PERIODICIDAD " +
                       "FROM REP_PAGOS_TEORICOS " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "'";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS IDENTIFICADOS 
    //CONSIDERANDO EL TIPO DE FECHA
    [WebMethod]
    public string getRepPagosIdentFecha(int tipo, string fecIni, string fecFin, int tipoFecha)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        string fecAct = string.Empty;
        string fecSig = string.Empty;
        string strCap = string.Empty;
        string strInt = string.Empty;
        string strFecGL = string.Empty;
        string strFecPago = string.Empty;
        decimal cantidad;
        decimal capital;
        decimal interes;
        decimal deposito;
        decimal garantia;
        decimal total;
        bool cond = false;
        int cont;
        int i;

        //INDICA QUE LA BUSQUEDA SE REALIZARA UTILIZANDO LA FECHA DE OPERACION
        if (tipoFecha == 1)
        {
            strFecPago = "AND TRUNC(MP.IDENTIFICA) BETWEEN TO_DATE('" + fecIni + "', 'dd/mm/yyyy') AND TO_DATE('" + fecFin + "', 'dd/mm/yyyy') ";
            strFecGL = "AND TRUNC(PGS.FREGISTRO) BETWEEN TO_DATE('" + fecIni + "', 'dd/mm/yyyy') AND TO_DATE('" + fecFin + "', 'dd/mm/yyyy') ";
        }
        //INDICA QUE LA BUSQUEDA SE REALIZARA UTILIZADO LA FECHA VALOR
        else if (tipoFecha == 2)
        {
            strFecPago = "AND MP.FREALDEP BETWEEN TO_DATE('" + fecIni + "', 'dd/mm/yyyy') AND TO_DATE('" + fecFin + "', 'dd/mm/yyyy') ";
            strFecGL = "AND PGS.FPAGO BETWEEN TO_DATE('" + fecIni + "', 'dd/mm/yyyy') AND TO_DATE('" + fecFin + "', 'dd/mm/yyyy') ";
        }

        if (tipo == 1 || tipo == 4)
        {
            string strCuenta = string.Empty;
            if (tipo == 1)
                strCuenta = "AND MP.CDGCB <> '12' ";
            if (tipo == 4)
                strCuenta = "AND MP.CDGCB = '12' ";

            query = "SELECT TO_CHAR(FREALDEP,'DD/MM/YYYY') FECHAPAGO " +
                    ",MP.CDGCLNS " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT NOMBRE FROM NS WHERE CDGEM = MP.CDGEM AND CODIGO = MP.CDGCLNS) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = MP.CDGEM AND CODIGO = MP.CDGCLNS) " +
                    "END NOMBRE " +
                    ",MP.CICLO " +
                    //",MP.REFERENCIA " +
                    ",NULL REFERENCIA " +
                    ",MP.CDGCB ID_BANCO " +
                    ",IB.NOMBRE BANCO " +
                    ",CB.NUMERO CTA_BCO " +
                    ",MP.CANTIDAD " +
                    ",MP.SECUENCIA " +
                    ",MP.PAGADOCAP CAPITAL " +
                    ",MP.PAGADOINT INTERES " +
                    ",MP.PAGADOREC RECARGOS " +
                    ",NVL(ROUND(((MP.CANTIDAD) / (SELECT CD.TOTAL_PAGAR FROM TBL_CIERRE_DIA CD, PRN WHERE CD.CDGEM = MP.CDGEM AND CD.CDGCLNS = MP.CDGCLNS AND CD.CICLO = MP.CICLO AND CD.CLNS = MP.CLNS AND PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PRN.INICIO = CD.FECHA_CALC)) * (SELECT SUM(TOTAL) FROM MICROSEGURO WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CICLO = MP.CICLO AND CLNS = MP.CLNS AND ESTATUS IN ('R','V') AND (FORMA_PAGO IS NULL OR FORMA_PAGO = 'F')),2),0) SEGURO " +
                    ",MP.ACTUALIZARPE USUARIO " +
                    ",TO_CHAR(IDENTIFICA,'DD/MM/YYYY') FECHAOPERA " +
                    ",TO_CHAR(NVL((SELECT FDEPOSITO FROM PDI WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CICLO = MP.CICLO AND CLNS = MP.CLNS AND FECHAIM = MP.FREALDEP AND SECUENCIAIM = MP.SECUENCIA AND ESTATUS = 'IP'), MP.FREALDEP),'DD/MM/YYYY') FECHADEP " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT CDGOCPE FROM PRN WHERE CDGEM = MP.CDGEM AND CDGNS = MP.CDGNS AND CICLO = MP.CICLO) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT CDGOCPE FROM PRC WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CLNS = MP.CLNS AND CICLO = MP.CICLO) " +
                    "END ASESOR " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PRN, PE WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PE.CDGEM = PRN.CDGEM AND PE.CODIGO = PRN.CDGOCPE) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PRC, PE WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND PE.CDGEM = PRC.CDGEM AND PE.CODIGO = PRC.CDGOCPE) " +
                    "END NOM_ASESOR " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT PE.TELEFONO FROM PRN, PE WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PE.CDGEM = PRN.CDGEM AND PE.CODIGO = PRN.CDGOCPE) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT PE.TELEFONO FROM PRC, PE WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND PE.CDGEM = PRC.CDGEM AND PE.CODIGO = PRC.CDGOCPE) " +
                    "END NUM_NOMINA " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT CO.NOMBRE FROM PRN, CO WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGNS AND PRN.CICLO = MP.CICLO AND CO.CDGEM = PRN.CDGEM AND CO.CODIGO = PRN.CDGCO) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT CO.NOMBRE FROM PRC, CO WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND CO.CDGEM = PRC.CDGEM AND CO.CODIGO = PRC.CDGCO) " +
                    "END SUCURSAL " +
                    "FROM CB, IB, MP " +
                    "WHERE CB.CDGEM = IB.CDGEM " +
                    "AND CB.CDGIB = IB.CODIGO " +
                    "AND MP.CDGEM = CB.CDGEM " +
                    "AND MP.CDGEM = IB.CDGEM " +
                    "AND MP.CDGCB = CB.CODIGO " +
                    "AND CB.CDGEM = '" + empresa + "' " +
                    "AND MP.TIPO = 'PD' " +
                    "AND MP.ESTATUS <> 'E' " +
                    strCuenta +
                    strFecPago +
                    "ORDER BY MP.FREALDEP, MP.IDENTIFICA, MP.CDGCLNS, MP.CICLO";
        }
        else if (tipo == 2)
        {
            query = "SELECT TO_CHAR(PGS.FPAGO,'DD/MM/YYYY') FECHAPAGO " +
                    ",PGS.CDGCLNS " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT NOMBRE FROM NS WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "END NOMBRE " +
                    ",PGS.CICLO " +
                    ",PGS.REFERENCIA " +
                    ",PGS.CDGCB ID_BANCO " +
                    ",IB.NOMBRE BANCO " +
                    ",CB.NUMERO CTA_BCO " +
                    ",PGS.CANTIDAD " +
                    ",TO_CHAR(PGS.SECPAGO) SECUENCIA " +
                    ",NULL AS CAPITAL " +
                    ",NULL AS INTERES " +
                    ",NULL AS RECARGOS " +
                    ",NULL AS SEGURO " +
                    ",PGS.CDGPE USUARIO " +
                    ",TO_CHAR(FREGISTRO,'DD/MM/YYYY') FECHAOPERA " +
                    ",TO_CHAR(NVL((SELECT FDEPOSITO FROM PDI WHERE CDGEM = PGS.CDGEM AND CDGCLNS = PGS.CDGCLNS AND CLNS = PGS.CLNS AND FECHAIM = PGS.FPAGO AND SECUENCIAIM = PGS.SECPAGO AND ESTATUS = 'IG'), PGS.FPAGO),'DD/MM/YYYY') FECHADEP " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT CDGACPE FROM NS WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT CDGOCPE FROM CL WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "END ASESOR " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM NS, PE WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND PE.CDGEM = NS.CDGEM AND PE.CODIGO = NS.CDGACPE) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM CL, PE WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND PE.CDGEM = CL.CDGEM AND PE.CODIGO = CL.CDGOCPE) " +
                    "END NOM_ASESOR " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT PE.TELEFONO FROM NS, PE WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND PE.CDGEM = NS.CDGEM AND PE.CODIGO = NS.CDGACPE) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT PE.TELEFONO FROM CL, PE WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND PE.CDGEM = CL.CDGEM AND PE.CODIGO = CL.CDGOCPE) " +
                    "END NUM_NOMINA " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT CO.NOMBRE FROM NS, CO WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND CO.CDGEM = NS.CDGEM AND CO.CODIGO = NS.CDGCO) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT CO.NOMBRE FROM CL, CO WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND CO.CDGEM = CL.CDGEM AND CO.CODIGO = CL.CDGCO) " +
                    "END SUCURSAL " +
                    "FROM CB, IB, PAG_GAR_SIM PGS " +
                    "WHERE CB.CDGEM = IB.CDGEM " +
                    "AND CB.CDGIB = IB.CODIGO " +
                    "AND PGS.CDGEM = CB.CDGEM " +
                    "AND PGS.CDGEM = IB.CDGEM " +
                    "AND PGS.CDGCB = CB.CODIGO " +
                    "AND PGS.CDGEM = '" + empresa + "' " +
                    "AND PGS.ESTATUS = 'RE' " +
                    "AND PGS.CDGCB NOT IN ('12','19') " +
                    strFecGL +
                    "ORDER BY PGS.FPAGO, PGS.FREGISTRO, PGS.CDGCLNS, PGS.CICLO";
        }
        else if (tipo == 3)
        {
            query = "SELECT TO_CHAR(FREALDEP,'DD/MM/YYYY') FECHAPAGO " +
                    ",MP.CDGCLNS " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT NOMBRE FROM NS WHERE CDGEM = MP.CDGEM AND CODIGO = MP.CDGCLNS) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = MP.CDGEM AND CODIGO = MP.CDGCLNS) " +
                    "END NOMBRE " +
                    ",MP.CICLO " +
                    ",MP.REFERENCIA " +
                    ",MP.CDGCB ID_BANCO " +
                    ",IB.NOMBRE BANCO " +
                    ",CB.NUMERO CTA_BCO " +
                    ",MP.CANTIDAD " +
                    ",MP.SECUENCIA " +
                    ",MP.PAGADOCAP CAPITAL " +
                    ",MP.PAGADOINT INTERES " +
                    ",MP.PAGADOREC RECARGOS " +
                    ",NVL(ROUND(((MP.CANTIDAD) / (SELECT CD.TOTAL_PAGAR FROM TBL_CIERRE_DIA CD, PRN WHERE CD.CDGEM = MP.CDGEM AND CD.CDGCLNS = MP.CDGCLNS AND CD.CICLO = MP.CICLO AND CD.CLNS = MP.CLNS AND PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PRN.INICIO = CD.FECHA_CALC)) * (SELECT SUM(TOTAL) FROM MICROSEGURO WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CICLO = MP.CICLO AND CLNS = MP.CLNS AND ESTATUS IN ('R','V') AND (FORMA_PAGO IS NULL OR FORMA_PAGO = 'F')),2),0) SEGURO " +
                    ",MP.ACTUALIZARPE USUARIO " +
                    ",TO_CHAR(IDENTIFICA,'DD/MM/YYYY') FECHAOPERA " +
                    ",TO_CHAR(NVL((SELECT FDEPOSITO FROM PDI WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CICLO = MP.CICLO AND CLNS = MP.CLNS AND FECHAIM = MP.FREALDEP AND SECUENCIAIM = MP.SECUENCIA AND ESTATUS = 'IP'), MP.FREALDEP),'DD/MM/YYYY') FECHADEP " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT CDGOCPE FROM PRN WHERE CDGEM = MP.CDGEM AND CDGNS = MP.CDGNS AND CICLO = MP.CICLO) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT CDGOCPE FROM PRC WHERE CDGEM = MP.CDGEM AND CDGCLNS = MP.CDGCLNS AND CLNS = MP.CLNS AND CICLO = MP.CICLO) " +
                    "END ASESOR " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PRN, PE WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PE.CDGEM = PRN.CDGEM AND PE.CODIGO = PRN.CDGOCPE) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM PRC, PE WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND PE.CDGEM = PRC.CDGEM AND PE.CODIGO = PRC.CDGOCPE) " +
                    "END NOM_ASESOR " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT PE.TELEFONO FROM PRN, PE WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGCLNS AND PRN.CICLO = MP.CICLO AND PE.CDGEM = PRN.CDGEM AND PE.CODIGO = PRN.CDGOCPE) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT PE.TELEFONO FROM PRC, PE WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND PE.CDGEM = PRC.CDGEM AND PE.CODIGO = PRC.CDGOCPE) " +
                    "END NUM_NOMINA " +
                    ",CASE WHEN MP.CLNS = 'G' THEN " +
                        "(SELECT CO.NOMBRE FROM PRN, CO WHERE PRN.CDGEM = MP.CDGEM AND PRN.CDGNS = MP.CDGNS AND PRN.CICLO = MP.CICLO AND CO.CDGEM = PRN.CDGEM AND CO.CODIGO = PRN.CDGCO) " +
                    "WHEN MP.CLNS = 'I' THEN " +
                        "(SELECT CO.NOMBRE FROM PRC, CO WHERE PRC.CDGEM = MP.CDGEM AND PRC.CDGCLNS = MP.CDGCLNS AND PRC.CLNS = MP.CLNS AND PRC.CICLO = MP.CICLO AND CO.CDGEM = PRC.CDGEM AND CO.CODIGO = PRC.CDGCO) " +
                    "END SUCURSAL " +
                    "FROM CB, IB, MP " +
                    "WHERE CB.CDGEM = IB.CDGEM " +
                    "AND CB.CDGIB = IB.CODIGO " +
                    "AND MP.CDGEM = CB.CDGEM " +
                    "AND MP.CDGEM = IB.CDGEM " +
                    "AND MP.CDGCB = CB.CODIGO " +
                    "AND CB.CDGEM = '" + empresa + "' " +
                    "AND MP.TIPO = 'PD' " +
                    "AND MP.ESTATUS <> 'E' " +
                    "AND MP.CDGCB NOT IN ('12','19') " +
                    strFecPago +
                    "UNION " +
                    "SELECT TO_CHAR(PGS.FPAGO,'DD/MM/YYYY') FECHAPAGO " +
                    ",PGS.CDGCLNS " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT NOMBRE FROM NS WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "END NOMBRE " +
                    ",PGS.CICLO " +
                    ",PGS.REFERENCIA " +
                    ",PGS.CDGCB ID_BANCO " +
                    ",IB.NOMBRE BANCO " +
                    ",CB.NUMERO CTA_BCO " +
                    ",PGS.CANTIDAD " +
                    ",TO_CHAR(PGS.SECPAGO) SECUENCIA " +
                    ",NULL AS CAPITAL " +
                    ",NULL AS INTERES " +
                    ",NULL AS RECARGOS " +
                    ",NULL AS SEGURO " +
                    ",PGS.CDGPE USUARIO " +
                    ",TO_CHAR(FREGISTRO,'DD/MM/YYYY') FECHAOPERA " +
                    ",TO_CHAR(NVL((SELECT FDEPOSITO FROM PDI WHERE CDGEM = PGS.CDGEM AND CDGCLNS = PGS.CDGCLNS AND CLNS = PGS.CLNS AND FECHAIM = PGS.FPAGO AND SECUENCIAIM = PGS.SECPAGO AND ESTATUS = 'IG'), PGS.FPAGO),'DD/MM/YYYY') FECHADEP " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT CDGACPE FROM NS WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT CDGOCPE FROM CL WHERE CDGEM = PGS.CDGEM AND CODIGO = PGS.CDGCLNS) " +
                    "END ASESOR " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM NS, PE WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND PE.CDGEM = NS.CDGEM AND PE.CODIGO = NS.CDGACPE) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) FROM CL, PE WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND PE.CDGEM = CL.CDGEM AND PE.CODIGO = CL.CDGOCPE) " +
                    "END NOM_ASESOR " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT PE.TELEFONO FROM NS, PE WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND PE.CDGEM = NS.CDGEM AND PE.CODIGO = NS.CDGACPE) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT PE.TELEFONO FROM CL, PE WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND PE.CDGEM = CL.CDGEM AND PE.CODIGO = CL.CDGOCPE) " +
                    "END NUM_NOMINA " +
                    ",CASE WHEN PGS.CLNS = 'G' THEN " +
                        "(SELECT CO.NOMBRE FROM NS, CO WHERE NS.CDGEM = PGS.CDGEM AND NS.CODIGO = PGS.CDGCLNS AND CO.CDGEM = NS.CDGEM AND CO.CODIGO = NS.CDGCO) " +
                    "WHEN PGS.CLNS = 'I' THEN " +
                        "(SELECT CO.NOMBRE FROM CL, CO WHERE CL.CDGEM = PGS.CDGEM AND CL.CODIGO = PGS.CDGCLNS AND CO.CDGEM = CL.CDGEM AND CO.CODIGO = CL.CDGCO) " +
                    "END SUCURSAL " +
                    "FROM CB, IB, PAG_GAR_SIM PGS " +
                    "WHERE CB.CDGEM = IB.CDGEM " +
                    "AND CB.CDGIB = IB.CODIGO " +
                    "AND PGS.CDGEM = CB.CDGEM " +
                    "AND PGS.CDGEM = IB.CDGEM " +
                    "AND PGS.CDGCB = CB.CODIGO " +
                    "AND PGS.CDGEM = '" + empresa + "' " +
                    "AND PGS.ESTATUS = 'RE' " +
                    "AND PGS.CDGCB NOT IN ('12','19') " +
                    strFecGL +
                    "ORDER BY 1,2,4,15";
        }

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

        if (res == 1)
        {
            cont = dref.Tables[0].Rows.Count;
            dsPagos.dtPagosDataTable dt = new dsPagos.dtPagosDataTable();
            try
            {
                cantidad = 0;
                capital = 0;
                interes = 0;
                deposito = 0;
                garantia = 0;
                total = 0;
                for (i = 0; i < cont; i++)
                {
                    cond = false;
                    DataRow dr = dt.NewRow();
                    dr["FECHA"] = dref.Tables[0].Rows[i]["FECHAPAGO"].ToString();
                    dr["FECHAOPERA"] = dref.Tables[0].Rows[i]["FECHAOPERA"].ToString();
                    dr["FECHADEP"] = dref.Tables[0].Rows[i]["FECHADEP"].ToString();
                    dr["CODIGO"] = dref.Tables[0].Rows[i]["CDGCLNS"].ToString();
                    dr["GRUPO"] = dref.Tables[0].Rows[i]["NOMBRE"].ToString();
                    dr["REFERENCIA"] = dref.Tables[0].Rows[i]["REFERENCIA"].ToString();
                    dr["CICLO"] = dref.Tables[0].Rows[i]["CICLO"].ToString();
                    dr["CANTIDAD"] = dref.Tables[0].Rows[i]["CANTIDAD"].ToString();
                    dr["CDGCB"] = dref.Tables[0].Rows[i]["ID_BANCO"].ToString();
                    dr["BANCO"] = dref.Tables[0].Rows[i]["BANCO"].ToString();
                    dr["CUENTA"] = dref.Tables[0].Rows[i]["CTA_BCO"].ToString();
                    dr["SECUENCIA"] = dref.Tables[0].Rows[i]["SECUENCIA"].ToString();
                    dr["CAPITAL"] = dref.Tables[0].Rows[i]["CAPITAL"].ToString();
                    dr["INTERES"] = dref.Tables[0].Rows[i]["INTERES"].ToString();
                    dr["RECARGOS"] = dref.Tables[0].Rows[i]["RECARGOS"].ToString();
                    dr["SEGURO"] = dref.Tables[0].Rows[i]["SEGURO"].ToString();
                    dr["USUARIO"] = dref.Tables[0].Rows[i]["USUARIO"].ToString();
                    dr["ASESOR"] = dref.Tables[0].Rows[i]["ASESOR"].ToString();
                    dr["NOMASESOR"] = dref.Tables[0].Rows[i]["NOM_ASESOR"].ToString();
                    dr["NUMNOMINA"] = dref.Tables[0].Rows[i]["NUM_NOMINA"].ToString();
                    dr["SUCURSAL"] = dref.Tables[0].Rows[i]["SUCURSAL"].ToString();

                    cantidad += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTIDAD"].ToString());
                    strCap = dref.Tables[0].Rows[i]["CAPITAL"].ToString();
                    strInt = dref.Tables[0].Rows[i]["INTERES"].ToString();
                    if (strCap != "" || strInt != "")
                    {
                        capital += strCap != "" ? Convert.ToDecimal(strCap) : 0;
                        interes += strInt != "" ? Convert.ToDecimal(strInt) : 0;
                        deposito += strCap != "" ? Convert.ToDecimal(strCap) : 0;
                        deposito += strInt != "" ? Convert.ToDecimal(strInt) : 0;
                    }
                    else
                    {
                        garantia += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTIDAD"].ToString());
                    }
                    total += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTIDAD"].ToString());

                    dt.Rows.Add(dr);

                    if (i + 1 < cont)
                    {
                        fecSig = dref.Tables[0].Rows[i + 1]["FECHAPAGO"].ToString();
                    }
                    fecAct = dref.Tables[0].Rows[i]["FECHAPAGO"].ToString();

                    if (fecSig != fecAct)
                    {
                        DataRow dfec = dt.NewRow();
                        dfec["FECHA"] = "-- SUBTOTAL --";
                        dfec["CANTIDAD"] = cantidad;
                        dt.Rows.Add(dfec);
                        DataRow drLine = dt.NewRow();
                        dt.Rows.Add(drLine);
                        cantidad = 0;
                        cond = true;
                    }
                }
                if (i == cont && cond == false)
                {
                    DataRow dfec = dt.NewRow();
                    dfec["FECHA"] = "-- SUBTOTAL --";
                    dfec["CANTIDAD"] = cantidad;
                    dt.Rows.Add(dfec);
                    DataRow drLine = dt.NewRow();
                    dt.Rows.Add(drLine);
                }
                DataRow dtot = dt.NewRow();
                dtot["FECHA"] = "-- TOTAL --";
                dtot["REFERENCIA"] = cont;
                dtot["CANTIDAD"] = total;
                dtot["CAPITAL"] = capital;
                dtot["INTERES"] = interes;
                dtot["RECARGOS"] = deposito;
                dtot["BANCO"] = garantia;
                dt.Rows.Add(dtot);
                ds.Tables.Add(dt);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "";
            }

            xml = ds.GetXml();
        }
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS DE MICROSEGUROS
    [WebMethod]
    public string getRepPagosMicroseguros(string mes, string anio)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string fechaInicio = "01/" + mes + "/" + anio;
        string fechaFin = "LAST_DAY(TO_DATE('" + fechaInicio + "', 'DD/MM/YYYY'))";
        string xml = "";
        int iRes;
        try
        {
            string query = "SELECT A.CDGCLNS GRUPO " +
                           ",(SELECT NOMBRE FROM NS WHERE CDGEM = A.CDGEM AND CODIGO = A.CDGCLNS) NOM_GRUPO " +
                           ",A.CICLO " +
                           ",A.CDGCL ACREDITADO " +
                           ",(SELECT NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) FROM CL WHERE CDGEM = A.CDGEM AND CODIGO = A.CDGCL) NOM_ACREDITADO " +
                           ",TO_CHAR(A.INICIO, 'DD/MM/YYYY') INICIO " +
                           ",SUM(A.PAGO_MICRO_SIN_IVA) PAGO_SEG_SIN_IVA " +
                           ",SUM(A.PAGO_MICRO) - SUM(A.PAGO_MICRO_SIN_IVA) IVA_SEG " +
                           "FROM " +
                           "(SELECT PRC.CDGEM " +
                           ",PRC.CDGCLNS " +
                           ",PRC.CICLO " +
                           ",PRC.CDGCL " +
                           ",PRN.INICIO " +
                           ",NVL(ROUND(((MP.CANTIDAD * (PRC.CANTENTRE / PRN.CANTENTRE)) / (CD.TOTAL_PAGAR * (PRC.CANTENTRE / PRN.CANTENTRE))) * (SELECT TOTAL FROM MICROSEGURO WHERE CDGEM = PRC.CDGEM AND CDGCLNS = PRC.CDGCLNS AND CICLO = PRC.CICLO AND CDGCL = PRC.CDGCL AND ESTATUS IN ('R','V') AND (FORMA_PAGO IS NULL OR FORMA_PAGO = 'F')),2),0) PAGO_MICRO " +
                           ",NVL(ROUND((ROUND(((MP.CANTIDAD * (PRC.CANTENTRE / PRN.CANTENTRE)) / (CD.TOTAL_PAGAR * (PRC.CANTENTRE / PRN.CANTENTRE))) * (SELECT TOTAL FROM MICROSEGURO WHERE CDGEM = PRC.CDGEM AND CDGCLNS = PRC.CDGCLNS AND CICLO = PRC.CICLO AND CDGCL = PRC.CDGCL AND ESTATUS IN ('R','V') AND (FORMA_PAGO IS NULL OR FORMA_PAGO = 'F')),2) / 1.16),2),0) PAGO_MICRO_SIN_IVA " +
                           "FROM MP, PRN, PRC, TBL_CIERRE_DIA CD " +
                           "WHERE MP.CDGEM = '" + empresa + "' " + 
                           "AND MP.FREALDEP BETWEEN '" + fechaInicio + "' AND " + fechaFin + " " +
                           "AND MP.TIPO IN ('PD') " +
                           "AND PRN.CDGEM = MP.CDGEM " +
                           "AND PRN.CDGNS = MP.CDGCLNS " +
                           "AND PRN.CICLO = MP.CICLO " +
                           "AND PRN.SITUACION IN ('E','L') " +
                           "AND PRC.CDGEM = PRN.CDGEM " +
                           "AND PRC.CDGNS = PRN.CDGNS " +
                           "AND PRC.CICLO = PRN.CICLO " +
                           "AND PRC.CLNS = MP.CLNS " +
                           "AND PRC.SITUACION IN ('E','L') " +
                           "AND CD.CDGEM = MP.CDGEM " +
                           "AND CD.CDGCLNS = MP.CDGCLNS " +
                           "AND CD.CICLO = MP.CICLO " +
                           "AND CD.FECHA_CALC = PRN.INICIO) A " +
                           "GROUP BY A.CDGEM " +
                           ",A.CDGCLNS " +
                           ",A.CICLO " +
                           ",A.CDGCL " +
                           ",A.INICIO " +
                           "HAVING SUM(A.PAGO_MICRO) > 0 " +
                           "ORDER BY 1";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS ESPERADOS CON DIFERENCIA
    [WebMethod]
    public string getRepPagosPayCash(string fecha)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;
        try
        {
            string query = "SELECT (SELECT COUNT(*) FROM PAYCASH_PAGO WHERE FECHA BETWEEN '" + fecha + "' AND '" + fecha + "' AND PROCESADO = 'S') MOVIMIENTOS " +
                           ",(SELECT SUM(MONTO) FROM PAYCASH_PAGO WHERE FECHA BETWEEN '" + fecha + "' AND '" + fecha + "' AND PROCESADO = 'S') MONTO_MOVS " +
                           ",(SELECT COUNT(*) FROM MP WHERE FREALDEP BETWEEN '" + fecha + "' AND '" + fecha + "' AND TIPO = 'PD' AND CDGCB = '09') PAGOS " +
                           ",(SELECT COUNT(*) FROM PAG_GAR_SIM WHERE FPAGO BETWEEN '" + fecha + "' AND '" + fecha + "' AND ESTATUS = 'RE' AND CDGCB = '09') GL " +
                           ",(SELECT COUNT(*) FROM PDI WHERE FDEPOSITO BETWEEN '" + fecha + "' AND '" + fecha + "' AND ESTATUS = 'RE' AND CDGCB = '09') NOIDEN " +
                           ",(SELECT COUNT(*) FROM (SELECT CDGCLNS, CANTIDAD, COUNT(*) FROM MP WHERE CDGEM = '" + empresa + "' AND FREALDEP = '" + fecha + "' AND TIPO = 'PD' AND CDGCB = '09' GROUP BY CDGCLNS, CANTIDAD HAVING COUNT(*) > 1)) PAGOS_DUP " +
                           ",(SELECT COUNT(*) FROM (SELECT CDGCLNS, CANTIDAD, COUNT(*) FROM PAG_GAR_SIM WHERE CDGEM = '" + empresa + "' AND FPAGO = '" + fecha + "' AND ESTATUS = 'RE' AND CDGCB = '09' GROUP BY CDGCLNS, CANTIDAD HAVING COUNT(*) > 1)) GARS_DUP " +
                           ",(SELECT COUNT(*) FROM (SELECT PR.CDGCLNS, (SELECT COUNT(*) " +
                                                                       "FROM PAYCASH_PAGO A, PAYCASH_REF B " +
                                                                       "WHERE A.CDGEM = PP.CDGEM " +
                                                                       "AND A.MONTO = PP.MONTO " +
                                                                       "AND A.FECHA = PP.FECHA " +
                                                                       "AND A.REFERENCIA <> PP.REFERENCIA " +
                                                                       "AND B.CDGEM = A.CDGEM " +
                                                                       "AND B.REF_PAYCASH = A.REFERENCIA " +
                                                                       "AND B.CDGCLNS = PR.CDGCLNS) CONTEO " +
                                                   "FROM PAYCASH_REF PR, PAYCASH_PAGO PP " +
                                                   "WHERE PP.CDGEM = '" + empresa + "' " +
                                                   "AND PP.FECHA = '" + fecha + "' " +
                                                   "AND PR.CDGEM = PP.CDGEM " +
                                                   "AND PR.REF_PAYCASH = PP.REFERENCIA " +
                                                   "ORDER BY PP.FREGISTRO DESC, PP.HORA DESC) C " +
                                                   "WHERE C.CONTEO > 0) MOVS_DUP " +
                           ",(SELECT COUNT(*) FROM (SELECT PR.CDGCLNS, (SELECT COUNT(*) " +
                                                                       "FROM PAYCASH_PAGO A " +
                                                                       "WHERE A.CDGEM = PP.CDGEM " + 
                                                                       "AND A.REFERENCIA = PP.REFERENCIA " +
                                                                       "AND A.MONTO = PP.MONTO " +
                                                                       "AND A.FECHA = '" + fecha + "') DUPLICADO " +
                                                    "FROM PAYCASH_REF PR, PAYCASH_PAGO PP " +
                                                    "WHERE PR.CDGEM = '" + empresa + "' " +
                                                    "AND PP.FECHA = TO_DATE('" + fecha + "') - 1 " +
                                                    "AND PP.REFERENCIA = PR.REF_PAYCASH " +
                                                    "ORDER BY PP.FREGISTRO DESC, PP.HORA DESC) A " +
                                                    "WHERE A.DUPLICADO > 0) MOVS_DUP_ANT " +  
                           "FROM DUAL";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PAGOS NO IDENTIFICADOS
    [WebMethod]
    public string getRepPDI(string fecIni, string fecFin, int noIden, int iden)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;

        try
        {
            if (noIden == 1)
                queryEstatus = "'RE'";
            if (iden == 1)
                queryEstatus += (queryEstatus != "" ? "," : "") + "'IP','IG'";


            string query = "SELECT TO_CHAR(FDEPOSITO,'DD/MM/YYYY') FECHA, " +
                           "PDI.REFERENCIA, " +
                           "PDI.CANTIDAD, " +
                           "IB.NOMBRE BANCO, " +
                           "PDI.CDGCB, " +
                           "CB.NUMERO CUENTA, " +
                //"PDI.DESCRIPCION, " +
                           "REPLACE(PDI.DESCRIPCION, 'Resultado de la validación: ', '') DESCRIPCION, " +
                           "PDI.SECUENCIA, " +
                           "PDI.CDGCLNS, " +
                           "(CASE WHEN PDI.CDGCLNS IS NOT NULL THEN " +
                                     "(SELECT NS.NOMBRE " +
                                      "FROM NS " +
                                      "WHERE NS.CDGEM = PDI.CDGEM " +
                                      "AND NS.CODIGO = PDI.CDGCLNS) " +
                                 "ELSE '' END) NOMCLNS, " +
                           "PDI.CICLO, " +
                           "PDI.CDGPE_IDEN, " +
                           "(CASE WHEN PDI.CDGPE_IDEN IS NOT NULL THEN " +
                                     "(SELECT NOMBREC(NULL,NULL,NULL,'A',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) " +
                                      "FROM PE " +
                                      "WHERE PE.CDGEM = PDI.CDGEM " +
                                      "AND PE.CODIGO = PDI.CDGPE_IDEN) " +
                                 "ELSE '' END) NOMPE_IDEN, " +
                           "TO_CHAR(PDI.FECHA_IDEN,'DD/MM/YYYY') FECHAIDEN " +
                           "FROM PDI, CB, IB " +
                           "WHERE PDI.CDGEM = '" + empresa + "' " +
                           "AND PDI.ESTATUS IN (" + queryEstatus + ") " +
                           "AND PDI.CDGCB <> '12' " +
                           "AND PDI.FDEPOSITO BETWEEN TO_DATE('" + fecIni + "', 'DD/MM/YYYY') AND TO_DATE('" + fecFin + "', 'DD/MM/YYYY') " +
                           "AND PDI.CDGEM = CB.CDGEM " +
                           "AND PDI.CDGCB = CB.CODIGO " +
                           "AND CB.CDGEM = IB.CDGEM " +
                           "AND CB.CDGIB = IB.CODIGO " +
                           "ORDER BY FDEPOSITO";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["FECHA"] = "-- TOTAL --";
                dtot["REFERENCIA"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(CANTIDAD)", ""));
                dtot["CANTIDAD"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(CANTIDAD)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }

            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE EL ANALISIS DE ALERTAS DE PLD
    [WebMethod]
    public string getRepPersonasExpuestas(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;
        try
        {
            string query = "SELECT TO_CHAR(CR.FREGISTRO, 'DD/MM/YYYY HH24:MM') FREGISTRO " +
                           ",CR.CDGCL " +
                           ",NOMBREC(NULL,NULL,'I','N',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) NOMBRECL " +
                           ",DECODE(NIVEL,'A','ALTO','M','MEDIO','B','BAJO') NIVEL " +
                           ",OBSERVACION " +
                           ",CDGPE " +
                           ",NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMUSUARIO " +
                           "FROM CONSULTA_PEP CR " +
                           "INNER JOIN CL " +
                           "ON CL.CDGEM = '" + empresa + "' " +
                           "AND CR.CDGCL = CL.CODIGO " +
                           "INNER JOIN PE " +
                           "ON PE.CDGEM = CR.CDGEM " +
                           "AND PE.CODIGO = CR.CDGPE " +
                           "WHERE CR.CDGEM = '" + empresa + "' " +
                           "AND TRUNC(CR.FREGISTRO) >= '" + fechaIni + "' " +
                           "AND TRUNC(CR.FREGISTRO) <= '" + fechaFin + "'";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION GENERAL DE LOS CLIENTES MARCADOS PARA FONDEO
    [WebMethod]
    public string getRepPersonasFondeo(string orgFond, string lineaCred, string fecSaldo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryLinea = string.Empty;

        if (lineaCred != "0")
            queryLinea = " AND F.CDGLC = '" + lineaCred + "' ";

        try
        {
            string query = " SELECT PRC.CDGCL PERSONA_ID " +
                "                 , CL.CURP" +
                "                 , '''' || CL.IFE INE " +
                "                 , CL.PRIMAPE PRIMER_AP " +
                "                 , CL.SEGAPE SEGUNDO_AP " +
                "                 , CASE WHEN CL.NOMBRE2 IS NULL THEN CL.NOMBRE1 " +
                "                        ELSE CL.NOMBRE1 || ' ' || CL.NOMBRE2 " +
                "                         END NOMBRE " +
                "                 , TO_CHAR(CL.NACIMIENTO, 'DD/MM/YYYY') FECHA_NAC " +
                "                 , CASE WHEN EF.NOMBRE = 'AGUASCALIENTES' THEN 'AS' " +
                "                        WHEN EF.NOMBRE = 'BAJA CALIFORNIA' THEN 'BC' " +
                "                        WHEN EF.NOMBRE = 'BAJA CALIFORNIA SUR' THEN 'BS' " +
                "                        WHEN EF.NOMBRE = 'CAMPECHE' THEN 'CC' " +
                "                        WHEN EF.NOMBRE = 'COAHUILA' THEN 'CL' " +
                "                        WHEN EF.NOMBRE = 'COLIMA' THEN 'CM' " +
                "                        WHEN EF.NOMBRE = 'CHIAPAS' THEN 'CS' " +
                "                        WHEN EF.NOMBRE = 'CHIHUAHUA' THEN 'CH' " +
                "                        WHEN EF.NOMBRE = 'DISTRITO FEDERAL' THEN 'DF' " +
                "                        WHEN EF.NOMBRE = 'DURANGO' THEN 'DG' " +
                "                        WHEN EF.NOMBRE = 'GUANAJUATO' THEN 'GT' " +
                "                        WHEN EF.NOMBRE = 'GUERRERO' THEN 'GR' " +
                "                        WHEN EF.NOMBRE = 'HIDALGO' THEN 'HG' " +
                "                        WHEN EF.NOMBRE = 'JALISCO' THEN 'JC' " +
                "                        WHEN EF.NOMBRE = 'MEXICO' THEN 'MC' " +
                "                        WHEN EF.NOMBRE = 'MICHOACAN' THEN 'MN' " +
                "                        WHEN EF.NOMBRE = 'MORELOS' THEN 'MS' " +
                "                        WHEN EF.NOMBRE = 'NAYARIT' THEN 'NT' " +
                "                        WHEN EF.NOMBRE = 'NUEVO LEON' THEN 'NL' " +
                "                        WHEN EF.NOMBRE = 'OAXACA' THEN 'OC' " +
                "                        WHEN EF.NOMBRE = 'PUEBLA' THEN 'PL' " +
                "                        WHEN EF.NOMBRE = 'QUERETARO' THEN 'QT' " +
                "                        WHEN EF.NOMBRE = 'QUINTANA ROO' THEN 'QR' " +
                "                        WHEN EF.NOMBRE = 'SAN LUIS POTOSI' THEN 'SP' " +
                "                        WHEN EF.NOMBRE = 'SINALOA' THEN 'SL' " +
                "                        WHEN EF.NOMBRE = 'SONORA' THEN 'SR' " +
                "                        WHEN EF.NOMBRE = 'TABASCO' THEN 'TC' " +
                "                        WHEN EF.NOMBRE = 'TAMAULIPAS' THEN 'TS' " +
                "                        WHEN EF.NOMBRE = 'TLAXCALA' THEN 'TL' " +
                "                        WHEN EF.NOMBRE = 'VERACRUZ' THEN 'VZ' " +
                "                        WHEN EF.NOMBRE = 'YUCATAN' THEN 'YN' " +
                "                        WHEN EF.NOMBRE = 'ZACATECAS' THEN 'ZS' " +
                "                        ELSE 'NE' END CVE_EDO_NAC " +
                "                 , DECODE(CL.SEXO, 'F', '1', 'M', '2') SEXO " +
                "                 , NULL TELEFONO " +
                "                 , CASE WHEN CL.EDOCIVIL = 'S' THEN 1 " +
                "                        WHEN CL.EDOCIVIL = 'C' THEN 2 " +
                "                        WHEN CL.EDOCIVIL = 'V' THEN 3 " +
                "                        WHEN CL.EDOCIVIL = 'D' THEN 4 " +
                "                        WHEN CL.EDOCIVIL = 'U' THEN 5 " +
                "                        WHEN CL.EDOCIVIL IS NULL THEN 1 " +
                "                         END CVE_EDO_CIVIL " +
                "                 , 5 TIPO_VIALIDAD " +
                "                 , CL.CALLE NOMBRE_VIALIDAD " +
                "                 , NULL NUM_EXT_NUM " +
                "                 , NULL NUM_EXT_ALF " +
                "                 , 'NA' NUM_EXT_ANT " +
                "                 , 0 NUM_INT_NUM " +
                "                 , NULL NUM_INT_ALF " +
                "                 , 7 TIPO_ASENTAMIENTO " +
                "                 , COL.NOMBRE NOMBRE_ASENTAMIENTO " +
                "                 , COL.CDGPOSTAL CP " +
                "                 , '''' || LPAD(IA.ID_INEGI, 9, '0') CVE_LOCALIDAD " +
                "                 , CASE WHEN CL.NIVESCOLAR = 'P' THEN 1 " +
                "                        WHEN CL.NIVESCOLAR = 'S' THEN 2 " +
                "                        WHEN CL.NIVESCOLAR = 'T' THEN 3 " +
                "                        WHEN CL.NIVESCOLAR = 'U' THEN 3 " +
                "                        WHEN CL.NIVESCOLAR = 'C' THEN 3 " +
                "                        WHEN CL.NIVESCOLAR = 'B' THEN 4 " +
                "                        WHEN CL.NIVESCOLAR = 'L' THEN 5 " +
                "                        WHEN CL.NIVESCOLAR = 'O' THEN 6 " +
                "                        WHEN CL.NIVESCOLAR = 'R' THEN 6 " +
                "                        WHEN CL.NIVESCOLAR = 'N' THEN 7 " +
                "                        WHEN CL.NIVESCOLAR IS NULL THEN 7 " +
                "                         END ESTUDIOS " +
                "                 , 561990 ACTIVIDAD " +
                "                 , TO_CHAR(PRN.INICIO, 'DD/MM/YYYY') FEC_INICIO_ACT_PRODUCTIVA " +
                "                 , 17 UBICACION_NEGOCIO " +
                "                 , 0 PERSONAS_TRABAJANDO " +
                "                 , 4 ROL_EN_HOGAR " +
                "                 , NULL RFC " +
                "                 , NULL FAMILIA " +
                "                 , 'N' LENGUA_INDIGENA " +
                "                 , 'N' DISCAPACIDAD " +
                "                 , 'N' USO_INTERNET " +
                "                 , 'N' REDES_SOCIALES " +
                "                 , ORF.NOMBRE1 NOMORF " +
                "                 , LC.DESCRIPCION NOMLC " +
                "                 , D.DESCRIPCION NOMDISP " +
                "              FROM TBL_CIERRE_DIA CD " +
                "              JOIN PRN ON CD.CDGEM = PRN.CDGEM AND CD.CDGCLNS = PRN.CDGNS AND CD.CICLO = PRN.CICLO " +
                "              JOIN PRC ON PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO " +
                "              JOIN PRC_FONDEO F ON PRC.CDGEM = F.CDGEM AND PRC.CDGNS = F.CDGNS AND PRC.CICLO = F.CICLO AND PRC.CDGCL = F.CDGCL " +
                "              JOIN ORF ON F.CDGEM = ORF.CDGEM AND F.CDGORF = ORF.CODIGO " +
                "              JOIN LC ON F.CDGEM = LC.CDGEM AND F.CDGORF = LC.CDGORF AND F.CDGLC = LC.CODIGO " +
                "              JOIN DISPOSICION D ON F.CDGEM = D.CDGEM AND F.CDGORF = D.CDGORF AND F.CDGLC = D.CDGLC AND F.CDGDISP = D.CODIGO " +
                "              JOIN CO ON PRN.CDGEM = CO.CDGEM AND PRN.CDGCO = CO.CODIGO " +
                "              JOIN CL ON PRC.CDGEM = CL.CDGEM AND PRC.CDGCL = CL.CODIGO " +
                "              JOIN EF ON CL.NACIOEF = EF.CODIGO " +
                "              JOIN MU ON CL.CDGEF = MU.CDGEF AND CL.CDGMU = MU.CODIGO " +
                "              JOIN LO ON CL.CDGEF = LO.CDGEF AND CL.CDGMU = LO.CDGMU AND CL.CDGLO = LO.CODIGO " +
                "              JOIN COL ON CL.CDGEF = COL.CDGEF AND CL.CDGMU = COL.CDGMU AND CL.CDGLO = COL.CDGLO AND CL.CDGCOL = COL.CODIGO " +
                "              JOIN INEGI_SEPOMEX_ACT IA ON CL.CDGEF || CL.CDGMU || CL.CDGLO = IA.ID_SEPOMEX " +
                "              JOIN NS ON CD.CDGEM = NS.CDGEM AND CD.CDGCLNS = NS.CODIGO " +
                "              JOIN SC ON PRC.CDGEM = SC.CDGEM AND PRC.CDGNS = SC.CDGNS AND PRC.CDGCL = SC.CDGCL AND PRC.CICLO = SC.CICLO " +
                "         LEFT JOIN PI ON SC.CDGEM = PI.CDGEM AND SC.CDGCL = PI.CDGCL AND SC.CDGNS = PI.CDGNS AND SC.CDGPI = PI.PROYECTO " +
                "             WHERE CD.CDGEM = '" + empresa + "' " +
                "               AND CD.FECHA_CALC = TO_DATE('" + fecSaldo + "') " +
                "               AND CD.CLNS = 'G' " +
                "               AND SC.CANTAUTOR > 0 " +
                "               AND PRN.INICIO >= TRUNC(TO_DATE('" + fecSaldo + "', 'DD/MM/YYYY'), 'MM') " +
                "               AND F.CDGEM = CD.CDGEM " +
                "               AND F.CDGORF = '" + orgFond + "' " +
                "                 " + queryLinea + " " +
                "               AND F.FREPSDO = TO_DATE('" + fecSaldo + "') " +
                "          ORDER BY F.CDGLC, F.CDGDISP";

            int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
            if (res == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string msg = e.Message;
            return "";
        }
    }

    //METODO QUE GENERA LA POLIZA DE GASTOS POR INTERES DE FIDEICOMISOS (FACTURAS)
    [WebMethod]
    public string getRepPolizaComisionPago(string anio, string mes, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery("SPPOLIZA_COMISIONPORPAGO", CommandType.StoredProcedure,
                     oP.ParamsPolizaComisionPago(empresa, anio, mes, usuario));

        query = "SELECT CUENTA, NOMBRE, CARGO_ME, ABONO_ME, TRUNC(CARGO, 2) CARGO, TRUNC(ABONO, 2) ABONO, REFERENCIA " +
                ",CONCEPTO " +
                ",DIARIO " +
                ",SEGMENTO " +
                "FROM POLIZA_COMISIONESPAGOS " +
                "WHERE CDGEM = '" + empresa + "' " +
                "AND CDGPE = '" + usuario + "' " +
                "ORDER BY ORDEN, REFERENCIA ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE GENERA LA POLIZA DE GASTOS POR INTERES DE FIDEICOMISOS (FACTURAS)
    [WebMethod]
    public string getRepPolizaGastosInteres(string anio, string mes, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery("SPPOLIZA_GASTOSPORINTERES", CommandType.StoredProcedure,
                     oP.ParamsPolizaGastosInteres(empresa, anio, mes, usuario));

        query = "SELECT CUENTA, NOMBRE, CARGO_ME, ABONO_ME, TRUNC(CARGO, 2) CARGO, TRUNC(ABONO, 2) ABONO, REFERENCIA " +
            "         , CONCEPTO, DIARIO, SEGMENTO " +
            "      FROM POLIZA_GASTOSPORINTERES " +
            "     WHERE CDGEM = '" + empresa + "' " +
            "       AND CDGPE = '" + usuario + "' " +
            "  ORDER BY ORDEN, NOMBRE ";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PREFONDEO DE MICROSEGUROS
    [WebMethod]
    public string getRepPrefMicroSeg(string fecha)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
       
        string query = " SELECT DF.CDGCL, NOMBREC(DF.CDGEM,DF.CDGCL,'I','A','','','','') NOMBREC,DF.CDGPMS, "
                    + "  PM.DESCRIPCION, NOMBREC(NULL,NULL,NULL,'A',DB.NOMBRE1,DB.NOMBRE2,DB.PRIMAPE,DB.SEGAPE) NOMBENEF, "
                    + " DB.MONTO, DB.CDGCB ,CB.NUMERO,IB.CODIGO, IB.NOMBRE AS NOMBREIB, DB.FEXP, DB.NOCHEQUE,"
                    + " CO.NOMBRE AS NOMBRESUC,DF.CDGCO, DB.FPAGO "
                    + " FROM DEFUNCION DF "
                    + " INNER JOIN DEFUNCION_BENEFICIARIO DB "
                    + "  ON DB.CDGEM = DF.CDGEM "
                    + "  AND DF.CODIGO = DB.CDGDEFUN "
                    + "  AND DF.CDGEM = '" + empresa + "' "
                    + "  AND DB.FPAGO = '" + fecha + "' "
                    + " INNER JOIN CO "
                    + "  ON  CO.CODIGO = DF.CDGCO "
                    + "  AND  CO.CDGEM = DF.CDGEM "
                    + " INNER JOIN CB "
                    + "  ON CB.CDGEM = DF.CDGEM "
                    + "  AND CB.CODIGO = DB.CDGCB "
                    + " INNER JOIN IB"
                    + "  ON IB.CODIGO = CB.CDGIB "
                    + "  AND IB.CDGEM = DF.CDGEM "
                    + " INNER JOIN PRODUCTO_MICROSEGURO PM "
                    + "  ON PM.CDGEM = CB.CDGEM "
                    + "  AND PM.CODIGO = DF.CDGPMS ";


        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PREFONDEO
    [WebMethod]
    public string getRepPrefondeoAgrup(string fecha)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string sucAct = string.Empty;
        string sucSig = string.Empty;
        string bancoAct = string.Empty;
        string bancoSig = string.Empty;
        Boolean band = true;
        decimal cantSolic;
        decimal cantAutor;
        decimal cantSolicBanco;
        decimal cantAutorBanco;
        decimal totalSolic;
        decimal totalAutor;
        int cont;
        int i;

        string query = "SELECT SN.CDGNS, " +
                       "NS.NOMBRE GRUPO, " +
                       "SN.CICLO, " +
                       "TO_CHAR(SN.INICIO, 'DD/MM/YYYY') INICIO, " +
                       "FNASESORCTABANCO('C',SN.CDGEM, SN.CDGOCPE, SN.CDGDO) CDGCB, " +      //FUNCION QUE SE UTILIZA PARA OBTENER 
                       "FNASESORCTABANCO('N',SN.CDGEM, SN.CDGOCPE, SN.CDGDO) NUMCUENTA, " +  //LA INFORMACION DE CUENTA ASIGNADA
                       "FNASESORCTABANCO('B',SN.CDGEM, SN.CDGOCPE, SN.CDGDO) BANCO, " +      //AL ASESOR PARA SU DESEMBOLSO 
                       "(CASE WHEN CDGDOF = 'C' THEN 'CHEQUE' " +
                            "WHEN CDGDOF = 'O' THEN 'ORDEN DE PAGO' END) DESEMBOLSO, " +
                       "(CASE WHEN SN.SITUACION = 'S' THEN 'Solicitado' " +
                             "WHEN SN.SITUACION = 'A' THEN DECODE((SELECT SITUACION " +
                                                                   "FROM PRN " +
                                                                   "WHERE PRN.CDGEM = SN.CDGEM " +
                                                                     "AND PRN.CDGNS = SN.CDGNS " +
                                                                     "AND PRN.CICLO = SN.CICLO), 'A', 'Aut. Cartera', " +
                                                                                                "'T', 'Aut. Tesoreria', " +
                                                                                                "'E', 'Entregado') END) SITUA, " +
                       "SN.CANTSOLIC, " +
                       "SN.CANTAUTOR, " +
                       "SN.CDGCO, " +
                       "CO.NOMBRE COORD " +
                       "FROM SN, NS, CO " +
                       "WHERE SN.CDGEM = '" + empresa + "' " +
                       "AND SN.INICIO = '" + fecha + "' " +
                       "AND SN.SITUACION <> 'R' " +
                       "AND (SELECT COUNT(*) FROM PRN WHERE PRN.CDGEM = SN.CDGEM AND PRN.CDGNS = SN.CDGNS AND PRN.CICLO = SN.CICLO AND SITUACION IN ('D')) = 0 " +
                       "AND NS.CDGEM = SN.CDGEM " +
                       "AND NS.CODIGO = SN.CDGNS " +
                       "AND CO.CDGEM = SN.CDGEM " +
                       "AND CO.CODIGO = SN.CDGCO " +
            /*** SE INCORPORAN LOS REGISTROS DE DEVOLUCION DE GARANTIAS ***/
                       "UNION " +
                       "SELECT PGS.CDGCLNS CDGNS, " +
                       "NS.NOMBRE GRUPO, " +
                       "PGS.CICLO, " +
                       "TO_CHAR(PGS.FEXPCHEQUE, 'DD/MM/YYYY') INICIO, " +
                       "PGS.CDGCB, " +
                       "(SELECT CB.NUMERO " +
                          "FROM CB " +
                         "WHERE CB.CDGEM = PGS.CDGEM " +
                           "AND CB.CODIGO = PGS.CDGCB) NUMCUENTA, " +
                       "(SELECT IB.NOMBRE " +
                          "FROM IB, CB " +
                         "WHERE CB.CDGEM = PGS.CDGEM " +
                           "AND CB.CODIGO = PGS.CDGCB " +
                           "AND IB.CDGEM = CB.CDGEM " +
                           "AND IB.CODIGO = CB.CDGIB) BANCO, " +
                       "'CHEQUE' DESEMBOLSO, " +
                       "'DEV GARANTIA' SITUA, " +
                       "ABS(PGS.CANTIDAD) CANTSOLIC, " +
                       "ABS(PGS.CANTIDAD) CANTAUTOR, " +
                       "NS.CDGCO, " +
                       "CO.NOMBRE COORD " +
                       "FROM PAG_GAR_SIM PGS, CAT_MOVS_GAR CMG, NS, CO " +
                       "WHERE PGS.CDGEM = '" + empresa + "' " +
                       "AND PGS.FPAGO = '" + fecha + "' " +
                       "AND PGS.CLNS = 'G' " +
                       "AND PGS.ESTATUS <> 'CA' " +
                       "AND CMG.CODIGO = PGS.ESTATUS " +
                       "AND CMG.TIPO = 'D' " +
                       "AND NS.CDGEM = PGS.CDGEM " +
                       "AND NS.CODIGO = PGS.CDGCLNS " +
                       "AND CO.CDGEM = NS.CDGEM " +
                       "AND CO.CODIGO = NS.CDGCO " +
            /*** SE INCORPORAN LOS REGISTROS DE DEVOLUCION DE EXCEDENTES ***/
                       "UNION " +
                       "SELECT PDE.CDGCLNS CDGNS, " +
                       "NOMBREC(PDE.CDGEM,PDE.CDGCLNS,PDE.CLNS,'N',NULL,NULL,NULL,NULL) GRUPO, " +
                       "PDE.CICLO, " +
                       "TO_CHAR(PDE.FEXPCHEQUE, 'DD/MM/YYYY') INICIO, " +
                       "PDE.CDGCB, " +
                       "(SELECT CB.NUMERO " +
                          "FROM CB " +
                         "WHERE CB.CDGEM = PDE.CDGEM " +
                           "AND CB.CODIGO = PDE.CDGCB) NUMCUENTA, " +
                       "(SELECT IB.NOMBRE " +
                          "FROM IB, CB " +
                         "WHERE CB.CDGEM = PDE.CDGEM " +
                           "AND CB.CODIGO = PDE.CDGCB " +
                           "AND IB.CDGEM = CB.CDGEM " +
                           "AND IB.CODIGO = CB.CDGIB) BANCO, " +
                       "'CHEQUE' DESEMBOLSO, " +
                       "'DEV EXCEDENTE' SITUA, " +
                       "ABS(PDE.CANTIDAD) CANTSOLIC, " +
                       "ABS(PDE.CANTIDAD) CANTAUTOR, " +
                       "CASE WHEN PDE.CLNS = 'G' THEN " +
                       "    (SELECT CDGCO FROM NS WHERE CDGEM = PDE.CDGEM AND CODIGO = PDE.CDGCLNS) " +
                       "WHEN PDE.CLNS = 'I' THEN " +
                       "    (SELECT CDGCO FROM CL WHERE CDGEM = PDE.CDGEM AND CODIGO = PDE.CDGCLNS) " +
                       "END CDGCO, " +
                       "CASE WHEN PDE.CLNS = 'G' THEN " +
                       "    (SELECT CO.NOMBRE FROM NS, CO WHERE NS.CDGEM = PDE.CDGEM AND NS.CODIGO = PDE.CDGCLNS AND CO.CDGEM = NS.CDGEM AND CO.CODIGO = NS.CDGCO) " +
                       "WHEN PDE.CLNS = 'I' THEN " +
                       "    (SELECT CO.NOMBRE FROM CL, CO WHERE CL.CDGEM = PDE.CDGEM AND CL.CODIGO = PDE.CDGCLNS AND CO.CDGEM = CL.CDGEM AND CO.CODIGO = CL.CDGCO) " +
                       "END COORD " +
                       "FROM PAG_DEV_EXC PDE, CAT_DEV_EXCEDENTE CDE " +
                       "WHERE PDE.CDGEM = '" + empresa + "' " +
                       "AND PDE.FPAGO = '" + fecha + "' " +
                       "AND PDE.ESTATUS <> 'CA' " +
                       "AND CDE.CODIGO = PDE.ESTATUS " +
                       "AND CDE.TIPO = 'D' " +
                       /*** SE INCORPORAN LOS REGISTROS DE MICROSEGUROS ***/
                       " UNION ALL " +
                       " SELECT (SELECT PRN.CDGNS FROM PRC, PRN WHERE PRC.CDGEM = DF.CDGEM AND PRC.CDGCL = DF.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "') CDGNS " +
                       " ,(SELECT NS.NOMBRE FROM PRC, PRN, NS WHERE PRC.CDGEM = DF.CDGEM AND PRC.CDGCL = DF.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "' AND NS.CDGEM = PRN.CDGEM AND NS.CODIGO = PRN.CDGNS) GRUPO " +
                       " ,(SELECT CASE WHEN PRN.CICLOD IS NOT NULL THEN PRN.CICLOD ELSE PRN.CICLO END CICLO FROM PRC, PRN WHERE PRC.CDGEM = DF.CDGEM AND PRC.CDGCL = DF.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "') CICLO " +
                       " ,TO_CHAR(DB.FPAGO, 'DD/MM/YYYY') INICIO " +
                       " ,DB.CDGCB " +
                       " ,CB.NUMERO AS NUMCUENTA " +
                       " ,IB.NOMBRE AS BANCO " +
                       " ,'CHEQUE' AS DESEMBOLSO, 'DEFUNCIÓN' SITUA, DB.MONTO AS CANTSOLIC, DB.MONTO AS CANTAUTOR, DF.CDGCO,  " +
                              " CO.NOMBRE AS COORD  " +
                        " FROM DEFUNCION DF " +
                        " INNER JOIN DEFUNCION_BENEFICIARIO DB " +
                            " ON DB.CDGEM = DF.CDGEM  " +
                            " AND DF.CODIGO = DB.CDGDEFUN  " +
                            " AND DF.CDGEM =  '" + empresa + "' " +
                            " AND DB.FPAGO = '" + fecha + "' " +
                            " AND DB.ESTATUS = 'V' " +
                            " AND DB.FORENT = 'C' " +
                        " INNER JOIN CO " +
                            " ON CO.CDGEM = DF.CDGEM " +
                            " AND CO.CODIGO = DF.CDGCO " +
                        " LEFT JOIN CB " +
                            " ON CB.CDGEM = DB.CDGEM " +
                            " AND CB.CODIGO = DB.CDGCB " +
                        " LEFT JOIN IB " +
                            " ON IB.CDGEM = CB.CDGEM " +
                            " AND IB.CODIGO = CB.CDGIB " +
                        " INNER JOIN PRODUCTO_MICROSEGURO PM " +
                            " ON PM.CDGEM = DF.CDGEM " +
                            " AND PM.CODIGO = DF.CDGPMS " +
                        " INNER JOIN MICROSEGURO MS " +
                            " ON MS.CDGEM = DF.CDGEM " +
                            " AND MS.CDGCL= DF.CDGCL " +
                            " AND MS.CDGPMS= DF.CDGPMS " +
                            " AND MS.INICIO = DF.INICIOPMS " +
                        // DIAGNOSTICOS
                        " UNION ALL " +
                        " SELECT (SELECT PRN.CDGNS FROM PRC, PRN WHERE PRC.CDGEM = D.CDGEM AND PRC.CDGCL = D.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "') CDGNS " +
                        " ,(SELECT NS.NOMBRE FROM PRC, PRN, NS WHERE PRC.CDGEM = D.CDGEM AND PRC.CDGCL = D.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "' AND NS.CDGEM = PRN.CDGEM AND NS.CODIGO = PRN.CDGNS) GRUPO " +
                        " ,(SELECT CASE WHEN PRN.CICLOD IS NOT NULL THEN PRN.CICLOD ELSE PRN.CICLO END CICLO FROM PRC, PRN WHERE PRC.CDGEM = D.CDGEM AND PRC.CDGCL = D.CDGCL AND PRN.CDGEM = PRC.CDGEM AND PRN.CDGNS = PRC.CDGNS AND PRN.CICLO = PRC.CICLO AND PRN.INICIO = FNFECHAULTPRN(PRC.CDGEM, PRC.CDGCL, 'G') AND PRN.INICIO <= '" + fecha + "') CICLO " +
                        " , TO_CHAR(DB.FPAGO, 'DD/MM/YYYY') INICIO, DB.CDGCB, CB.NUMERO NUMCUENTA " +
                        "      , IB.NOMBRE BANCO, 'CHEQUE' DESEMBOLSO, 'DIAGNOSTICO' SITUA, DB.MONTO CANTSOLIC, DB.MONTO CANTAUTOR, D.CDGCO " +
                        "      , CO.NOMBRE COORD " +
                        "   FROM DIAGNOSTICO D " +
                        "   JOIN DIAGNOSTICO_BENEFICIARIO DB ON DB.CDGEM = D.CDGEM AND D.CODIGO = DB.CDGDIAG AND D.CDGEM =  '" + empresa + "' " +
                        "                                   AND DB.FPAGO = '" + fecha + "' AND DB.ESTATUS = 'V' AND DB.FORENT = 'C' " +
                        "   JOIN CO ON CO.CDGEM = D.CDGEM AND CO.CODIGO = D.CDGCO " +
                        " LEFT JOIN CB ON CB.CDGEM = DB.CDGEM AND CB.CODIGO = DB.CDGCB " +
                        " LEFT JOIN IB ON IB.CDGEM = CB.CDGEM AND IB.CODIGO = CB.CDGIB " +
                        "   JOIN PRODUCTO_MICROSEGURO PM ON PM.CDGEM = D.CDGEM AND PM.CODIGO = D.CDGPMS " +
                        "   JOIN MICROSEGURO MS ON MS.CDGEM = D.CDGEM AND MS.CDGCL= D.CDGCL AND MS.CDGPMS= D.CDGPMS AND MS.INICIO = D.INICIOPMS " +
                        "ORDER BY CDGCO, CDGCB, CDGNS";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);

        if (res == 1)
        {
            cont = dref.Tables[0].Rows.Count;
            dsRepPrefondeo.dtPrefondeoDataTable dt = new dsRepPrefondeo.dtPrefondeoDataTable();
            try
            {
                cantSolic = 0;
                cantAutor = 0;
                cantSolicBanco = 0;
                cantAutorBanco = 0;
                totalSolic = 0;
                totalAutor = 0;

                if (cont > 0)
                {
                    for (i = 0; i < cont; i++)
                    {
                        band = true;
                        DataRow dr = dt.NewRow();
                        dr["CDGNS"] = dref.Tables[0].Rows[i]["CDGNS"].ToString();
                        dr["GRUPO"] = dref.Tables[0].Rows[i]["GRUPO"].ToString();
                        dr["CICLO"] = dref.Tables[0].Rows[i]["CICLO"].ToString();
                        dr["INICIO"] = dref.Tables[0].Rows[i]["INICIO"].ToString();
                        dr["CDGCB"] = dref.Tables[0].Rows[i]["CDGCB"].ToString();
                        dr["NUMCUENTA"] = dref.Tables[0].Rows[i]["NUMCUENTA"].ToString();
                        dr["BANCO"] = dref.Tables[0].Rows[i]["BANCO"].ToString();
                        dr["DESEMBOLSO"] = dref.Tables[0].Rows[i]["DESEMBOLSO"].ToString();
                        dr["SITUA"] = dref.Tables[0].Rows[i]["SITUA"].ToString();
                        dr["CANTSOLIC"] = dref.Tables[0].Rows[i]["CANTSOLIC"].ToString();
                        dr["CANTAUTOR"] = dref.Tables[0].Rows[i]["CANTAUTOR"].ToString();
                        dr["CDGCO"] = dref.Tables[0].Rows[i]["CDGCO"].ToString();
                        dr["COORD"] = dref.Tables[0].Rows[i]["COORD"].ToString();

                        cantSolic += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTSOLIC"].ToString());
                        cantAutor += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTAUTOR"].ToString());
                        cantSolicBanco += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTSOLIC"].ToString());
                        cantAutorBanco += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTAUTOR"].ToString());
                        totalSolic += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTSOLIC"].ToString());
                        totalAutor += Convert.ToDecimal(dref.Tables[0].Rows[i]["CANTAUTOR"].ToString());

                        dt.Rows.Add(dr);

                        if (i + 1 < cont)
                        {
                            sucSig = dref.Tables[0].Rows[i + 1]["CDGCO"].ToString();
                        }
                        sucAct = dref.Tables[0].Rows[i]["CDGCO"].ToString();

                        if (i + 1 < cont)
                        {
                            bancoSig = dref.Tables[0].Rows[i + 1]["CDGCB"].ToString();
                        }
                        bancoAct = dref.Tables[0].Rows[i]["CDGCB"].ToString();

                        if (bancoSig != bancoAct)
                        {
                            DataRow dfec = dt.NewRow();
                            dfec["CDGNS"] = "-- SUBTOTAL BANCO --";
                            dfec["CANTSOLIC"] = cantSolicBanco;
                            dfec["CANTAUTOR"] = cantAutorBanco;
                            dt.Rows.Add(dfec);
                            cantSolicBanco = 0;
                            cantAutorBanco = 0;
                        }

                        if (sucSig != sucAct)
                        {
                            //Agrega el registro de subtotal de la Sucursal antes de cambiar 
                            DataRow dfec = dt.NewRow();
                            dfec["CDGNS"] = "-- SUBTOTAL SUCURSAL --";
                            dfec["CANTSOLIC"] = cantSolic;
                            dfec["CANTAUTOR"] = cantAutor;
                            dt.Rows.Add(dfec);
                            DataRow drLine = dt.NewRow();
                            dt.Rows.Add(drLine);
                            cantSolic = 0;
                            cantAutor = 0;
                            band = false;
                        }
                    }

                    if (i == cont && band == true)
                    {
                        //Agrega el ultimo registro de subtotal de Cuenta antes del ultimo subtotal de sucursal
                        DataRow dCta = dt.NewRow();
                        dCta["CDGNS"] = "-- SUBTOTAL BANCO --";
                        dCta["CANTSOLIC"] = cantSolicBanco;
                        dCta["CANTAUTOR"] = cantAutorBanco;
                        dt.Rows.Add(dCta);

                        //Agrega el ultimo registro de subtotal de Sucursal
                        DataRow dfec = dt.NewRow();
                        dfec["CDGNS"] = "-- SUBTOTAL SUCURSAL --";
                        dfec["CANTSOLIC"] = cantSolic;
                        dfec["CANTAUTOR"] = cantAutor;
                        dt.Rows.Add(dfec);
                        DataRow drLine = dt.NewRow();
                        dt.Rows.Add(drLine);
                    }
                    //Agrega el registro de totales del reporte
                    DataRow dtot = dt.NewRow();
                    dtot["CDGNS"] = "-- TOTAL --";
                    dtot["GRUPO"] = cont;
                    dtot["CANTSOLIC"] = totalSolic;
                    dtot["CANTAUTOR"] = totalAutor;
                    dt.Rows.Add(dtot);
                    ds.Tables.Add(dt);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "";
            }

            xml = ds.GetXml();
        }
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE PRESTAMOS
    [WebMethod]
    public string getRepPrestamos(int prestamos, int ciclos, string fecIniDesde, string fecIniHasta, string fecFinDesde,
                                     string fecFinHasta, string fecPagos, int sitCart, int sitTes, int sitSaldo,
                                     int sitLiq, int sitDev, int cartVig, int cartVenc, int cartRest, int cartCast, string usuario,
                                     string nomUsuario, string region, string sucursal, string coord, string asesor, 
                                     string tipoProd)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepPrestamos.dtPrestamosDataTable dt = new dsRepPrestamos.dtPrestamosDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_PRESTAMOS", CommandType.StoredProcedure,
                     oP.ParamsPrestamos(empresa, fecIniDesde, fecIniHasta, usuario, region, sucursal, coord, asesor, fecPagos,
                                        cartVig, cartVenc, cartRest, cartCast, sitSaldo, sitLiq, sitCart, sitTes, sitDev, ciclos, fecFinDesde,
                                        fecFinHasta, tipoProd));

            string query = "SELECT RS.* " +
                           ",TO_CHAR(SYSDATE, 'DD/MM/YYYY') FECHAIMP " +
                           ",TO_CHAR(SYSDATE, 'HH24:MI:SS') HORAIMP " +
                           ",(SELECT REF_PAYCASH FROM PAYCASH_REF WHERE CDGEM = RS.CDGEM AND CDGCLNS = RS.CDGCLNS AND CDGTPC = RS.CDGTPC AND TIPO = 1) REF_INS_PAG " +
                           ",(SELECT REF_PAYCASH FROM PAYCASH_REF WHERE CDGEM = RS.CDGEM AND CDGCLNS = RS.CDGCLNS AND CDGTPC = RS.CDGTPC AND TIPO = 0) REF_INS_GAR " +
                           "FROM REP_PRESTAMOS RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CDGPE = '" + usuario + "' " +
                           "ORDER BY RS.CDGCLNS, RS.CICLO, RS.INICIO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drPrn = dt.NewRow();
                drPrn["COD_GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drPrn["GRUPO"] = dref.Tables[0].Rows[i]["NOMCLNS"];
                drPrn["CICLO"] = dref.Tables[0].Rows[i]["CICLO"];
                drPrn["INICIO"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["INICIO"]).ToString("dd/MM/yyyy");
                drPrn["PLAZO"] = dref.Tables[0].Rows[i]["PLAZO"];
                drPrn["PERIODICIDAD"] = dref.Tables[0].Rows[i]["PERIODICIDAD"];
                drPrn["FIN"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["FIN"]).ToString("dd/MM/yyyy");
                drPrn["TASA"] = dref.Tables[0].Rows[i]["TASA"];
                drPrn["PARCIALIDAD"] = dref.Tables[0].Rows[i]["PARCIALIDAD"];
                drPrn["SITUACION"] = dref.Tables[0].Rows[i]["SITUACION"];
                drPrn["CANT_AUTOR"] = dref.Tables[0].Rows[i]["CANTAUTOR"];
                drPrn["CANT_ENTRE"] = dref.Tables[0].Rows[i]["CANTENTRE"];
                drPrn["ENTRREAL"] = dref.Tables[0].Rows[i]["ENTRREAL"];
                drPrn["CANT_TOTAL"] = dref.Tables[0].Rows[i]["TOTALPAGAR"];
                drPrn["PAGOS_COMP"] = dref.Tables[0].Rows[i]["PAGOSCOMP"];
                drPrn["CARGOS"] = dref.Tables[0].Rows[i]["CARGOS"];
                drPrn["ABONOS"] = dref.Tables[0].Rows[i]["ABONOS"];
                drPrn["PAGADO_CAPITAL"] = dref.Tables[0].Rows[i]["PAGADOCAP"];
                drPrn["PAGADO_INTERES"] = dref.Tables[0].Rows[i]["PAGADOINT"];
                drPrn["PAGADO_RECARGOS"] = dref.Tables[0].Rows[i]["PAGADOREC"];
                drPrn["SDO_CAPITAL"] = dref.Tables[0].Rows[i]["SALDOCAP"];
                drPrn["FEC_ULT_PAGO"] = dref.Tables[0].Rows[i]["ULTIMOPAGFEC"].ToString() != "" ? Convert.ToDateTime(dref.Tables[0].Rows[i]["ULTIMOPAGFEC"]).ToString("dd/MM/yyyy") : "";
                drPrn["ULT_PAGO"] = dref.Tables[0].Rows[i]["ULTIMOPAGCANT"];
                drPrn["SDO_TOTAL"] = dref.Tables[0].Rows[i]["SALDOTOTAL"];
                drPrn["COD_ASESOR"] = dref.Tables[0].Rows[i]["CDGOCPE"];
                drPrn["ASESOR"] = dref.Tables[0].Rows[i]["NOMPE"];
                drPrn["COD_REGION"] = dref.Tables[0].Rows[i]["CDGRG"];
                drPrn["REGION"] = dref.Tables[0].Rows[i]["NOMRG"];
                drPrn["COD_SUCURSAL"] = dref.Tables[0].Rows[i]["CDGCO"];
                drPrn["SUCURSAL"] = dref.Tables[0].Rows[i]["NOMCO"];
                drPrn["COD_GTE"] = dref.Tables[0].Rows[i]["CDGGTE"];
                drPrn["GERENTE"] = dref.Tables[0].Rows[i]["NOMGTE"];
                drPrn["MUJERES"] = dref.Tables[0].Rows[i]["MUJERES"];
                drPrn["HOMBRES"] = dref.Tables[0].Rows[i]["HOMBRES"];
                drPrn["NUM_CTES"] = dref.Tables[0].Rows[i]["NUMCTES"];
                drPrn["REFERENCIA"] = dref.Tables[0].Rows[i]["REFPAG"];
                drPrn["REFERENCIAGL"] = dref.Tables[0].Rows[i]["REFGAR"];
                drPrn["SDO_GL"] = dref.Tables[0].Rows[i]["SALDOGL"];
                drPrn["MORA_TOTAL"] = dref.Tables[0].Rows[i]["MORATOTAL"];
                drPrn["MORA_CAPITAL"] = dref.Tables[0].Rows[i]["MORACAP"];
                drPrn["MORA_INTERES"] = dref.Tables[0].Rows[i]["MORAINT"];
                drPrn["DIAS_MORA"] = dref.Tables[0].Rows[i]["DIAS_MORA"];
                drPrn["DIAS_ATRASO"] = dref.Tables[0].Rows[i]["DIAS_ATRASO"];
                drPrn["RECARGOS"] = dref.Tables[0].Rows[i]["MORATORIOS"];
                drPrn["TIPOCART"] = dref.Tables[0].Rows[i]["TIPOCART"];
                drPrn["FECHA_REP"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["FECHA_REP"]).ToString("dd/MM/yyyy");
                drPrn["COD_COORD"] = dref.Tables[0].Rows[i]["CDGCOPE"];
                drPrn["COORDINADOR"] = dref.Tables[0].Rows[i]["NOMCOPE"];
                drPrn["COD_ASESOR_INI"] = dref.Tables[0].Rows[i]["CDGINIPE"];
                drPrn["ASESOR_INI"] = dref.Tables[0].Rows[i]["NOMINIPE"];
                drPrn["COD_GRUPO_ANT"] = dref.Tables[0].Rows[i]["CDGCLNSANT"];
                drPrn["NOM_GRUPO_ANT"] = dref.Tables[0].Rows[i]["NOMCLNSANT"];
                drPrn["CICLO_ANT"] = dref.Tables[0].Rows[i]["CICLOANT"];
                drPrn["NOMINA_ASESOR"] = dref.Tables[0].Rows[i]["NOMINAOCPE"];
                drPrn["TIPOPROD"] = dref.Tables[0].Rows[i]["TIPOPROD"];
                drPrn["REFINSPAG"] = dref.Tables[0].Rows[i]["REF_INS_PAG"];
                drPrn["REFINSGAR"] = dref.Tables[0].Rows[i]["REF_INS_GAR"];
                dt.Rows.Add(drPrn);
            }

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dt.NewRow();
                dtot["COD_GRUPO"] = "-- TOTAL --";
                dtot["CANT_AUTOR"] = Convert.ToDecimal(dt.Compute("Count(CANT_ENTRE)", ""));
                dtot["CANT_ENTRE"] = Convert.ToDecimal(dt.Compute("Sum(CANT_ENTRE)", ""));
                dtot["ENTRREAL"] = Convert.ToDecimal(dt.Compute("Sum(ENTRREAL)", ""));
                dt.Rows.Add(dtot);
            }

            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LAS CAUSAS DE RECHAZO DE CREDITO EN UN PERIODO DE TIEMPO
    [WebMethod]
    public string getRepProrrateoConsultas(string año, string mes, double costo, string usuario
        , int redondear, string tipo)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string query = string.Empty;
        int iRes;

        iRes = oE.myExecuteNonQuery("SP_REP_PRORR_CONS", CommandType.StoredProcedure,
                     oP.ParamsRepProrrateoConsultas(empresa, año, mes, costo, usuario, redondear));

        if (tipo == "Acumulado")
        {
            query = "SELECT CDGEM, ANIO, MES, CDGRG, REGION, CDGCO, SUCURSAL, NOEMP, CDGOCPE, ASESOR " +
                    ",SUM(CONSULTAS) CONSULTAS " +
                    ",ROUND((COUNT(*) / (SELECT COUNT(*) FROM REP_PRORR_CONS WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') * " + costo + "),2) COSTO " +
                    ",CDGPE " +
                    "FROM REP_PRORR_CONS " +
                    "WHERE CDGEM = '" + empresa + "' " +
                    "AND CDGPE = '" + usuario + "' " +
                    "GROUP BY CDGEM, ANIO, MES, CDGRG, REGION, CDGCO, SUCURSAL, NOEMP, CDGOCPE, ASESOR, CDGPE " +
                    "ORDER BY CDGEM, ANIO, MES, CDGRG, REGION, CDGCO, SUCURSAL, NOEMP, CDGOCPE, ASESOR, CDGPE ";
        }
        else if (tipo == "Detalle")
        {
            query = "SELECT CDGEM, ANIO, MES, CDGRG, REGION, CDGCO, SUCURSAL, NOEMP, CDGOCPE, ASESOR " +
                    ",CDGCL, CLIENTE, CONSULTAS " +
                    ",COSTO " +
                    ",CDGPE " +
                    "FROM REP_PRORR_CONS " +
                    "WHERE CDGEM = '" + empresa + "' " +
                    "AND CDGPE = '" + usuario + "' " +
                    "ORDER BY CDGEM, ANIO, MES, CDGRG, REGION, CDGCO, SUCURSAL, NOEMP, CDGOCPE, ASESOR, CDGCL, CLIENTE, CONSULTAS, COSTO, CDGPE ";
        }


        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LAS CAUSAS DE RECHAZO DE CREDITO EN UN PERIODO DE TIEMPO
    [WebMethod]
    public string getRepRechazoCredito(string fechaIni, string fechaFin)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT TO_CHAR(RE.FECHARECHAZO, 'DD/MM/YYYY') FECHA_RECH, " +
                       "TO_CHAR(SN.INICIO, 'DD/MM/YYYY') FECHA_INICIO, " +
                       "CO.CDGRG COD_REGION, " +
                       "RG.NOMBRE REGION, " +
                       "SN.CDGCO COD_SUCURSAL, " +
                       "CO.NOMBRE SUCURSAL, " +
                       "RE.CDGCL COD_ACRED, " +
                       "NOMBREC(NULL,NULL,NULL,'A',CL.NOMBRE1,CL.NOMBRE2,CL.PRIMAPE,CL.SEGAPE) AS NOMBRE_CL, " +
                       "RE.CDGCLNS COD_GRUPO, " +
                       "NS.NOMBRE GRUPO, " +
                       "RE.CICLOD CICLO, " +
                       "SC.CANTSOLIC, " +
                       "CR.DESCRIPCION CAUSA_RECH, " +
                       "(SELECT NOMBREC(NULL,NULL,'I','N',NOMBRE1,NOMBRE2,PRIMAPE,SEGAPE) FROM PE WHERE CDGEM = SN.CDGEM AND CODIGO = SN.CDGOCPE) NOM_ASESOR " + 
                       "FROM SC_RECHAZO RE, CAT_CAUSA_RECHAZO CR, CL, NS, SN, SC, CO, RG " +
                       "WHERE RE.CDGEM = '" + empresa + "' " +
                       "AND TRUNC(RE.FECHARECHAZO) BETWEEN '" + fechaIni + "' AND '" + fechaFin + "' " +
                       "AND RE.CLNS = 'G' " +
                       "AND CR.CODIGO = RE.CDGRECH " +
                       "AND NS.CDGEM = RE.CDGEM " +
                       "AND NS.CODIGO = RE.CDGCLNS " +
                       "AND CL.CDGEM = RE.CDGEM " +
                       "AND CL.CODIGO = RE.CDGCL " +
                       "AND SN.CDGEM = RE.CDGEM " +
                       "AND SN.CDGNS = RE.CDGCLNS " +
                       "AND SN.CICLO = RE.CICLO " +
                       "AND SC.CDGEM = RE.CDGEM " +
                       "AND SC.CDGNS = RE.CDGCLNS " +
                       "AND SC.CICLO = RE.CICLO " +
                       "AND SC.CDGCL = RE.CDGCL " +
                       "AND SC.CLNS = RE.CLNS " +
                       "AND SN.CDGEM = CO.CDGEM " +
                       "AND SN.CDGCO = CO.CODIGO " +
                       "AND CO.CDGEM = RG.CDGEM " +
                       "AND CO.CDGRG = RG.CODIGO " +
                       "ORDER BY FECHARECHAZO";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL RESUMEN DE SEGUIMIENTO DE VEHICULOS
    [WebMethod]
    public string getRepResSegVeh(string fInicio, string fFin, string region, string sucursal, string usuario)
    {
        DataSet dref = new DataSet();
        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_KMT_VEHICULO", CommandType.StoredProcedure,
                     oP.ParamsRepResSegVeh(empresa, fInicio, fFin, region, sucursal, usuario));

            string query = "SELECT REGION " +
                           ",SUCURSAL " +
                           ",MARCA " +
                           ",MODELO " +
                           ",SERIE " +
                           ",ASESOR " +
                           ",TO_CHAR(FASIGNACION , 'DD/MM/YYYY') FASIGNACION " +
                           ",TRUNC(SEMANA1, 2) SEMANA1 " +
                           ",TRUNC(SEMANA2, 2) SEMANA2 " +
                           ",TRUNC(SEMANA3, 2) SEMANA3 " +
                           ",TRUNC(SEMANA4, 2) SEMANA4 " +
                           ",TRUNC(SEMANA5, 2) SEMANA5 " +
                           ",TRUNC(TOTAL, 2) TOTAL " +
                           "FROM REP_KMT_VEH " +
                           "WHERE CDGEM = '" + empresa + "' " +
                           "AND CDGPE = '" + usuario + "' ";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA 
    [WebMethod]
    public string getRepSaldoCierre(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string status = string.Empty;

        try
        {
            int iRes = oE.myExecuteNonQuery(ref status, "SP_REP_SALDOS_CIERRE", CommandType.StoredProcedure,
                             oP.ParamsSaldosCierre(empresa, fecha, usuario));

            string query = "SELECT TO_CHAR(FLIQUIDA,'DD/MM/YYYY') FLIQUIDA " +
                           ",ROUND(RS.SDOCAPITAL,2) SDOCAPITAL " +
                           ",ROUND(RS.SDOTOTAL,2) SDOTOTAL " +
                           ",ROUND(RS.SDO_INT_DEV_NO_COB,2) SDO_INT_DEV_NO_COB " +
                           ",ROUND(RS.SDO_INT_POR_DEV,2) SDO_INT_POR_DEV " +
                           ",ROUND(RS.MORA_CAPITAL,2) MORA_CAPITAL " +
                           ",ROUND(RS.MORA_TOTAL,2) MORA_TOTAL " +
                           ",RS.* " +
                           ",TO_CHAR(FINICIO,'DD/MM/YYYY') INICIO " +
                           ",TO_CHAR(FFIN,'DD/MM/YYYY') FIN " +
                           "FROM REP_SALDO_CIERRE RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CVE_USUARIO = '" + usuario + "' " +
                           "ORDER BY RS.CDGCLNS, RS.FINICIO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDOCAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOCAPITAL)", ""));
                dtot["SDOTOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOTOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA 
    [WebMethod]
    public string getRepSaldoCierreEsp(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string status = string.Empty;

        try
        {
            int iRes = oE.myExecuteNonQuery(ref status, "SP_REP_SALDOS_CIERRE_ESP", CommandType.StoredProcedure,
                             oP.ParamsSaldosCierre(empresa, fecha, usuario));

            string query = "SELECT TO_CHAR(FLIQUIDA,'DD/MM/YYYY') FLIQUIDA " +
                           ",RS.* " +
                           ",TO_CHAR(FINICIO,'DD/MM/YYYY') INICIO " +
                           ",TO_CHAR(FFIN,'DD/MM/YYYY') FIN " +
                           "FROM REP_SALDO_CIERRE RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CVE_USUARIO = '" + usuario + "' " +
                           "ORDER BY RS.CDGCLNS, RS.FINICIO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDOCAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOCAPITAL)", ""));
                dtot["SDOTOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOTOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA MENSUAL
    [WebMethod]
    public string getRepSaldoCierreMens(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            int iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_MENS", CommandType.StoredProcedure,
                             oP.ParamsSaldosCierre(empresa, fecha, usuario));

            string query = "SELECT TO_CHAR(FLIQUIDA,'DD/MM/YYYY') FLIQUIDA " +
                           ",RS.* " +
                           ",TO_CHAR(FINICIO,'DD/MM/YYYY') INICIO " +
                           ",TO_CHAR(FFIN,'DD/MM/YYYY') FIN " +
                           "FROM REP_SALDO_CIERRE RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CVE_USUARIO = '" + usuario + "' " +
                           "ORDER BY RS.CDGCLNS, RS.FINICIO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["REGION"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDOCAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOCAPITAL)", ""));
                dtot["SDOTOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDOTOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA 
    [WebMethod]
    public string getRepSaldoCierreAcred(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        int iRes;
        string xml = "";
        string empresa = cdgEmpresa;
        
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_ACRED", CommandType.StoredProcedure,
                oP.ParamsCierreAcred(empresa, fecha, usuario));

            string query = "SELECT TRUNC(SC.SDO_CAPITAL,2) SDO_CAPITAL " +
                           ",TRUNC(SC.SDO_TOTAL,2) SDO_TOTAL " +
                           ",TRUNC(SC.SDO_INT_DEV_NO_COB,2) SDO_INT_DEV_NO_COB " +
                           ",TRUNC(SC.SDO_INT_POR_DEV,2) SDO_INT_POR_DEV " +
                           ",TRUNC(SC.MORA_CAPITAL_IND,2) MORA_CAPITAL_IND " +
                           ",TRUNC(SC.MORA_TOTAL_IND,2) MORA_TOTAL_IND " +
                           ",TRUNC(SC.SALDO_GL,2) SALDO_GL " +
                           ",TO_CHAR (SC.INICIO, 'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR (SC.FIN, 'DD/MM/YYYY') FFIN " +
                           ",TO_CHAR (SC.FECHA_LIQUIDA, 'DD/MM/YYYY') FLIQUIDA " +
                           ",SC.CDGEM " +
                           ",SC.CDGCLNS " +
                           ",SC.CICLO " +
                           ",SC.CLNS " +
                           ",SC.FECHA_CALC " +
                           ",SC.CDGRG " +
                           ",SC.NOMRG " +
                           ",SC.CDGCO " +
                           ",SC.NOMCO " +
                           ",SC.CDGOCPE " +
                           ",SC.NOMOCPE " +
                           ",SC.NOMBRE " +
                           ",SC.CDGCL " +
                           ",SC.NOMNS " +
                           ",SC.CANTENTRE " +
                           ",SC.TASA " +
                           ",SC.PLAZO " +
                           ",SC.DIAS_MORA " +
                           ",SC.NOMORF " +
                           ",SC.NOMLC " +
                           ",SC.NOMDISP " +
                           ",SC.NOMPI " +
                           ",SC.RANGO_MORA " +
                           ",SC.REP_CRED_VENC " +
                           ",SC.TIPO_CART " +
                           ",SC.CDGNS " +
                           ",SC.CDGPE " +
                           ",SC.TIPOPROD " +
                           ",SC.CDGORF " +
                           ",SC.CDGLC " +
                           ",SC.CDGDISP " +
                           "FROM REP_SALDO_CIERRE_ACRED SC " +
                           "WHERE SC.CDGEM = '" + empresa + "' " + 
                           "AND SC.CDGPE = '" + usuario + "' " +
                           "ORDER BY CDGCLNS, CICLO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["NOMRG"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_CAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_CAPITAL)", ""));
                dtot["SDO_TOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_TOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA ESPECIAL
    [WebMethod]
    public string getRepSaldoCierreAcredEsp(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        int iRes;
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_ACRED_ESP", CommandType.StoredProcedure,
                oP.ParamsCierreAcred(empresa, fecha, usuario));

            string query = "SELECT SC.* " +
                           ",TO_CHAR(SC.INICIO,'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR(SC.FIN,'DD/MM/YYYY') FFIN " +
                           ",TO_CHAR(SC.FECHA_LIQUIDA,'DD/MM/YYYY') FLIQUIDA " +
                           "FROM REP_SALDO_CIERRE_ACRED SC " +
                           "WHERE SC.CDGEM = '" + empresa + "' " +
                           "AND SC.CDGPE = '" + usuario + "' " +
                           "ORDER BY CDGCLNS, CICLO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["NOMRG"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_CAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_CAPITAL)", ""));
                dtot["SDO_TOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_TOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE CONSULTA LOS SALDOS AL CIERRE DEL DIA MENSUAL
    [WebMethod]
    public string getRepSaldoCierreAcredMens(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        int iRes;
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CIERRE_ACRED_MEN", CommandType.StoredProcedure,
                oP.ParamsCierreAcred(empresa, fecha, usuario));

            string query = "SELECT SC.* " +
                           ",TO_CHAR(SC.INICIO,'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR(SC.FIN,'DD/MM/YYYY') FFIN " +
                           ",TO_CHAR(SC.FECHA_LIQUIDA,'DD/MM/YYYY') FLIQUIDA " +
                           "FROM REP_SALDO_CIERRE_ACRED SC " +
                           "WHERE SC.CDGEM = '" + empresa + "' " +
                           "AND SC.CDGPE = '" + usuario + "' " +
                           "ORDER BY CDGCLNS, CICLO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dref.Tables[0].NewRow();
                dtot["NOMRG"] = "-- TOTAL --";
                dtot["PLAZO"] = Convert.ToDecimal(dref.Tables[0].Compute("Count(PLAZO)", ""));
                dtot["SDO_CAPITAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_CAPITAL)", ""));
                dtot["SDO_TOTAL"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_TOTAL)", ""));
                dtot["SDO_INT_DEV_NO_COB"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_DEV_NO_COB)", ""));
                dtot["SDO_INT_POR_DEV"] = Convert.ToDecimal(dref.Tables[0].Compute("Sum(SDO_INT_POR_DEV)", ""));
                dref.Tables[0].Rows.Add(dtot);
            }
            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION DEL CONTROL DE PAGOS ACUMULADO POR ACREDITADO SEGUN LA FECHA DE CONSULTA
    [WebMethod]
    public string getRepSaldoCierreControlPagos(string fecha, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string queryEstatus = string.Empty;
        string strAsesor = string.Empty;
        
        if (puesto == "A")
            strAsesor = "AND PRN.CDGOCPE = '" + usuario + "' ";
        else
            strAsesor = "AND PRN.CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') ";

        string query = "SELECT REGION " + 
                            ",COORD " +
                            ",CONTRATO " +
                            ",ASESOR " +
                            ",CDGNS " +
                            ",GRUPO " +
                            ",CDGCL " +
                            ",ACRED " +
                            ",CICLO " +
                            ",FINICIO " +
                            ",FFIN " +
                            ",CANTENTRE " +
                            ",SITUA " +
                            ",PAGOCOMP " +
                            ",SALDOTOTAL " +
                            ",DIASMORA " +
                            ",PARCIALIDAD " +
                            ",PAGO_SEM " +
                            ",PAGO_EXT " +
                            ",APORTACRED " +
                            ",(PAGO_SEM + PAGO_EXT + APORTACRED) PAGOREAL " +
                            ",((PAGO_SEM + PAGO_EXT + APORTACRED) - PAGOCOMP) DIFERENCIA " +
                            ",DEVACRED " +
                            ",AHORROACRED " +
                            ",MULTAACRED " +
                            ",(TOTALPAGAR - (PAGO_SEM + PAGO_EXT + APORTACRED)) SALDO " +
                            "FROM (SELECT A.REGION " +                     //DEFINE EL ORIGEN DE DATOS AGRUPADO
                            ",A.COORD " +                
                            ",A.CONTRATO " +
                            ",A.ASESOR " +
                            ",A.CDGNS " +
                            ",A.GRUPO " +
                            ",A.CDGCL " +
                            ",A.ACRED " +
                            ",A.CICLO " +
                            ",A.FINICIO " +
                            ",A.FFIN " +
                            ",A.CANTENTRE " +
                            ",A.SITUA " +
                            ",A.PAGOCOMP " +
                            ",A.SALDOTOTAL " +
                            ",A.DIASMORA " +
                            ",A.PARCIALIDAD " +
                            ",A.TOTALPAGAR " +
                            ",NVL(SUM(PAGOSEM),0) PAGO_SEM " +              //SUMATORIA DE LOS PAGOS SEMANALES
                            ",NVL(SUM(PAGOEXT),0) PAGO_EXT " +              //SUMATORIA DE LOS PAGOS EXTEMPORANEOS
                            ",NVL(SUM(APORT_ACRED),0) APORTACRED " +        //SUMATORIA DE LAS APORTACIONES DE CREDITO
                            ",SUM(DEV_ACRED) DEVACRED " +
                            ",SUM(AHORRO_ACRED) AHORROACRED " +
                            ",SUM(MULTA_ACRED) MULTAACRED " +
                             "FROM (SELECT RG.NOMBRE REGION " +
                             ",CO.NOMBRE COORD " +
                             ",PRN.CDGNS || PRN.CICLO CONTRATO " +
                             ",NOMBREC (NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE) ASESOR " +
                             ",PRN.CDGNS " +
                             ",NS.NOMBRE GRUPO " +
                             ",PRC.CDGCL " +
                             ",NOMBREC(CL.CDGEM, CL.CODIGO, 'I', 'N', NULL, NULL, NULL, NULL) ACRED " +
                             ",PRN.CICLO " +
                             ",TO_CHAR (PRN.INICIO, 'DD/MM/YYYY') FINICIO " +
                             ",TO_CHAR (DECODE (NVL (PRN.periodicidad, ''), " +
                                               "'S', PRN.inicio + (7 * NVL (PRN.plazo, 0)), " +
                                               "'Q', PRN.inicio + (15 * NVL (PRN.plazo, 0)), " +
                                               "'C', PRN.inicio + (14 * NVL (PRN.plazo, 0)), " +
                                               "'M', PRN.inicio + (30 * NVL (PRN.plazo, 0)), " +
                                               "'', ''), 'DD/MM/YYYY') AS FFIN " +
                            ",PRC.CANTENTRE " +
                            ",CASE WHEN CSA.TIPO = 'S' THEN " +
                                "CSA.PAGO_REAL " +
                            "END PAGOSEM " +
                            ",CASE WHEN CSA.TIPO = 'P' THEN " +
                                "CSA.PAGO_REAL " +
                            "END PAGOEXT " +
                            ",CSA.PAGO_REAL " +
                            ",CSA.APORT_ACRED " +
                            ",CSA.DEV_ACRED " +
                            ",CSA.AHORRO_ACRED " +
                            ",CSA.MULTA_ACRED " +
                            ",DECODE(PRN.SITUACION,'E','ENTREGADO','L','LIQUIDADO') SITUA " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * (PagoVencidoCapitalPrN(PrN.CdgEm,  PRN.CdgNs, PrN.Ciclo,PrN.CantEntre, PrN.Tasa, PrN.Plazo,PrN.Periodicidad, PrN.CdgMCI, Prn.Inicio, Prn.DiaJunta,Prn.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,PrN.ModoApliReca,'" + fecha + "',null,'S')),2) PAGOCOMP " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * (SALDOTOTALPRN(PrN.CdgEm, PrN.CdgNS, PrN.Ciclo, PrN.CantEntre, PrN.Tasa,    PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio, PrN.DiaJunta,    PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesfasePago, PrN.CdgTI,    PrN.ModoApliReca, '" + fecha + "')),2) SALDOTOTAL " +
                            ",CASE WHEN PRN.SITUACION = 'E' THEN " +
                                "(SELECT DIAS_MORA FROM TBL_CIERRE_DIA WHERE CDGEM = PRN.CDGEM AND CDGCLNS = PRN.CDGNS AND CLNS = 'G' AND CICLO = PRN.CICLO AND FECHA_CALC = '" + fecha + "') " +
                            "ELSE " +
                                "0 " +
                            "END DIASMORA " +
                            ",ROUND((PRC.CANTENTRE / PRN.CANTENTRE) * PARCIALIDADPrN (PrN.CdgEm, PrN.CdgNs, PrN.Ciclo, NVL(PrN.cantentre,PrN.Cantautor), PrN.Tasa, PrN.Plazo, PrN.Periodicidad, PrN.CdgMCI, PrN.Inicio,    PrN.DiaJunta, PrN.MULTPER, PrN.PeriGrCap, PrN.PeriGrInt, PrN.DesFasePago, PrN.CdgTi, NULL),3) PARCIALIDAD " +
                            ",round((PRC.CANTENTRE / PRN.CANTENTRE) * (round(decode(nvl(PRN.periodicidad,''), 'S', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(4 * 100), " +
                                           "'Q', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0) * 15)/(30 * 100), " +
                                           "'C', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(2 * 100), " +
                                           "'M', nvl(PRN.cantentre,0) + (nvl(PRN.tasa,0) * nvl(PRN.plazo,0) * nvl(PRN.cantentre,0))/(100), " +
                                           "'',  ''),2)) ,2) AS TOTALPAGAR " +
                            "FROM TBL_CIERRE_DIA CD, PRN, PRC, CL, CO, RG, PE, NS, " +
                            "(SELECT CDGEM, CDGNS, CICLO, CDGCL, TIPO, SUM(PAGOREAL) PAGO_REAL, " +
                              "SUM(APORT) APORT_ACRED, SUM(DEVOLUCION) DEV_ACRED, SUM(AHORRO) AHORRO_ACRED, SUM(MULTA) MULTA_ACRED " +
                              "FROM CONTROL_PAGOS_ACRED " +
                              "WHERE CDGEM = '" + empresa + "' " +
                              "AND FREALPAGO <= '" + fecha + "' " +
                              "GROUP BY CDGEM, CDGNS, CICLO, CDGCL, TIPO) CSA " +
                            "WHERE CD.CDGEM = '" + empresa + "' " +
                            "AND CD.FECHA_CALC = '" + fecha + "' " +
                            "AND CD.CLNS = 'G' " +
                            "AND PRN.CDGEM = CD.CDGEM " +
                            "AND PRN.CDGNS = CD.CDGCLNS " +
                            "AND PRN.CICLO = CD.CICLO " +
                            "AND PRN.CANTENTRE > 0 " +
                            "AND PRN.SITUACION IN ('E', 'L') " +
                            strAsesor +
                            "AND PRC.CDGEM = PRN.CDGEM " +
                            "AND PRC.CDGNS = PRN.CDGNS " +
                            "AND PRC.CICLO = PRN.CICLO " +
                            "AND PRC.SITUACION IN ('E', 'L') " +
                            "AND PRC.CANTENTRE > 0 " +
                            "AND CSA.CDGEM = PRC.CDGEM " +
                            "AND CSA.CDGNS = PRC.CDGNS " +
                            "AND CSA.CICLO = PRC.CICLO " +
                            "AND CSA.CDGCL = PRC.CDGCL " +
                            "AND CL.CDGEM = PRC.CDGEM " +
                            "AND CL.CODIGO = PRC.CDGCL " +
                            "AND CO.CDGEM = PRN.CDGEM " +
                            "AND CO.CODIGO = PRN.CDGCO " +
                            "AND RG.CDGEM = CO.CDGEM " +
                            "AND RG.CODIGO = CO.CDGRG " +
                            "AND PE.CDGEM = PRN.CDGEM " +
                            "AND PE.CODIGO = PRN.CDGOCPE " +
                            "AND NS.CDGEM = PRN.CDGEM " +
                            "AND NS.CODIGO = PRN.CDGNS) A " +
                            "GROUP BY A.REGION " +
                            ",A.COORD " +
                            ",A.CONTRATO " +
                            ",A.ASESOR " +
                            ",A.CDGNS " +
                            ",A.GRUPO " +
                            ",A.CDGCL " +
                            ",A.ACRED " +
                            ",A.CICLO " +
                            ",A.FINICIO " +
                            ",A.FFIN " +
                            ",A.CANTENTRE " +
                            ",A.SITUA " +
                            ",A.PAGOCOMP " +
                            ",A.SALDOTOTAL " +
                            ",A.DIASMORA " +
                            ",A.PARCIALIDAD " +
                            ",A.TOTALPAGAR) " +
                            "ORDER BY CDGNS, CICLO";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LOS SALDO PARA FINES CONTABLES
    [WebMethod]
    public string getRepSaldosContable(string fecha, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        try
        {
            int iRes = oE.myExecuteNonQuery("SP_REP_SALDOS_CONT", CommandType.StoredProcedure,
                             oP.ParamsSaldosCierre(empresa, fecha, usuario));

            string query = "SELECT ROUND(RS.SDOCAPITAL,2) SDOCAPITAL " +
                           ",ROUND(RS.SDOTOTAL,2) SDOTOTAL " +
                           ",ROUND(RS.SDO_INT_DEV_NO_COB,2) SDO_INT_DEV_NO_COB " +
                           ",ROUND(RS.SDO_INT_POR_DEV,2) SDO_INT_POR_DEV " +
                           ",ROUND(RS.SDOGL,2) SDOGL " +
                           ",ROUND(RS.MORA_CAPITAL,2) MORA_CAPITAL " +
                           ",ROUND(RS.MORA_TOTAL,2) MORA_TOTAL " +
                           ",RS.* " +
                           ",TO_CHAR(FINICIO,'DD/MM/YYYY') INICIO " +
                           ",TO_CHAR(FFIN,'DD/MM/YYYY') FIN " +
                           "FROM REP_SALDO_CIERRE RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CVE_USUARIO = '" + usuario + "' " +
                           "ORDER BY RS.CDGCLNS, RS.FINICIO";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA LISTA DE ASESORES DISPONIBLES PARA EL USUARIO
    [WebMethod]
    public string getObtieneAsesores(string empresa, string usuario, string puesto)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string query = string.Empty;
        
        if (puesto == "C")
            query = "SELECT B.* FROM PE A, PE B " +
                    "WHERE A.CDGEM = '" + empresa + "' " +
                    "AND A.CODIGO = '" + usuario + "' " +
                    "AND A.PUESTO = 'C' " +
                    "AND B.CDGEM = A.CDGEM AND B.CALLE = A.TELEFONO AND B.ACTIVO = 'S'";
        else
            query = "SELECT * FROM PE " +
                    "WHERE CDGEM = '" + empresa + "' " +
                    "AND CDGCO IN (SELECT DISTINCT(CDGCO) FROM PCO WHERE CDGEM = '" + empresa + "' AND CDGPE = '" + usuario + "') " +
                    "AND PUESTO IN ('A','C') " +
                    "AND ACTIVO = 'S'";

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EJECUTA EL PROCEDIMIENTO ENCARGADO DE REGISTRAR LOS ASESORES QUE PUEDE CONSULTAR EL USUARIO
    [WebMethod]
    public string getObtieneAsesoresUsuario(string usuario)
    {
        int iRes;
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        iRes = oE.myExecuteNonQuery("SP_ASIGNA_ASESORES", CommandType.StoredProcedure,
                oP.ParamsAsignaAsesores(empresa, usuario));

        string query = "SELECT * " +
                       "FROM REP_ASIGNA_ASESORES " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGPE = '" + usuario + "'";

        iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA EL SALDO DE GARANTIA DE AQUELLOS CREDITOS AUTORIZADOS CON EXCEPCION
    [WebMethod]
    public string getRepSaldoGL(string fecha, string situacion)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT PRN.CDGNS, " +
                       "NS.NOMBRE GRUPO, " +
                       "PRN.CICLO, " +
                       "TO_CHAR(INICIO,'DD/MM/YYYY') INICIO, " +
                       "((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                              "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) MONTO, " +
                       "FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G', '" + fecha + "') SALDO_GL, " +
                       "(((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                               "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) - FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G', '" + fecha + "')) FALTANTE, " +
                       "PRN.CDGCO, " +
                       "CO.NOMBRE NOMCO " +
                       "FROM PRN, NS, CO " +
                       "WHERE PRN.CDGEM = '" + empresa + "' " +
                       "AND PRN.SITUACION = '" + situacion + "' " +
                       "AND (((CASE WHEN SITUACION = 'T' THEN PRN.CANTAUTOR " +
                                   "WHEN SITUACION = 'E' THEN PRN.CANTENTRE END) * 0.10) - FNSDOGARANTIA(PRN.CDGEM, PRN.CDGNS, PRN.CICLO, 'G', '" + fecha + "')) BETWEEN 1 AND 100 " +
                       "AND NS.CDGEM = PRN.CDGEM " +
                       "AND NS.CODIGO = PRN.CDGNS " +
                       "AND CO.CDGEM = PRN.CDGEM " +
                       "AND CO.CODIGO = PRN.CDGCO";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    /*//METODO QUE OBTIENE DIFERENTES PROMEDIOS DURANTE EL MES INDICADO
    [WebMethod]
    public string getRepSaldoPromMens(string mes, string anio)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT TCD.REGION " +
                       ",TCD.NOM_SUCURSAL " +
                       ",TCD.COD_ASESOR " +
                       ",TCD.NOM_ASESOR " +
                       ",PE.TELEFONO NUM_NOMINA " +
                       ",ROUND((SUM(TCD.MONTO_ENTREGADO)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) CANTENTRE " +
                       ",ROUND((SUM(TCD.SDO_CAPITAL)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) SDOCAPITAL " +
                       ",ROUND((SUM(TCD.SDO_TOTAL)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) SDOTOTAL " +
                       ",ROUND(((SUM(TCD.TASA)/COUNT(CDGCLNS))),2) TASA " +
                       ",ROUND((SUM(TCD.MORA_TOTAL)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) MORA " +
                       ",ROUND((COUNT(TCD.CDGCLNS)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) GRUPOS " +
                       ",ROUND((SUM(TCD.NO_CLIENTES)/TO_CHAR(LAST_DAY('01/' || LPAD(" + mes + ",2,'0') || '/' || " + anio + "),'DD')),2) CLIENTES " +
                       "FROM TBL_CIERRE_DIA TCD, PE " +
                       "WHERE TCD.CDGEM = '" + empresa + "' " +
                       "AND TO_NUMBER(TO_CHAR(TCD.FECHA_CALC,'MM')) = " + mes + " " +
                       "AND TO_NUMBER(TO_CHAR(TCD.FECHA_CALC,'YYYY')) = " + anio + " " +
                       "AND (SELECT COUNT(CDGCLNS) FROM PRN_LEGAL WHERE CDGEM = TCD.CDGEM AND CDGCLNS = TCD.CDGCLNS AND CLNS = TCD.CLNS AND CICLO = TCD.CICLO AND TRUNC(ALTA) <= TCD.FECHA_CALC) = 0 " +
                       "AND PE.CDGEM = TCD.CDGEM " +
                       "AND PE.CODIGO = TCD.COD_ASESOR " +
                       "GROUP BY TCD.REGION " +
                       ",TCD.NOM_SUCURSAL " +
                       ",TCD.COD_ASESOR " +
                       ",TCD.NOM_ASESOR " +
                       ",PE.TELEFONO";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }*/

    //METODO QUE OBTIENE SALDOS PROMEDIO DE ACUERDO A LOS CRITERIOS SELECCIONADOS
    [WebMethod]
    public string getRepSaldoPromMens(string region, string sucursal, string coord, string asesor, string fecha, string nivMora)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string strRegion = string.Empty;
        string strSucursal = string.Empty;
        string strCoord = string.Empty;
        string strAsesor = string.Empty;
        string strNivMora = string.Empty;
        string empresa = cdgEmpresa;

        if (region != "000")
        {
            strRegion = "AND TCD.REGION = (SELECT NOMBRE FROM RG WHERE CDGEM = '" + empresa + "' AND CODIGO = '" + region + "') ";
        }

        if (sucursal != "000")
        {
            strSucursal = "AND TCD.COD_SUCURSAL = '" + sucursal + "' ";
        }

        if (coord != "00")
        {
            strCoord = "AND TCD.CDGCRD = '" + coord + "' ";
        }

        if (asesor != "000000")
        {
            strAsesor = "AND TCD.COD_ASESOR = '" + asesor + "' ";
        }

        if (nivMora == "1")
        {
            strNivMora = "AND TCD.DIAS_MORA <= 60 ";
        }

        string query = "SELECT A.* " +
                       ",(SELECT CLASIFICACION FROM PARAMETROS_INC_ASESOR_CLA WHERE CART_PROM_INI <= A.SDOTOTAL AND CART_PROM_FIN >= A.SDOTOTAL) CLAS_ASESOR " +
                       "FROM (SELECT TCD.REGION " +
                       ",TCD.NOM_SUCURSAL " +
                       ",TCD.COD_ASESOR " +
                       ",TCD.NOM_ASESOR " +
                       ",TCD.NOM_COOR " +
                       ",PE.TELEFONO NUM_NOMINA " +
                       ",ROUND((SUM(TCD.MONTO_ENTREGADO)/TO_NUMBER(" + fecha.Substring(0,2) + ")),2) CANTENTRE " +
                       ",ROUND((SUM(TCD.SDO_CAPITAL)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) SDOCAPITAL " +
                       ",ROUND((SUM(TCD.SDO_TOTAL)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) SDOTOTAL " +
                       ",ROUND(((SUM(TCD.TASA)/COUNT(CDGCLNS))),2) TASA " +
                       ",ROUND((SUM(TCD.MORA_CAPITAL)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) MORACAPITAL " +
                       ",ROUND((SUM(TCD.MORA_TOTAL)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) MORA " +
                       ",ROUND((COUNT(TCD.CDGCLNS)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) GRUPOS " +
                       ",ROUND((SUM(TCD.NO_CLIENTES)/TO_NUMBER(" + fecha.Substring(0, 2) + ")),2) CLIENTES " +
                       "FROM TBL_CIERRE_DIA TCD, PE " +
                       "WHERE TCD.CDGEM = '" + empresa + "' " +
                       "AND TCD.FECHA_CALC BETWEEN '01/" + fecha.Substring(3,2) + "/" + fecha.Substring(6,4) + "' AND '" + fecha + "' " +
                       strRegion +
                       strSucursal + 
                       strCoord +
                       strAsesor +
                       strNivMora +
                       "AND (SELECT COUNT(CDGCLNS) FROM PRN_LEGAL WHERE CDGEM = TCD.CDGEM AND CDGCLNS = TCD.CDGCLNS AND CLNS = TCD.CLNS AND CICLO = TCD.CICLO AND TIPO <> 'O' AND TRUNC(ALTA) <= TCD.FECHA_CALC) = 0 " +
                       "AND PE.CDGEM = TCD.CDGEM " +
                       "AND PE.CODIGO = TCD.COD_ASESOR " +
                       "GROUP BY TCD.REGION " +
                       ",TCD.NOM_SUCURSAL " +
                       ",TCD.COD_ASESOR " +
                       ",TCD.NOM_ASESOR " +
                       ",TCD.NOM_COOR " +
                       ",PE.TELEFONO) A";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE OBTIENE DIFERENTES PROMEDIOS DURANTE EL MES INDICADO
    [WebMethod]
    public string getRepSegChequera(string cuenta, string usuario)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string strCta = string.Empty;

        if (cuenta != "")
            strCta = "AND CH.CDGCB = '" + cuenta + "' ";

        string query = "SELECT CH.CDGCO " +
                       ",(SELECT NOMBRE FROM CO WHERE CDGEM = CH.CDGEM AND CODIGO = CH.CDGCO) NOMCO " +
                       ",CH.CDGCB " +
                       ",(SELECT NOMBRE FROM CB WHERE CDGEM = CH.CDGEM AND CODIGO = CH.CDGCB) NOMCB " +
                       ",(SELECT NUMERO FROM CB WHERE CDGEM = CH.CDGEM AND CODIGO = CH.CDGCB) NUMCB " +
                       ",(SELECT IB.NOMBRE FROM CB, IB WHERE CB.CDGEM = CH.CDGEM AND CB.CODIGO = CH.CDGCB AND IB.CDGEM = CB.CDGEM AND IB.CODIGO = CB.CDGIB) NOMIB " +
                       ",FNSIGCHEQUE(CH.CDGEM,CH.CDGCB) CHQSIG " +
                       ",TO_NUMBER(NVL(MAX(CH.CHEQUEFINAL),0)) CHQFINAL " +
                       ",TO_NUMBER(NVL(MAX(CH.CHEQUEFINAL),0)) - FNSIGCHEQUE(CH.CDGEM,CH.CDGCB) CHQDISP " + 
                       "FROM CHEQUERA CH " +
                       "WHERE CH.CDGEM = '" + empresa + "' " +
                       strCta +
                       "GROUP BY CH.CDGEM, CH.CDGCB, CH.CDGCO " +
                       "HAVING TO_NUMBER(NVL(MAX(CH.CHEQUEFINAL),0)) - FNSIGCHEQUE(CH.CDGEM,CH.CDGCB) >= 0 " +
                       "ORDER BY CH.CDGCO, CH.CDGCB";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE CONSULTA LA INFORMACION DE LOS REGISTROS DEL KILOMETRAJE POR FECHA DE LOS VEHICULOS
    [WebMethod]
    public string getRepSeguimientoVehiculo(string fInicio, string fFin, string region, string sucursal)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;
        string qrySuc = "";
        string qryReg = "";
        if (region != "000")
            qryReg = "AND TSV.CDGRG = '" + region + "' ";
        if (sucursal != "000")
            qrySuc = "AND TSV.CDGCO = '" + sucursal + "' ";
        try
        {
            string query = "SELECT RG.NOMBRE REGION, CO.NOMBRE SUCURSAL, TRV.MARCA, TRV.MODELO, TRV.SERIE, TRV.DESCRIPCION, " +
                           "PE.TELEFONO NOMINA, " +
                           "NOMBREC(NULL, NULL, 'I', 'N', PE.NOMBRE1, PE.NOMBRE2, PE.PRIMAPE, PE.SEGAPE ) NOM_USUARIO, " +
                           "TO_CHAR(TSV.FECHA, 'DD/MM/YYYY') FECHA, TRUNC(TSV.KILOMETRAJE, 2) KILOMETRAJE, " +
                           "TO_CHAR(TSV.FASIGNACION, 'DD/MM/YYYY') FASIGNACION " +
                           "FROM RG, CO, TBL_REG_VEHICULO TRV, TBL_SEG_VEHICULO TSV, PE " +
                           "WHERE TSV.CDGEM = '" + empresa + "' " +
                           "AND TSV.FECHA BETWEEN '" + fInicio + "' AND '" + fFin + "' " +
                           qryReg +
                           qrySuc +
                           "AND RG.CDGEM = TSV.CDGEM " +
                           "AND RG.CODIGO = TSV.CDGRG " +
                           "AND CO.CDGEM = TSV.CDGEM " +
                           "AND CO.CODIGO = TSV.CDGCO " +
                           "AND PE.CDGEM = TSV.CDGEM " +
                           "AND PE.CODIGO = TSV.CDGPE " +
                           "AND TRV.CDGEM = TSV.CDGEM " +
                           "AND TRV.CODIGO = TSV.CDGVEH";
                             
            int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            if (iRes == 1)
                xml = dref.GetXml();
            return xml;
        }
        catch (Exception e)
        {
            string mensaje = e.Message;
            return mensaje;
        }
    }

    //METODO QUE SIMULA EL MONTO A PAGAR EN UNA FECHA DETERMINADA
    [WebMethod]
    public string getRepSimulacion(string grupo, string ciclo, string fecha)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string empresa = cdgEmpresa;

        string query = "SELECT SALDOTOTALPRN(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,PRN.CANTENTRE,PRN.TASA,PRN.PLAZO,PRN.PERIODICIDAD,PRN.CDGMCI,PRN.INICIO,PRN.DIAJUNTA,PRN.MULTPER,PRN.PERIGRCAP,PRN.PERIGRINT,PRN.DESFASEPAGO,PRN.CDGTI,PRN.MODOAPLIRECA,'" + fecha + "') SALDO_TOTAL " +
                       ",SALDOCAPITALPRN(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,PRN.CANTENTRE,PRN.TASA,PRN.PLAZO,PRN.PERIODICIDAD,PRN.CDGMCI,PRN.INICIO,PRN.DIAJUNTA,PRN.MULTPER,PRN.PERIGRCAP,PRN.PERIGRINT,PRN.DESFASEPAGO,PRN.CDGTI,PRN.MODOAPLIRECA,'" + fecha + "',NULL,'N') SALDO_CAP " +
                       ",INTERESVIGENTE(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'G','" + fecha + "') - PAGADOINTERESTOTAL(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'G','" + fecha + "') SALDO_INT " +
                       ",((INTERESPRN(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'" + fecha + "') - CARGOSINTPRN(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'" + fecha + "')) - (INTERESVIGENTE(PRN.CDGEM,PRN.CDGNS,PRN.CICLO,'G','" + fecha + "'))) COND_INT " +
                       "FROM PRN " +
                       "WHERE CDGEM = '" + empresa + "' " +
                       "AND CDGNS = '" + grupo + "' " +
                       "AND CICLO = '" + ciclo + "' " +
                       "AND SITUACION = 'E'";

        int iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (iRes == 1)
            xml = dref.GetXml();
        return xml;
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE SOLICITUDES
    [WebMethod]
    public string getRepSolicitudes(int prestamos, string fecSolDesde, string fecSolHasta, string fecIniDesde,
                                    string fecIniHasta, int sitSolic, int sitCart, int sitRech, string usuario,
                                    string nomUsuario, string region, string sucursal, string asesor)
    {
        DataSet dref = new DataSet();
        DataSet ds = new DataSet();

        dsRepPrestamos.dtPrestamosDataTable dt = new dsRepPrestamos.dtPrestamosDataTable();

        string empresa = cdgEmpresa;
        string xml = "";
        DateTime fecAut;
        int i;
        int contFilas;
        int iRes;
        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_SOLICITUDES_NVO", CommandType.StoredProcedure,
                     oP.ParamsSolicitudesNvo(empresa, fecSolDesde, fecSolHasta, fecIniDesde, fecIniHasta, usuario,
                                             region, sucursal, asesor, sitSolic, sitCart, sitRech));

            string query = "SELECT RS.*, " +
                           "TO_CHAR(SYSDATE,'DD/MM/YYYY') FECHAIMP, " +
                           "TO_CHAR(SYSDATE,'HH24:MI:SS') HORAIMP " +
                           "FROM REP_SOLICITUDES RS " +
                           "WHERE RS.CDGEM = '" + empresa + "' " +
                           "AND RS.CDGPE = '" + usuario + "' " +
                           "ORDER BY RS.INICIO, RS.CDGCLNS";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            contFilas = dref.Tables[0].Rows.Count;

            for (i = 0; i < contFilas; i++)
            {
                DataRow drPrn = dt.NewRow();
                drPrn["TIPOPROD"] = dref.Tables[0].Rows[i]["TIPOPROD"];
                drPrn["COD_GRUPO"] = dref.Tables[0].Rows[i]["CDGCLNS"];
                drPrn["GRUPO"] = dref.Tables[0].Rows[i]["NOMCLNS"];
                drPrn["CICLO"] = dref.Tables[0].Rows[i]["CICLO"];
                drPrn["INICIO"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["INICIO"]).ToString("dd/MM/yyyy");
                drPrn["SOLICITUD"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["SOLICITUD"]).ToString("dd/MM/yyyy HH:mm:ss");
                drPrn["PLAZO"] = dref.Tables[0].Rows[i]["PLAZO"];
                drPrn["PERIODICIDAD"] = dref.Tables[0].Rows[i]["PERIODICIDAD"];
                drPrn["FIN"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["FIN"]).ToString("dd/MM/yyyy");
                drPrn["TASA"] = dref.Tables[0].Rows[i]["TASA"];
                drPrn["SITUACION"] = dref.Tables[0].Rows[i]["SITUACION"];
                drPrn["CANT_SOLIC"] = dref.Tables[0].Rows[i]["CANTSOLIC"];
                drPrn["CANT_AUTOR"] = dref.Tables[0].Rows[i]["CANTAUTOR"];
                drPrn["COD_ASESOR"] = dref.Tables[0].Rows[i]["CDGOCPE"];
                drPrn["ASESOR"] = dref.Tables[0].Rows[i]["NOMPE"];
                drPrn["COD_REGION"] = dref.Tables[0].Rows[i]["CDGRG"];
                drPrn["REGION"] = dref.Tables[0].Rows[i]["NOMRG"];
                drPrn["COD_SUCURSAL"] = dref.Tables[0].Rows[i]["CDGCO"];
                drPrn["SUCURSAL"] = dref.Tables[0].Rows[i]["NOMCO"];
                drPrn["MUJERES"] = dref.Tables[0].Rows[i]["MUJERES"];
                drPrn["HOMBRES"] = dref.Tables[0].Rows[i]["HOMBRES"];
                drPrn["SDO_GL"] = dref.Tables[0].Rows[i]["SALDOGL"];
                drPrn["FECHA_REP"] = Convert.ToDateTime(dref.Tables[0].Rows[i]["FECHA_REP"]).ToString("dd/MM/yyyy");
                drPrn["AUTCARPE"] = dref.Tables[0].Rows[i]["AUTCARPE"];
                drPrn["FAUTCAR"] = DateTime.TryParse(dref.Tables[0].Rows[i]["FAUTCAR"].ToString(), out fecAut) ? fecAut.ToString("dd/MM/yyyy HH:mm:ss") : "";
                drPrn["TIEMPOAUT"] = dref.Tables[0].Rows[i]["TIEMPOAUT"];
                dt.Rows.Add(drPrn);
            }

            if (dref.Tables[0].Rows.Count > 0)
            {
                DataRow dtot = dt.NewRow();
                dtot["COD_GRUPO"] = "-- TOTAL --";
                dtot["SITUACION"] = Convert.ToDecimal(dt.Compute("Count(CANT_SOLIC)", ""));
                dtot["CANT_SOLIC"] = Convert.ToDecimal(dt.Compute("Sum(CANT_SOLIC)", ""));
                dtot["CANT_AUTOR"] = Convert.ToDecimal(dt.Compute("Sum(CANT_AUTOR)", ""));
                dt.Rows.Add(dtot);
            }

            ds.Tables.Add(dt);
            xml = ds.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE TASA REAL
    [WebMethod]
    public string getRepSueldoArchivo(string usuario)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            string query = "SELECT ACS.* " +
                           ",TO_CHAR(ACS.FECHA,'DD/MM/YYYY') FECACT " +
                           "FROM REP_ACT_SUELDO ACS " +
                           "WHERE ACS.CDGEM = '" + empresa + "' " +
                           "AND ACS.CDGPE = '" + usuario + "'";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }
    
    //METODO QUE EXTRAE LA INFORMACION QUE SE DESPLEGARA EN EL REPORTE DE TASA REAL
    [WebMethod]
    public string getRepTasaReal(string mes, string anio, string usuario)
    {
        DataSet dref = new DataSet();

        string empresa = cdgEmpresa;
        string xml = "";
        int iRes;

        try
        {
            iRes = oE.myExecuteNonQuery("SP_REP_TASA_REAL", CommandType.StoredProcedure,
                     oP.ParamsTasaReal(empresa, mes, anio, usuario));

            string query = "SELECT TS.* " +
                           ",TO_CHAR(TS.INICIO,'DD/MM/YYYY') FINICIO " +
                           ",TO_CHAR(TS.FIN,'DD/MM/YYYY') FFIN " +
                           ",TO_CHAR(TS.FIN_REAL,'DD/MM/YYYY') FFIN_REAL " +
                           ",TO_CHAR(sysdate,'DD/MM/YYYY') AS FECHAIMP " +
                           ",TO_CHAR(sysdate,'HH24:MI:SS') AS HORAIMP " +
                           "FROM REP_TASA_REAL TS " +
                           "WHERE TS.CDGEM = '" + empresa + "' " +
                           "AND TS.CDGPE = '" + usuario + "' " +
                           "ORDER BY CDGCLNS";

            iRes = oE.ExecuteDS(ref dref, query, CommandType.Text);

            xml = dref.GetXml();
            return xml;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "";
        }
    }

    //METODO QUE EXTRAE EL RESULTADO PARA EL REPORTE DE VIGENCIA DE SEGURO POR ASESOR
    [WebMethod]
    public string getRepVigenciaSeguro(string asesor, string sucursal)
    {
        DataSet dref = new DataSet();
        string xml = "";
        string qryAsesor = string.Empty;
        string qrySucursal = string.Empty;
        string empresa = cdgEmpresa;
        string status = string.Empty;

        if (asesor != null && asesor != string.Empty)
        {
            qryAsesor = "CDGOCPE = '" + asesor + "' ";
        }
        if (sucursal != null && sucursal != string.Empty)
        {
            qrySucursal = "CDGCO = '" + sucursal + "' ";
        }
        string query = "SELECT M.CDGCL,NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE_ACRED, "
                    + " M.INICIO, M.FIN,  DECODE(M.CDGPMS,'001','VIDA','002','CANCER') TIPO_PROD, PRN.CDGCO, CO.NOMBRE SUCURSAL, "
                    + " PRN.CDGOCPE, NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMPE "
                    + " ,NS.CODIGO CDGCLNS " 
                    + " ,NS.NOMBRE NOMCLNS "
                    + " FROM MICROSEGURO M  "
                    + " INNER JOIN CL ON "
                        + " CL.CDGEM = M.CDGEM "
                        + " AND CL.CODIGO = M.CDGCL "
                    + " INNER JOIN PRN ON "
                        + " PRN.CDGEM = M.CDGEM "
                        + " AND PRN.SITUACION IN ('E','L') "
                        + " AND PRN.CANTENTRE > 0 "
                    + " INNER JOIN NS ON "
                        + " NS.CDGEM = PRN.CDGEM "
                        + " AND NS.CODIGO = PRN.CDGNS "
                    + " INNER JOIN PRC ON "
                        + " PRC.CDGEM = PRN.CDGEM "
                        + " AND PRC.CDGCLNS = PRN.CDGNS "
                        + " AND PRC.CICLO = PRN.CICLO "
                        + " AND PRC.CDGCL = M.CDGCL "
                        + " AND PRC.CLNS = M.CLNS  "
                        + " AND PRC.SITUACION IN ('E','L') "
                        + " AND PRC.CANTENTRE > 0 "
                    + " INNER JOIN PE ON "
                        + " PE.CDGEM = PRN.CDGEM "
                        + " AND PE.CODIGO = PRN.CDGOCPE "
                    + " INNER JOIN CO ON "
                        + " CO.CDGEM = PRN.CDGEM "
                        + " AND CO.CODIGO = PRN.CDGCO "
                    + " WHERE  "
                    + " M.CDGEM = '" + empresa + "' "
                    + " AND M.CLNS = 'G' "
                    + " AND M.FIN >= TRUNC(SYSDATE) "
                    + " AND PRN." + qrySucursal
                    + " AND PRN." + qryAsesor
                    + " UNION "
                    + " SELECT M.CDGCL,NOMBREC(CL.CDGEM,CL.CODIGO,'I','N',NULL,NULL,NULL,NULL) NOMBRE_ACRED, "
                    + " M.INICIO, M.FIN,  DECODE(M.CDGPMS,'001','VIDA','002','CANCER') TIPO_PROD, PRC.CDGCO, CO.NOMBRE SUCURSAL, "
                    + " PRC.CDGOCPE, NOMBREC(NULL,NULL,'I','N',PE.NOMBRE1,PE.NOMBRE2,PE.PRIMAPE,PE.SEGAPE) NOMPE "
                    + " ,NULL CDGCLNS " 
                    + " ,NULL NOMCLNS " 
                    + " FROM MICROSEGURO M  "
                    + " INNER JOIN CL ON "
                        + " CL.CDGEM = M.CDGEM "
                        + " AND CL.CODIGO = M.CDGCL "
                    + " INNER JOIN PRC ON "
                        + " PRC.CDGEM = M.CDGEM "
                        + " AND PRC.CDGCL = M.CDGCL "
                        + " AND PRC.CLNS = M.CLNS  "
                        + " AND PRC.SITUACION IN ('E','L') "
                        + " AND PRC.CANTENTRE > 0 "
                    + " INNER JOIN PE ON "
                        + " PE.CDGEM = PRC.CDGEM "
                        + " AND PE.CODIGO = PRC.CDGOCPE "
                    + " INNER JOIN CO ON "
                        + " CO.CDGEM = PRC.CDGEM "
                        + " AND CO.CODIGO = PRC.CDGCO "
                    + " WHERE  "
                    + " M.CDGEM = '" + empresa + "' "
                    + " AND M.CLNS = 'I' "
                    + " AND M.FIN >= TRUNC(SYSDATE) "
                    + " AND PRC." + qrySucursal
                    + " AND PRC." + qryAsesor;

        int res = oE.ExecuteDS(ref dref, query, CommandType.Text);
        if (res == 1)
            xml = dref.GetXml();
        return xml;
    }

    #endregion
}