Imports System.ComponentModel

Public Class ProgressUpdater
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Private _pVal As Integer, _pMax As Integer, _pText As String

    Public Property Text As String
        Get
            Return _pText
        End Get
        Set(value As String)
            _pText = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Text)))
        End Set
    End Property
    Public Property Value As Integer
        Get
            Return _pVal
        End Get
        Set(newValue As Integer)
            _pVal = newValue

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Value)))
        End Set
    End Property

    Public Property Maximum As Integer
        Get
            Return _pMax
        End Get
        Set(newValue As Integer)
            _pMax = newValue

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Maximum)))
        End Set
    End Property
End Class