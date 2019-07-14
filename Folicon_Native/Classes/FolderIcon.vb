'-----------------------------------------------------------------------------
'File:           FolderIcon.cs
'Copyright:      (c) 2005, Evan Stone, All Rights Reserved
'Author:         Evan Stone
'Description:    Simple class that demonstrates how to assign an icon to a folder.
'Version:        1.0
'Date:           January 17, 2005
'Comments: 
'EULA:           THIS SOURCE CODE MAY NOT BE DISTRIBUTED IN ANY FASHION WITHOUT
'                THE PRIOR CONSENT OF THE AUTHOR. THIS SOURCE CODE IS LICENSED 
'                �AS IS� WITHOUT WARRANTY AS TO ITS PERFORMANCE AND THE 
'                COPYRIGHT HOLDER MAKES NO WARRANTIES OF ANY KIND, EXPRESSED 
'                OR IMPLIED, INCLUDING BUT NOT LIMITED TO IMPLIED WARRANTIES 
'                OF MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. 
'                IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, 
'                INDIRECT, INCIDENTAL, SPECIAL, PUNITIVE OR CONSEQUENTIAL 
'                DAMAGES OR LOST PROFITS, EVEN IF THE END USER HAS OR HAS NOT 
'                BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
Imports System.Drawing
Imports System.IO

Public Class FolderIcon
    Private _folderPathRenamed As String = ""
    Private _iniPathRenamed As String = ""
    Public Property IniPath() As String
        Get
            Return _iniPathRenamed
        End Get
        Set(ByVal value As String)
            _iniPathRenamed = value
        End Set
    End Property

    Public Property FolderPath() As String
        Get
            Return Me._folderPathRenamed
        End Get
        Set(ByVal value As String)
            _folderPathRenamed = value
            If Not _folderPathRenamed.EndsWith("\") Then
                _folderPathRenamed &= "\"
            End If
        End Set
    End Property
    Public Sub New(ByVal folderPath As String)
        Me.FolderPath = folderPath
    End Sub

    Public Sub CreateFolderIcon(ByVal iconFilePath As String, ByVal infoTip As String)
        If CreateFolder() Then
            CreateDesktopIniFile(iconFilePath, infoTip)
            SetIniFileAttributes()
            SetFolderAttributes()
        End If
    End Sub
    ''' <summary>
    ''' Sets the Target Folder path and configures it to display an icon in Windows Explorer.
    ''' </summary>
    ''' <param name="targetFolderPath">Folder to display with icon</param>
    ''' <param name="iconFilePath">Path to icon [-containing] file</param>
    ''' <param name="infoTip">Text to be displayed in the InfoTip shown by Windows Explorer</param>
    Public Sub CreateFolderIcon(ByVal targetFolderPath As String, ByVal iconFilePath As String, ByVal infoTip As String)
        FolderPath = targetFolderPath
        Me.CreateFolderIcon(iconFilePath, infoTip)
    End Sub
    Public Shared Function GetIconFromBitmap(ByVal FileName As String) As Icon
        Using Bmp As New Bitmap(FileName)
            Return Icon.FromHandle(Bmp.GetHicon)
        End Using
    End Function
    Private Function CreateFolder() As Boolean
        ' Check for a path in the folderPath variable, which we use to 
        ' create the folder if it does not exist.
        If Me.FolderPath.Length = 0 Then
            Return False
        End If

        ' If the directory exists, then just return true.
        If Directory.Exists(Me.FolderPath) Then
            Return True
        End If

        Try
            ' Try to create the directory.
            Dim di As DirectoryInfo = Directory.CreateDirectory(Me.FolderPath)
        Catch e As Exception
            Return False
        End Try

        Return True
    End Function
    ''' <summary>
    ''' Creates the desktop.ini file which points to a .ico file
    ''' </summary>
    ''' <param name="iconFilePath">Path to icon [-containing] file</param>
    ''' <param name="getIconFromDLL">Indicates that the icon is embedded in a DLL (or EXE)</param>
    ''' <param name="iconIndex">Index of icon embedded in DLL or EXE; set to zero if getIconFromDLL is false</param>
    ''' <param name="infoTip">Text to be displayed in the InfoTip shown by Windows Explorer</param>
    Private Function CreateDesktopIniFile(ByVal iconFilePath As String, ByVal getIconFromDLL As Boolean, ByVal iconIndex As Integer, ByVal infoTip As String) As Boolean
        ' check some things that must (or should) be true before we continue...
        ' determine if the Folder exists
        If Not Directory.Exists(Me.FolderPath) Then
            Return False
        End If

        ' determine whether the icon file exists
        If Not File.Exists(iconFilePath) Then
            Return False
        End If

        If Not getIconFromDLL Then
            iconIndex = 0
        End If

        ' Set path to the desktop.ini file
        Me.IniPath = Me.FolderPath & "desktop.ini"

        ' Write .ini settings to the desktop.ini file
        'IniWriter.WriteValue(".ShellClassInfo", "IconResource", iconFilePath & ",0", Me.IniPath)
        IniWriter.WriteValue(".ShellClassInfo", "IconResource", infoTip & ".ico" & ",0", Me.IniPath)
        'IniWriter.WriteValue(".ShellClassInfo", "IconIndex", iconIndex.ToString(), Me.IniPath)
        'IniWriter.WriteValue(".ShellClassInfo", "InfoTip", infoTip, Me.IniPath)

        Return True
    End Function


    ''' <summary>
    ''' Creates a desktop.ini file to reference an icon file.
    ''' </summary>
    ''' <param name="iconFilePath">Path to icon file (.ico)</param>
    ''' <param name="infoTip">Text to be displayed in the InfoTip shown by Windows Explorer</param>
    Private Sub CreateDesktopIniFile(ByVal iconFilePath As String, ByVal infoTip As String)
        Me.CreateDesktopIniFile(iconFilePath, False, 0, infoTip)
    End Sub


    ''' <summary>
    ''' Sets the ini file folder's attributes to Hidden and System
    ''' </summary>
    Private Function SetIniFileAttributes() As Boolean
        ' determine if the Folder exists
        If Not File.Exists(Me.IniPath) Then
            Return False
        End If

        ' Set ini file attribute to "Hidden"
        If (File.GetAttributes(Me.IniPath) And FileAttributes.Hidden) <> FileAttributes.Hidden Then
            File.SetAttributes(Me.IniPath, File.GetAttributes(Me.IniPath) Or FileAttributes.Hidden)
        End If

        ' Set ini file attribute to "System"
        If (File.GetAttributes(Me.IniPath) And FileAttributes.System) <> FileAttributes.System Then
            File.SetAttributes(Me.IniPath, File.GetAttributes(Me.IniPath) Or FileAttributes.System)
        End If

        Return True
    End Function


    ''' <summary>
    ''' Sets the folder's attributes to System
    ''' </summary>
    Private Function SetFolderAttributes() As Boolean
        ' determine if the Folder exists
        If Not Directory.Exists(Me.FolderPath) Then
            Return False
        End If

        ' Set folder attribute to "System"
        If (File.GetAttributes(Me.FolderPath) And FileAttributes.System) <> FileAttributes.System Then
            File.SetAttributes(Me.FolderPath, File.GetAttributes(Me.FolderPath) Or FileAttributes.System)
        End If

        Return True
    End Function
End Class
