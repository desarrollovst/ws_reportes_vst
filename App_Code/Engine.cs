using System;
using System.Data;
using System.Configuration;
//using System.Linq;
//using System.Data.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
//using System.Xml.Linq;
using Oracle.DataAccess.Client;



/// <summary>
/// Summary description for Engine
/// </summary>
public class Engine
{
    
    public string oradb;
    private OracleConnection OracleConn; 
    private OracleDataAdapter OracleAdap;
    private OracleCommand OracleCmd;
    private Boolean IsTransaction;

    public Engine()
    {
        //
        // TODO: Add constructor logic here
        //
        oradb = ConfigurationManager.AppSettings.Get("Oracle");
    }

    
    #region ConectarOracle

    public Boolean ConectarOracle(Boolean _IsTransaction)
    {
        try
        {
            OracleConn = new OracleConnection(oradb);
            OracleConn.Open();

            if (OracleConn.State == ConnectionState.Open)
                return true;

            IsTransaction = _IsTransaction;
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }

        return false;
    }

    #endregion

    #region DesconectarOracle

    public void DesconectarOracle()
    {
        try
        {
            OracleConn.Close();
            OracleConn.Dispose();
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }

        return;
    }

    #endregion

    #region ExecuteDS

    public int ExecuteDS(ref DataSet ds, string sQuery, CommandType ctType, OracleParameter[] colParameters)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                if (colParameters != null)
                    OracleCmd.Parameters.Add(colParameters);
                OracleAdap.SelectCommand = OracleCmd;
                OracleAdap.Fill(ds);

                return 1;
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }

    public int ExecuteDS(ref DataSet ds, string sQuery, CommandType ctType)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                OracleAdap.SelectCommand = OracleCmd;
                OracleAdap.Fill(ds);
                return 1;
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }

    public int myExecuteNonQuery(string sQuery, CommandType ctType, string Connection)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                OracleAdap.SelectCommand = OracleCmd;
                return OracleAdap.SelectCommand.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }

    public int myExecuteNonQuery(ref string status, string sQuery, CommandType ctType, OracleParameter[] colParameters)
    {
        int res;
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                if (colParameters != null)
                {
                    for (int i = 0; i < colParameters.Length; i++)
                    {
                        OracleCmd.Parameters.Add(colParameters[i]);
                    }
                }
                OracleAdap.SelectCommand = OracleCmd;
                res = OracleAdap.SelectCommand.ExecuteNonQuery();
                status = OracleCmd.Parameters[colParameters.Length - 1].Value.ToString();

                if (res == -1)
                    return 1;
                else
                    return -1;
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
            return -1;
        }
        finally
        {
            DesconectarOracle();
        }
        return -1;
    }

    public int myExecuteNonQuery(string sQuery, CommandType ctType, OracleParameter[] colParameters)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                if (colParameters != null)
                {
                    for (int i = 0; i < colParameters.Length; i++)
                    {
                        OracleCmd.Parameters.Add(colParameters[i]);
                    }
                }
                OracleAdap.SelectCommand = OracleCmd;
                return OracleAdap.SelectCommand.ExecuteNonQuery();                
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }

    public int myExecuteNonQuery(string sQuery, CommandType ctType, OracleParameter[] colParameters, ref int status)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;

                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                if (colParameters != null)
                    OracleCmd.Parameters.Add(colParameters);
                OracleAdap.SelectCommand = OracleCmd;

                int r = OracleAdap.SelectCommand.ExecuteNonQuery();
                int.TryParse(OracleCmd.Parameters[colParameters.Length - 1].Value.ToString(),out status);
                return r;
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }

    public int myExecuteNonQuery(string sQuery, CommandType ctType)
    {
        try
        {
            if (!ConectarOracle(false))
                return -1;

            if (OracleConn.State == ConnectionState.Open)
            {
                OracleCmd = new OracleCommand(sQuery, OracleConn);
                OracleCmd.CommandTimeout = 0;
               
                OracleAdap = new OracleDataAdapter();
                OracleCmd.CommandType = ctType;
                OracleAdap.SelectCommand = OracleCmd;
                return OracleAdap.SelectCommand.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            string MsgError = ex.Message;
        }
        finally
        {
            DesconectarOracle();
        }
        return -2;
    }
    #endregion
}
