Imports System.Drawing
Imports System.IO
Imports FoliconNative.Modules

Public Class ProIcon
    Dim _filePath As string

    Public Sub New(filePath As string)
        _filePath = filePath
    End Sub

    Public Function RenderToBitmap() As Bitmap
        Return RenderTargetBitmapTo32bppArgb(AsRenderTargetBitmap())
    End Function

    Private Function AsRenderTargetBitmap() As BitmapSource
        using img As New Bitmap(_filePath)
            using icon = New Bitmap(img, 256, 256)
                Return LoadBitmap(icon)
            End Using
        End Using
    End Function

    Private Function RenderTargetBitmapTo32bppArgb(rtb As BitmapSource) As Bitmap
        Dim stream As New MemoryStream()
        Dim encoder As BitmapEncoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(rtb))
        encoder.Save(stream)
        Return New Bitmap(stream) 'png; 
    End Function
End Class
