Imports System.Drawing
Imports Google
Imports Google.Apis.Customsearch.v1
Imports Google.Apis.Services
Imports Xceed.Wpf.Toolkit

Public Class ProSearchResults
    Dim _titleToSearch As String
    Dim _i As Integer = 0
    ReadOnly _imageSize As Windows.Size = New Windows.Size(128, 128)
    ReadOnly _pics As New ArrayList
    'Dim _myResult As GoogleResult
    ReadOnly _msgStyle = New Style()

    ReadOnly _service = New CustomsearchService(New BaseClientService.Initializer With {
                                                   .ApiKey = APIkeyGoogle,
                                                   .ApplicationName = "FoliCon"
                                                   })

    Private ReadOnly _listRequest As CseResource.ListRequest = _service.Cse.List()

    Private Sub CreatePicBox(link As String, img As Image)


        Dim pic As New Controls.Image()
        pic.Height = _imageSize.Height
        pic.Width = _imageSize.Width
        pic.Source = LoadBitmap(img)
        _pics.Add(pic)
        pic.Tag = link
        AddHandler pic.MouseLeftButtonDown, AddressOf pic_DoubleClick
        WrapPanel1.Children.Add(pic)
    End Sub


    Private Sub pic_DoubleClick(sender As Object, e As MouseButtonEventArgs)
        If e.ClickCount = 2 Then
            Dim image1 As New ImageToDownload()
            With image1
                .LocalPath = SelectedFolderPath & "\" & Fnames(_i) & "\" & Fnames(_i) & ".png"
                .RemotePath = CType(sender, Controls.Image).Tag.ToString()
            End With
            AddToPickedListDataTable("", _titleToSearch, "", SelectedFolderPath & "\" & Fnames(_i), Fnames(_i))
            ImgDownloadList.Add(image1)
            FolderProcessedCount += 1
            _i += 1
            If Not _i >= Fnames.Length - 1 Then
                StartSearch()
            Else
                Close()
            End If
        End If
    End Sub

    Private Async Sub StartSearch()
        WrapPanel1.Children.Clear()
        IsEnabled = False
        _titleToSearch = Nothing
        If Not RetryMovieTitle = Nothing Then
            _titleToSearch = RetryMovieTitle
        Else
            Dim titleCleaner As New TitleCleaner
            _titleToSearch = titleCleaner.Clean(Fnames(_i))
        End If
        BusyIndicator1.BusyContent = "Searching for " & _titleToSearch & "..."
        BusyIndicator1.IsBusy = True
        Title = "Pick Icon for " & _titleToSearch

        Await GoogleSearchAsync(_titleToSearch)


        RetryMovieTitle = Nothing
        BusyIndicator1.IsBusy = False
        IsEnabled = True
    End Sub

    Private Async Function GoogleSearchAsync(query As String) As Task
        Dim searchResult
search: _listRequest.Q = query & " Folder Icon"
        Try
            searchResult = Await _listRequest.ExecuteAsync()
        Catch ex As GoogleApiException
            Dim msgResult
            If ex.HttpStatusCode = 429 Then
                msgResult = MessageBox.Show("Too many requests OR Daily Quota limit Exhausted!", "Error",
                                            MessageBoxButton.YesNoCancel, MessageBoxImage.Error, MessageBoxResult.Cancel,
                                            _msgStyle)
            Else
                msgResult =
                    MessageBox.Show(
                        "Error Code: " & ex.HttpStatusCode & vbCrLf & "Error: " & ex.Error.Message.ToString(), "Error",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Error, MessageBoxResult.Cancel, _msgStyle)
            End If
            Select Case msgResult
                Case MessageBoxResult.Yes : GoTo search
                Case MessageBoxResult.No : btnSkip_Click(Nothing, Nothing)
                    Exit Function
                Case MessageBoxResult.Cancel : Close()
                    Exit Function
            End Select
        End Try
        If searchResult.SearchInformation.TotalResults > 0 Then
            For Each item In searchResult.Items
                Dim bm = Await GetBitmapFromUrlAsync(item.Image.ThumbnailLink)
                CreatePicBox(item.Link, bm)
                bm.Dispose()
            Next
        Else
            MessageBox.Show("No result Found, Try to search again with correct title", "No Result", MessageBoxButton.OK,
                            MessageBoxImage.Warning)
            txtSearchAgain.Focus()
        End If
    End Function

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        _msgStyle.Setters.Add(New Setter(MessageBox.YesButtonContentProperty, "Retry"))
        _msgStyle.Setters.Add(New Setter(MessageBox.NoButtonContentProperty, "Skip"))
        _msgStyle.Setters.Add(New Setter(MessageBox.CancelButtonContentProperty, "Stop Searching"))
        _listRequest.Cx = "004393948537616506289:-yahvfs2ys0"
        _listRequest.FileType = "png"
        _listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image
        _listRequest.Fields = "searchInformation/totalResults,items(link,image/thumbnailLink)"
        StartSearch()
    End Sub

    Private Sub btnSkip_Click(sender As Object, e As RoutedEventArgs) Handles btnSkip.Click
        _i += 1
        If Not _i >= Fnames.Length - 1 Then
            StartSearch()
        Else
            Close()
        End If
    End Sub

    Private Sub btnSearchAgain_Click(sender As Object, e As RoutedEventArgs) Handles btnSearchAgain.Click
        RetryMovieTitle = txtSearchAgain.Text
        StartSearch()
    End Sub
End Class
