
Public Class IconLayoutViewModel

    Private _task As Task(Of String)
    Private _p2 As String

    Protected Sub New(task As Threading.Tasks.Task(Of String), p2 As String)
        ' TODO: Complete member initialization 
        _task = task
        _p2 = p2
    End Sub

    Public Property FolderJpg() As ImageSource
    Public Property IsVisible() As String
    Public Property Rating() As String

    Public Sub New(ByVal folderJpgPath As String, ByVal Rating As String, ByVal isVisible As String)
        Me.Rating = Rating
        Me.IsVisible = isVisible
        FolderJpg = CType((New ImageSourceConverter()).ConvertFromString(folderJpgPath), ImageSource)
    End Sub
End Class



