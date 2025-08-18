using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;

/// <summary>
/// Descripción breve de formatoCURP
/// </summary>
public class formatoCURP
{
    int i;

    string Paterno;
    string Materno;
    string Nombre;
    string NombreRFC;
    string wRFC;
    string wFecNac;
    string[] Arre2;
    string[] Arre6;
    string[] Arre8;
    string[] Arre9;
    string[] Anex11;
    string[] Anex12;
    string[] anex21;
    string[] anex22;
    string[] anex31;
    string[] anex32;

    public string genera_rfc(string CL_PAT, string CL_MAT, string CL_NOM, string DL_FecNac)
    {
        Boolean finice;
        int cont;

        /*** SEGMENTO DE CODIGO QUE GARANTIZA QUE EL APELLIDO PATERNO CUENTE AL 
        MENOS CON TRES CARACTERES ***/
        CL_PAT = CL_PAT.ToUpper();
        if (CL_PAT.Length < 3)
        {
            cont = CL_PAT.Length;
            for (i = 0; i < 3 - cont; i++)
                CL_PAT = CL_PAT + "X";
        }

        /*** SEGMENTO DE CODIGO QUE GARANTIZA QUE EL APELLIDO MATERNO CUENTE AL 
        MENOS CON TRES CARACTERES ***/
        CL_MAT = CL_MAT.ToUpper();
        if (CL_MAT.Length < 3)
        {
            cont = CL_MAT.Length;
            for (i = 0; i < 3 - cont; i++)
                CL_MAT = CL_MAT + "X";
        }

        CL_NOM = CL_NOM.ToUpper();

        wRFC = "";

        //ELIMINA ESPACIOS AL PRINCIPIO Y FINAL DE LAS CADENAS QUE INTEGRAN EL NOMBRE
        Paterno = CL_PAT.Trim();
        Materno = CL_MAT.Trim();
        Nombre = CL_NOM.Trim();

        //CONVIERTE LA FECHA DE NACIMIENTO AL FORMATO AAMMDD
        wFecNac = DL_FecNac.Substring(8, 2) + DL_FecNac.Substring(3, 2) + DL_FecNac.Substring(0, 2);

        //VERIFICA LOS NOMBRES
        if (!verificaNombres())
        {
            return "";
        }

        finice = false;

        /***SEGMENTO DE CODIGO QUE SE ENCARGA DE ELIMINAR CONJUNCIONES O ARTICULOS PRESENTES EN 
        LOS ELEMENTOS QUE INTEGRAN EL NOMBRE DEL CLIENTE ***/
        Paterno = Octava(Paterno);
        Materno = Octava(Materno);
        Nombre = Octava(Nombre);

        Nombre = Sexta(Nombre);

        Paterno = Tercera(Paterno);
        Materno = Tercera(Materno);
        NombreRFC = Nombre;
        Nombre = Tercera(Nombre);
        

        //CULMINA EL PROCESO SI NO ES POSIBLE GENERAR LA CLAVE POR AUSENCIA DE DATOS
        if (Paterno.Length == 0 || Materno.Length == 0)
        {
            SEPTIMA();
            finice = true;
        }

        if (!finice)
        {
            obtieneSegmento();
        }
        
        wRFC = wRFC.Substring(0, 4) + wRFC.Substring(4, 6);
        //wRFC += homonimoRFC();
        //wRFC += DigitoRFC();
        return wRFC;
    }

    public void muestra()
    {
        long van;
        van = palabrasIncorrectas(Arre9, wRFC.Substring(0, 4));
        if (van > 0)
        {
            wRFC = wRFC.Substring(0, 1) + "X" + wRFC.Substring(2);
        }
    }

    /*** FUNCION QUE CONCATENA LAS PRIMERAS 4 LETRAS DE LA CLAVE CON LA CADENA QUE INDICA 
    LA FECHA DE NACIMIENTO ***/
    public void obtieneSegmento()
    {
        string Letra = "X";
        int x;
        long van;
        //Letra = Paterno.Substring(1, 1);
        for (x = 1; x < Paterno.Length; x++)
        {
            van = palabrasIncorrectas(Arre2, Paterno.Substring(x, 1));
            if (van >= 0)
            {
                //SEGMENTO QUE SE UTILIZA PARA INCORPORAR LA PRIMERA VOCAL 
                //PRESENTE EN EL APELLIDO PATERNO
                Letra = Arre2[van];
                x = Paterno.Length;
            }
            else
                Letra = "X";
        }
        wRFC = Paterno.Substring(0, 1) + Letra + Materno.Substring(0, 1) + Nombre.Substring(0, 1);
        wRFC = wRFC + wFecNac;
        muestra();
    }

