Imports System.Configuration
Imports System.Net.Http
Imports Newtonsoft.Json
Imports Xceed.Wpf.Toolkit
Imports Folicon_Native.DArt

Class testPage
    Private Async Sub Button_Click(sender As Object, e As RoutedEventArgs)
        
      Dim accessToken=Await GetClientAccessTokenAsync()
      
        

    End Sub
End Class
