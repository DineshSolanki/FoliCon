Imports System.ComponentModel

Public Class ViewModelBase
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
#Disable Warning CA1030 ' Use events where appropriate
    Public Sub RaisePropertyChanged(ByVal propertyName As String)
#Enable Warning CA1030 ' Use events where appropriate
        PropertyChangedEvent?.Invoke(Me, New PropertyChangedEventArgs(propertyName))
    End Sub


End Class