    //FUNCION QUE ELIMINA ESPACIOS EN BLANCO DE LAS CADENAS RECIBIDAS
    private string Tercera(string vNombre)
    {
        int ind;
        if (vNombre.Length >= 2)
        {
            vNombre = vNombre.Trim();
            ind = vNombre.IndexOf(" ", 1);
            if (ind >= 1)
                vNombre = vNombre.Substring(0, ind);
        }
        return vNombre;

    }

    /*** FUNCION QUE ELIMINA LAS COINCIDENCIAS CON LOS NOMBRES JOSE O MARIA
    UTILIZA LA VARIABLE DE TIPO ARRAY DENOMINADA Arre6 ***/
    private string Sexta(string vNombre)
    {
        long xx;
        if (vNombre.IndexOf(" ", 0) >= 0)
        {
            for (xx = 0; xx < Arre6.Length; xx++)
                vNombre = vNombre.Replace(Arre6[xx], "");
        }
        return vNombre;
    }

    //SEGMENTO DE CODIGO QUE INDICA LA IMPOSIBILIDAD DE GENERAR LA CLAVE POR LA AUSENCIA DE DATOS
    private void SEPTIMA()
    {
        string UnoSolo;
        if (Paterno.Length == 0 && Materno.Length > 0)
            UnoSolo = Materno;
        else
        {
            if (Paterno.Length > 0 && Materno.Length == 0)
                UnoSolo = Paterno;
            else
                UnoSolo = Nombre;
        }
        wRFC = UnoSolo.Substring(0, 2) + Nombre.Substring(0, 2) + wFecNac + "000";
        muestra();
    }

    /*** FUNCION QUE ELIMINA DE LOS APELLIDOS O LOS NOMBRES LAS CONJUNCIONES, ARTICULOS, ETC (EJ: DEL , LA , LOS )
    UTILIZA LA VARIABLE DE TIPO ARRAY DENOMINADA Arre8 ***/
    private string Octava(string vNombre)
    {
        long i;
        int ind;
        for (i = 0; i < Arre8.Length; i++)
        {
            ind = vNombre.IndexOf(Arre8[i]);
            if (ind >= 0)
            {
                if ((ind - 1) < 0 || vNombre.Substring((ind - 1), 1) == " ")
                    vNombre = vNombre.Replace(Arre8[i], "");
            }
        }
        return vNombre;
    }

    private Boolean verificaNombres()
    {
        string wLos3;
        wLos3 = Paterno.Trim() + " " + Materno.Trim() + " " + Nombre.Trim();
        while (wLos3.IndexOf(" ", 0) > 0)
        {
            string v = wLos3.IndexOf(" ", 0).ToString();
            wLos3 = wLos3.Replace(" ", "");
        }
        if (wLos3.Length <= 6)
            return false;
        else
            return true;
    }

    //FUNCION QUE REEMPLEZA LAS PALABRAS ALTISONANTES DENTRO DEL PRIMER SEGMENTO DE LA CLAVE
    public long palabrasIncorrectas(string[] vMatriz, string vValor)
    {
        long i;
        Boolean vStop;
        vStop = false;
        for (i = 0; (i < vMatriz.Length && !vStop); i++)
        {
            if (vMatriz[i] == vValor)
            {
                vStop = true;
                break;
            }
        }
        if (vStop)
            return i;
        else
            return -1;
    }

