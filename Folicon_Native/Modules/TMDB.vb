
Imports System.Collections.ObjectModel
Imports System.Net.TMDb
Imports System.Threading

Module TMDB
    public class resultResponse
        public Property Result As Object
        public Property MediaType As string
    End Class

    public Async Function SearchIt(query As string, serviceClient As ServiceClient) As Task(Of resultResponse)
        Dim r
        Dim mt As string
        If SearchMod = "Movie"
            If query.ToLower.Contains("collection")
                r = Await serviceClient.Collections.SearchAsync(query, "en-US", 1, CancellationToken.none)
                mt = "Collection"
            Else
                r =
                    Await _
                        serviceClient.Movies.SearchAsync(query, "en-US", false, Nothing, True, 1, CancellationToken.none)
                mt = "Movie"
            End If
        ElseIf SearchMod = "TV"
            r = Await serviceClient.Shows.SearchAsync(query, "en-US", Nothing, True, 1, CancellationToken.None)
            mt = "TV"
        ElseIf SearchMod = "Auto (Movies & TV Shows)"
            ' TODO: Write logic to search for all
            Dim iop As Net.TMDb.Resources
            r = await serviceClient.SearchAsync(query, "en-US", False, 1, CancellationToken.none)
            mt = "Auto (Movies & TV Shows)"
        End If
        Searchresultob = r
        Dim response As New resultResponse() With {.Result = r, .MediaType = mt}
        Return response
    End Function


    Public Sub ResultPicked(result As Object, resultType As String, pickIndex As integer)
        If result.Results(pickIndex).Poster Is nothing
            throw New Exception("NoPoster")
        End If
        Dim localPosterPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) &
                              ".png"
        Dim folderPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex)
        Dim folderName = Fnames(FolderNameIndex)
        Dim posterUrl = String.Concat("http://image.tmdb.org/t/p/original", result.Results(pickIndex).Poster)
        'If Not String.IsNullOrEmpty(posterUrl)
        If resultType = "TV"
            Dim searchResult = DirectCast(result, Shows)
            Dim pickedResult = searchResult.Results(pickIndex)
            AddToPickedListDataTable(localPosterPath,
                                     pickedResult.Name, pickedResult.VoteAverage,
                                     folderPath,
                                     folderName, pickedResult.FirstAirDate.Value.Year)
        ElseIf resultType = "Movie"
            Dim searchResult = DirectCast(result, Movies)
            Dim pickedResult = searchResult.Results(pickIndex)

            AddToPickedListDataTable(localPosterPath,
                                     pickedResult.Title, pickedResult.VoteAverage,
                                     folderPath, folderName, pickedResult.ReleaseDate.Value.Year)
        ElseIf resultType = "Collection"

            Dim searchResult = DirectCast(result, Collections)
            Dim pickedResult = searchResult.Results(pickIndex)
            AddToPickedListDataTable(localPosterPath,
                                     pickedResult.Name, "",
                                     folderPath, folderName)
            ElseIf resultType = "Auto (Movies & TV Shows)"
                    Dim mediaType = result.Results(pickIndex).GetType()
                    If mediaType.Name = "Show"
                        Dim pickedResult = DirectCast(result.Results(pickIndex), show)
                        AddToPickedListDataTable(localPosterPath,
                                                 pickedResult.Name, pickedResult.VoteAverage,
                                                 folderPath,
                                                 folderName, pickedResult.FirstAirDate.Value.Year)
                    Elseif mediaType.name = "Movie"
                        Dim pickedResult = DirectCast(result.Results(pickIndex), Movie)
                        AddToPickedListDataTable(localPosterPath,
                                                 pickedResult.Title, pickedResult.VoteAverage,
                                                 folderPath, folderName, pickedResult.ReleaseDate.Value.Year)
                    End If
        End If
        FolderProcessedCount += 1
        Dim tempImage As New ImageToDownload() With{
                .LocalPath=localPosterPath,
                .RemotePath=posterUrl}
        ImgDownloadList.Add(tempImage)
        'End If
    End Sub

    public Function ExtractTvDetailsIntoListItem(result As shows) As ObservableCollection(Of ListItem)
        Dim items As New ObservableCollection(Of ListItem)()
        dim mediaName as String
        dim year As String
        Dim rating as String
        Dim overview as String
        For Each item In result.Results
            mediaName = item.Name
            year = If(item.FirstAirDate Isnot Nothing, item.FirstAirDate.Value.Year, "")
            rating = item.VoteAverage
            overview = item.Overview
            items.Add(new ListItem(mediaName, year, rating, overview))
        Next
        Return items
    End Function

    public Function ExtractMoviesDetailsIntoListItem(result As Movies) As ObservableCollection(Of ListItem)
        Dim items As New ObservableCollection(Of ListItem)()
        dim mediaName as String
        dim year As String
        Dim rating as String
        Dim overview as String
        For Each item In result.Results
            mediaName = item.Title
            year = If(item.ReleaseDate Isnot Nothing, item.ReleaseDate.Value.Year, "")
            rating = item.VoteAverage
            overview = item.Overview
            items.Add(new ListItem(mediaName, year, rating, overview))
        Next
        Return items
    End Function

    public Function ExtractResourceDetailsIntoListItem(result As Resources) As ObservableCollection(Of ListItem)
        Dim items As New ObservableCollection(Of ListItem)()
        dim mediaName = ""
        dim year = ""
        Dim rating = ""
        Dim overview = ""
        For Each item In result.Results
            Dim mediaType = item.GetType()
            If mediaType.Name = "Show"
                Dim res As Show = item
                mediaName = res.Name
                year = If(res.FirstAirDate Isnot Nothing, res.FirstAirDate.Value.Year, "")
                rating = res.VoteAverage
                overview = res.Overview
            ElseIf mediaType.Name = "Movie"
                Dim res As Movie = item
                mediaName = res.Title
                year = If(res.ReleaseDate Isnot Nothing, res.ReleaseDate.Value.Year, "")
                rating = res.VoteAverage
                overview = res.Overview
                'ElseIf mediaType.Name="Collection"
                '        Dim res As Collection=item
                '        mediaName = res.Name
                '        year = ""
                '        rating = ""
                '        overview = ""
            End If

            items.Add(new ListItem(mediaName, year, rating, overview))
        Next
        Return items
    End Function

    public Function ExtractCollectionDetailsIntoListItem(result As Collections) As ObservableCollection(Of ListItem)
        Dim items As New ObservableCollection(Of ListItem)()
        dim mediaName as String
        dim year As String
        Dim rating as String
        Dim overview as String
        For Each item In result.Results
            mediaName = item.Name
            year = ""
            rating = ""
            overview = ""
            items.Add(new ListItem(mediaName, year, rating, overview))
        Next
        Return items
    End Function
End Module
