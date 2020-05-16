
Imports System.Drawing
Imports System.Drawing.IconLib

Public Class PngToIcoService
    Public Shared Sub Convert(bitmap As Bitmap, ByVal icoPath As String)
        Dim mIcon As New MultiIcon()
        mIcon.Add("Untitled").CreateFrom(bitmap, IconOutputFormat.Vista)
        mIcon.SelectedIndex = 0
        mIcon.Save(icoPath, MultiIconFormat.ICO)
    End Sub
End Class

