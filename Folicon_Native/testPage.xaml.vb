
Imports System.Threading
Imports FoliconNative.Modules
Imports IGDB
Imports IGDB.Models

Class TestPage
    Private Async Sub Button_Click(sender As Object, e As RoutedEventArgs)
        
        Dim client =IGDB.Client.Create(ApikeyIgdb)
        Dim query="Desert storm"
        

       'Dim r= Await client.QueryAsync (Of Game)(IGDB.Client.Endpoints.Games,"search ""Halo"";fields name,total_rating,summary,cover.*;")
       Dim r= Await client.QueryAsync(of game)(IGDB.Client.Endpoints.games,"search " & """"& query &"""" & "; fields *;")
       ' client.QueryAsync(IGDB.Client.Endpoints.Covers)
        For Each o  In r
            If o.Cover.Value IsNot nothing
                Dim posterUrl= IGDB.ImageHelper.GetImageUrl(o.Cover.Value.ImageId,ImageSize.HD720)
                MessageBox.Show(posterUrl.Substring(2))
            End If
        Next
        

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        BusyIndicator1.IsBusy = True
    End Sub
End Class
