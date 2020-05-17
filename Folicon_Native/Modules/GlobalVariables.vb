Imports System.Configuration
Imports System.Data
Imports System.Net.Http

Namespace Modules

    Module GlobalVariables
        Public FolderNameIndex As Integer = 0
        Public SelectedFolderPath As String = Nothing
        Public SearchMod As String = "Movie"
        Public IconMode As String = "Poster Mode"
        Public PickedIndex As Integer = 0
        Public RetryMovieTitle As String = Nothing
        Public Fnames() As String = Nothing
        Public PickedListDataTable As DataTable = New DataTable
        Public Searchresultob As Object
        Public SearchTitle As String = Nothing
        Public ImgDownloadList As New List(Of ImageToDownload)
        Public FolderProcessedCount As Integer = 0
        Public IconProcessedCount As Integer = 0
        Public Apikeygb As String = ConfigurationManager.AppSettings.Get("GBAPI")
        Public ApikeyIgdb As String = ConfigurationManager.AppSettings.Get("IGDBAPI")
        Public ApikeyTmdb As String = ConfigurationManager.AppSettings.Get("TMDBAPI")
        Public ClientSecretDArt As String = ConfigurationManager.AppSettings.Get("DeviantClientSecret")
        Public ClientIdDArt As String = ConfigurationManager.AppSettings.Get("DeviantClientId")
        Public Responseformatgb As String = "json"
        Public Fieldlistgb As String = "*"
        Public HttpC as new HttpClient()
    End Module
End NameSpace