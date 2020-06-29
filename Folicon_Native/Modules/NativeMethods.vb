Imports System.Runtime.InteropServices

Module NativeMethods
    <DllImport("kernel32", CharSet:=CharSet.Unicode)>
    Public Function WritePrivateProfileString(ByVal iniSection As String, ByVal iniKey As String, ByVal iniValue As String, ByVal iniFilePath As String) As Integer
    End Function
    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function Wow64DisableWow64FsRedirection(ByRef ptr As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function Wow64RevertWow64FsRedirection(ByVal ptr As IntPtr) As Boolean
    End Function

    <DllImport("gdi32")>
    Public Function DeleteObject(o As IntPtr) As Integer
    End Function
    <DllImport("shell32.dll")>
    Public Sub SHChangeNotify(ByVal wEventId As Integer, ByVal uFlags As Integer,
        ByVal dwItem1 As Integer, ByVal dwItem2 As Integer)
    End Sub
    <DllImport("shell32.dll", CharSet:=CharSet.Unicode)>
    Public Function ILCreateFromPath(ByVal pszPath As String) As IntPtr
    End Function
    Public Const SHCNE_ASSOCCHANGED = &H8000000
    Public Const SHCNF_IDLIST = 0
    Public Const SHCNE_UPDATEDIR = &H1000
    Public Const SHCNF_FLUSHNOWAIT = &H2000
    <DllImport("Shell32.dll", CharSet:=CharSet.Unicode)>
    Public Function SHGetSetFolderCustomSettings(ByRef pfcs As LPSHFOLDERCUSTOMSETTINGS, ByVal pszPath As String, ByVal dwReadWrite As UInteger) As UInteger
    End Function

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
    Friend Structure LPSHFOLDERCUSTOMSETTINGS
        Public dwSize As UInteger
        Public dwMask As UInteger
        Public pvid As IntPtr
        Public pszWebViewTemplate As String
        Public cchWebViewTemplate As UInteger
        Public pszWebViewTemplateVersion As String
        Public pszInfoTip As String
        Public cchInfoTip As UInteger
        Public pclsid As IntPtr
        Public dwFlags As UInteger
        Public pszIconFile As String
        Public cchIconFile As UInteger
        Public iIconIndex As Integer
        Public pszLogo As String
        Public cchLogo As UInteger
    End Structure

End Module
