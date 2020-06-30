Namespace ViewModel
    Public Class DelegateCommand
        Implements ICommand

        Private ReadOnly _execute As Action(Of Object)
        Private ReadOnly _canExecute As Predicate(Of Object)

        Public Sub New(ByVal execute As Action(Of Object), Optional ByVal canExecute As Predicate(Of Object) = Nothing)
            If execute = Nothing Then
                Throw New ArgumentNullException(NameOf(execute))
            Else
                _execute = execute
            End If
            _canExecute = canExecute
        End Sub

        Public Function CanExecute(ByVal parameter As Object) As Boolean Implements ICommand.CanExecute
            Return If(_canExecute?.Invoke(parameter), True)
        End Function
        Public Sub Execute(ByVal parameter As Object) Implements ICommand.Execute
            _execute(parameter)
        End Sub
        Public Sub RaiseCanExecuteChanged()
            CanExecuteChangedEvent?.Invoke(Me, EventArgs.Empty)
        End Sub

        Public Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
    End Class

End Namespace

