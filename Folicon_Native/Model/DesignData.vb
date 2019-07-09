Imports System.Threading

Namespace Model


    Public Class DummyIconLayoutViewModel
        Inherits IconLayoutViewModel

        Public Sub New()
            MyBase.New(DummyFolderJpg(), "8.1")
        End Sub

        Private Shared Async Function DummyFolderJpg() As Tasks.Task(Of String)
            Dim folderJpgPath = "C:\Temp\folder.jpg"
            If Not System.IO.File.Exists(folderJpgPath) Then
                ' Call (Await Downloadimage("http://ia.media-imdb.com/images/M/MV5BODU4MjU4NjIwNl5BMl5BanBnXkFtZTgwMDU2MjEyMDE@._V1_SX300.jpg", folderJpgPath)
                Await DownloadImage("http://ia.media-imdb.com/images/M/MV5BODU4MjU4NjIwNl5BMl5BanBnXkFtZTgwMDU2MjEyMDE@._V1_SX300.jpg", folderJpgPath, CancellationToken.None)
            End If
            Return folderJpgPath
        End Function
    End Class
End Namespace