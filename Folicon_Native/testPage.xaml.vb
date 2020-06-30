
Imports System.IO
Imports FoliconNative.ViewModel
Imports Notifications.Wpf

Class TestPage
    ReadOnly fileWatcher As New FileSystemWatcher("E:\Movies\") With {
            .NotifyFilter = NotifyFilters.CreationTime _
                                    Or NotifyFilters.DirectoryName _
                                    Or NotifyFilters.Attributes
        }
    Private ReadOnly _notificationManager As New NotificationManager()

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler fileWatcher.Created, AddressOf FolderCreatedEvent
        AddHandler fileWatcher.Renamed, AddressOf FolderRenamedEvent
        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        fileWatcher.EnableRaisingEvents = True
        MessageBox.Show(fileWatcher.Path)

    End Sub
    Private Sub FolderCreatedEvent(ByVal source As Object, ByVal e As FileSystemEventArgs)
        If e.ChangeType = WatcherChangeTypes.Created Then
            Dim content = New NotificationContent With {
     .Title = "New Media found",
     .Message = "Click me! to create icons.",
     .Type = NotificationType.Information
 }
            'Dim clickContent = New NotificationContent With {
            '    .Title = "Clicked!",
            '    .Message = "Window notification was clicked!",
            '    .Type = NotificationType.Success
            '}
            _notificationManager.Show(content, onClick:=New Action(AddressOf OpenApp))

        End If

    End Sub
    Public Sub FolderRenamedEvent(ByVal source As Object, ByVal e As RenamedEventArgs)

        Dim notiman As New NotificationManager()
        notiman.Show(New NotificationContent With {.Title = "Folder Renamed",
                         .Message = e.Name,
                         .Type = NotificationType.Information})

    End Sub
    Private Sub OpenApp()
        Dim mainfrm As New MainWindow()
        mainfrm.Show()
    End Sub
End Class
