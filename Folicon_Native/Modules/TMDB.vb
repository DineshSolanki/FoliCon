Imports System.Collections.ObjectModel
Imports System.Net.TMDb
Imports System.Threading

Module TMDB
    public class resultResponse
        public Property Result As Object
        public Property MediaType As string
    End Class

    public Async Function SearchIt(query As string, serviceClient As ServiceClient) As Task(Of resultResponse)
        Dim isAutoPicked as Boolean=false
        Dim r As Object : Dim mt As string
        If SearchMod = "Movie"
            If query.ToLower.Contains("collection")
                r=Await serviceClient.Collections.SearchAsync(query, "en-US", 1, CancellationToken.none)
                mt="Collection"
            Else
               r= Await serviceClient.Movies.SearchAsync(query, "en-US", false, Nothing, True, 1, CancellationToken.none)
                mt="Movie"
            End If
        ElseIf SearchMod = "TV"
            r=Await serviceClient.Shows.SearchAsync(query, "en-US", Nothing, True, 1, CancellationToken.None)
            mt="TV"
        ElseIf SearchMod="All"
            ' TODO: Write logic to search for all
        End If
        Searchresultob=r
        Dim response As New resultResponse() With {.Result = r, .MediaType = mt}
        Return response
    End Function


    Public Sub ResultPicked(result As Object, resultType As String, pickIndex As integer)
        Dim localPosterPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) &
                              ".png"
        Dim folderPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex)
        Dim folderName = Fnames(FolderNameIndex)
        Dim posterUrl = String.Concat("http://image.tmdb.org/t/p/original", result.Results(pickIndex).Poster)
        If Not String.IsNullOrEmpty(posterUrl)
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
                'ElseIf  resultType="All"
                '    Dim searchResult=DirectCast(result,Resources)
                '    Dim pickedResult=searchResult.Results(pickIndex)
                '    AddToPickedListDataTable(localPosterPath,
                '                             ,pickedResult.VoteAverage,
                '                             folderPath,
                '                             folderName,pickedResult.FirstAirDate)
            End If
            FolderProcessedCount += 1
            Dim tempImage As New ImageToDownload() With{
                    .LocalPath=localPosterPath,
                    .RemotePath=posterUrl}
            ImgDownloadList.Add(tempImage)
        End If
    End Sub
    public Function ExtractTvDetailsIntoListItem(result As shows) As ObservableCollection(Of ListItem)
        Dim items As New ObservableCollection(Of ListItem)()
        dim mediaName as String
        dim year As String
        Dim rating as String
        Dim overview as String
        For Each item In result.Results
            mediaName=item.Name
            year=item.FirstAirDate.Value.Year
            rating=item.VoteAverage
            overview=item.Overview
            items.Add(new ListItem(mediaName,year,rating,overview))
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
            mediaName=item.Title
            year=If(item.ReleaseDate Isnot Nothing, item.ReleaseDate.Value.Year, "") 
            rating=item.VoteAverage
            overview=item.Overview
            items.Add(new ListItem(mediaName,year,rating,overview))
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
            mediaName=item.Name
            year=""
            rating=""
            overview=""
            items.Add(new ListItem(mediaName,year,rating,overview))
        Next
        Return items
    End Function
End Module
