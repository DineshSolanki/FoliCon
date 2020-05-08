Imports Newtonsoft.Json

Public Class Url

    <JsonProperty("type")>
    Public Property Type As String

    <JsonProperty("template")>
    Public Property Template As String
End Class

Public Class Request

    <JsonProperty("title")>
    Public Property Title As String

    <JsonProperty("totalResults")>
    Public Property TotalResults As String

    <JsonProperty("searchTerms")>
    Public Property SearchTerms As String

    <JsonProperty("count")>
    Public Property Count As Integer

    <JsonProperty("startIndex")>
    Public Property StartIndex As Integer

    <JsonProperty("inputEncoding")>
    Public Property InputEncoding As String

    <JsonProperty("outputEncoding")>
    Public Property OutputEncoding As String

    <JsonProperty("safe")>
    Public Property Safe As String

    <JsonProperty("cx")>
    Public Property Cx As String

    <JsonProperty("fileType")>
    Public Property FileType As String

    <JsonProperty("searchType")>
    Public Property SearchType As String
End Class

Public Class NextPage

    <JsonProperty("title")>
    Public Property Title As String

    <JsonProperty("totalResults")>
    Public Property TotalResults As String

    <JsonProperty("searchTerms")>
    Public Property SearchTerms As String

    <JsonProperty("count")>
    Public Property Count As Integer

    <JsonProperty("startIndex")>
    Public Property StartIndex As Integer

    <JsonProperty("inputEncoding")>
    Public Property InputEncoding As String

    <JsonProperty("outputEncoding")>
    Public Property OutputEncoding As String

    <JsonProperty("safe")>
    Public Property Safe As String

    <JsonProperty("cx")>
    Public Property Cx As String

    <JsonProperty("fileType")>
    Public Property FileType As String

    <JsonProperty("searchType")>
    Public Property SearchType As String
End Class

Public Class Queries

    <JsonProperty("request")>
    Public Property Request As Request()

    <JsonProperty("nextPage")>
    Public Property NextPage As NextPage()
End Class

Public Class Context

    <JsonProperty("title")>
    Public Property Title As String
End Class

Public Class SearchInformation

    <JsonProperty("searchTime")>
    Public Property SearchTime As Double

    <JsonProperty("formattedSearchTime")>
    Public Property FormattedSearchTime As String

    <JsonProperty("totalResults")>
    Public Property TotalResults As String

    <JsonProperty("formattedTotalResults")>
    Public Property FormattedTotalResults As String
End Class

Public Class Image

    <JsonProperty("contextLink")>
    Public Property ContextLink As String

    <JsonProperty("height")>
    Public Property Height As Integer

    <JsonProperty("width")>
    Public Property Width As Integer

    <JsonProperty("byteSize")>
    Public Property ByteSize As Integer

    <JsonProperty("thumbnailLink")>
    Public Property ThumbnailLink As String

    <JsonProperty("thumbnailHeight")>
    Public Property ThumbnailHeight As Integer

    <JsonProperty("thumbnailWidth")>
    Public Property ThumbnailWidth As Integer
End Class

Public Class Item

    <JsonProperty("kind")>
    Public Property Kind As String

    <JsonProperty("title")>
    Public Property Title As String

    <JsonProperty("htmlTitle")>
    Public Property HtmlTitle As String

    <JsonProperty("link")>
    Public Property Link As String

    <JsonProperty("displayLink")>
    Public Property DisplayLink As String

    <JsonProperty("snippet")>
    Public Property Snippet As String

    <JsonProperty("htmlSnippet")>
    Public Property HtmlSnippet As String

    <JsonProperty("mime")>
    Public Property Mime As String

    <JsonProperty("image")>
    Public Property Image As Image
End Class

Public Class GoogleResult

    <JsonProperty("kind")>
    Public Property Kind As String

    <JsonProperty("url")>
    Public Property Url As Url

    <JsonProperty("queries")>
    Public Property Queries As Queries

    <JsonProperty("context")>
    Public Property Context As Context

    <JsonProperty("searchInformation")>
    Public Property SearchInformation As SearchInformation

    <JsonProperty("items")>
    Public Property Items As Item()
End Class


