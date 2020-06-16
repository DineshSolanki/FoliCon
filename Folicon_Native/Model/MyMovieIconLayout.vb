Imports System.IO

Namespace Model

    Public Class MyMovieIconLayout
        Private _task As Task(Of String)
        Private _p2 As String

        Protected Sub New(task As Task(Of String), p2 As String)

            _task = task
            'Dim thisMemoryStream As New MemoryStream(My.Computer.FileSystem.ReadAllBytes(_task.Result))
            'Dim imageSourceConverter = New ImageSourceConverter()
            'FolderJpg = CType(ImageSourceConverter.ConvertFrom(thisMemoryStream), ImageSource)
            _p2 = p2
            'Rating = _p2
        End Sub

        Public Property FolderJpg() As ImageSource
        Public Property RatingVisibilty() As String
        Public Property Rating() As String
        Public Property MockupVisibility As String

        Public Sub New(ByVal folderJpgPath As String, ByVal Rating As String, ByVal ratingVisibilty As String, ByVal mockupVisibility As String)
            Me.RatingVisibilty = ratingVisibilty
            Me.Rating = Rating
            Me.MockupVisibility = mockupVisibility
            Dim thisMemoryStream As New MemoryStream(My.Computer.FileSystem.ReadAllBytes(folderJpgPath))
            Dim imageSourceConverter = New ImageSourceConverter()
            Me.FolderJpg = CType(imageSourceConverter.ConvertFrom(thisMemoryStream), ImageSource)
        End Sub
    End Class
End Namespace