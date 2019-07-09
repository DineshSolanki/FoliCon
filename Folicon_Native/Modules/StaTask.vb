
Imports System.Threading
Module StaTask




    Public Function Start(Of T)(ByVal func As Func(Of T)) As Task(Of T)
        Dim tcs = New TaskCompletionSource(Of T)()
        Dim thread As Thread = New Thread(Sub()
                                              Try
                                                  tcs.SetResult(func())
                                              Catch e As Exception
                                                  tcs.SetException(e)
                                              End Try
                                          End Sub)
        thread.SetApartmentState(ApartmentState.STA)
        thread.Start()
        Return tcs.Task
    End Function
End Module
