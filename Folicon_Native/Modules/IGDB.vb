
Imports System.Collections.ObjectModel
Imports IGDB
Imports IGDB.Models

Namespace Modules
    Public Class Igdbf
        Public Shared Async Function SearchGame(query As String, client As IGDBApi) As Task(Of Game())
            Contracts.Contract.Assert(client IsNot Nothing)
            Dim response = Await client.QueryAsync(Of Game)(IGDB.Client.Endpoints.Games, "search " & """" & query & """" & "; fields name,first_release_date,total_rating,summary,cover.*;")
            Searchresultob = response
            Return response
        End Function
        Public shared sub ResultPicked(result As Game)
            Contracts.Contract.requires(result Isnot nothing)
            If result.Cover Is Nothing
                throw New Exception("NoPoster")
            End If
            Dim localPosterPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex) & "\" & Fnames(FolderNameIndex) &
                                  ".png"
            Dim folderPath = SelectedFolderPath & "\" & Fnames(FolderNameIndex)
            Dim folderName = Fnames(FolderNameIndex)
            Dim year=If(result.FirstReleaseDate IsNot Nothing,result.FirstReleaseDate.Value.Year,"")
            Dim posterUrl = ImageHelper.GetImageUrl(result.Cover.Value.ImageId, ImageSize.HD720)
            AddToPickedListDataTable(localPosterPath,result.Name,"",folderPath,folderName,year)
            FolderProcessedCount += 1
            Dim tempImage As New ImageToDownload() With{
                    .LocalPath=localPosterPath,
                    .RemotePath="https://" & posterUrl.Substring(2) }
            ImgDownloadList.Add(tempImage)
        End sub
        public Shared Function ExtractGameDetailsIntoListItem(result As Game()) As ObservableCollection(Of ListItem)
            Contracts.Contract.Requires(result IsNot nothing)
            Dim items As New ObservableCollection(Of ListItem)()
            dim mediaName as String
            dim year As String
            Dim rating as String
            Dim overview As String
            Dim poster As String
            For Each item In result
                mediaName = item.Name
                year = If(item.FirstReleaseDate Isnot Nothing, item.FirstReleaseDate.Value.Year, "")
                'rating = If(item.TotalRating Isnot Nothing, item.TotalRating,"") 
                overview = item.Summary
                poster = If(item.Cover IsNot Nothing, "https://" & ImageHelper.GetImageUrl(item.Cover.Value.ImageId, ImageSize.HD720).Substring(2), Nothing)
                items.Add(New ListItem(mediaName, year, "", overview, poster))
            Next
            Return items
            End Function
    End Class
End NameSpace