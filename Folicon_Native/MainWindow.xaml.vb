Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports Xceed.Wpf.Toolkit
Imports FoliconNative.Modules

Class MainWindow
    Private ReadOnly progressUpdater1 As New ProgressUpdater
    ReadOnly _serviceClient As New Net.TMDb.ServiceClient(ApikeyTmdb)
    ReadOnly _igdbClient = IGDB.Client.Create(ApikeyIgdb)
    Dim stopIconDownload As Boolean = False
    Dim ignoreAmbigousTitle As Boolean = False


    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        Dim myHandler As New NetworkAvailabilityChangedEventHandler(AddressOf AvailabilityChanged)
        AddHandler NetworkChange.NetworkAvailabilityChanged, myHandler
        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub AvailabilityChanged(sender As Object, e As NetworkAvailabilityEventArgs)

        If e.IsAvailable Then

            Dispatcher.Invoke(Sub()
                                  NetworkImage.Source = New BitmapImage(New Uri("/Model/Strong-WiFi.png", UriKind.Relative))
                              End Sub)
        Else

            Dispatcher.Invoke(Sub()
                                  NetworkImage.Source = New BitmapImage(New Uri("/Model/No-WiFi.png", UriKind.Relative))
                              End Sub)

        End If
    End Sub

    Private Sub RadioButton_Checked(sender As Object, e As RoutedEventArgs)
        Dim selectedBtn As RadioButton = sender
        SearchMod = selectedBtn.Content.ToString
    End Sub

    Private Sub Loadbtn_Click(sender As Object, e As RoutedEventArgs) Handles Loadbtn.Click
        Dim folderBrowserDialog = NewFolderBrowseDialog("Selected Folder")
        If folderBrowserDialog.ShowDialog Then
            ProcessedFolderValue.Content = 0
            IconsProcessedValue.Content = 0
            TotalIconsValue.Content = 0
            FolderProcessedCount = 0
            FinalistView.Items.Clear()
            SelectedFolderPath = folderBrowserDialog.SelectedPath
            SelectedFolderlbl.Content = SelectedFolderPath
            SearchAndMakehbtn.IsEnabled = True
            PosterProgressBar.Value = 0
            BusyIndicator1.DataContext = progressUpdater1
            GetFileNames()
        End If
    End Sub


    Private Async Sub SearchAndMakebtn_ClickAsync(sender As Object, e As RoutedEventArgs) _
        Handles SearchAndMakehbtn.Click
        GetFileNames()
        SkipAll = False
        If Not IsNullOrEmpty(Fnames) Then
            If ValidFolder(SelectedFolderPath) Then
                If My.Computer.Network.IsAvailable Then
                    FolderProcessedCount = 0
                    FinalistView.Items.Clear()
                    IconProcessedCount = 0
                    If IconMode = "Poster" Then 'Poster Mode
                        SearchAndMakehbtn.IsEnabled = False
                        FolderNameIndex = 0
                        Dim isAutoPicked As Boolean
                        GetReadyForSearch()
                        For Each itemTitle As String In Fnames
                            lblStatus.Content = "Searching..."
                            Cursor = Cursors.Wait
                            isAutoPicked = False
                            Dim sr As New SearchResult With {
                                .Owner = Me
                            }
                            SearchTitle = New TitleCleaner().Clean(itemTitle)

                            Dim response =
                                    If _
                                    (SearchMod = "Game", Await Igdbf.SearchGame(SearchTitle, _igdbClient),
                                     Await SearchIt(SearchTitle, _serviceClient))
                            Dim resultCount As Integer =
                                    If(SearchMod = "Game", response.Length, response.Result.TotalCount)
                            If resultCount = 0 Then
                                MessageBox.Show(
                                    "Nothing found for " & itemTitle & vbCrLf & "Try Searching with Other Title " &
                                    vbCrLf & "OR Check search Mode")
                                sr.ShowDialog()
                            ElseIf resultCount = 1 Then

                                Try
                                    If (SearchMod = "Game") Then
                                        Igdbf.ResultPicked(response(0))
                                    Else
                                        ResultPicked(response.Result, response.MediaType, 0)
                                    End If
                                Catch ex As Exception
                                    If ex.Message = "NoPoster" Then
                                        FolderNameIndex += 1
                                        MessageBox.Show("No poster found for " & SearchTitle)
                                        Continue For
                                    End If

                                End Try

                                isAutoPicked = True
                            ElseIf resultCount > 1 Then
                                If Not ignoreAmbigousTitle Then : sr.mediaType = response.MediaType : sr.ShowDialog() : End If
                            End If
                            If isAutoPicked OrElse sr.DialogResult Then
                                FinalistView.Items.Add(New ListItem() With {
                                                          .Title =
                                                          PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)(
                                                              "Title").ToString(),
                                                          .Year =
                                                          PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)(
                                                              "Year").ToString(),
                                                          .Rating =
                                                          PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)(
                                                              "Rating").ToString(),
                                                          .Folder =
                                                          PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)(
                                                              "Folder").ToString()
                                                          })
                            End If
                            Cursor = Cursors.Arrow
                            lblStatus.Content = "Idle"
                            FolderNameIndex += 1
                            If SkipAll Then : Exit For : End If
                        Next
                    Else 'Professional Mode
                        GetReadyForSearch()
                        Dim gPage As New ProSearchResultsDArt With {
                            .Owner = Me
                        }
                        gPage.ShowDialog()
                        If PickedListDataTable.Rows.Count > 0 Then
                            For i = 0 To PickedListDataTable.Rows.Count - 1
                                FinalistView.Items.Add(New ListItem() With {
                                                          .Title = PickedListDataTable.Rows(i)("Title").ToString(),
                                                          .Year = PickedListDataTable.Rows(i)("Year").ToString(),
                                                          .Rating = PickedListDataTable.Rows(i)("Rating").ToString(),
                                                          .Folder = PickedListDataTable.Rows(i)("Folder").ToString()
                                                          })
                            Next

                        End If

                    End If
                    ProcessedFolderValue.Content = FolderProcessedCount.ToString
                    TotalIconsValue.Content = ImgDownloadList.Count.ToString
                    progressUpdater1.Maximum = ImgDownloadList.Count.ToString
                    progressUpdater1.Text = "Downloading Icon 1/" & ImgDownloadList.Count.ToString & "..."
                    PosterProgressBar.Maximum = ImgDownloadList.Count.ToString
                    SetColumnWidth(FinalistView)
                    If ImgDownloadList.Count > 0 Then
                        Await DoWorkOfDownloadAsync()
                    Else
                        SearchAndMakehbtn.IsEnabled = True
                    End If
                Else
                    MessageBox.Show("Sorry, Internet is Not available.", "Network Error")
                End If
            End If
        Else
            MessageBox.Show("Folder already have Icons or is Empty", "Folder Error")
        End If
    End Sub

    Private Async Function DoWorkOfDownloadAsync() As Task
        SearchAndMakehbtn.IsEnabled = False
        lblStatus.Content = "Creating Icons..."
        Await DownloadAndMakeIcons()
        lblStatus.Content = "Idle"
    End Function

    Private Sub RadioButton_Checked_1(sender As Object, e As RoutedEventArgs)
        Dim selectedbtn As RadioButton = sender
        IconMode = selectedbtn.Content.ToString
        If IconMode = "Professional" Then
            GridSearchMode.Visibility = Visibility.Hidden
        Else
            GridSearchMode.Visibility = Visibility.Visible
        End If
    End Sub
    Private Async Function DownloadAndMakeIcons() As Task
        stopIconDownload = False
        BusyIndicator1.IsBusy = True
        Dim i = 0
        For Each img As ImageToDownload In ImgDownloadList
            If stopIconDownload Then
                MakeIcons()
                SearchAndMakehbtn.IsEnabled = True
                Return
            End If
            Await DownloadImageFromUrlAsync(img.RemotePath, img.LocalPath)
            i += 1
            progressUpdater1.Text = "Downloading Icon " & i & "/" & progressUpdater1.Maximum & "..."
            progressUpdater1.Value = i
            PosterProgressBar.Value = i
        Next
        BusyIndicator1.IsBusy = False
        If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum.ToString() Then
            BusyIndicator1.IsBusy = True
            MakeIcons()
        End If
        SearchAndMakehbtn.IsEnabled = True
    End Function

    Private Sub Window_Drop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            SelectedFolderPath = CType(e.Data.GetData(DataFormats.FileDrop), Array).GetValue(0).ToString
        End If

        Dim data As String = CType(e.Data.GetData(DataFormats.FileDrop), Array).GetValue(0).ToString
        If Directory.Exists(data) Then
            SelectedFolderPath = data
            FolderProcessedCount = 0
            FinalistView.Items.Clear()
            SelectedFolderlbl.Content = SelectedFolderPath
            SearchAndMakehbtn.IsEnabled = True
            PosterProgressBar.Value = 0
            BusyIndicator1.DataContext = progressUpdater1
            GetFileNames()
        End If
    End Sub

    Private Sub FinalistView_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
        Handles FinalistView.MouseDoubleClick
        Dim item = FinalistView.SelectedItem
        If item IsNot Nothing Then
            Process.Start(FinalistView.SelectedItem.Folder)
        End If
    End Sub

    Private Sub DownloadCancelbtn_Click(sender As Object, e As RoutedEventArgs)
        stopIconDownload = True
    End Sub

    Private Sub BusyPosterProgressBar_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        If progressUpdater1.Value = progressUpdater1.Maximum Then
            BusyIndicator1.IsBusy = False
        End If
    End Sub

    Private Sub MakeIcons()
        BusyIndicator1.IsBusy = True
        If IconMode = "Poster" AndAlso Not SearchMod = "Game" Then
            MakeIco(My.Settings.isRatingVisible, My.Settings.isPosterMockupUsed)
        Else
            MakeIco()
        End If
        IconsProcessedValue.Content = IconProcessedCount.ToString()
        BusyIndicator1.IsBusy = False
        Select Case _
            MessageBox.Show(
                "Note:The Icon may take some time to reload. " & vbCrLf &
                " To Force Reload, click on Restart Explorer " & vbCrLf & "OK to Open Folder", "Icon(s) Created",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information)
            Case MessageBoxResult.OK
                Process.Start(SelectedFolderPath)
        End Select
    End Sub

    Private Sub MainForm_Loaded(sender As Object, e As RoutedEventArgs) Handles MainForm.Loaded
        chkIsPosterOverlayVisible.IsChecked = My.Settings.isPosterMockupUsed
        chkIsRatingVisible.IsChecked = My.Settings.isRatingVisible
        NetworkImage.Source = If(IsNetworkAvailable(),
            New BitmapImage(New Uri("/Model/Strong-WiFi.png", UriKind.Relative)),
            New BitmapImage(New Uri("/Model/No-WiFi.png", UriKind.Relative)))

    End Sub

    Private Function IsNetworkAvailable() As Boolean
        If My.Computer.Network.IsAvailable Then
            Try
                Dns.GetHostEntry("www.google.com")
                Return True
            Catch
                Return False
            End Try
        End If
        Return False
    End Function


    Private Sub MenuApiConfigBtn_Click(sender As Object, e As RoutedEventArgs) Handles MenuApiConfigBtn.Click
        Dim fr As New ApiConfig()
        fr.ShowDialog()
    End Sub

    Private Sub MenubtnHelp_Click(sender As Object, e As RoutedEventArgs) Handles MenubtnHelp.Click
        Dim url = "https://github.com/DineshSolanki/FoliCon"
        Process.Start(url)
    End Sub

    Private Sub MenuRExbtn_Click(sender As Object, e As RoutedEventArgs) Handles MenuRExbtn.Click
        RefreshIconCache()
    End Sub

    Private Sub SelectedFolderlbl_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
        Handles SelectedFolderlbl.MouseDoubleClick
        Process.Start(SelectedFolderPath)
    End Sub

    Private Sub MenuItem_Click(sender As Object, e As RoutedEventArgs)
        Dim about As New Gat.Controls.About() With {
                .Title = "FoliCon v2.3.0",
                .ApplicationLogo = New BitmapImage(New Uri("\Model\folicon Icon.ico", UriKind.Relative)),
                .Description = "FoliCon is more than just a typical folder Icon changer" & vbCrLf _
                             & "It automates this task to a greater extent, it has two different modes for different designs of folder Icons," & vbCrLf _
                             & "and it can fetch 'Games,Movies, and shows' folder icons.",
                .Version = "2.3.0",
                .PublisherLogo = New BitmapImage(New Uri("\Model\folicon Icon.ico", UriKind.Relative)),
                .AdditionalNotes = "Developed by Dinesh Solanki",
                .Copyright = "GNU General Public License v3.0"}
        about.Show()
    End Sub

    Private Sub chkIsRatingVisible_Click(sender As Object, e As RoutedEventArgs) Handles chkIsRatingVisible.Click
        If chkIsRatingVisible.IsChecked Then
            My.Settings.isRatingVisible = True
        Else
            My.Settings.isRatingVisible = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub chkIsPosterOverlayVisible_Click(sender As Object, e As RoutedEventArgs) Handles chkIsPosterOverlayVisible.Click
        If chkIsPosterOverlayVisible.IsChecked Then
            My.Settings.isPosterMockupUsed = True
        Else
            My.Settings.isPosterMockupUsed = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub MenuDeleteIconsbtn_Click(sender As Object, e As RoutedEventArgs) Handles MenuDeleteIconsbtn.Click
        If MessageBox.Show("Are you sure you want to delete all Icons?", "Confirm Icon Deletion", MessageBoxButton.OKCancel, MessageBoxImage.Question) = MessageBoxResult.OK Then
            If Directory.Exists(SelectedFolderPath) Then
                DeleteIconsFromPath(SelectedFolderPath)
                MessageBox.Show("Icons Deleted Sucessfully", "Icons Deleted", MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                MessageBox.Show("Directory is Empty", "Empty Directory", MessageBoxButton.OK, MessageBoxImage.Warning)
            End If
        End If


    End Sub

    Private Sub chkIgnoreAmbiguous_Click(sender As Object, e As RoutedEventArgs) Handles chkIgnoreAmbiguous.Click
        If chkIgnoreAmbiguous.IsChecked Then
            ignoreAmbigousTitle = True
        Else
            ignoreAmbigousTitle = False
        End If
    End Sub
End Class
