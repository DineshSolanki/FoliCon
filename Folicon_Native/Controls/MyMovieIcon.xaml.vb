Imports System.Drawing
Imports System.IO
Public Class MyMovieCIcon
    Public Sub New(ByVal dataContext As Object)
        InitializeComponent()
        Me.DataContext = dataContext
    End Sub

    Public Function RenderToBitmap() As Bitmap
        Return RenderTargetBitmapTo32bppArgb(AsRenderTargetBitmap())
    End Function

    Private Function AsRenderTargetBitmap() As RenderTargetBitmap

        Dim size = New Windows.Size(256, 256)
        Me.Measure(size)
        Me.Arrange(New Rect(size))

        Dim rtb As New RenderTargetBitmap(size.Width, size.Height, 96, 96, PixelFormats.Default)
        rtb.Render(Me)

        Return rtb
    End Function

    Private Function RenderTargetBitmapTo32bppArgb(ByVal rtb As RenderTargetBitmap) As Bitmap
        Dim stream As New MemoryStream()
        Dim encoder As BitmapEncoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(rtb))
        encoder.Save(stream)
        Return New Bitmap(stream) 'png; 

    End Function
End Class
