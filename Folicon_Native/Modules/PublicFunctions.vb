Option Strict Off

Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Net.TMDb
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Interop
Imports Folicon_Native.Model
Imports Newtonsoft.Json.Linq
Imports Ookii.Dialogs.Wpf
Imports Xceed.Wpf.Toolkit

Module PublicFunctions
    <DllImport("gdi32")>
    Private Function DeleteObject(o As IntPtr) As Integer

    End Function


    Public Function LoadBitmap(source As Bitmap) As BitmapSource
        Dim ip As IntPtr = source.GetHbitmap()
        Dim bs As BitmapSource

        Try
            bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
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
    Public Async Function PerformActualSearch(title As String) As Task(Of Object)
        Dim http = New HttpClient()
        If SearchMod = "Game" Then
            http.BaseAddress = New Uri("http://www.giantbomb.com/api/")
        Else
            http.BaseAddress = New Uri("http://api.themoviedb.org/3/")
        End If
        Dim url As String = Nothing
        If SearchMod = "Movie" Then
            If title.ToLower.Contains("collection") Then
                url = "search/collection?api_key=" & APIkeyTMDB & "&language=en-US&query=" & title & "&page=1"
            Else
                url = "search/movie?api_key=" & APIkeyTMDB & "&language=en-US&query=" & title
            End If
        ElseIf SearchMod = "TV" Then
            url = "search/tv?api_key=" & APIkeyTMDB & "&language=en-US&query=" & title
        ElseIf SearchMod = "Auto" Then
            url = "search/multi?api_key=" & APIkeyTMDB & "&language=en-US&query=" & title
        ElseIf SearchMod = "Game" Then
            url = "search?api_key=" & APIkeygb & "&format=" & Responseformatgb & "&query=" & title & "&field_list=" & Fieldlistgb
        End If

        Using Response
            Try
                Response = Await http.GetAsync(url)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                Return -1
            End Try
            Dim jsonData = Await Response.Content.ReadAsStringAsync()
            Searchresultob = JValue.Parse(jsonData)
        End Using
        Return Searchresultob


    End Function
    Public Sub ResultPicked(pickedIndex As Integer)
        If Searchresultob.Item("results")(pickedIndex).Item("poster_path").ToString IsNot "null" Then

            If Not Fnames(FolderNameIndex).ToLower.Contains("collection") Then
                Dim releaseDate = CDate(Searchresultob.Item("results")(pickedIndex).Item(DateProperty).ToString)
                AddToPickedListDataTable(SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) & ".png", Searchresultob.Item("results")(pickedIndex).Item(INameProperty), Searchresultob.Item("results")(pickedIndex).Item("vote_average"), SelectedFolderPath & "\" & Fnames(FolderNameIndex), Fnames(FolderNameIndex), releaseDate.Year.ToString)
            Else
                AddToPickedListDataTable(SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) & ".png", Searchresultob.Item("results")(pickedIndex).Item(INameProperty), Searchresultob.Item("results")(pickedIndex).Item("vote_average"), SelectedFolderPath & "\" & Fnames(FolderNameIndex), Fnames(FolderNameIndex))
            End If

            FolderProcessedCount += 1

            Dim image1 As New ImageToDownload()
            With image1
                .LocalPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) & ".png"
                If IconMode = "Poster" Then
                    .RemotePath = "https://image.tmdb.org/t/p/w500/" & Searchresultob.Item("results")(pickedIndex).Item("poster_path").ToString
                Else
                    .RemotePath = GoogleURl
                End If


            End With
            ImgDownloadList.Add(image1)
        Else
            MessageBox.Show("sorry, No Poster Found, Please try in Professional Mode")
        End If

    End Sub
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
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 Or SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12
        Dim httpWebRequest = DirectCast(WebRequest.Create(url), HttpWebRequest)
        Dim httpWebResponse = DirectCast(httpWebRequest.GetResponse(), HttpWebResponse)
        If (httpWebResponse.StatusCode <> HttpStatusCode.OK AndAlso httpWebResponse.StatusCode <> HttpStatusCode.Moved AndAlso httpWebResponse.StatusCode <> HttpStatusCode.Redirect) OrElse Not httpWebResponse.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase) Then
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

    Public Async Function DownloadImage(filename As String, localPath As String, cancellationToken As CancellationToken) As Task
        If Not File.Exists(localPath) Then
            Dim folder As String = Path.GetDirectoryName(localPath)
            Directory.CreateDirectory(folder)
            Dim storage = New StorageClient()
            Using fileStream = New FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, Short.MaxValue, True)
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
        Dim icon As Bitmap
        Using task As Task(Of Bitmap) = Start(Function() New MyMovieCIcon(New MyMovieIconLayout(filmFolderPath, rating, isVisible)).RenderToBitmap())
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
                dirInf.Refresh()
            End If
        Next
    End Sub
    Public Function GetBitmapFromUrl(url As String) As Bitmap
        Dim myRequest = CType(WebRequest.Create(url), HttpWebRequest)
        myRequest.Method = "GET"
        Dim myResponse = CType(myRequest.GetResponse(), HttpWebResponse)
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
    Public Sub AddToPickedListDataTable(poster As String, title As String, rating As String, folder As String, folderName As String, Optional year As String = "")
        Dim nRow As DataRow
        nRow = PickedListDataTable.NewRow()
        nRow.Item("Poster") = poster
        nRow.Item("Title") = title
        nRow.Item("Year") = year
        nRow.Item("Rating") = rating
        nRow.Item("Folder") = folder
        nRow.Item("FolderName") = folderName
        PickedListDataTable.Rows.Add(nRow)
    End Sub
End Module
