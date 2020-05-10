Imports Google.Apis.Customsearch.v1

Class testPage
    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        Dim service = New CustomsearchService(New CustomsearchService.Initializer With {
                                                 .ApiKey = APIkeyGoogle,
                                                 .ApplicationName = "FoliCon"
                                                 })
        Dim lst = service.Cse.List()
        lst.Cx = "004393948537616506289:-yahvfs2ys0"
        lst.FileType = "png"
        lst.SearchType = CseResource.ListRequest.SearchTypeEnum.Image
        lst.Q = "Forrest Gump folder icon"
        lst.Fields = "searchInformation/totalResults,items(link,image/thumbnailLink)"
        Dim sc = lst.Execute()

        service.Dispose()
        'MessageBox.Show()
    End Sub
End Class
