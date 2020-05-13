Imports Newtonsoft.Json

Public Class DArtTokenResponse

        <JsonProperty("access_token")>
        Public Property AccessToken As String

        <JsonProperty("token_type")>
        Public Property TokenType As String

        <JsonProperty("expires_in")>
        Public Property ExpiresIn As Integer

        <JsonProperty("error")>
        Public Property ErrorType As String

        <JsonProperty("error_description")>
        Public Property ErrorDescription As String

        <JsonProperty("status")>
        Public Property Status As String
End Class
