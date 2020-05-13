Imports System.ComponentModel
Imports System.Configuration
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports Xceed.Wpf.Toolkit

Class MainWindow
    Private WithEvents BackgrundWorker1 As New BackgroundWorker With {
        .WorkerSupportsCancellation = True, .WorkerReportsProgress = True}

    Dim _draggedFileName As String = Nothing
    Private ReadOnly progressUpdater1 As New ProgressUpdater

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
        If SearchMod = "TV" Then
            DateProperty = "first_air_date"
            INameProperty = "name"
        Else
            DateProperty = "release_date"
            INameProperty = "title"
        End If
    End Sub

    Private Sub Loadbtn_Click(sender As Object, e As RoutedEventArgs) Handles Loadbtn.Click
        Dim folderBrowserDialog = NewFolderBrowseDialog("Selected Folder")
        If folderBrowserDialog.ShowDialog Then
            SelectedFolderPath = folderBrowserDialog.SelectedPath
            SelectedFolderlbl.Content = SelectedFolderPath
            Searchbtn.IsEnabled = True
            PosterProgressBar.Value = 0
            BusyIndicator1.DataContext = progressUpdater1
            GetFileNames()
        End If
    End Sub


    Private Async Sub Searchbtn_ClickAsync(sender As Object, e As RoutedEventArgs) Handles Searchbtn.Click
        GetFileNames()
        If Not IsNullOrEmpty(Fnames) Then
            If ValidFolder(SelectedFolderPath) Then
                If My.Computer.Network.IsAvailable Then
                    If IconMode = "Poster" Then 'Poster Mode
                        Searchbtn.IsEnabled = False
                        FolderNameIndex = 0
                        Dim isAutoPicked As Boolean
                        GetReadyForSearch()
                        For Each itemTitle As String In Fnames
                            isAutoPicked = False
                            SearchTitle = New TitleCleaner().Clean(itemTitle)
                            Dim response = Await PerformActualSearch(SearchTitle)
                            Dim result As String
                            Dim sr As New SearchResult
                            If SearchMod = "Game" Then
                                result = response.item("number_of_total_results")
                            Else
                                result = response.item("total_results")
                            End If
                            If result = 0 Then
                                MessageBox.Show(
                                    "Nothing found for " & itemTitle & vbCrLf & "Try Searching with Other Title " &
                                    vbCrLf & "OR Check search Mode")
                                sr.ShowDialog()
                            ElseIf result = 1 Then
                                ResultPicked(0)
                                isAutoPicked = True
                            ElseIf result > 1 Then
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
                    'Mouse.SetCursor(Cursors.Arrow)
                    ProcessedFolderValue.Content = FolderProcessedCount.ToString
                    TotalIconsValue.Content = ImgDownloadList.Count.ToString
                    progressUpdater1.Maximum = ImgDownloadList.Count.ToString
                    progressUpdater1.Text = "Downloading Icon 1/" & ImgDownloadList.Count.ToString & "..."
                    PosterProgressBar.Maximum = ImgDownloadList.Count.ToString
                    SetColumnWidth(FinalistView)
                    If ImgDownloadList.Count > 0 Then
                        BusyIndicator1.IsBusy = True
                        DoWorkOfDownload()
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
        Searchbtn.IsEnabled = False
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
        Searchbtn.IsEnabled = True
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
            If IconMode = "Poster" Then
                MakeIco("visible")
            Else
                MakeIco()
            End If

            IconsProcessedValue.Content = IconProcessedCount.ToString()
            BusyIndicator1.IsBusy = False
            Select Case _
                MessageBox.Show("Note:The Icon may take some time to reload. " & vbCrLf & " To Force Reload, click on Restart Explorer "& vbCrLf &"OK to Open Folder", "Icon(s) Created", MessageBoxButton.OKCancel,
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
                Dim ipHost As IPHostEntry = Dns.GetHostEntry("www.google.com")
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
        Dim url As String = "https://github.com/DineshSolanki/FoliCon"
        Process.Start(url)
    End Sub

    Private Sub MenuRExbtn_Click(sender As Object, e As RoutedEventArgs) Handles MenuRExbtn.Click
        RefreshIconCache()
    End Sub

    Private Sub SelectedFolderlbl_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles SelectedFolderlbl.MouseDoubleClick
        Process.Start(SelectedFolderPath)
    End Sub
End Class
