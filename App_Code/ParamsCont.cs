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
/// Descripción breve de ParamsCont
/// </summary>
public class ParamsCont
{
	public ParamsCont()
	{
		//
		// TODO: Agregar aquí la lógica del constructor
		//
	}

    public OracleParameter[] ParamsPoliza(string empresa, string fecha, string tipo, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECHA", OracleDbType.Varchar2),
                                               new OracleParameter("prmTIPO", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecha;
        OracleParameters[2].Value = tipo;
        OracleParameters[3].Value = usuario;

        return OracleParameters;
    }

    public OracleParameter[] ParamsPolizaFecha(string empresa, string fecIni, string fecFin, string tipo, string usuario)
    {
        OracleParameter[] OracleParameters = { new OracleParameter("prmCDGEM", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECINI", OracleDbType.Varchar2),
                                               new OracleParameter("prmFECFIN", OracleDbType.Varchar2),
                                               new OracleParameter("prmTIPO", OracleDbType.Varchar2),
                                               new OracleParameter("prmCDGPE", OracleDbType.Varchar2)};

        OracleParameters[0].Value = empresa;
        OracleParameters[1].Value = fecIni;
        OracleParameters[2].Value = fecFin;
        OracleParameters[3].Value = tipo;
        OracleParameters[4].Value = usuario;

        return OracleParameters;
    }
}
