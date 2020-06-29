
Imports System.Collections.ObjectModel
Imports System.Net.TMDb
Imports System.Threading

Namespace Modules

    Module TMDB
        Dim posterBase As String = "https://image.tmdb.org/t/p/w200"
        Public Class ResultResponse
            Public Property Result As Object
            Public Property MediaType As String
        End Class

        Public Async Function SearchIt(query As String, serviceClient As ServiceClient) As Task(Of ResultResponse)
            Dim r = Nothing
            Dim mt As String = ""
            If SearchMod = "Movie" Then

                If query.ToLower.Contains("collection") Then
                    r = Await serviceClient.Collections.SearchAsync(query, "en-US", 1, CancellationToken.None)
                    mt = "Collection"
                Else
                    r =
                        Await _
                            serviceClient.Movies.SearchAsync(query, "en-US", False, Nothing, True, 1, CancellationToken.None)
                    mt = "Movie"
                End If
            ElseIf SearchMod = "TV" Then
                r = Await serviceClient.Shows.SearchAsync(query, "en-US", Nothing, True, 1, CancellationToken.None)
                mt = "TV"
            ElseIf SearchMod = "Auto (Movies & TV Shows)" Then
                ' TODO: Write logic to search for all
                Dim iop As Net.TMDb.Resources
                r = Await serviceClient.SearchAsync(query, "en-US", False, 1, CancellationToken.None)
                mt = "Auto (Movies & TV Shows)"
            End If
            Searchresultob = r
            Dim response As New ResultResponse() With {.Result = r, .MediaType = mt}
            Return response
        End Function


        Public Sub ResultPicked(result As Object, resultType As String, pickIndex As Integer)
            If result.Results(pickIndex).Poster Is Nothing Then
                Throw New Exception("NoPoster")
            End If
            Dim localPosterPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) &
                                  ".png"
            Dim folderPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex)
            Dim folderName = Fnames(FolderNameIndex)
            Dim posterUrl = String.Concat("http://image.tmdb.org/t/p/original", result.Results(pickIndex).Poster)
            'If Not String.IsNullOrEmpty(posterUrl)
            If resultType = "TV" Then
                Dim searchResult = DirectCast(result, Shows)
                Dim pickedResult = searchResult.Results(pickIndex)
                AddToPickedListDataTable(localPosterPath,
                                         pickedResult.Name, pickedResult.VoteAverage,
                                         folderPath,
                                         folderName, pickedResult.FirstAirDate.Value.Year)
            ElseIf resultType = "Movie" Then
                Dim searchResult = DirectCast(result, Movies)
                Dim pickedResult = searchResult.Results(pickIndex)

                AddToPickedListDataTable(localPosterPath,
                                         pickedResult.Title, pickedResult.VoteAverage,
                                         folderPath, folderName, pickedResult.ReleaseDate.Value.Year)
            ElseIf resultType = "Collection" Then

                Dim searchResult = DirectCast(result, Collections)
                Dim pickedResult = searchResult.Results(pickIndex)
                AddToPickedListDataTable(localPosterPath,
                                         pickedResult.Name, "",
                                         folderPath, folderName)
            ElseIf resultType = "Auto (Movies & TV Shows)" Then
                Dim mediaType = result.Results(pickIndex).GetType()
                If mediaType.Name = "Show" Then
                    Dim pickedResult = DirectCast(result.Results(pickIndex), Show)
                    AddToPickedListDataTable(localPosterPath,
                                             pickedResult.Name, pickedResult.VoteAverage,
                                             folderPath,
                                             folderName, pickedResult.FirstAirDate.Value.Year)
                ElseIf mediaType.Name = "Movie" Then
                    Dim pickedResult = DirectCast(result.Results(pickIndex), Movie)
                    AddToPickedListDataTable(localPosterPath,
                                             pickedResult.Title, pickedResult.VoteAverage,
                                             folderPath, folderName, pickedResult.ReleaseDate.Value.Year)
                End If
            End If
            FolderProcessedCount += 1
            Dim tempImage As New ImageToDownload() With {
                    .LocalPath = localPosterPath,
                    .RemotePath = posterUrl}
            ImgDownloadList.Add(tempImage)
            'End If
        End Sub

        Public Function ExtractTvDetailsIntoListItem(result As Shows) As ObservableCollection(Of ListItem)
            Dim items As New ObservableCollection(Of ListItem)()
            Dim mediaName As String
            Dim year As String
            Dim rating As String
            Dim overview As String
            Dim poster As String
            For Each item In result.Results
                mediaName = item.Name
                year = If(item.FirstAirDate IsNot Nothing, item.FirstAirDate.Value.Year, "")
                rating = item.VoteAverage
                overview = item.Overview
                poster = If(item.Poster IsNot Nothing, posterBase & item.Poster, Nothing)
                items.Add(New ListItem(mediaName, year, rating, overview, poster))
            Next
            Return items
        End Function

        Public Function ExtractMoviesDetailsIntoListItem(result As Movies) As ObservableCollection(Of ListItem)
            Dim items As New ObservableCollection(Of ListItem)()
            Dim mediaName As String
            Dim year As String
            Dim rating As String
            Dim overview As String
            Dim poster As String
            For Each item In result.Results
                mediaName = item.Title
                year = If(item.ReleaseDate IsNot Nothing, item.ReleaseDate.Value.Year, "")
                rating = item.VoteAverage
                overview = item.Overview
                poster = If(item.Poster IsNot Nothing, posterBase & item.Poster, Nothing)
                items.Add(New ListItem(mediaName, year, rating, overview, poster))
            Next
            Return items
        End Function

        Public Function ExtractResourceDetailsIntoListItem(result As Resources) As ObservableCollection(Of ListItem)
            Dim items As New ObservableCollection(Of ListItem)()
            Dim mediaName = ""
            Dim year = ""
            Dim rating = ""
            Dim overview = ""
            Dim poster = Nothing
            For Each item In result.Results
                Dim mediaType = item.GetType()
                If mediaType.Name = "Show" Then
                    Dim res As Show = item
                    mediaName = res.Name
                    year = If(res.FirstAirDate IsNot Nothing, res.FirstAirDate.Value.Year, "")
                    rating = res.VoteAverage
                    overview = res.Overview
                    poster = If(res.Poster IsNot Nothing, posterBase & res.Poster, Nothing)
                ElseIf mediaType.Name = "Movie" Then
                    Dim res As Movie = item
                    mediaName = res.Title
                    year = If(res.ReleaseDate IsNot Nothing, res.ReleaseDate.Value.Year, "")
                    rating = res.VoteAverage
                    overview = res.Overview
                    poster = If(res.Poster IsNot Nothing, posterBase & res.Poster, Nothing)
                End If

                items.Add(New ListItem(mediaName, year, rating, overview, poster))
            Next
            Return items
        End Function

        Public Function ExtractCollectionDetailsIntoListItem(result As Collections) As ObservableCollection(Of ListItem)
            Dim items As New ObservableCollection(Of ListItem)()
            Dim mediaName As String
            Dim year As String
            Dim rating As String
            Dim overview As String
            Dim poster As String
            For Each item In result.Results
                mediaName = item.Name
                year = ""
                rating = ""
                overview = ""
                poster = If(item.Poster IsNot Nothing, posterBase & item.Poster, Nothing)
                items.Add(New ListItem(mediaName, year, rating, overview, poster))
            Next
            Return items
        End Function
    End Module
End NameSpace