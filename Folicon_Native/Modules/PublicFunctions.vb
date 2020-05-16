Option Strict Off

Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.TMDb
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Interop
Imports FoliconNative.Model
Imports Ookii.Dialogs.Wpf
Imports Xceed.Wpf.Toolkit

Module PublicFunctions
    <DllImport("kernel32.dll", SetLastError := True)>
    Private Function Wow64DisableWow64FsRedirection(ByRef ptr As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError := True)>
    Private Function Wow64RevertWow64FsRedirection(ByVal ptr As IntPtr) As Boolean
    End Function

    <DllImport("gdi32")>
    Private Function DeleteObject(o As IntPtr) As Integer
    End Function

    Public Function LoadBitmap(source As Bitmap) As BitmapSource
        Dim ip As IntPtr = source.GetHbitmap()
        Dim bs As BitmapSource

        Try
            bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                                                       BitmapSizeOptions.FromEmptyOptions())
        Finally
            DeleteObject(ip)
        End Try

        Return bs
    End Function

    Public Function NewFolderBrowseDialog(description As String) As VistaFolderBrowserDialog
        Dim folderBrowser As New VistaFolderBrowserDialog()
        With folderBrowser
            .Description = description
            .UseDescriptionForTitle = True
        End With
        Return folderBrowser
    End Function

    Public Sub GetReadyForSearch()
        ImgDownloadList.Clear()
        InitPickedListDataTable()
    End Sub

    Public Function IsNullOrEmpty(myStringArray() As String) As Boolean
        Return myStringArray Is Nothing OrElse myStringArray.Length < 1 OrElse myStringArray(0) = ""
    End Function

    Public Sub SetColumnWidth(listView As ListView)
        Dim gridView = TryCast(listView.View, GridView)
        If gridView IsNot Nothing Then
            For Each column In gridView.Columns
                If Double.IsNaN(column.Width) Then
                    column.Width = column.ActualWidth
                End If
                column.Width = Double.NaN
            Next column
        End If
    End Sub

    ''' <summary>
    ''' Creates an array of all Folders Names which do not have an icon assigned 
    ''' </summary>
    ''' 
    Public Sub GetFileNames()
        If Not String.IsNullOrEmpty(SelectedFolderPath) Then
            Dim folderNames As String = String.Empty
            Dim separator As String = String.Empty
            For Each folder As String In Directory.GetDirectories(SelectedFolderPath)
                If Not File.Exists(folder & "\" & Path.GetFileName(folder) & ".ico") Then
                    folderNames &= separator & Path.GetFileName(folder)
                    separator = ","
                End If
            Next
            Fnames = folderNames.Split(CType(",", Char))
        Else
            MessageBox.Show("Folder is Empty or not Selected", "Folder Error")
        End If
    End Sub

    ''' <summary>
    ''' Check if there are Any folder in the Selected Path 
    ''' </summary>
    Public Function ValidFolder(path As String) As Boolean
        If Not path = Nothing Then
            If Directory.GetDirectories(path).Length > 0 Then
                Return True
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' Sub That can Download image from any URL and save to local path
    ''' </summary>
    ''' <param name="url"> The URL of Image to Download</param>
    ''' <param name="saveFilename">The Local Path Of Downloaded Image</param>
    Public Sub DownloadImageFromUrl(url As String, saveFilename As String)
        Dim httpWebRequest = DirectCast(WebRequest.Create(New Uri(url)), HttpWebRequest)
        Dim httpWebResponse = DirectCast(httpWebRequest.GetResponse(), HttpWebResponse)
        If _
            (httpWebResponse.StatusCode <> HttpStatusCode.OK AndAlso httpWebResponse.StatusCode <> HttpStatusCode.Moved AndAlso
             httpWebResponse.StatusCode <> HttpStatusCode.Redirect) OrElse
            Not httpWebResponse.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase) Then
            Return
        End If
        Using stream = httpWebResponse.GetResponseStream()
            Using fileStream = File.OpenWrite(saveFilename)
                Dim bytes = New Byte(4095) {}
                Dim read = 0
                Do
                    If stream Is Nothing Then
                        Continue Do
                    End If
                    read = stream.Read(bytes, 0, bytes.Length)
                    fileStream.Write(bytes, 0, read)
                Loop While read <> 0
            End Using
        End Using
    End Sub

    Public Async Function DownloadImage(filename As String, localPath As String,
                                        optional cancellationToken As CancellationToken = nothing) _
        As Task
        If cancellationToken = Nothing
            cancellationToken = CancellationToken.none
        End If
        If Not File.Exists(localPath) Then
            Dim folder As String = Path.GetDirectoryName(localPath)
            Directory.CreateDirectory(folder)
            Dim storage = New StorageClient()
            Using _
                fileStream =
                    New FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, Short.MaxValue, True)
                Try
                    Await storage.DownloadAsync(filename, fileStream, cancellationToken)
                Catch ex As Exception
                    Trace.TraceError(ex.ToString())
                End Try
            End Using
        End If
    End Function

    ''' <summary>
    ''' Converts From PNG to ICO
    ''' </summary>
    ''' <param name="filmFolderPath"> Path where to save and where PNG is Downloaded</param>
    ''' <param name="rating"> if Wants to Include rating on Icon</param>
    ''' <param name="isVisible">Show rating or NOT</param>
    Public Sub BuildFolderIco(filmFolderPath As String, rating As String, isVisible As String)
        If Not File.Exists(filmFolderPath) Then
            Exit Sub
        End If
        isVisible = If(rating <> "", "Visible", "Hidden")
        If Not rating = "10"
            rating = If(Not rating.Contains("."), rating & ".0", rating)
        End If


        Dim icon As Bitmap
        Using _
            task As Task(Of Bitmap) =
                Start(
                    Function() _
                         New MyMovieCIcon(New MyMovieIconLayout(filmFolderPath, rating, isVisible)).RenderToBitmap())
            task.Wait()
            icon = task.Result
        End Using
        Call PngToIcoService.Convert(icon, filmFolderPath.Replace("png", "ico"))
        icon.Dispose()
    End Sub

    Public Sub HideIcons(icoFile As String)
        ' Set icon file attribute to "Hidden"
        If (File.GetAttributes(icoFile) And FileAttributes.Hidden) <> FileAttributes.Hidden Then
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) Or FileAttributes.Hidden)
        End If

        ' Set icon file attribute to "System"
        If (File.GetAttributes(icoFile) And FileAttributes.System) <> FileAttributes.System Then
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) Or FileAttributes.System)
        End If
    End Sub

    ''' <summary>
    ''' Creates Icons from PNG
    ''' </summary>
    Public Sub MakeIco(Optional ByVal isVisible As String = "Hidden")
        'Try
        Dim folderNames = ""
        Dim fNames() As String
        Dim separator = ""
        For Each folder As String In Directory.GetDirectories(SelectedFolderPath)
            folderNames &= separator & Path.GetFileName(folder)
            separator = ","
        Next
        fNames = folderNames.Split(",")
        For Each i As String In fNames
            Dim tempI = i
            Dim targetFile = SelectedFolderPath & "\" & i & "\" & i & ".ico"
            If File.Exists(SelectedFolderPath & "\" & i & "\" & i & ".png") AndAlso Not File.Exists(targetFile) Then
                Dim rating As String =
                        PickedListDataTable.AsEnumerable().Where(Function(p) p("FolderName").ToString() = tempI).Select(
                            Function(p) p("Rating").ToString()).FirstOrDefault()


                BuildFolderIco(SelectedFolderPath & "\" & i & "\" & i & ".png", rating, isVisible)
                IconProcessedCount += 1
                File.Delete(SelectedFolderPath & "\" & i & "\" & i & ".png") '<--IO Exception here
            End If

            If File.Exists(targetFile) Then
                HideIcons(targetFile)
                'File.Delete(SelectedFolderPath & "\" & i & "\" & i & ".jpg")
                Dim myFolderIcon As New FolderIcon(SelectedFolderPath & "\" & i)
                myFolderIcon.CreateFolderIcon(targetFile, i)

                Dim dirInf As New DirectoryInfo(SelectedFolderPath & "\" & i & "\")
                dirInf.Attributes = dirInf.Attributes Or FileAttributes.Directory
                dirInf.Attributes = dirInf.Attributes Or FileAttributes.ReadOnly
                dirInf.Refresh()
            End If
        Next
    End Sub

    Public sub RefreshIconCache()
        Dim wow64Value As IntPtr = IntPtr.Zero
        Wow64DisableWow64FsRedirection(wow64Value)
        Dim objProcess As Process
        objProcess = New Process()
        objProcess.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.System) & "\ie4uinit.exe"
        objProcess.StartInfo.Arguments = "-ClearIconCache"
        objProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal
        objProcess.Start()
        objProcess.WaitForExit()
        objProcess.Close()
        RestartExplorer()
    End sub

    Public Async Function GetBitmapFromUrlAsync(url As String) As Task(Of Bitmap)
        Dim myRequest = CType(WebRequest.Create(New Uri(url)), HttpWebRequest)
        myRequest.Method = "GET"
        Dim myResponse = CType(Await myRequest.GetResponseAsync().ConfigureAwait(False), HttpWebResponse)
        Dim bmp As New Bitmap(myResponse.GetResponseStream())
        myResponse.Close()
        Return bmp
    End Function

    Public Sub InitPickedListDataTable()
        PickedListDataTable.Columns.Clear()
        PickedListDataTable.Rows.Clear()
        Dim column1 = New DataColumn("Poster") With {
                .DataType = Type.GetType("System.String")
                }
        Dim column2 = New DataColumn("Title") With {
                .DataType = Type.GetType("System.String")
                }
        Dim column3 = New DataColumn("Year") With {
                .DataType = Type.GetType("System.String")
                }
        Dim column4 = New DataColumn("Rating") With {
                .DataType = Type.GetType("System.String")
                }
        Dim column5 = New DataColumn("Folder") With {
                .DataType = Type.GetType("System.String")
                }
        Dim column6 = New DataColumn("FolderName") With {
                .DataType = Type.GetType("System.String")
                }

        PickedListDataTable.Columns.Add(column1)
        PickedListDataTable.Columns.Add(column2)
        PickedListDataTable.Columns.Add(column3)
        PickedListDataTable.Columns.Add(column4)
        PickedListDataTable.Columns.Add(column5)
        PickedListDataTable.Columns.Add(column6)
    End Sub

    Public Sub AddToPickedListDataTable(poster As String, title As String, rating As String, folder As String,
                                        folderName As String, Optional year As String = "")
        Dim nRow As DataRow
        If rating = "0"
            rating = ""
        End If
        nRow = PickedListDataTable.NewRow()
        nRow.Item("Poster") = poster
        nRow.Item("Title") = title
        nRow.Item("Year") = year
        nRow.Item("Rating") = rating
        nRow.Item("Folder") = folder
        nRow.Item("FolderName") = folderName
        PickedListDataTable.Rows.Add(nRow)
    End Sub

    Sub KillExplorer()
        Dim taskKill As ProcessStartInfo = New ProcessStartInfo("taskkill", "/F /IM explorer.exe")
        taskKill.WindowStyle = ProcessWindowStyle.Hidden
        Dim process As Process = New Process()
        process.StartInfo = taskKill
        process.Start()
        process.WaitForExit()
    End Sub

    Private Sub RestartExplorer()
        KillExplorer()
        Process.Start("explorer.exe")
    End Sub
End Module
