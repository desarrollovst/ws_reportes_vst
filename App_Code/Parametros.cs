using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Oracle.DataAccess.Client;

/// <summary>
/// Descripción breve de Parametros
/// </summary>
public class Parametros
{
	public Parametros()
	{
		//
		// TODO: Agregar aquí la lógica del constructor
		//
	}

    #region ORACLE_PARAMETERS

    public OracleParameter[] ParamsAnalisis(string empresa, string codigo, string ciclo, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("vCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("vCDGCLNS", OracleDbType.Varchar2),
                                               new OracleParameter("vCICLO", OracleDbType.Varchar2),
                                               new OracleParameter("vUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = codigo;
        OracleParameters[2].Value = ciclo;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsAnalisisCobranza(string empresa, DateTime fecha, int tipoCart, string usuario,
                                                    string coord, string asesor, string supervisor)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("vCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("vSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("vASESOR", OracleDbType.Varchar2),
                                               new OracleParameter("dFECHAHASTA", OracleDbType.Date),
                                               new OracleParameter("vUSUARIO", OracleDbType.Varchar2),                                        
                                               new OracleParameter("vSUPERVISOR", OracleDbType.Varchar2),
                                               new OracleParameter("vTIPOCART", OracleDbType.Varchar2) };

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = coord;
        OracleParameters[2].Value = asesor;
        OracleParameters[3].Value = fecha;
        OracleParameters[4].Value = usuario;
        OracleParameters[5].Value = supervisor;
        OracleParameters[6].Value = tipoCart;

        return OracleParameters;
    }

    public OracleParameter[] ParamsAsignaAsesores(string empresa, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
											   new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsBandas(string empresa, DateTime fecha, int tipoCart, string usuario, string region,
                                           string coord, string asesor)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("vCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("dFECHAPAGOS", OracleDbType.Date),
                                               new OracleParameter("vREGION", OracleDbType.Varchar2),
                                               new OracleParameter("vSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("vASESOR", OracleDbType.Varchar2), 
                                               new OracleParameter("vUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("vTIPOCAR", OracleDbType.Int32)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = region;
        OracleParameters[3].Value = coord;
        OracleParameters[4].Value = asesor;
        OracleParameters[5].Value = usuario;
        OracleParameters[6].Value = tipoCart;

        return OracleParameters;
    }

    public OracleParameter[] ParamsCierreAcred(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = {   new OracleParameter("prmCDGEM", OracleDbType.Varchar2),   
                                                 new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsCierreAsignaFondeo(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;
        OracleParameters[3].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsCifrasControl(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;
        OracleParameters[3].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsCirculoCredito(string empresa, string fecIni, string fecFin, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINI", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECFIN", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecIni;
        OracleParameters[2].Value = fecFin;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsCodigosUsuario(string empresa, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = usuario;
        OracleParameters[2].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsConciliacionGL(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("pCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("pFECHA", OracleDbType.Date),
                                               new OracleParameter("pCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsConciliacionPagos(string empresa, DateTime fecha, string coord, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("pCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("pFECHA", OracleDbType.Date),
                                               new OracleParameter("pCDGCO", OracleDbType.Varchar2),
                                               new OracleParameter("pCDGPE", OracleDbType.Varchar2)};


        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = coord;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsChqImp(string empresa, string fechaIni, string fechaFin, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHAINI", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHAFIN", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fechaIni;
        OracleParameters[2].Value = fechaFin;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsIndicadores(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;
        OracleParameters[3].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsMora(string empresa, DateTime fecha, int nivel, int cartVig, int cartVenc, int cartRest, int cartCast, string usuario,
                                           string region, string sucursal, string coord, string asesor, string tipoProd, string nivelMora)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("vCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("dFECHAHASTA", OracleDbType.Date),
                                               new OracleParameter("vNIVEL", OracleDbType.Int32),
                                               new OracleParameter("vUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("vREGION", OracleDbType.Varchar2),
                                               new OracleParameter("vSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("vCOORD", OracleDbType.Varchar2),
                                               new OracleParameter("vASESOR", OracleDbType.Varchar2), 
                                               new OracleParameter("vCARTVIG", OracleDbType.Int32),
                                               new OracleParameter("vCARTVENC", OracleDbType.Int32),
                                               new OracleParameter("vCARTREST", OracleDbType.Int32),
                                               new OracleParameter("vCARTCAST", OracleDbType.Int32),
                                               new OracleParameter("vTIPOPROD", OracleDbType.Varchar2),
                                               new OracleParameter("vNIVMORA", OracleDbType.Varchar2) };

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = nivel;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Value = region;
        OracleParameters[5].Value = sucursal;
        OracleParameters[6].Value = coord;
        OracleParameters[7].Value = asesor;
        OracleParameters[8].Value = cartVig;
        OracleParameters[9].Value = cartVenc;
        OracleParameters[10].Value = cartRest;
        OracleParameters[11].Value = cartCast;
        OracleParameters[12].Value = tipoProd;
        OracleParameters[13].Value = nivelMora;

        return OracleParameters;
    }

    public OracleParameter[] ParamsOperacionesFrac(string empresa, int mes, int anio, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmMES", OracleDbType.Int32),
                                               new OracleParameter("prmANIO", OracleDbType.Int32),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = mes;
        OracleParameters[2].Value = anio;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsOperacIndMonto(string empresa, int mes, int anio, decimal monto, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmMES", OracleDbType.Int32),
                                               new OracleParameter("prmANIO", OracleDbType.Int32),
                                               new OracleParameter("prmMONTO", OracleDbType.Decimal),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMensaje", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = mes;
        OracleParameters[2].Value = anio;
        OracleParameters[3].Value = monto;
        OracleParameters[4].Value = usuario;
        OracleParameters[5].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsPolizaComisionPago(string empresa, string anio, string mes, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmANIO", OracleDbType.Int32),
                                               new OracleParameter("prmMES", OracleDbType.Int32),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMENSAJE", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = int.Parse(anio);
        OracleParameters[2].Value = int.Parse(mes);
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsPolizaGastosInteres(string empresa, string anio, string mes, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmANIO", OracleDbType.Int32),
                                               new OracleParameter("prmMES", OracleDbType.Int32),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("vMENSAJE", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = anio;
        OracleParameters[2].Value = mes;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsPrestamos(string empresa, string fecIniDesde, string fecIniHasta, string usuario, string region,
                                             string sucursal, string coord, string asesor, string fecPagos, int cartVig, int cartVenc, int cartRest,
                                             int cartCast, int sitSaldo, int sitLiq, int sitCart, int sitTes, int sitDev, int ciclos, string fecFinDesde,
                                             string fecFinHasta, string tipoProd)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINIDESDE", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINIHASTA", OracleDbType.Varchar2),
                                               new OracleParameter("prmUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("prmREGION", OracleDbType.Varchar2),
                                               new OracleParameter("prmSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("prmCOORD", OracleDbType.Varchar2),
                                               new OracleParameter("prmASESOR", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECPAGOS", OracleDbType.Varchar2),
                                               new OracleParameter("vCARTVIG", OracleDbType.Int32),
                                               new OracleParameter("vCARTVENC", OracleDbType.Int32),
                                               new OracleParameter("vCARTREST", OracleDbType.Int32),
                                               new OracleParameter("vCARTCAST", OracleDbType.Int32),
                                               new OracleParameter("prmSITSALDO", OracleDbType.Int32),
                                               new OracleParameter("prmSITLIQ", OracleDbType.Int32),
                                               new OracleParameter("prmSITCART", OracleDbType.Int32),
                                               new OracleParameter("prmSITTES", OracleDbType.Int32),
                                               new OracleParameter("prmSITDEV", OracleDbType.Int32),
                                               new OracleParameter("prmCICLOS", OracleDbType.Int32),
                                               new OracleParameter("prmFECFINDESDE", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECFINHASTA", OracleDbType.Varchar2),
                                               new OracleParameter("vTIPOPROD", OracleDbType.Varchar2)};
        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecIniDesde;
        OracleParameters[2].Value = fecIniHasta;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Value = region;
        OracleParameters[5].Value = sucursal;
        OracleParameters[6].Value = coord;
        OracleParameters[7].Value = asesor;
        OracleParameters[8].Value = fecPagos;
        OracleParameters[9].Value = cartVig;
        OracleParameters[10].Value = cartVenc;
        OracleParameters[11].Value = cartRest;
        OracleParameters[12].Value = cartCast;
        OracleParameters[13].Value = sitSaldo;
        OracleParameters[14].Value = sitLiq;
        OracleParameters[15].Value = sitCart;
        OracleParameters[16].Value = sitTes;
        OracleParameters[17].Value = sitDev;
        OracleParameters[18].Value = ciclos;
        OracleParameters[19].Value = fecFinDesde;
        OracleParameters[20].Value = fecFinHasta;
        OracleParameters[21].Value = tipoProd;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRespChequeras(string empresa, string cuenta, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGCB", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("prmMENSAJE", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = cuenta;
        OracleParameters[2].Value = fecha;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepComisionesDistribuidasMensual(string empresa, int anio, int mes, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                                new OracleParameter("prmANIO", OracleDbType.Int32),
                                                new OracleParameter("prmANIO", OracleDbType.Int32),
                                                new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = anio;
        OracleParameters[2].Value = mes;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepConciliacionCheques(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                                new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                                new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepConciliacionChequesMen(string empresa, string fechaIni, string fechaFin, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                                new OracleParameter("prmFECHAI", OracleDbType.Varchar2),
                                                new OracleParameter("prmFECHAF", OracleDbType.Varchar2),
                                                new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fechaIni;
        OracleParameters[2].Value = fechaFin;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }


    public OracleParameter[] ParamsRepContPagos(string empresa, string fecha, string region, string sucursal, string asesor, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmREGION", OracleDbType.Varchar2),
                                               new OracleParameter("prmSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("prmASESOR", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("prmMENSAJE", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = region;
        OracleParameters[3].Value = sucursal;
        OracleParameters[4].Value = asesor;
        OracleParameters[5].Value = usuario;
        OracleParameters[6].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepPagosSemanalesEsperados(string empresa, string inicio, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmINICIO", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = inicio;
        OracleParameters[2].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepProrrateoConsultas(string empresa, string año, string mes, double costo, string usuario
        , int redondear)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmANIO", OracleDbType.Varchar2),
                                               new OracleParameter("prmMES", OracleDbType.Varchar2),
                                               new OracleParameter("prmCOSTO", OracleDbType.Double),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                               new OracleParameter("prmREDONDEAR", OracleDbType.Int32),
                                               new OracleParameter("vMENSAJE", OracleDbType.Varchar2, 200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = año;
        OracleParameters[2].Value = mes;
        OracleParameters[3].Value = costo;
        OracleParameters[4].Value = usuario;
        OracleParameters[5].Value = redondear;
        OracleParameters[6].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsRepResSegVeh(string empresa, string fInicio, string fFin, string region, string sucursal, string usuario)
    {
        OracleParameter[] OracleParameters = {   new OracleParameter("prmCDGEM", OracleDbType.Varchar2),   
                                                 new OracleParameter("prmFINICIO", OracleDbType.Varchar2),
                                                 new OracleParameter("prmFFIN", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGRG", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGCO", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                                 new OracleParameter("vMensaje", OracleDbType.Varchar2,200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fInicio;
        OracleParameters[2].Value = fFin;
        OracleParameters[3].Value = region;
        OracleParameters[4].Value = sucursal;
        OracleParameters[5].Value = usuario;
        OracleParameters[6].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsSaldosCierre(string empresa, string fecha, string usuario)
    {
        OracleParameter[] OracleParameters = {   new OracleParameter("prmEMPRESA", OracleDbType.Varchar2),   
                                                 new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGPE", OracleDbType.Varchar2),
                                                 new OracleParameter("prmMensaje", OracleDbType.Varchar2,200)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = usuario;
        OracleParameters[3].Direction = ParameterDirection.Output;

        return OracleParameters;
    }

    public OracleParameter[] ParamsSolicitudes(string empresa, string fecIniDesde, string fecIniHasta, string usuario, string region,
                                               string coord, string asesor, int sitSolic, int sitCart, int sitRech)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINIDESDE", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINIHASTA", OracleDbType.Varchar2),
                                               new OracleParameter("prmUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("prmREGION", OracleDbType.Varchar2),
                                               new OracleParameter("prmSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("prmASESOR", OracleDbType.Varchar2),
                                               new OracleParameter("prmSITSOLIC", OracleDbType.Int32),
                                               new OracleParameter("prmSITCART", OracleDbType.Int32),
                                               new OracleParameter("prmSITRECH", OracleDbType.Int32)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecIniDesde;
        OracleParameters[2].Value = fecIniHasta;
        OracleParameters[3].Value = usuario;
        OracleParameters[4].Value = region;
        OracleParameters[5].Value = coord;
        OracleParameters[6].Value = asesor;
        OracleParameters[7].Value = sitSolic;
        OracleParameters[8].Value = sitCart;
        OracleParameters[9].Value = sitRech;

        return OracleParameters;
    }

    public OracleParameter[] ParamsSolicitudesNvo(string empresa, string fecSolDesde, string fecSolHasta, string fecIniDesde,
                                               string fecIniHasta, string usuario, string region, string sucursal,
                                               string asesor, int sitSolic, int sitCart, int sitRech)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECSOLDESDE", OracleDbType.Varchar2), 
                                               new OracleParameter("prmFECSOLHASTA", OracleDbType.Varchar2), 
                                               new OracleParameter("prmFECINIDESDE", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINIHASTA", OracleDbType.Varchar2),
                                               new OracleParameter("prmUSUARIO", OracleDbType.Varchar2),
                                               new OracleParameter("prmREGION", OracleDbType.Varchar2),
                                               new OracleParameter("prmSUCURSAL", OracleDbType.Varchar2),
                                               new OracleParameter("prmASESOR", OracleDbType.Varchar2),
                                               new OracleParameter("prmSITSOLIC", OracleDbType.Int32),
                                               new OracleParameter("prmSITCART", OracleDbType.Int32),
                                               new OracleParameter("prmSITRECH", OracleDbType.Int32)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecSolDesde;
        OracleParameters[2].Value = fecSolHasta;
        OracleParameters[3].Value = fecIniDesde;
        OracleParameters[4].Value = fecIniHasta;
        OracleParameters[5].Value = usuario;
        OracleParameters[6].Value = region;
        OracleParameters[7].Value = sucursal;
        OracleParameters[8].Value = asesor;
        OracleParameters[9].Value = sitSolic;
        OracleParameters[10].Value = sitCart;
        OracleParameters[11].Value = sitRech;

        return OracleParameters;
    }

    public OracleParameter[] ParamsTasaReal(string empresa, string mes, string anio, string usuario)
    {
        OracleParameter[] OracleParameters = {   new OracleParameter("prmEMPRESA", OracleDbType.Varchar2),   
                                                 new OracleParameter("prmMES", OracleDbType.Varchar2),
                                                 new OracleParameter("prmANIO", OracleDbType.Varchar2),
                                                 new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = mes;
        OracleParameters[2].Value = anio;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }

    #endregion
}
