Imports System.Configuration

Public Class ApiConfig
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        txtTMDBApi.Text=ConfigurationManager.AppSettings.Get("TMDBAPI")
        TxtClientSecret.Text=ConfigurationManager.AppSettings.Get("DeviantClientSecret")
        TxtClientID.Text=ConfigurationManager.AppSettings.Get("DeviantClientId")
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As RoutedEventArgs) Handles BtnSave.Click
        Dim tmdbApi As String=txtTMDBApi.text
        Dim clientSecret As String=TxtClientSecret.Text
        Dim clientId As String= TxtClientID.Text
        If Not String.IsNullOrEmpty(tmdbApi) AndAlso Not String.IsNullOrEmpty(clientSecret) AndAlso Not String.IsNullOrEmpty(clientId)
            dim config As Configuration =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
            config.AppSettings.Settings("TMDBAPI").Value = tmdbApi
            config.AppSettings.Settings("DeviantClientSecret").Value = clientSecret
            config.AppSettings.Settings("DeviantClientId").Value = clientId
            config.Save(ConfigurationSaveMode.Modified)
            ConfigurationManager.RefreshSection("appSettings")
            Else 
                Xceed.Wpf.Toolkit.MessageBox.Show("All values are required!","Cannot Save configuration",MessageBoxButton.OK,MessageBoxImage.Error)
        End If
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnClose.Click
        close
    End Sub
End Class
