Imports System.Runtime.InteropServices

''' <summary>
''' Wrapper class for WritePrivateProfileString Win32 API function.
''' </summary>
Public Class IniWriter
    <DllImport("kernel32")>
    Private Shared Function WritePrivateProfileString(ByVal iniSection As String, ByVal iniKey As String, ByVal iniValue As String, ByVal iniFilePath As String) As Integer
    End Function

    ''' <summary>
    ''' Adds to (or modifies) a value to an .ini file. If the file does not exist,
    ''' it will be created.
    ''' </summary>
    ''' <param name="iniSection">The section to which to add or modify a value.If the section does not exist,
    ''' it will be created.</param>
    ''' <param name="iniKey">The key to which to add or modify a value.If the key does not exist,
    ''' it will be created.</param>
    ''' <param name="iniValue">The value to write to the .ini file</param>
    ''' <param name="iniFilePath">The path to the .ini file to modify.</param>

    Public Shared Sub WriteValue(ByVal iniSection As String, ByVal iniKey As String, ByVal iniValue As String, ByVal iniFilePath As String)
        WritePrivateProfileString(iniSection, iniKey, iniValue, iniFilePath)
    End Sub

End Class