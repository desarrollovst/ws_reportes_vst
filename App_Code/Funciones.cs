using System;
using System.Collections.Generic;
using System.Web;

/// <summary>
/// Descripción breve de funciones
/// </summary>
public class Funciones
{
	public Funciones()
	{
		//
		// TODO: Agregar aquí la lógica del constructor
		//
	}

    public string formatoFecha(string fecha)
    {
        try
        {
            string fecMod = Convert.ToDateTime(fecha.ToString()).ToString("dd/MM/yyyy");
            return fecMod;
        }
        catch (Exception e)
        {
            string error = e.Message;
            return error;
        }
    }

    public string convFormatoFechaAnio(string fecha)
    {
        string fecMod = string.Empty;
        DateTime d;
        //Convierte la fecha con formato YYYY-MM-DD al formato "dd/MM/yyyy"
        if (fecha != "")
            fecha = fecha.Substring(8, 2) + "/" + fecha.Substring(5, 2) + "/" + fecha.Substring(0, 4);
        if (DateTime.TryParse(fecha, out d))
            fecMod = Convert.ToDateTime(fecha).ToString("dd/MM/yyyy");
        return fecMod;
    }

    public string convFormatoFecha(string fecha)
    {
        string fecMod = string.Empty;
        DateTime d;
        if (fecha != "")
            fecha = fecha.Substring(0, 2) + "/" + fecha.Substring(2, 2) + "/" + fecha.Substring(4, 4);
        if (DateTime.TryParse(fecha, out d))
            fecMod = Convert.ToDateTime(fecha).ToString("dd/MM/yyyy");
        return fecMod;
    }
}
