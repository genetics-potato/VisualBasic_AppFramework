﻿#Region "Microsoft.VisualBasic::924994bc47dad29ea3d131100855b86a, ..\sciBASIC#\gr\Microsoft.VisualBasic.Imaging\Drawing2D\g.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xieguigang (xie.guigang@live.com)
    '       xie (genetics@smrucc.org)
    ' 
    ' Copyright (c) 2016 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.SVG
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.MIME.Markup.HTML.CSS
Imports Microsoft.VisualBasic.Net.Http

Namespace Drawing2D

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="g">GDI+设备</param>
    ''' <param name="grct">绘图区域的大小</param>
    Public Delegate Sub IPlot(ByRef g As IGraphics, grct As GraphicsRegion)

    ''' <summary>
    ''' Data plots graphics engine common abstract.
    ''' </summary>
    Public Module g

        ''' <summary>
        ''' 默认的页边距大小都是100个像素
        ''' </summary>
        Public Const DefaultPadding$ = "padding:100px 100px 100px 100px;"

        ''' <summary>
        ''' 与<see cref="DefaultPadding"/>相比而言，这个padding的值在坐标轴Axis的label的绘制上空间更加大
        ''' </summary>
        Public Const DefaultLargerPadding$ = "padding:100px 100px 150px 150px;"
        ''' <summary>
        ''' 所有的页边距都是零
        ''' </summary>
        Public Const ZeroPadding$ = "padding: 0px 0px 0px 0px;"
        Public Const MediumPadding$ = "padding: 45px 45px 45px 45px;"
        Public Const SmallPadding$ = "padding: 30px 30px 30px 30px;"
        Public Const TinyPadding$ = "padding: 5px 5px 5px 5px;"

        ''' <summary>
        ''' 在这个模块的构造函数之中，程序会自动根据命令行所设置的环境参数来设置默认的图形引擎
        ''' 
        ''' ```
        ''' /@set graphic_driver=svg|gdi
        ''' ```
        ''' </summary>
        Sub New()
            Dim type$ = App.GetVariable("graphic_driver")

            If type.TextEquals("svg") Then
                g.__defaultDriver = Drivers.SVG
            ElseIf type.TextEquals("gdi") Then
                g.__defaultDriver = Drivers.GDI
            Else
                g.__defaultDriver = Drivers.Default
            End If
        End Sub

        ''' <summary>
        ''' 用户所指定的图形引擎驱动程序类型，但是这个值会被开发人员设定的驱动程序类型的值所覆盖，
        ''' 通常情况下，默认引擎选用的是``gdi+``引擎
        ''' </summary>
        ReadOnly __defaultDriver As Drivers = Drivers.Default

        ''' <summary>
        ''' 这个函数不会返回<see cref="Drivers.Default"/>
        ''' </summary>
        ''' <param name="developerValue">程序开发人员所设计的驱动程序的值</param>
        ''' <returns></returns>
        Private Function __getDriver(developerValue As Drivers) As Drivers
            If developerValue <> Drivers.Default Then
                Return developerValue
            Else
                If g.__defaultDriver = Drivers.Default Then
                    ' 默认为使用gdi引擎
                    Return Drivers.GDI
                Else
                    Return g.__defaultDriver
                End If
            End If
        End Function

        ''' <summary>
        ''' Data plots graphics engine. Default: <paramref name="size"/>:=(4300, 2000), <paramref name="padding"/>:=(100,100,100,100).
        ''' (用户可以通过命令行设置环境变量``graphic_driver``来切换图形引擎)
        ''' </summary>
        ''' <param name="size"></param>
        ''' <param name="padding">页边距</param>
        ''' <param name="bg">颜色值或者图片资源文件的url或者文件路径</param>
        ''' <param name="plotAPI"></param>
        ''' <param name="driver">驱动程序是默认与当前的环境参数设置相关的</param>
        ''' <returns></returns>
        ''' 
        <Extension>
        Public Function GraphicsPlots(ByRef size As Size, ByRef padding As Padding, bg$, plotAPI As IPlot, Optional driver As Drivers = Drivers.Default) As GraphicsData
            Dim image As GraphicsData

            If size.IsEmpty Then
                size = New Size(3600, 2000)
            End If
            If padding.IsEmpty Then
                padding = New Padding(100)
            End If

            If g.__getDriver(developerValue:=driver) = Drivers.SVG Then
                Dim svg As New GraphicsSVG(size)

                Call svg.Clear(bg.TranslateColor)
                Call plotAPI(svg, New GraphicsRegion With {
                       .Size = size,
                       .Padding = padding
                  })

                image = New SVGData(svg, size)
            Else
                ' using gdi+ graphics driver
                ' 在这里使用透明色进行填充，防止当bg参数为透明参数的时候被CreateGDIDevice默认填充为白色
                Using g As Graphics2D = size.CreateGDIDevice(Color.Transparent)
                    Dim rect As New Rectangle(New Point, size)

                    With g.Graphics

                        Call .FillBg(bg$, rect)

                        .CompositingQuality = CompositingQuality.HighQuality
                        .CompositingMode = CompositingMode.SourceOver
                        .InterpolationMode = InterpolationMode.HighQualityBicubic
                        .PixelOffsetMode = PixelOffsetMode.HighQuality
                        .SmoothingMode = SmoothingMode.HighQuality
                        .TextRenderingHint = TextRenderingHint.ClearTypeGridFit

                    End With

                    Call plotAPI(g, New GraphicsRegion With {
                         .Size = size,
                         .Padding = padding
                    })

                    image = New ImageData(g.ImageResource, size)
                End Using
            End If

            Return image
        End Function

        ''' <summary>
        ''' 自动根据表达式的类型来进行纯色绘制或者图形纹理画刷绘制
        ''' </summary>
        ''' <param name="g"></param>
        ''' <param name="bg$">
        ''' 1. 可能为颜色表达式
        ''' 2. 可能为图片的路径
        ''' 3. 可能为base64图片字符串
        ''' </param>
        <Extension>
        Public Sub FillBg(ByRef g As Graphics, bg$, rect As Rectangle)
            Dim bgColor As Color = bg.ToColor(onFailure:=Nothing)

            If Not bgColor.IsEmpty Then
                Call g.FillRectangle(New SolidBrush(bgColor), rect)
            Else
                Dim res As Image

                If bg.FileExists Then
                    res = LoadImage(path:=bg$)
                Else
                    res = Base64Codec.GetImage(bg$)
                End If

                Call g.DrawImage(res, rect)
            End If
        End Sub

        ''' <summary>
        ''' Data plots graphics engine.
        ''' </summary>
        ''' <param name="size"></param>
        ''' <param name="bg"></param>
        ''' <param name="plot"></param>
        ''' <returns></returns>
        ''' 
        <Extension>
        Public Function GraphicsPlots(plot As Action(Of IGraphics), ByRef size As Size, ByRef padding As Padding, bg$) As GraphicsData
            Return GraphicsPlots(size, padding, bg, Sub(ByRef g, rect) Call plot(g))
        End Function

        Public Function Allocate(Optional size As Size = Nothing, Optional padding$ = DefaultPadding, Optional bg$ = "white") As InternalCanvas
            Return New InternalCanvas With {
                .size = size,
                .bg = bg,
                .padding = padding
            }
        End Function

        <Extension>
        Public Function CreateGraphics(img As GraphicsData) As IGraphics
            If img.Driver = Drivers.SVG Then
                Dim svg = DirectCast(img, SVGData).SVG
                Return New GraphicsSVG(svg)
            Else
                Return Graphics2D.Open(DirectCast(img, ImageData).Image)
            End If
        End Function

        ''' <summary>
        ''' Draw shadow of a specifc <paramref name="polygon"/>
        ''' </summary>
        ''' <param name="g"></param>
        ''' <param name="polygon"></param>
        ''' <param name="shadowColor$"></param>
        ''' <param name="alphaLevels$"></param>
        ''' <param name="gradientLevels$"></param>
        <Extension> Public Sub DropdownShadows(g As IGraphics,
                                               polygon As GraphicsPath,
                                               Optional shadowColor$ = NameOf(Color.Gray),
                                               Optional alphaLevels$ = "0,120,150,200",
                                               Optional gradientLevels$ = "[0,0.125,0.5,1]")

            Dim alphas As Vector = alphaLevels
            ' Create a color blend to manage our colors And positions And
            ' since we need 3 colors set the default length to 3
            Dim colorBlend As New ColorBlend(alphas.Length)
            Dim baseColor As Color = shadowColor.TranslateColor

            ' here Is the important part of the shadow making process, remember
            ' the clamp mode on the colorblend object layers the colors from
            ' the outside to the center so we want our transparent color first
            ' followed by the actual shadow color. Set the shadow color to a 
            ' slightly transparent DimGray, I find that it works best.|
            colorBlend.Colors = alphas _
                .Select(Function(a) Color.FromArgb(a, baseColor)) _
                .ToArray

            ' our color blend will control the distance of each color layer
            ' we want to set our transparent color to 0 indicating that the 
            ' transparent color should be the outer most color drawn, then
            ' our Dimgray color at about 10% of the distance from the edge
            colorBlend.Positions = CType(gradientLevels, Vector).AsSingle

            ' this Is where we create the shadow effect, so we will use a 
            ' pathgradientbursh And assign our GraphicsPath that we created of a 
            ' Rounded Rectangle
            Using pgBrush As New PathGradientBrush(polygon) With {
                .WrapMode = WrapMode.Clamp,
                .InterpolationColors = colorBlend
            }
                ' fill the shadow with our pathgradientbrush
                Call g.FillPath(pgBrush, polygon)
            End Using
        End Sub

        ''' <summary>
        ''' 可以借助这个画布对象创建多图层的绘图操作
        ''' </summary>
        Public Class InternalCanvas

            Dim plots As New List(Of IPlot)

            Public Property size As Size
            Public Property padding As Padding
            Public Property bg As String

            Public Function InvokePlot() As GraphicsData
                Return GraphicsPlots(
                    size, padding, bg,
                    Sub(ByRef g, rect)

                        For Each plot As IPlot In plots
                            Call plot(g, rect)
                        Next
                    End Sub)
            End Function

            Public Shared Operator +(g As InternalCanvas, plot As IPlot) As InternalCanvas
                g.plots += plot
                Return g
            End Operator

            Public Shared Operator +(g As InternalCanvas, plot As IPlot()) As InternalCanvas
                g.plots += plot
                Return g
            End Operator

            Public Shared Narrowing Operator CType(g As InternalCanvas) As GraphicsData
                Return g.InvokePlot
            End Operator

            ''' <summary>
            ''' canvas invoke this plot.
            ''' </summary>
            ''' <param name="g"></param>
            ''' <param name="plot"></param>
            ''' <returns></returns>
            Public Shared Operator <=(g As InternalCanvas, plot As IPlot) As GraphicsData
                Dim size As Size = g.size
                Dim margin = g.padding
                Dim bg As String = g.bg

                Return GraphicsPlots(size, margin, bg, plot)
            End Operator

            Public Shared Operator >=(g As InternalCanvas, plot As IPlot) As GraphicsData
                Throw New NotSupportedException
            End Operator
        End Class
    End Module
End Namespace