    private void inicializa()
    {
        string cadena;
        cadena = "A,E,I,O,U";
        Arre2 = cadena.Split(',');

        cadena = "JOSE ,MARIA ,J ,MA ,J. ,M. ";
        Arre6 = cadena.Split(',');

        cadena = "DE ,DEL ,LA ,LOS ,LAS ,Y ,MC ,MAC ,VON ,VAN ";
        Arre8 = cadena.Split(',');

        cadena = "BACA,BAKA,BUEI,BUEY,CACA,CACO,CAGA,CAGO,CAKA,CAKO,COGE," +
        "COGI,COJA,COJE,COJI,COJO,COLA,CULO,FALO,FETO,GETA,GUEI,GUEY,JETA," +
        "JOTO,KACA,KACO,KAGA,KAGO,KAKA,KAKO,KOGE,KOGI,KOJA,KOJO,KOJE,KOJI," +
        "KOJO,KOLA,KULO,LILO,LOCA,LOCO,LOKA,LOKO,MAME,MAMO,MEAR,MEAS,MEON," +
        "MIAR,MION,MOCO,MOKO,MULA,MULO,NACA,NACO,PEDA,PEDO,PENE,PIPI,PITO," +
        "POPO,PUTA,PUTO,QULO,RATA,ROBA,ROBE,ROBO,RUIN,SENO,TETA,VACA,VAGA," +
        "VAGO,VAKA,VUEI,VUEY,WUEI,WUEY";
        Arre9 = cadena.Split(',');

        cadena = "*,0,1,2,3,4,5,6,7,8,9,&,\\," +
        "A,B,C,D,E,F,G,H,I,J,K,L,M," +
        "N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
        Anex11 = cadena.Split(',');

        cadena = "00,00,01,02,03,04,05,06,07,08,09," +
        "10,10,11,12,13,14,15,16,17,18,19," +
        "21,22,23,24,25,26,27,28,29,32,33," +
        "34,35,36,37,38,39";
        Anex12 = cadena.Split(',');

        cadena = "00,01,02,03,04,05,06,07,08,09,10,11,12," +
        "13,14,15,16,17,18,19,20,21,22,23,24,25," +
        "26,27,28,29,30,31,32,33";
        anex21 = cadena.Split(',');

        cadena = "1,2,3,4,5,6,7,8,9,A,B,C,D,E,F,G," +
        "H,I,J,K,L,M,N,P,Q,R,S,T,U,V,W,X," +
        "Y,Z";
        anex22 = cadena.Split(',');

        cadena = "0,1,2,3,4,5,6,7,8,9,A,B,C,D,E,F," +
        "G,H,I,J,K,L,M,N,&,O,P,Q,R,S,T,U," +
        "V,W,X,Y,Z,*";
        anex31 = cadena.Split(',');

        cadena = "00,01,02,03,04,05,06,07,08,09,10,11,12," +
        "13,14,15,16,17,18,19,20,21,22,23,24,25," +
        "26,27,28,29,30,31,32,33,34,35,36,37";
        anex32 = cadena.Split(',');
    }

    private string ExtraeConsonantes(string APaterno, string AMaterno, string Nombres)
    {
        string vocal = "AEIOU";
        string consonantes;
        int i;
        Boolean bandCons = false;
        consonantes = "";
        //if (APaterno.Length > 2 && AMaterno.Replace(" ", "").Length > 2)
        //{
            //Busca la primer consonante del apellido paterno
            for (i = 1; i < APaterno.Length; i++)
            {
                if (vocal.IndexOf(APaterno.Substring(i, 1), 0) < 0)
                {
                    consonantes = APaterno.Substring(i, 1);
                    bandCons = true;
                    break;
                }
            }
            //Incorpora una X si no encontro mas consonantes en el apellido paterno
            if (!bandCons)
                consonantes = "X";

            bandCons = false;
            //Busca la primer consonante del apellido materno
            for (i = 1; i < AMaterno.Length; i++)
            {
                if (vocal.IndexOf(AMaterno.Substring(i, 1)) < 0)
                {
                    consonantes = consonantes + AMaterno.Substring(i, 1);
                    bandCons = true;
                    break;
                }
            }
            //Incorpora una X si no encontro mas consonantes en el apellido materno
            if (!bandCons)
                consonantes = consonantes + "X";

            bandCons = false;
            for (i = 1; i < Nombres.Length; i++)
            {
                if (vocal.IndexOf(Nombres.Substring(i, 1)) < 0)
                {
                    consonantes = consonantes + Nombres.Substring(i, 1);
                    bandCons = true;
                    break;
                }
            }
            //Incorpora una X si no encontro mas consonantes en el nombre
            if (!bandCons)
                consonantes = consonantes + "X";
        //}
        return consonantes.Replace("Ñ", "X");
    }

    //FUNCION QUE GENERA EL DIGITO VERIFICADOR QUE SE AGREGA AL FINAL DE LA CLAVE
    public string digitoVerificador(string curp)
    {
        string segRaiz = curp.Substring(0, curp.Length);
        string chrCaracter = "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
        int[] intFactor;            // declarar el array  09         
        intFactor = new int[17];

        long lngSuma = 0;
        long lngDigito = 0;

        for (int i = 0; i < curp.Length; i++)
        {
            for (int j = 0; j < 37; j++)
            {
                int pos = chrCaracter.IndexOf(segRaiz.Substring(i, 1));
                intFactor[i] = pos;
            }
        }

        for (int k = 0; k < curp.Length; k++)
        {
            lngSuma = lngSuma + ((intFactor[k]) * (18 - k));
        }

        lngDigito = (10 - (lngSuma % 10));

        if (lngDigito == 10)
        {
            lngDigito = 0;
        }

        curp = curp + lngDigito;
        return curp;
    }

