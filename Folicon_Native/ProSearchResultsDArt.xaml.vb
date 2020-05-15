Imports System.Drawing
Imports Xceed.Wpf.Toolkit
Imports FoliconNative.DArt

Public Class ProSearchResultsDArt
    Dim _titleToSearch As String
    Dim _i As Integer = 0
    ReadOnly _imageSize As Windows.Size = New Windows.Size(128, 128)
    ReadOnly _pics As New ArrayList
    ReadOnly _msgStyle = New Style()
    Dim _accessToken as string

    Private Sub CreatePicBox(link As String, img As Bitmap)
        Dim pic As New Controls.Image()
        pic.Height = _imageSize.Height
        pic.Width = _imageSize.Width
        'pic.Source=img
        pic.Margin=New Thickness(5,5,5,5)
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
            If Not _i > Fnames.Length - 1 Then
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
        Await DArtSearchAsync(_titleToSearch)
        RetryMovieTitle = Nothing
        BusyIndicator1.IsBusy = False
        IsEnabled = True
    End Sub
    Private Async Function DArtSearchAsync(query As String,Optional offset As Integer=0) As Task
        Dim searchResult As DArtBrowseResult
         _accessToken=await GetClientAccessTokenAsync()
        searchResult =await Browse(_accessToken,query,offset)

        If searchResult.Results.Length>0
            For Each item In searchResult.Results
                Dim bm = Await GetBitmapFromUrlAsync(item.Thumbs(0).Src)
                CreatePicBox(item.Content.Src, bm)
                bm.Dispose()
            Next
            If searchResult.HasMore AndAlso searchResult.NextOffset<=30
                Await DArtSearchAsync(query,searchResult.NextOffset)
            End If
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
        StartSearch()
    End Sub

    Private Sub btnSkip_Click(sender As Object, e As RoutedEventArgs) Handles btnSkip.Click
        _i += 1
        If Not _i > Fnames.Length - 1 Then
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
