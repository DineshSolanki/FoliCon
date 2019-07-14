Imports System.ComponentModel
Imports System.Data

Class MainWindow
    Private WithEvents BackgrundWorker1 As New BackgroundWorker With {
        .WorkerSupportsCancellation = True, .WorkerReportsProgress = True}
    Dim _draggedFileName As String = Nothing
    Private progressUpdater1 As New ProgressUpdater
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


    Private Sub Searchbtn_Click(sender As Object, e As RoutedEventArgs) Handles Searchbtn.Click
        GetFileNames()
        If Not IsNullOrEmpty(Fnames) Then
            If ValidFolder(SelectedFolderPath) Then
                If My.Computer.Network.IsAvailable Then
                    If IconMode = "Poster" Then                    'Poster Mode
                        Dim searchPage As New SearchResult
                        searchPage.ShowDialog()
                        'Else                                    'Professional Mode
                        '    Dim Gpage As New googlePage
                        '    Gpage.ShowDialog()
                    End If
                    ProcessedFolderValue.Content = FolderProcessedCount.ToString
                    TotalIconsValue.Content = ImgDownloadList.Count.ToString
                    progressUpdater1.Maximum = ImgDownloadList.Count.ToString
                    progressUpdater1.Text = "Downloading Icon 1/" & ImgDownloadList.Count.ToString & "..."
                    PosterProgressBar.Maximum = ImgDownloadList.Count.ToString

                    Dim items As New List(Of ListItem)()
                    For Each r As DataRow In PickedListDataTable.Rows

                        items.Add(New ListItem() With {
                .Title = r("Title").ToString(),
                .Year = r("Year").ToString(),
                .Rating = r("Rating").ToString(),
                .Folder = r("Folder").ToString()
            })
                    Next
                    FinalistView.ItemsSource = items
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
        MessageBox.Show("Done!", "Posters Downloaded")
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

    Private Sub Makebtn_Click(sender As Object, e As RoutedEventArgs) Handles Makebtn.Click
        If ValidFolder(SelectedFolderPath) Then
            If PosterProgressBar.Value.ToString = PosterProgressBar.Maximum Then
                BusyIndicator1.IsBusy = True
                'Dim ts As New System.Threading.ThreadStart(Sub() MakeIco("visible"))
                'Dim t As New System.Threading.Thread(ts)
                't.Start()
                MakeIco("visible")
                IconsProcessedValue.Content = IconProcessedCount.ToString()
                BusyIndicator1.IsBusy = False
                Select Case MessageBox.Show("Press OK to Open Folder", "Icon(s) Created", MessageBoxButton.OKCancel, MessageBoxImage.Information)
                    Case MessageBoxResult.OK
                        Process.Start(SelectedFolderPath)
                End Select

            Else
                Dim result = MessageBox.Show("Please wait for the posters to Download..." & vbCrLf & "Have you already downloaded Images ?", "Please wait...", MessageBoxButton.YesNo)
                If result = MessageBoxResult.Yes Then
                    BusyIndicator1.IsBusy = True
                    'Dim ts As New System.Threading.ThreadStart(AddressOf MakeIco)
                    'Dim t As New System.Threading.Thread(ts)
                    't.Start()
                    MakeIco()
                    BusyIndicator1.IsBusy = False
                    IconsProcessedValue.Content = IconProcessedCount.ToString()
                    MessageBox.Show("Done!", "Icon(s) Created")
                End If
            End If
        Else
            MessageBox.Show("Sorry, Folder is Empty or not selected.", "Empty Folder")
        End If
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
End Class
