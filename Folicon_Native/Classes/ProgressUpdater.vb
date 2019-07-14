Imports System.ComponentModel

Public Class ProgressUpdater
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Private pVal As Integer, pMax As Integer, pText As String

    Public Property Text As String
        Get
            Return pText
        End Get
        Set(value As String)
            pText = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Text)))
        End Set
    End Property
    Public Property Value As Integer
        Get
            Return pVal
        End Get
        Set(newvalue As Integer)
            pVal = newvalue

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Value)))
        End Set
    End Property

    Public Property Maximum As Integer
        Get
            Return pMax
        End Get
        Set(newvalue As Integer)
            pMax = newvalue

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Maximum)))
        End Set
    End Property
End Class