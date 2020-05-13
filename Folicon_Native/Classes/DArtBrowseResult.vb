    Imports Newtonsoft.Json

Public Class Author

        <JsonProperty("userid")>
        Public Property Userid As String

        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("usericon")>
        Public Property Usericon As String

        <JsonProperty("type")>
        Public Property Type As String
    End Class

    Public Class Stats

        <JsonProperty("comments")>
        Public Property Comments As Integer

        <JsonProperty("favourites")>
        Public Property Favourites As Integer
    End Class

    Public Class Content

        <JsonProperty("src")>
        Public Property Src As String

        <JsonProperty("filesize")>
        Public Property Filesize As Integer

        <JsonProperty("height")>
        Public Property Height As Integer

        <JsonProperty("width")>
        Public Property Width As Integer

        <JsonProperty("transparency")>
        Public Property Transparency As Boolean
    End Class

    Public Class Thumb

        <JsonProperty("src")>
        Public Property Src As String

        <JsonProperty("height")>
        Public Property Height As Integer

        <JsonProperty("width")>
        Public Property Width As Integer

        <JsonProperty("transparency")>
        Public Property Transparency As Boolean
    End Class

    Public Class Result

        <JsonProperty("deviationid")>
        Public Property Deviationid As String

        <JsonProperty("printid")>
        Public Property Printid As String

        <JsonProperty("url")>
        Public Property Url As String

        <JsonProperty("title")>
        Public Property Title As String

        <JsonProperty("category")>
        Public Property Category As String

        <JsonProperty("category_path")>
        Public Property CategoryPath As String

        <JsonProperty("is_downloadable")>
        Public Property IsDownloadable As Boolean

        <JsonProperty("is_mature")>
        Public Property IsMature As Boolean

        <JsonProperty("is_favourited")>
        Public Property IsFavourited As Boolean

        <JsonProperty("is_deleted")>
        Public Property IsDeleted As Boolean

        <JsonProperty("author")>
        Public Property Author As Author

        <JsonProperty("stats")>
        Public Property Stats As Stats

        <JsonProperty("published_time")>
        Public Property PublishedTime As Integer

        <JsonProperty("allows_comments")>
        Public Property AllowsComments As Boolean

        <JsonProperty("content")>
        Public Property Content As Content

        <JsonProperty("thumbs")>
        Public Property Thumbs As Thumb()
    End Class

    Public Class DArtBrowseResult

        <JsonProperty("has_more")>
        Public Property HasMore As Boolean

        <JsonProperty("next_offset")>
        Public Property NextOffset As Integer

        <JsonProperty("results")>
        Public Property Results As Result()
    End Class

