Option Strict Off

Imports FoliconNative.Modules
Imports System.Collections.ObjectModel
Imports System.Net.TMDb
Imports System.Windows.Controls.Primitives
Imports IGDB.Models

Public Class SearchResult
    Dim ReadOnly _serviceClient as New ServiceClient(ApikeyTmdb)
    dim _igdbClient = IGDB.Client.Create(ApikeyIgdb)
    Private _listItem As ListItem

    Public Property ListItem() As ListItem
        Get
            Return _listItem
        End Get
        Set(ByVal value As ListItem)
            If _listItem IsNot value Then
                _listItem = value
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
        If Searchresultob IsNot Nothing AndAlso if(searchMod="Game",Searchresultob.Length ,Searchresultob.TotalCount) IsNot Nothing Then
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

        If SearchMod = "Game"
            Await Igdbf.SearchGame(titleToSearch, _igdbClient)
        Else
            Await SearchIt(titleToSearch, _serviceClient)
        End If
        RetryMovieTitle = Nothing
        If useBusy Then
            BusyIndicator1.IsBusy = False
        End If
        Window_Loaded(Nothing, Nothing)
    End Sub


    Public Sub FetchAndAddDetailsToListView(listViewName As ListView, result As Object, query As String)
        Contracts.Contract.Requires(listViewName IsNot Nothing)
        Contracts.Contract.Requires(result IsNot Nothing)
        Dim source As new ObservableCollection(Of ListItem)

        If SearchMod = "TV"
            Dim ob = DirectCast(result, Shows)
            source = ExtractTvDetailsIntoListItem(ob)
        ElseIf SearchMod = "Movie" Then

            If query.ToLower.Contains("collection") Then
                Dim ob = DirectCast(result, collections)
                source = ExtractCollectionDetailsIntoListItem(ob)
            Else
                Dim ob
                Try
                    ob = DirectCast(result, Movies)
                    source = ExtractMoviesDetailsIntoListItem(ob)
                Catch ex As Exception
                    ob = DirectCast(result, Collections)
                    source = ExtractCollectionDetailsIntoListItem(ob)
                End Try


            End If
        ElseIf SearchMod = "Auto (Movies & TV Shows)"
            Dim ob = DirectCast(result, Resources)
            source = ExtractResourceDetailsIntoListItem(ob)
        ElseIf SearchMod = "Game"
            Dim ob = DirectCast(result, Game())
            source = Igdbf.ExtractGameDetailsIntoListItem(ob)
        End If

        listViewName.ItemsSource = source
        SetColumnWidth(listViewName)
    End Sub

    Private Sub Skipbtn_Click(sender As Object, e As RoutedEventArgs) Handles Skipbtn.Click
        Close()
    End Sub

    Private Sub SearchAgainbtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchAgainbtn.Click
        If Not String.IsNullOrWhiteSpace(SearchTxt.Text) Then
            SearchTitle = SearchTxt.Text
            StartSearch(True)
        End If
    End Sub

    Private Sub Pickbtn_Click(sender As Object, e As RoutedEventArgs) Handles Pickbtn.Click
        If ListView1.SelectedItems.Count > 0 Then
            PickedIndex = ListView1.SelectedIndex
            Try
                If SearchMod = "Game"
                    Igdbf.ResultPicked(Searchresultob(PickedIndex))
                End If
                ResultPicked(Searchresultob, SearchMod, PickedIndex)
            Catch ex As Exception
                If ex.Message = "NoPoster"
                    MessageBox.Show("No poster found")
                    Skipbtn_Click(Nothing, nothing)
                    Exit Sub
                End If

            End Try

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

    Private Sub ListView1_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles ListView1.MouseDoubleClick
        Pickbtn_Click(Nothing, Nothing)
    End Sub
End Class
