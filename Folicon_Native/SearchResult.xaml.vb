Option Strict Off
Imports System.Collections.ObjectModel
Imports System.Net.Http
Imports Newtonsoft.Json.Linq
Imports Xceed.Wpf.Toolkit

Public Class SearchResult
    Dim i As Integer = 0
    Private _ListItem As ListItem
    Public Property ListItem() As ListItem
        Get
            Return _ListItem
        End Get
        Set(ByVal value As ListItem)
            If _ListItem IsNot value Then
                _ListItem = value
            End If
        End Set
    End Property
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        ListItem = New ListItem()
        ' Add any initialization after the InitializeComponent() call.

    End Sub




    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        ImgDownloadList.Clear()
        InitPickedListDataTable()
        StartSearch(True)
    End Sub
    Public Async Sub StartSearch(ByVal useBusy As Boolean)
        If useBusy Then
            BusyIndicator1.IsBusy = True
        End If

        Dim titleToSearch As String
        If Not RetryMovieTitle = Nothing Then
            titleToSearch = RetryMovieTitle
        Else
            titleToSearch = Fnames(i)
        End If
        BusyIndicator1.BusyContent = "Searching for " & titleToSearch & "..."
        OverviewText.Text = ""
        'Items.Clear()

        Await PerformAcctualSearch(titleToSearch)
        If ListView1.Items.Count > 0 Then
            ListView1.SelectedIndex = 0
            ListView1.Focus()
        End If

        'GoogleURl = Await GoogleIt(titleToSearch)
        RetryMovieTitle = Nothing
        If useBusy Then
            BusyIndicator1.IsBusy = False
        End If

    End Sub
    Private Async Function PerformAcctualSearch(title As String) As Task
        Dim cleantitle = New TitleCleaner().Clean(title)
        Dim http = New HttpClient()


        If SearchMod = "Game" Then
            http.BaseAddress = New Uri("http://www.giantbomb.com/api/")
        Else
            http.BaseAddress = New Uri("http://api.themoviedb.org/3/")
        End If
        Dim URL As String = Nothing
        If SearchMod = "Movie" Then
            If title.ToLower.Contains("collection") Then
                URL = "search/collection?api_key=" & ApikeyTMDB & "&language=en-US&query=" & cleantitle & "&page=1"
            Else
                URL = "search/movie?api_key=" & ApikeyTMDB & "&language=en-US&query=" & cleantitle
            End If
        ElseIf SearchMod = "TV" Then
            URL = "search/tv?api_key=" & ApikeyTMDB & "&language=en-US&query=" & cleantitle
        ElseIf SearchMod = "Auto" Then
            URL = "search/multi?api_key=" & ApikeyTMDB & "&language=en-US&query=" & cleantitle
        ElseIf SearchMod = "Game" Then
            URL = "search?api_key=" & Apikeygb & "&format=" & Responseformatgb & "&query=" & cleantitle & "&field_list=" & Fieldlistgb
        End If

        Using Response
            Try
                Response = Await http.GetAsync(URL)
            Catch ex As Exception
                'If SplashScreenManager1.IsSplashFormVisible Then
                '    SplashScreenManager1.CloseWaitForm()
                'End If
                MessageBox.Show(ex.Message)
                Exit Function
            End Try

            Dim jsonData = Await Response.Content.ReadAsStringAsync()
            Searchresultob = JValue.Parse(jsonData)
        End Using
        Dim result As String
        If SearchMod = "Game" Then
            result = Searchresultob.item("number_of_total_results")
        Else
            result = Searchresultob.item("total_results")
        End If
        If result = 0 Then
            'If SplashScreenManager1.IsSplashFormVisible Then
            '    SplashScreenManager1.CloseWaitForm()
            'End If
            MessageBox.Show("Nothing found for " & cleantitle & vbCrLf & "Try Searching with Other Title " & vbCrLf & "OR Check search Mode")
            ' Skipbtn.PerformClick()
            'Skipbtn_Click(Nothing, Nothing)

        Else
            FetchAndAddDetailsToListView(ListView1, Searchresultob, title)
        End If
        MovieTitle.Content = cleantitle
    End Function

    Public Sub FetchAndAddDetailsToListView(listviewName As ListView, jsonResult As Object, Title As String)
        Dim Items As New ObservableCollection(Of ListItem)()
        For Each m In jsonResult.item("results")
            Dim arr As String() = New String(3) {}
            Dim originalTitle As String = m.item("original_title")
            Dim releaseDate As String = m.item("release_date")
            Dim voteAverage As String = m.item("vote_average")
            Dim name As String = m.item("name")
            Dim originalName As String = m.item("original_name")
            Dim firstAirDate As String = m.item("first_air_date")
            Dim overview As String = m.item("overview")
            Select Case SearchMod
                Case "Movie"
                    If Title.ToLower.Contains("collection") Then
                        arr(0) = name
                    Else
                        arr(0) = originalTitle
                        If Not String.IsNullOrEmpty(releaseDate) Then
                            Dim dates As DateTime = CDate(releaseDate)
                            arr(1) = dates.Year.ToString()
                        End If
                        arr(2) = voteAverage
                    End If
                Case "TV"
                    arr(0) = name
                    If Not String.IsNullOrEmpty(firstAirDate) Then
                        Dim dates As DateTime = CDate(firstAirDate)
                        arr(1) = dates.Year.ToString()
                    End If
                    arr(2) = voteAverage
                Case "All"
                    If Not name = Nothing Then
                        arr(0) = name
                    ElseIf Not originalName = Nothing Then
                        arr(0) = originalName
                    ElseIf Not m.item("original_title") = Nothing Then
                        arr(0) = m.item("original_title")
                    End If
                    If Not String.IsNullOrEmpty(firstAirDate) Then
                        Dim dates As DateTime = CDate(firstAirDate)
                        arr(1) = dates.Year.ToString()
                    ElseIf Not String.IsNullOrEmpty(releaseDate) Then
                        Dim dates As DateTime = CDate(releaseDate)
                        arr(1) = dates.Year.ToString()
                    End If
                    arr(2) = voteAverage
            End Select
            Items.Add(New ListItem(arr(0), arr(1), arr(2), overview))

        Next
        listviewName.ItemsSource = Items
        SetColumnWidth(listviewName)


    End Sub

    Private Sub Skipbtn_Click(sender As Object, e As RoutedEventArgs) Handles Skipbtn.Click
        i += 1
        If Not i > Fnames.Length - 1 Then
            StartSearch(True)
        Else
            Close()
        End If
    End Sub

    Private Sub SearchAgainbtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchAgainbtn.Click
        RetryMovieTitle = SearchTxt.Text

        StartSearch(True)
    End Sub

    Private Sub Pickbtn_Click(sender As Object, e As RoutedEventArgs) Handles Pickbtn.Click

        If ListView1.SelectedItems.Count > 0 Then
            PickedMovieIndex = ListView1.SelectedIndex
            If Searchresultob.item("results")(PickedMovieIndex).item("poster_path").ToString IsNot "null" Then

                If Not Fnames(i).ToLower.Contains("collection") Then
                    Dim releaseDate As DateTime = CDate(Searchresultob.item("results")(PickedMovieIndex).item(DateProperty).ToString)
                    AddToPickedListDataTable(SelectedFolderPath & "\" & Fnames(i) & "\" & Fnames(i) & ".png", Searchresultob.item("results")(PickedMovieIndex).item(INameProperty), Searchresultob.item("results")(PickedMovieIndex).item("vote_average"), SelectedFolderPath & "\" & Fnames(i), Fnames(i), releaseDate.Year.ToString)
                Else
                    AddToPickedListDataTable(SelectedFolderPath & "\" & Fnames(i) & "\" & Fnames(i) & ".png", Searchresultob.item("results")(PickedMovieIndex).item(INameProperty), Searchresultob.item("results")(PickedMovieIndex).item("vote_average"), SelectedFolderPath & "\" & Fnames(i), Fnames(i))
                End If
                FolderProcessedCount += 1

                Dim image1 As New ImageToDownload()
                With image1
                    .LocalPath = SelectedFolderPath & "\" & Fnames(i) & "\" & Fnames(i) & ".png"
                    If IconMode = "Poster" Then
                        .RemotePath = "https://image.tmdb.org/t/p/w500/" & Searchresultob.item("results")(PickedMovieIndex).item("poster_path")
                    Else
                        .RemotePath = GoogleURl
                    End If
                    '.RemotePath = Searchresult.item("results")(PickedMovieIndex).item("poster_path")

                End With
                ImgDownloadList.Add(image1)
                'DownloadImage(Searchresult.item("results")(PickedMovieIndex).item("poster_path"), SelectedFolderPath & "\" & Fnames(i) & "\" & Fnames(i) & ".jpg", CancellationToken.None)
            Else
                MessageBox.Show("sorry, No Poster Found, Please try in Professional Mode")
            End If
            i += 1
            If Not i > Fnames.Length - 1 Then
                StartSearch(True)
            Else
                Close()
            End If
        End If
    End Sub

    Private Sub ListView1_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ListView1.SelectionChanged
        'If ListView1.SelectedItems.Count > 0 Then
        '    If Not ListView1.SelectedItems(0).Tag = Nothing Then
        '        OverviewText.Text = ListView1.SelectedItems(0).Tag.ToString()
        '    End If
        'End If
    End Sub

    Private Sub Slider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        OverviewText.FontSize = FontSlider.Value
    End Sub

    Private Sub SearchTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles SearchTxt.TextChanged
        RetryMovieTitle = SearchTxt.Text
        StartSearch(False)
    End Sub

    Private Sub ListView1_KeyDown(sender As Object, e As KeyEventArgs) Handles ListView1.KeyDown
        e.Handled = True
        If e.Key = Key.Enter Then
            Pickbtn_Click(sender, Nothing)
        Else
            If e.Key = Key.Escape Then
                Skipbtn_Click(sender, Nothing)
            End If
        End If
    End Sub

    Private Sub Window_KeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Escape Then
            Skipbtn_Click(sender, Nothing)
        End If
    End Sub
End Class
