Public Class IDObject
    Public Sub New()

    End Sub
    Public Sub New(id As Integer, mediatype As String)
        Me.ID = id
        Me.MediaType = mediatype
    End Sub

    Public Property ID As Integer
    Public Property MediaType As String

End Class
