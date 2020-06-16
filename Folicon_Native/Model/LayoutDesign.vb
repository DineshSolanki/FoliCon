Imports FoliconNative.Modules
Namespace Model
    Public Class LayoutDesign
        Inherits MyMovieCIcon
        Public Sub New()
            MyBase.New(New MyMovieIconLayout(DummyFolderJpg.Result, "8.1", "visible", "visible"))
        End Sub
        Private Shared Async Function DummyFolderJpg() As Task(Of String)
            Dim folderJpgPath = IO.Path.Combine(IO.Path.GetTempPath, "FoliconDummy.jpg")
            If Not IO.File.Exists(folderJpgPath) Then
                Await DownloadImageFromUrlAsync("https://m.media-amazon.com/images/M/MV5BNGVjNWI4ZGUtNzE0MS00YTJmLWE0ZDctN2ZiYTk2YmI3NTYyXkEyXkFqcGdeQXVyMTkxNjUyNQ@@._V1_QL50_SY1000_CR0,0,674,1000_AL_.jpg", folderJpgPath)
            End If
            Return folderJpgPath
        End Function
    End Class
End Namespace