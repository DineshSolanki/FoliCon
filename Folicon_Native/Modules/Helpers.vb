Option Strict Off

Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Windows.Interop
Imports FoliconNative.Model
Imports Ookii.Dialogs.Wpf
Imports Xceed.Wpf.Toolkit

Namespace Modules
    Module Helpers
        Public Function LoadBitmap(source As Bitmap) As BitmapSource
            Dim ip As IntPtr = source.GetHbitmap()
            Dim bs As BitmapSource

            Try
                bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                                                           BitmapSizeOptions.FromEmptyOptions())
            Finally
                Dim unused = DeleteObject(ip)
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
                        separator = "█" 'ALT+219
                    End If
                Next
                Fnames = folderNames.Split(CType("█", Char))
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
        ''' Async function That can Download image from any URL and save to local path
        ''' </summary>
        ''' <param name="url"> The URL of Image to Download</param>
        ''' <param name="saveFilename">The Local Path Of Downloaded Image</param>
        Public Async Function DownloadImageFromUrlAsync(url As String, saveFileName As String) As Task
            Dim response = Await HttpC.GetAsync(New Uri(url))
            Using fs As New FileStream(saveFileName, FileMode.Create)
                Await response.Content.CopyToAsync(fs)
            End Using
        End Function

        ''' <summary>
        ''' Converts From PNG to ICO
        ''' </summary>
        ''' <param name="filmFolderPath"> Path where to save and where PNG is Downloaded</param>
        ''' <param name="rating"> if Wants to Include rating on Icon</param>
        ''' <param name="ratingVisibility">Show rating or NOT</param>
        Public Sub BuildFolderIco(filmFolderPath As String, rating As String, ratingVisibility As String, mockupVisibility As String)
            If Not File.Exists(filmFolderPath) Then
                Exit Sub
            End If
            ratingVisibility = If(String.IsNullOrEmpty(rating), "Hidden", ratingVisibility)
            If Not String.IsNullOrEmpty(rating) AndAlso Not rating = "10" Then
                rating = If(Not rating.Contains("."), rating & ".0", rating)
            End If
            Dim icon As Bitmap
            If IconMode = "Professional" Then
                icon = New ProIcon(filmFolderPath).RenderToBitmap()
            Else
                Using _
                    task As Task(Of Bitmap) =
                        Start(
                            Function() _
                                 New MyMovieCIcon(New MyMovieIconLayout(filmFolderPath, rating, ratingVisibility, mockupVisibility)).
                                 RenderToBitmap())
                    task.Wait()
                    icon = task.Result
                End Using
            End If

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
        Public Sub MakeIco(Optional ByVal isRatingVisible As Boolean = False, Optional ByVal isMockupVisible As Boolean = True)
            'Try
            Dim ratingVisibility As String = If(isRatingVisible, "visible", "hidden")
            Dim mockupVisibility As String = If(isMockupVisible, "visible", "hidden")
            Dim folderNames = ""
            Dim fNames() As String
            Dim separator = ""
            For Each folder As String In Directory.GetDirectories(SelectedFolderPath)
                folderNames &= separator & Path.GetFileName(folder)
                separator = "█"
            Next
            fNames = folderNames.Split("█")
            For Each i As String In fNames
                Dim tempI = i
                Dim targetFile = SelectedFolderPath & "\" & i & "\" & i & ".ico"
                If File.Exists(SelectedFolderPath & "\" & i & "\" & i & ".png") AndAlso Not File.Exists(targetFile) Then
                    Dim rating As String =
                            PickedListDataTable.AsEnumerable().Where(Function(p) p("FolderName").ToString() = tempI).
                            Select(
                                Function(p) p("Rating").ToString()).FirstOrDefault()


                    BuildFolderIco(SelectedFolderPath & "\" & i & "\" & i & ".png", rating, ratingVisibility, mockupVisibility)
                    IconProcessedCount += 1
                    File.Delete(SelectedFolderPath & "\" & i & "\" & i & ".png") '<--IO Exception here
                End If

                If File.Exists(targetFile) Then
                    HideIcons(targetFile)
                    SetFolderIcon(i & ".ico", SelectedFolderPath & "\" & i)
                End If
            Next
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        End Sub

        Public Sub RefreshIconCache()
            Dim wow64Value As IntPtr = IntPtr.Zero
            Wow64DisableWow64FsRedirection(wow64Value)
            Dim objProcess As Process
            objProcess = New Process()
            objProcess.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.System) &
                                            "\ie4uinit.exe"
            objProcess.StartInfo.Arguments = "-ClearIconCache"
            objProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal
            objProcess.Start()
            objProcess.WaitForExit()
            objProcess.Close()
            RestartExplorer()
        End Sub

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
            If rating = "0" Then
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
            Dim taskKill As ProcessStartInfo = New ProcessStartInfo("taskkill", "/F /IM explorer.exe") With {
                .WindowStyle = ProcessWindowStyle.Hidden
            }
            Dim process As Process = New Process With {
                .StartInfo = taskKill
            }
            process.Start()
            process.WaitForExit()
            process.Dispose()
        End Sub

        Private Sub RestartExplorer()
            KillExplorer()
            Process.Start("explorer.exe")
        End Sub
        Public Sub WriteIDtoFile(ByVal ID As Integer, ByVal folderPath As String)
            Dim fileName As String = Path.Combine(folderPath, "id.folicon")
            Using sw As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter _
                (fileName, True)
                sw.WriteLine(ID)
            End Using
            If File.Exists(fileName) Then
                HideIcons(fileName)
            End If
        End Sub
        Public Function GetID(folderPath As String) As Integer
            Dim fileName As String = Path.Combine(folderPath, "id.folicon")
            If File.Exists(fileName) Then
                Using sr As StreamReader = My.Computer.FileSystem.OpenTextFileReader _
                (fileName)
                    Return sr.ReadLine()
                End Using
            End If
            Return Nothing
        End Function
        Public Sub DeleteIconsFromPath(folderPath As String)
            For Each folder In Directory.EnumerateDirectories(folderPath)
                Dim foldername = Path.GetFileNameWithoutExtension(folder)
                Dim IcoFile = Path.Combine(folder, foldername & ".ico")
                Dim IniFile = Path.Combine(folder, "desktop.ini")
                File.Delete(IcoFile)
                File.Delete(IniFile)
            Next
        End Sub
        Public Function GetFileNamesFromFolder(ByVal folder As String) As ArrayList
            Dim itemList As New ArrayList()
            If Not String.IsNullOrEmpty(folder) Then
                For Each file As String In Directory.GetFiles(folder)
                    itemList.Add(Path.GetFileName(file))
                Next
            End If
            Return itemList
        End Function
        ''' <summary>
        ''' Set folder icon for a given folder.
        ''' </summary>
        ''' <param name="icoFile"> path to the icon file [MUST BE .Ico]</param>
        ''' <param name="FolderPath">path to the folder</param>
        Private Sub SetFolderIcon(ByVal icoFile As String, ByVal FolderPath As String)
            Try
                Dim FolderSettings As New LPSHFOLDERCUSTOMSETTINGS With {
                    .dwMask = &H10,
                    .pszIconFile = icoFile
                }
                'FolderSettings.iIconIndex = 0;

                Dim FCS_READ As UInteger = &H1
                Dim FCS_FORCEWRITE As UInteger = &H2
                Dim FCS_WRITE As UInteger = FCS_READ Or FCS_FORCEWRITE

                Dim pszPath As String = FolderPath
                Dim HRESULT As UInteger = SHGetSetFolderCustomSettings(FolderSettings, pszPath, FCS_FORCEWRITE)
            Catch ex As Exception
                ' log exception
            End Try
            ApplyChanges(FolderPath)
        End Sub


        Public Sub ApplyChanges(folderPath As String)
            Dim pidl = ILCreateFromPath(folderPath)
            SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_FLUSHNOWAIT, pidl, Nothing)
        End Sub

    End Module
End Namespace