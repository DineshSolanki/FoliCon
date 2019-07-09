Imports System.Collections.ObjectModel

Public Class ItemList
    Inherits ObservableCollection(Of ListItem)
    Public Sub New()
        MyBase.New()
    End Sub

End Class
