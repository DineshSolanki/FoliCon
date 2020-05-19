Imports System.ComponentModel
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports Xceed.Wpf.Toolkit
Imports FoliconNative.Modules

Class MainWindow
    Private WithEvents BackgrundWorker1 As New BackgroundWorker With {
        .WorkerSupportsCancellation = True, .WorkerReportsProgress = True}

    Dim _draggedFileName As String = Nothing
    Private ReadOnly progressUpdater1 As New ProgressUpdater
    ReadOnly _serviceClient as New Net.TMDb.ServiceClient(ApikeyTmdb)
    ReadOnly _igdbClient = IGDB.Client.Create(ApikeyIgdb)

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
                            Cursor = Cursors.Wait
                            isAutoPicked = False
                            Dim sr as New SearchResult()
                            SearchTitle = New TitleCleaner().Clean(itemTitle)

                            Dim response =
                                    If _
                                    (SearchMod = "Game", Await Igdbf.SearchGame(SearchTitle, _igdbClient),
                                     await SearchIt(SearchTitle, _serviceClient))
                            Dim resultCount as Integer =
                                    If(SearchMod = "Game", response.Length, response.Result.TotalCount)
                            If resultCount = 0
                                MessageBox.Show(
                                    "Nothing found for " & itemTitle & vbCrLf & "Try Searching with Other Title " &
                                    vbCrLf & "OR Check search Mode")
                                sr.ShowDialog()
                            ElseIf resultCount = 1
                                Try
                                    If (SearchMod = "Game")
                                        Igdbf.ResultPicked(response(0))
                                    Else
                                        ResultPicked(response.Result, response.MediaType, 0)
                                    End If
                                Catch ex As Exception
                                    If ex.Message = "NoPoster"
                                        FolderNameIndex += 1
                                        MessageBox.Show("No poster found for " & SearchTitle)
                                        Continue For
                                    End If

                                End Try

                                isAutoPicked = true
                            ElseIf resultCount > 1
                                sr.ShowDialog()
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
                            FolderNameIndex += 1
                        Next
                    Else 'Professional Mode
                        GetReadyForSearch()
                        Dim gPage As New ProSearchResultsDArt()
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
                        BusyIndicator1.IsBusy = True
                        DoWorkOfDownload()
                        else
                            SearchAndMakehbtn.IsEnabled=true
                    End If
                Else
                    MessageBox.Show("Sorry, Internet is Not available.", "Network Error")
                End If
            End If
        Else
            MessageBox.Show("Folder already have Icons or is Empty", "Folder Error")
        End If
    End Sub

    Private Sub DoWorkOfDownload()
        SearchAndMakehbtn.IsEnabled = False
        BackgrundWorker1.RunWorkerAsync()     '<== Causes Exception when searching, while posters are downloading
    End Sub

    Private Sub RadioButton_Checked_1(sender As Object, e As RoutedEventArgs)
        Dim selectedbtn As RadioButton = sender
        IconMode = selectedbtn.Content.ToString
        If IconMode = "Professional" Then
            GridSearchMode.Visibility = Visibility.Hidden
        Else
            GridSearchMode.Visibility = Visibility.Visible
        End If
    End Sub


    Private Sub BackgrundWorker1_DoWork(sender As Object, e As DoWorkEventArgs) Handles BackgrundWorker1.DoWork
        Dim i = 0
        For Each img As ImageToDownload In ImgDownloadList
            If (BackgrundWorker1.CancellationPending = True) Then
                e.Cancel = True
                Return
            End If
            DownloadImageFromUrl(img.RemotePath, img.LocalPath)
            i += 1
            BackgrundWorker1.ReportProgress(i)
        Next
    End Sub

    Private Sub BackgrundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) _
        Handles BackgrundWorker1.RunWorkerCompleted
        BusyIndicator1.IsBusy = False
        If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum.ToString() Then
            BusyIndicator1.IsBusy = True
            MakeIcons()
        End If
        SearchAndMakehbtn.IsEnabled = True
    End Sub

    Private Sub Window_Drop(sender As Object, e As DragEventArgs)
        'If e.Data.GetDataPresent(DataFormats.FileDrop) Then
        '    SelectedFolderPath = CType(e.Data.GetData(DataFormats.FileDrop), Array).GetValue(0).ToString
        'End If

        'If e.Effects = DragDropEffects.Move Then
        '    SelectedFolderPath = _draggedFileName
        '    MessageBox.Show("Folder Loaded: " & _draggedFileName)
        '    e.Effects = DragDropEffects.None
        'End If
    End Sub

    Private Sub Window_DragEnter(sender As Object, e As DragEventArgs)
        'Dim data As String = CType(e.Data.GetData(DataFormats.FileDrop), Array).GetValue(0).ToString
        'If Directory.Exists(data) Then
        '    ' here you can restrict allowed files for drag & drop operation
        '    e.Effects = DragDropEffects.Move
        '    _draggedFileName = data
        'End If
    End Sub

    Private Sub FinalistView_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
        Handles FinalistView.MouseDoubleClick
        Dim item = FinalistView.SelectedItem
        If item IsNot Nothing Then
            Process.Start(FinalistView.SelectedItem.Folder)
        End If
    End Sub

    Private Sub DownloadCancelbtn_Click(sender As Object, e As RoutedEventArgs)
        BackgrundWorker1.CancelAsync()
    End Sub

    Private Sub BackgrundWorker1_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) _
        Handles BackgrundWorker1.ProgressChanged
        progressUpdater1.Text = "Downloading Icon " & e.ProgressPercentage & "/" & progressUpdater1.Maximum & "..."
        progressUpdater1.Value = e.ProgressPercentage
        PosterProgressBar.Value = e.ProgressPercentage
    End Sub

    Private Sub BusyPosterProgressBar_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        If progressUpdater1.Value = progressUpdater1.Maximum Then
            BusyIndicator1.IsBusy = False
        End If
    End Sub

    Private Sub MakeIcons()
        If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum.ToString() Then
            BusyIndicator1.IsBusy = True
            If IconMode = "Poster" AndAlso Not SearchMod="Game" Then
                MakeIco("visible")
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
        End If
    End Sub

    Private Sub MainForm_Loaded(sender As Object, e As RoutedEventArgs) Handles MainForm.Loaded
        If isNetworkAvailable() Then
            NetworkImage.Source = New BitmapImage(New Uri("/Model/Strong-WiFi.png", UriKind.Relative))
        Else
            NetworkImage.Source = New BitmapImage(New Uri("/Model/No-WiFi.png", UriKind.Relative))
        End If
    End Sub

    Private Function isNetworkAvailable() As Boolean
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
        Dim fr as New ApiConfig()
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
        Dim about As New Gat.Controls.About() With{
                .Title="FoliCon v2.0",
                .ApplicationLogo=New BitmapImage(New Uri("\Model\folicon Icon.ico",UriKind.Relative)),
                .Description="FoliCon is more than just a typical folder Icon changer" & vbCrLf _
                             & "It automates this task to a greater extent, it has two different modes for different designs of folder Icons," & vbCrLf _
                             & "and it can fetch 'Games,Movies, and shows' folder icons.",
                .Version="2.0",
                .PublisherLogo=New BitmapImage(New Uri("\Model\folicon Icon.ico",UriKind.Relative)),
                .AdditionalNotes="Developed by Dinesh Solanki",
                .Copyright="GNU General Public License v3.0"}
        about.Show()
    End Sub
End Class
