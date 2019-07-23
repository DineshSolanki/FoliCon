Imports System.ComponentModel
Imports System.Net.NetworkInformation

Class MainWindow
    Private WithEvents BackgrundWorker1 As New BackgroundWorker With {
        .WorkerSupportsCancellation = True, .WorkerReportsProgress = True}
    Dim _draggedFileName As String = Nothing
    Private progressUpdater1 As New ProgressUpdater

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        Dim myHandler As New NetworkAvailabilityChangedEventHandler(AddressOf AvailabilityChanged)
        AddHandler NetworkChange.NetworkAvailabilityChanged, myHandler
        ' Add any initialization after the InitializeComponent() call.

    End Sub
    Private Sub AvailabilityChanged(ByVal sender As Object, ByVal e As NetworkAvailabilityEventArgs)

        If e.IsAvailable Then

            Me.Dispatcher.Invoke(Sub()
                                     NetworkImage.Source = New BitmapImage(New Uri("/Model/Strong-WiFi.png", UriKind.Relative))
                                 End Sub)
        Else

            Me.Dispatcher.Invoke(Sub()
                                     NetworkImage.Source = New BitmapImage(New Uri("/Model/No-WiFi.png", UriKind.Relative))
                                 End Sub)

        End If
    End Sub

    Private Sub RadioButton_Checked(sender As Object, e As RoutedEventArgs)
        Dim selectedbtn As RadioButton = sender
        SearchMod = selectedbtn.Content.ToString
        If SearchMod = "TV" Then
            DateProperty = "first_air_date"
            INameProperty = "name"
        Else
            DateProperty = "release_date"
            INameProperty = "title"
        End If

    End Sub

    Private Sub Loadbtn_Click(sender As Object, e As RoutedEventArgs) Handles Loadbtn.Click
        Dim folderbrowse = NewFolderBrowseDialog("Selected Folder")
        If folderbrowse.ShowDialog Then
            SelectedFolderPath = folderbrowse.SelectedPath
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
                    If IconMode = "Poster" Then                    'Poster Mode
                        'Mouse.SetCursor(Cursors.Wait) 
                        Searchbtn.IsEnabled = False
                        FolderNameIndex = 0
                        Dim isAutoPicked As Boolean
                        GetReadyForSearch()
                        For Each Title As String In Fnames
                            isAutoPicked = False
                            SearchTitle = New TitleCleaner().Clean(Title)
                            Dim response = Await PerformAcctualSearch(SearchTitle)
                            Dim result As String
                            Dim sr As New SearchResult
                            If SearchMod = "Game" Then
                                result = response.item("number_of_total_results")
                            Else
                                result = response.item("total_results")
                            End If
                            If result = 0 Then
                                MessageBox.Show("Nothing found for " & Title & vbCrLf & "Try Searching with Other Title " & vbCrLf & "OR Check search Mode")
                                sr.ShowDialog()
                            ElseIf result = 1 Then
                                ResultPicked(0)
                                isAutoPicked = True
                            ElseIf result > 1 Then
                                ' MessageBox.Show("Too Many Results, Please Pick The Correct Title", "Ambigious Title")
                                sr.ShowDialog()
                            End If
                            If isAutoPicked OrElse sr.DialogResult Then
                                FinalistView.Items.Add(New ListItem() With {
               .Title = PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)("Title").ToString(),
               .Year = PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)("Year").ToString(),
               .Rating = PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)("Rating").ToString(),
               .Folder = PickedListDataTable.Rows(PickedListDataTable.Rows.Count - 1)("Folder").ToString()
           })
                            End If

                            FolderNameIndex += 1
                        Next

                        'Dim searchPage As New SearchResult
                        'searchPage.ShowDialog()
                        'Else                                    'Professional Mode
                        '    Dim Gpage As New googlePage
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

    End Sub


    Private Sub BackgrundWorker1_DoWork(sender As Object, e As DoWorkEventArgs) Handles BackgrundWorker1.DoWork
        Dim i As Integer = 0
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

    Private Sub BackgrundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgrundWorker1.RunWorkerCompleted
        BusyIndicator1.IsBusy = False
        If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum Then
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

    Private Sub FinalistView_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles FinalistView.MouseDoubleClick
        Dim item = FinalistView.SelectedItem
        If item IsNot Nothing Then
            Process.Start(FinalistView.SelectedItem.Folder)
        End If
    End Sub

    Private Sub DownloadCancelbtn_Click(sender As Object, e As RoutedEventArgs)
        BackgrundWorker1.CancelAsync()
    End Sub

    Private Sub BackgrundWorker1_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles BackgrundWorker1.ProgressChanged
        progressUpdater1.Text = "Downloading Icon " & e.ProgressPercentage & "/" & progressUpdater1.Maximum & "..."
        progressUpdater1.Value = e.ProgressPercentage
        PosterProgressBar.Value = e.ProgressPercentage
    End Sub

    Private Sub BusyPosterProgessBar_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        If progressUpdater1.Value = progressUpdater1.Maximum Then
            BusyIndicator1.IsBusy = False
        End If
    End Sub
    Private Sub MakeIcons()
        If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum Then
            BusyIndicator1.IsBusy = True
            MakeIco("visible")
            IconsProcessedValue.Content = IconProcessedCount.ToString()
            BusyIndicator1.IsBusy = False
            Select Case MessageBox.Show("Press OK to Open Folder", "Icon(s) Created", MessageBoxButton.OKCancel, MessageBoxImage.Information)
                Case MessageBoxResult.OK
                    Process.Start(SelectedFolderPath)
            End Select
        End If
    End Sub

    Private Sub MainForm_Loaded(sender As Object, e As RoutedEventArgs) Handles MainForm.Loaded
        If My.Computer.Network.IsAvailable Then
            NetworkImage.Source = New BitmapImage(New Uri("/Model/Strong-WiFi.png", UriKind.Relative))
        Else
            NetworkImage.Source = New BitmapImage(New Uri("/Model/No-WiFi.png", UriKind.Relative))

        End If

    End Sub
End Class
