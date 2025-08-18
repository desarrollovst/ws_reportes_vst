using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.VisualBasic;

/// <summary>
/// Descripción breve de funcionesFinancieras
/// </summary>
public class FuncionesFinancieras
{
	public FuncionesFinancieras()
	{
		//
		// TODO: Agregar aquí la lógica del constructor
		//
	}

    public string GeneraCAT(string ValorCat, int NoPagos, string Periodo)
    {
        // ValorCat es una cadena de este tipo
        // <ValorCat>-49000.00, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.87, 4695.91</ValorCat> 
        // <Periodo>Semanal</Periodo> <Periodo>Quincenal</Periodo> <Periodo>Mensual</Periodo> 
        int NoSemanas = 0;
        char[] sep = { ',' };
        string[] Array = ValorCat.Split(sep);
        double[] valueArray;
        valueArray = new double[NoPagos + 1];

        for (int i = 0; i < Array.Length; i++)
        {
            valueArray[i] = double.Parse(Array[i]);
        }
        if ((Periodo == "Semanal") || (Periodo == "semanas"))
            NoSemanas = 52;
        else if ((Periodo == "Quincenal") || (Periodo == "catorcenas"))
            NoSemanas = 26;
        else if ((Periodo == "Mensual") || (Periodo == "meses"))
            NoSemanas = 12;
        else if (Periodo == "trimestres")
            NoSemanas = 4;
        else if (Periodo == "semestres")
            NoSemanas = 2;

        double guess = 0.1;
        double CalcRetRate = (Financial.IRR(ref valueArray, guess)) * 100;
        string IRR = CalcRetRate.ToString("#0.000");
        double CAT = (Math.Pow(1 + (double.Parse(IRR) / 100), NoSemanas) - 1) * 100;
        return CAT.ToString("##0.0");
   }
}
