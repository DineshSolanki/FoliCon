Imports System.Net
Imports System.Net.Http
Imports Newtonsoft.Json

Public Class ProSearchResults
    Dim i As Integer = 0
    ReadOnly imageSize As Size = New Size(128, 128)
    Private Const StartHeight As Integer = 0
    Dim pics As New ArrayList
    Dim myresult As GoogleResult
    Private Sub CreatePicBox(index As Integer, img As System.Drawing.Image)
        Dim pic As New System.Windows.Controls.Image()
        'pic.BorderStyle = BorderStyle.None
        pic.Height = imageSize.Height
        pic.Width = imageSize.Width
        'pic.Location = New Point(imageSize.Width * (index Mod 10), (index \ 10) * imageSize.Height + StartHeight)

        pic.Source = loadBitmap(img)
        pics.Add(pic)
        pic.Tag = myresult.Items(index).Link
        AddHandler pic.MouseLeftButtonDown, AddressOf pic_DoubleClick
        '' Adjust for scrolled location
        'If AutoScrollPosition.Y <> 0 Then
        '    pic.Location = New Point(pic.Location.X, pic.Location.Y + AutoScrollPosition.Y)
        'End If
        WrapPanel1.Children.Add(pic)
    End Sub


    Private Async Sub pic_DoubleClick(ByVal sender As Object, e As MouseButtonEventArgs)
        If e.ClickCount = 2 Then
            Dim image1 As New ImageToDownload()
            With image1
                .LocalPath = SelectedFolderPath & "\" & Fnames(i) & "\" & Fnames(i) & ".png"
                .RemotePath = CType(sender, Controls.Image).Tag.ToString()
            End With
            AddToPickedListDataTable("", SearchTitle, "", SelectedFolderPath & "\" & Fnames(i), Fnames(i))
            ImgDownloadList.Add(image1)
            FolderProcessedCount += 1
            i += 1
            If Not i >= Fnames.Length - 1 Then
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
            titleToSearch = titleCleaner.Clean(Fnames(i))
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
        'Try
        Dim start As Integer = 1
            Dim http = New HttpClient()
            Dim SearchEngineID As String = "004393948537616506289:-yahvfs2ys0"
        Dim searchQuery = query & " Folder Icon"
        Dim response As New HttpResponseMessage
        http.BaseAddress = New Uri("https://www.googleapis.com/customsearch/")
        'http.BaseAddress = New Uri("https://www.googleapis.com/customsearch/v1/siterestrict?")
search: Using response
            Try
                response = Await http.GetAsync("v1?key=" & APIkeyGoogle & "&fields=searchInformation/totalResults,items(link,image/thumbnailLink)" & "&cx=" & SearchEngineID & "&q=" & searchQuery & "&start=" & start & "&searchType=image&fileType=png&img&dimension=512alt=json")
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                    Return
                End Try

            If (response.StatusCode = HttpStatusCode.OK) Then
                Dim jsonData = Await response.Content.ReadAsStringAsync()
                myresult = JsonConvert.DeserializeObject(Of GoogleResult)(jsonData)
            Else
                Dim msg = New Xceed.Wpf.Toolkit.MessageBox() With {
                        .YesButtonContent = "Retry",
                        .NoButtonContent = "Skip",
                        .CancelButtonContent = "Stop Searching"
                        }
                Dim msgResult = msg.Show(response.StatusCode & vbCrLf & response.ReasonPhrase, "Message", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning)
                'MessageBox.Show(response.StatusCode & vbCrLf & response.ReasonPhrase, "Message")
                Select Case msgResult
                    Case MessageBoxResult.Yes : GoTo search
                    Case MessageBoxResult.No : btnSkip_Click(Nothing, Nothing) : Exit Function
                    Case MessageBoxResult.Cancel : Close()
                End Select

            End If

            End Using
            '            If CInt(myresult.SearchInformation.TotalResults) >= 30 Then
            '                maxpage = 4
            '            Else
            '                maxpage = CInt(myresult.SearchInformation.TotalResults)
            '            End If
            If CInt(myresult.SearchInformation.TotalResults) > 0 Then
            For i As Integer = 0 To myresult.Items.Count() - 1
                If i = 10 Then
                    Exit For
                End If
                Dim bm = GetBitmapFromURL(myresult.Items(i).Image.ThumbnailLink)
                CreatePicBox(i, bm)
            Next
        End If
        'Catch ex As Exception
        '    MessageBox.Show(ex.Message & vbCrLf & ex.StackTrace)
        'End Try

    End Function
    Function updatePageNo(ByVal value As Decimal) As Decimal
        Static pageno As Decimal = 0
        pageno += value
        Return pageno
    End Function

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        'Xceed.Wpf.Toolkit.MessageBox.Show("")


        Await StartSearch()
    End Sub

    Private Async Sub btnSkip_Click(sender As Object, e As RoutedEventArgs) Handles btnSkip.Click
        i += 1
        If Not i >= Fnames.Length - 1 Then
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
