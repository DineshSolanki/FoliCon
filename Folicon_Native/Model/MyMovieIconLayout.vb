Imports System.IO

Namespace Model

    Public Class MyMovieIconLayout
        Private _task As Task(Of String)
        Private _p2 As String

        Protected Sub New(task As Task(Of String), p2 As String)

            _task = task
            _p2 = p2
        End Sub

        Public Property FolderJpg() As ImageSource
        Public Property IsVisible() As String
        Public Property Rating() As String

        Public Sub New(ByVal folderJpgPath As String, ByVal Rating As String, ByVal isVisible As String)
            Me.IsVisible = isVisible
            Me.Rating = Rating
            Dim thisMemoryStream As New MemoryStream(My.Computer.FileSystem.ReadAllBytes(folderJpgPath))
            Dim imageSourceConverter = New ImageSourceConverter()
            Me.FolderJpg = CType(imageSourceConverter.ConvertFrom(thisMemoryStream), ImageSource)
        End Sub
    End Class
End Namespace