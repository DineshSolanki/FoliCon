Imports System.Net
Imports System.Net.Http
Imports Newtonsoft.Json
Imports Xceed.Wpf.Toolkit

Public Class ProSearchResults
    Dim _i As Integer = 0
    ReadOnly _imageSize As Size = New Size(128, 128)
    Dim ReadOnly _pics As New ArrayList
    Dim _myResult As GoogleResult
    ReadOnly _msgStyle = New Style()

    Private Sub CreatePicBox(index As Integer, img As System.Drawing.Image)
        Dim pic As New Controls.Image()
        pic.Height = _imageSize.Height
        pic.Width = _imageSize.Width
        pic.Source = LoadBitmap(img)
        _pics.Add(pic)
        pic.Tag = _myResult.Items(index).Link
        AddHandler pic.MouseLeftButtonDown, AddressOf pic_DoubleClick
        WrapPanel1.Children.Add(pic)
    End Sub


    Private Async Sub pic_DoubleClick(sender As Object, e As MouseButtonEventArgs)
        If e.ClickCount = 2 Then
            Dim image1 As New ImageToDownload()
            With image1
                .LocalPath = SelectedFolderPath & "\" & Fnames(_i) & "\" & Fnames(_i) & ".png"
                .RemotePath = CType(sender, Controls.Image).Tag.ToString()
            End With
            AddToPickedListDataTable("", SearchTitle, "", SelectedFolderPath & "\" & Fnames(_i), Fnames(_i))
            ImgDownloadList.Add(image1)
            FolderProcessedCount += 1
            _i += 1
            If Not _i >= Fnames.Length - 1 Then
                Await StartSearch()
            Else
                Close()
            End If
        End If
    End Sub

    Private Async Function StartSearch() As Task
        WrapPanel1.Children.Clear()
        'SplashScreenManager1.ShowWaitForm()
        IsEnabled = False
        Dim titleToSearch As String
        If Not RetryMovieTitle = Nothing Then
            titleToSearch = RetryMovieTitle
        Else
            Dim titleCleaner As New TitleCleaner
            titleToSearch = titleCleaner.Clean(Fnames(_i))
        End If
        'SplashScreenManager1.SetWaitFormDescription("Searching for " & titleToSearch & "...")
        Title = "Pick Icon for " & titleToSearch
        Await GoogleSearch(titleToSearch)
        RetryMovieTitle = Nothing
        'If SplashScreenManager1.IsSplashFormVisible Then
        '    SplashScreenManager1.CloseWaitForm()
        'End If
        IsEnabled = True
    End Function

    Private Async Function GoogleSearch(query As String) As Task
        Dim maxpage = 1
        Dim start = 1
        Dim http = New HttpClient()
        Dim searchEngineId = "004393948537616506289:-yahvfs2ys0"
        Dim searchQuery = query & " Folder Icon"
        Dim response As New HttpResponseMessage
        http.BaseAddress = New Uri("https://www.googleapis.com/customsearch/")
        'http.BaseAddress = New Uri("https://www.googleapis.com/customsearch/v1/siterestrict?")
search: Using response
            Try
                response =
                    Await _
                        http.GetAsync(
                            "v1?key=" & APIkeyGoogle &
                            "&fields=searchInformation/totalResults,items(link,image/thumbnailLink)" & "&cx=" &
                            searchEngineId & "&q=" & searchQuery & "&start=" & start &
                            "&searchType=image&fileType=png&img&dimension=512alt=json")
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                Return
            End Try

            If (response.StatusCode = HttpStatusCode.OK) Then
                Dim jsonData = Await response.Content.ReadAsStringAsync()
                _myResult = JsonConvert.DeserializeObject(Of GoogleResult)(jsonData)
            Else
                Dim msgResult =
                        MessageBox.Show("HTTP Response Code: " & response.StatusCode & vbCrLf & response.ReasonPhrase,
                                        "Message", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning,
                                        MessageBoxResult.No, _msgStyle)
                Select Case msgResult
                    Case MessageBoxResult.Yes : GoTo search
                    Case MessageBoxResult.No : btnSkip_Click(Nothing, Nothing)
                        Exit Function
                    Case MessageBoxResult.Cancel : Close()
                        Exit Function
                End Select

            End If

        End Using
        If CInt(_myResult.SearchInformation.TotalResults) > 0 Then
            For i = 0 To _myResult.Items.Count() - 1
                If i = 10 Then
                    Exit For
                End If
                Dim bm = GetBitmapFromUrl(_myResult.Items(i).Image.ThumbnailLink)
                CreatePicBox(i, bm)
            Next
        End If
    End Function

    Function UpdatePageNo(value As Decimal) As Decimal
        Static pageNo As Decimal = 0
        pageNo += value
        Return pageNo
    End Function

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        _msgStyle.Setters.Add(New Setter(MessageBox.YesButtonContentProperty, "Retry"))
        _msgStyle.Setters.Add(New Setter(MessageBox.NoButtonContentProperty, "Skip"))
        _msgStyle.Setters.Add(New Setter(MessageBox.CancelButtonContentProperty, "Stop Searching"))
        Await StartSearch()
    End Sub

    Private Async Sub btnSkip_Click(sender As Object, e As RoutedEventArgs) Handles btnSkip.Click
        _i += 1
        If Not _i >= Fnames.Length - 1 Then
            Await StartSearch()
        Else
            Close()
        End If
    End Sub

    Private Async Sub btnSearchAgain_Click(sender As Object, e As RoutedEventArgs) Handles btnSearchAgain.Click
        RetryMovieTitle = txtSearchAgain.Text
        Await StartSearch()
    End Sub
End Class
