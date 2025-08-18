
Partial Class TxtExport
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim cad As String
        Dim strName As String = Request("titulo").ToString() & ".txt"

        cad = "hola" & "\n" & "adios"
        'cad = Request("texto").ToString()
        With Response
            .ContentEncoding = Encoding.Default
            .AddHeader("Content-disposition", "attachment;filename=" & strName)
            .ContentType = "application/octet-stream"
            .Write(cad)
            'BinaryWrite(btFile)
            .End()
        End With
    End Sub
End Class

