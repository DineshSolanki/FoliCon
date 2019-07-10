Option Strict Off
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.TMDb
Imports System.Threading
Imports Folicon_Native.Model
Imports Ookii.Dialogs.Wpf

Module PublicFunctions

    ''' <summary>
    ''' Creates an array of all Folders Names which do not have an icon assigned 
    ''' </summary>
    ''' 
    Public Function NewFolderBrowseDialog(description As String) As VistaFolderBrowserDialog
        Dim folderBrowser As New VistaFolderBrowserDialog()
        With folderBrowser
            .Description = description
            .UseDescriptionForTitle = True
        End With

        Return folderBrowser


    End Function
    Public Function IsNullOrEmpty(ByVal myStringArray() As String) As Boolean
        Return myStringArray Is Nothing OrElse myStringArray.Length < 1 OrElse myStringArray(0) = ""
    End Function

    Public Sub GetFileNames()
        ' Fnames = Nothing
        If Not String.IsNullOrEmpty(SelectedFolderPath) Then
            Dim foldernames As String = String.Empty
            Dim seprator As String = String.Empty
            For Each folder As String In Directory.GetDirectories(SelectedFolderPath)
                If Not File.Exists(folder & "\" & Path.GetFileName(folder) & ".ico") Then
                    foldernames &= seprator & Path.GetFileName(folder)
                    seprator = ","
                End If
            Next
            Fnames = foldernames.Split(CType(",", Char))
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
                fileStream.Dispose()
            End Using
        End Using
    End Sub

    Public Async Function DownloadImage(ByVal filename As String, ByVal localpath As String, ByVal cancellationToken As CancellationToken) As Task
        If Not File.Exists(localpath) Then
            Dim folder As String = Path.GetDirectoryName(localpath)
            Directory.CreateDirectory(folder)
            Dim storage = New StorageClient()
            Using fileStream = New FileStream(localpath, FileMode.Create, FileAccess.Write, FileShare.None, Short.MaxValue, True)
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
    ''' <param name="filmfolderpath"> Path where to save and where PNG is Downloaded</param>
    ''' <param name="rating"> if Wants to Include rating on Icon</param>
    ''' <param name="IsVisible">Show rating or NOT</param>
    Public Sub BuildFolderIco(filmfolderpath As String, rating As String, IsVisible As String)
        If Not File.Exists(filmfolderpath) Then
            Exit Sub
        End If
        Dim icon As Bitmap
        Using task As Task(Of Bitmap) = Start(Function() New MyMovieCIcon(New MyMovieIconLayout(filmfolderpath, rating, IsVisible)).RenderToBitmap())
            task.Wait()
            icon = task.Result
        End Using
        'Dim icon As Bitmap = CType(System.Drawing.Image.FromFile(filmfolderpath), Bitmap)
        Call New PngToIcoService().Convert(icon, filmfolderpath.Replace("png", "ico"))
        icon.Dispose()
    End Sub

    Public Sub HideIcons(icofile As String)
        ' Set icon file attribute to "Hidden"
        If (File.GetAttributes(icofile) And FileAttributes.Hidden) <> FileAttributes.Hidden Then
            File.SetAttributes(icofile, File.GetAttributes(icofile) Or FileAttributes.Hidden)
        End If

        ' Set icon file attribute to "System"
        If (File.GetAttributes(icofile) And FileAttributes.System) <> FileAttributes.System Then
            File.SetAttributes(icofile, File.GetAttributes(icofile) Or FileAttributes.System)
        End If
    End Sub
    ''' <summary>
    ''' Creates Icons from PNG
    ''' </summary>
    Public Sub MakeIco(Optional ByVal isVisible As String = "Hidden")
        'Try
        Dim foldernames As String = ""
        Dim fnames() As String
        Dim seprator As String = ""
        For Each folder As String In Directory.GetDirectories(SelectedFolderPath)
            foldernames &= seprator & Path.GetFileName(folder)
            seprator = ","
        Next
        fnames = foldernames.Split(",")
        For Each i As String In fnames
            Dim targetfile = SelectedFolderPath & "\" & i & "\" & i & ".ico"
            If File.Exists(SelectedFolderPath & "\" & i & "\" & i & ".png") AndAlso Not File.Exists(targetfile) Then
                Dim Rating As String = PickedListDataTable.AsEnumerable().Where(Function(p) p("FolderName").ToString() = i).Select(Function(p) p("Rating").ToString()).FirstOrDefault()


                BuildFolderIco(SelectedFolderPath & "\" & i & "\" & i & ".png", Rating, isVisible)
                IconProcessedCount += 1
                File.Delete(SelectedFolderPath & "\" & i & "\" & i & ".png") '<--IO Exception here
            End If

            If File.Exists(targetfile) Then
                HideIcons(targetfile)
                'File.Delete(SelectedFolderPath & "\" & i & "\" & i & ".jpg")
                Dim myFolderIcon As New FolderIcon(SelectedFolderPath & "\" & i)
                myFolderIcon.CreateFolderIcon(targetfile, i)

                Dim dirInf As New DirectoryInfo(SelectedFolderPath & "\" & i & "\")
                dirInf.Refresh()
            End If
        Next


        'MessageBox.Show("Done!", "Icon(s) Created")
    End Sub
    Public Sub InitPickedListDataTable()
        PickedListDataTable.Columns.Clear()
        PickedListDataTable.Rows.Clear()
        Dim column1 As DataColumn = New DataColumn("Poster") With {
            .DataType = Type.GetType("System.String")
        }
        Dim column2 As DataColumn = New DataColumn("Title") With {
            .DataType = Type.GetType("System.String")
        }
        Dim column3 As DataColumn = New DataColumn("Year") With {
            .DataType = Type.GetType("System.String")
        }
        Dim column4 As DataColumn = New DataColumn("Rating") With {
            .DataType = Type.GetType("System.String")
        }
        Dim column5 As DataColumn = New DataColumn("Folder") With {
            .DataType = Type.GetType("System.String")
        }
        Dim column6 As DataColumn = New DataColumn("FolderName") With {
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
