Imports System.ComponentModel
Imports System.Data
Imports System.IO
Imports Ookii.Dialogs.Wpf

Class MainWindow
    Private WithEvents BackgrundWorker1 As New BackgroundWorker
    Dim _draggedFileName As String = Nothing
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
                    If ImgDownloadList.Count > 0 Then
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
        BackgrundWorker1.RunWorkerAsync()
    End Sub

    Private Sub RadioButton_Checked_1(sender As Object, e As RoutedEventArgs)
        Dim selectedbtn As RadioButton = sender
        IconMode = selectedbtn.Content.ToString
    End Sub

    Private Sub BackgrundWorker1_DoWork(sender As Object, e As DoWorkEventArgs) Handles BackgrundWorker1.DoWork
        For Each i As ImageToDownload In ImgDownloadList
            DownloadImageFromUrl(i.RemotePath, i.LocalPath)
            Me.Dispatcher.Invoke(Sub()
                                     PosterProgressBar.Value += 1
                                 End Sub)

        Next
    End Sub

    Private Sub BackgrundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgrundWorker1.RunWorkerCompleted
        PosterProgressBar.Value = 100
        MessageBox.Show("Done!", "Posters Downloaded")
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
            If PosterProgressBar.Value.ToString = "100" Then
                BusyIndicator1.IsBusy = True
                Dim ts As New System.Threading.ThreadStart(Sub() MakeIco("visible"))
                Dim t As New System.Threading.Thread(ts)
                t.Start()
                IconsProcessedValue.Content = IconProcessedCount.ToString()
                BusyIndicator1.IsBusy = False
                MessageBox.Show("Done!", "Icon(s) Created")

            Else
                Dim result = MessageBox.Show("Please wait for the posters to Download..." & vbCrLf & "Have you already downloaded Images ?", "Please wait...", MessageBoxButton.YesNo)
                If result = MessageBoxResult.Yes Then
                    BusyIndicator1.IsBusy = True
                    Dim ts As New System.Threading.ThreadStart(AddressOf MakeIco)
                    Dim t As New System.Threading.Thread(ts)
                    t.Start()
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
End Class
