Imports System.Text.RegularExpressions
Namespace Modules


    Module TitleCleaner
        Public Function Clean(ByVal title As String) As String
            Dim normalizedTitle As String = title.Replace("-"c, " "c).Replace("_"c, " "c).Replace("."c, " "c)

            ' \s* --Remove any whitespace which would be left at the end after this substitution
            ' \(? --Remove optional bracket starting (720p)
            ' (\d{4}) --Remove year from movie
            ' (420)|(720)|(1080) resolutions
            ' (year|resolutions) find at least one main token to remove
            ' p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
            ' .* --Remove all trailing information after having found year or resolution as junk usually follows
            Dim cleanTitle As String = Regex.Replace(normalizedTitle, "\s*\(?((\d{4})|(420)|(720)|(1080))p?i?\)?.*", "", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

            If String.IsNullOrWhiteSpace(cleanTitle) Then
                Return normalizedTitle
            Else
                Return cleanTitle
            End If
        End Function

    End Module
End Namespace
