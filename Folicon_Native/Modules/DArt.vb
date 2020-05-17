Imports System.Configuration
Imports Newtonsoft.Json

Namespace Modules
    Public Module DArt
        Private Async Function GenerateNewAccessToken() As Task(of string)
            Dim clientAccessToken = ""
            Dim url = "https://www.deviantart.com/oauth2/token?client_id=" & ClientIdDArt &
                      "&client_secret=" & ClientSecretDArt &
                      "&grant_type=client_credentials"
            Using response = Await HttpC.GetAsync(New Uri(url))
                Dim jsonData = Await response.Content.ReadAsStringAsync()
                dim tokenResponse = JsonConvert.DeserializeObject (Of DArtTokenResponse)(jsonData)
                dim config As Configuration =
                        ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                clientAccessToken = tokenResponse.AccessToken
                config.AppSettings.Settings("Token").Value = tokenResponse.AccessToken
                config.Save(ConfigurationSaveMode.Modified)
                ConfigurationManager.RefreshSection("appSettings")
            End Using
            Return clientAccessToken
        End Function

        public Async function GetClientAccessTokenAsync() As Task(of string)
            Dim clientAccessToken As String = ConfigurationManager.AppSettings.Get("Token")
            If Not string.IsNullOrEmpty(clientAccessToken)
                If not Await IsTokenValidAsync(clientAccessToken)
                    clientAccessToken = Await GenerateNewAccessToken()
                End If
            Else
                clientAccessToken = Await GenerateNewAccessToken()
            End If
            Return clientAccessToken
        end Function

        public Async function IsTokenValidAsync(clientAccessToken As string) As Task(of Boolean)
            Dim url = "https://www.deviantart.com/api/v1/oauth2/placebo?access_token=" & clientAccessToken
            Dim tokenResponse As DArtTokenResponse
            using response = Await httpC.GetAsync(New Uri(url))
                Dim jsonData = Await response.Content.ReadAsStringAsync()
                tokenResponse = JsonConvert.DeserializeObject (Of DArtTokenResponse)(jsonData)
            End Using
            If tokenResponse.Status = "success"
                Return true
            Else
                Return false
            End If
        End function

        public Async Function Browse(accessToken As string, query As string,Optional offset As Integer=0) As Task(Of DArtBrowseResult)
            Dim url ="https://www.deviantart.com/api/v1/oauth2/browse/popular?timerange=alltime&offset=" & offset & "&category_path=customization%2ficons%2fos%2fwin&q=" &
                    query & " folder icon" & "&limit=20&access_token=" & accessToken
            Dim result As DArtBrowseResult
            Using response = Await HttpC.GetAsync(New Uri(url))
                Dim jsonData = Await response.Content.ReadAsStringAsync()
                dim serializerSettings = new JsonSerializerSettings _
                        With{ .NullValueHandling = NullValueHandling.Ignore }
                result = JsonConvert.DeserializeObject (Of DArtBrowseResult)(jsonData, serializerSettings)
            End Using
            Return result
        End Function
    End Module
End Namespace
