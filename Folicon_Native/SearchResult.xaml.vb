Option Strict Off
Imports System.Collections.ObjectModel

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
        MovieTitle.Content = SearchTitle
        If Searchresultob IsNot Nothing OrElse Searchresultob.Item("total_results") IsNot Nothing OrElse Searchresultob.Item("number_of_total_results") IsNot Nothing Then
            FetchAndAddDetailsToListView(ListView1, Searchresultob, SearchTitle)
        Else
            SearchTxt.Focus()
        End If
    End Sub
    Public Async Sub StartSearch(ByVal useBusy As Boolean)
        If useBusy Then
            BusyIndicator1.IsBusy = True
        End If

        Dim titleToSearch As String
        If Not RetryMovieTitle = Nothing Then
            titleToSearch = RetryMovieTitle
        Else
            titleToSearch = SearchTitle
        End If
        BusyIndicator1.BusyContent = "Searching for " & titleToSearch & "..."
        OverviewText.Text = ""
        'Items.Clear()

        Await PerformAcctualSearch(titleToSearch)
        'GoogleURl = Await GoogleIt(titleToSearch)
        RetryMovieTitle = Nothing
        If useBusy Then
            BusyIndicator1.IsBusy = False
        End If
        Window_Loaded(Nothing, Nothing)
    End Sub


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
        Close()
    End Sub

    Private Sub SearchAgainbtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchAgainbtn.Click
        SearchTitle = SearchTxt.Text
        StartSearch(True)
        Window_Loaded(Nothing, Nothing)
    End Sub

    Private Sub Pickbtn_Click(sender As Object, e As RoutedEventArgs) Handles Pickbtn.Click
        If ListView1.SelectedItems.Count > 0 Then
            PickedMovieIndex = ListView1.SelectedIndex
            ResultPicked(PickedMovieIndex)
            DialogResult = True
            Close()
        End If
    End Sub

    Private Sub Slider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        OverviewText.FontSize = FontSlider.Value
    End Sub

    Private Sub SearchTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles SearchTxt.TextChanged
        RetryMovieTitle = SearchTxt.Text
        StartSearch(False)
    End Sub

End Class