    public string genera_curp(string APaterno, string AMaterno, string Nombres, string Fecha, string Sexo, string LNacimiento)
    {
        string CURP;
        string DIGITO1;
        string RFC;
        string vSexo = string.Empty;
        //SEGMENTO DE CODIGO QUE INICIALIZA LAS MATRICES QUE SE UTILIZAN DURANTE EL PROCESO
        inicializa();

        if (APaterno.Length == 0 && AMaterno.Length == 0)
            return " SIN APELLIDOS ";
        if (Nombres.Length == 0)
            return " SIN NOMBRE ";
        if (Fecha.Length == 0)
            return " SIN FECHA ";

        RFC = genera_rfc(APaterno, AMaterno, Nombres, Fecha);
        //SI EL AÑO DE NACIMIENTO ES MENOR AL AÑO 2000 SE ASIGNA COMO HOMOCLAVE EL 0 (CERO)
        //SI EL AÑO DE NACIMIENTO ES MAYOR O IGUAL AL AÑO 2000 SE ASIGNA COMO HOMOCLAVE LA LETRA A
        if (Convert.ToDateTime(Fecha).Year < 2000)
            DIGITO1 = "0";
        else
            DIGITO1 = "A";

        //' El sexo es H Hombre, M Mujer
        switch (Sexo)
        {
            case "H":
                vSexo = "H";
                break;
            case "M":
                vSexo = "M";
                break;
        }
        CURP = RFC + vSexo + LNacimiento + ExtraeConsonantes(Paterno, Materno, Nombre) + DIGITO1;
        CURP = digitoVerificador(CURP);
        return CURP;
    }

    public string homonimoRFC(){
        string strAux;
        string Homo;
        string strInfo;
        string Valores;
        int sumas;
        int SoloTres;
        int Cociente;
        int Residuo;
        int pos;
        int x;
        
        Homo = "0";
        Valores = "0";
        strInfo = (Paterno.Trim() + " " + Materno.Trim() + " " + NombreRFC.Trim());

        for(x = 0; x < strInfo.Length; x++)
        {
            strAux = strInfo.Substring(x, 1);
            
            pos = busqMatriz(Anex11, (strAux == " "? "*": strAux));
            if (pos > 0)
                Valores += Anex12[pos];
            else
                Valores += "00";
        }

        sumas = 0;
        for(x = 0; x < Valores.Length - 1; x++)
            sumas = sumas + (Convert.ToInt32(Valores.Substring(x, 2)) * Convert.ToInt32(Valores.Substring(x + 1, 1)));
        
        SoloTres = sumas % 1000;
        Cociente = Convert.ToInt32(Math.Floor((decimal)SoloTres / 34));
        Residuo = SoloTres - Cociente * 34;
        pos = busqMatriz(anex21, Cociente.ToString("00"));
        
        if (pos > 0)
            Homo = anex22[pos];
        else
            Homo = "1";
        pos = busqMatriz(anex21, Residuo.ToString("00"));
        
        if (pos > 0)
            Homo = Homo + anex22[pos];
        else
            Homo = Homo + "1";

        return Homo;
        //wRFC = Mid(wRFC, 1, 10) + Homo
    }

    //DIGITO VERIFICADOR DEL RFC ...
    private string DigitoRFC(){
        string digito;
        string unok;
        string valores;
        int van;    
        int x;
        long sumas;
        
        long residuo;
        long valor;
        valores = "";
        
        for(x = 0; x < wRFC.Length; x++)
        {
            unok = wRFC.Substring(x, 1);
            van = busqMatriz(anex31, unok == " "? "*": unok);
            if (van > 0)
                valores = valores + anex32[van];
            else
                valores = valores + "00";
        }
        sumas = 0;
        for(x = 0; x < 12; x++)
            sumas = sumas + Convert.ToInt32(valores.Substring(((x * 2) > 0? (x * 2) : 0), 2)) * (14 - (x + 1));
        residuo = (int)sumas - (int)(sumas / 11) * 11;
        if (residuo == 0)
            digito = "0";
        else
        {
            valor = 11 - residuo;
            if (valor == 10)
                digito = "A";
            else
                digito = valor.ToString();
        }
        return digito;
    }

    // Busca un valor en una matriz, si lo encuentra regresa el índice, si no lo encuentra regresa 0
    public int busqMatriz(string [] vMatriz, string vValor)
    {
        int i;
        bool vStop;
        vStop = false;
        for (i = 1; i < vMatriz.Length && !vStop; i++)
        {
            if (vMatriz[i] == vValor)
            {
                vStop = true;
                break;
            }
        }
        return (vStop? i: 0);
    }
}
