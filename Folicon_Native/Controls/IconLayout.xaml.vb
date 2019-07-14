
Imports System.Drawing
Imports System.IO
Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Public Class IconLayout
    Public Sub New(ByVal dataContext As Object)
        InitializeComponent()
        Me.DataContext = dataContext
    End Sub

    Public Function RenderToBitmap() As Bitmap
        Return RenderTargetBitmapTo32bppArgb(AsRenderTargetBitmap())
    End Function

    Private Function AsRenderTargetBitmap() As RenderTargetBitmap
        Dim size = New System.Windows.Size(256, 256)
        Me.Measure(size)
        Me.Arrange(New Rect(size))

        Dim rtb As New RenderTargetBitmap(CInt(size.Width), CInt(size.Height), 96, 96, PixelFormats.Default)
        rtb.Render(Me)
        Return rtb
    End Function

    Private Shared Function RenderTargetBitmapTo32bppArgb(ByVal rtb As RenderTargetBitmap) As Bitmap
        Dim stream As New MemoryStream()
        Dim encoder As BitmapEncoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(rtb))
        encoder.Save(stream)

        'Bitmap png = new Bitmap((int)rtb.Width, (int)rtb.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        'System.Drawing.Graphics.FromImage(png).DrawImage(new Bitmap(stream), 0, 0);

        Return New Bitmap(stream) 'png;
    End Function

End Class
