Imports System.Net.TMDb
Imports System.Threading

Class testPage
    Private Async Sub Button_Click(sender As Object, e As RoutedEventArgs)
        Dim st=New System.Net.TMDb.ServiceClient(ApikeyTmdb)

        Dim result=Await  st.SearchAsync("The Vampire Diaries", "en-US",True,1,CancellationToken.none)
        'Dim sc=new System.Net.TMDb.StorageClient()
        'sc.DownloadAsync()
        'Await DownloadImage(result.Results(0).Poster,"E:\Movies\Avengers - Endgame\ag.png",CancellationToken.None)
        MessageBox.Show("")


    End Sub
End Class
