Imports System.Threading
Imports FoliconNative.Modules
Namespace Model
    Public Class LayoutDesign
        Inherits MyMovieIconLayout
        Public Sub New()
            MyBase.New(DummyFolderJpg(), "8.1")
        End Sub
        Private Shared Async Function DummyFolderJpg() As Task(Of String)
            Dim folderJpgPath = "H:\Users\ \Documents\Video\24 (IN)\24 (IN).jpg"
            If Not IO.File.Exists(folderJpgPath) Then

            End If
            Return folderJpgPath
        End Function
    End Class
End Namespace