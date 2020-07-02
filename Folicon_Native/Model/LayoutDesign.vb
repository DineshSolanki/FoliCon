Imports System.Threading
Imports FoliconNative.Modules
Namespace Model
    Public Class LayoutDesign
        Inherits MyMovieIconLayout
        Public Sub New()
            MyBase.New("C:\dummy.jpg", "8.1", "visible", "visible")
        End Sub
        Private Shared Async Function DummyFolderJpgAsync() As Task(Of String)
            Dim tmpPath = IO.Path.GetTempPath
            Dim folderJpgPath = IO.Path.Combine(tmpPath, "posterDummy.jpg")
            'If Not IO.File.Exists(folderJpgPath) Then
            '    Await DownloadImageFromUrlAsync("https://image.tmdb.org/t/p/original/r0bgHi3MwGHTKPWyJdORsb4ukY8.jpg", folderJpgPath)
            'End If
            Return folderJpgPath
        End Function
        Private Shared Async Function GetImageAsync(url As String, savePath As String) As Task
            Await DownloadImageFromUrlAsync(url, savePath)
        End Function
    End Class
End Namespace