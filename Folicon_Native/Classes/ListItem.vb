Imports System.ComponentModel

Public Class ListItem
    Implements INotifyPropertyChanged
    Public Property Title() As String
    Public Property Year() As String
    Public Property Rating() As String
    Public Property Folder() As String
    Public Property Overview() As String
    Public Sub New(ByVal _title As String, ByVal _year As String, ByVal _rating As String, ByVal Optional _overview As String = Nothing, Optional ByVal _folder As String = "")
        Title = _title
        Year = _year
        Rating = _rating
        Overview = _overview
        Folder = _folder
    End Sub

    Public Sub New()
    End Sub

    Protected Sub OnPropertyChanged(ByVal e As PropertyChangedEventArgs)
        RaiseEvent PropertyChanged(Me, e)
    End Sub
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
End Class
