﻿Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Mathematical

Partial Module Scatter

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="fieldX$"></param>
    ''' <param name="fieldY$"></param>
    ''' <param name="valueField$"></param>
    ''' <param name="colorSchema$"></param>
    ''' <param name="levels%"></param>
    ''' <param name="size"></param>
    ''' <param name="padding$"></param>
    ''' <param name="xlabel$">
    ''' 如果这个参数为默认的空值，则函数会使用<paramref name="fieldX"/>的值作为标签值，
    ''' 但是如果为空字符串，则这个坐标轴的标签将不会被显示出来
    ''' </param>
    ''' <param name="ylabel$"></param>
    ''' <param name="legendTitle$"></param>
    ''' <returns></returns>
    <Extension>
    Public Function PlotHeatmap(data As IEnumerable(Of DataSet),
                                Optional fieldX$ = "X",
                                Optional fieldY$ = "Y",
                                Optional valueField$ = "value",
                                Optional colorSchema$ = "jet",
                                Optional levels% = 25,
                                Optional size As Size = Nothing,
                                Optional padding$ = g.DefaultPadding,
                                Optional bg$ = "white",
                                Optional xlabel$ = Nothing,
                                Optional ylabel$ = Nothing,
                                Optional legendTitle$ = Nothing) As Image

        Dim colors As Color() = Designer.GetColors(colorSchema, n:=levels)

        If xlabel Is Nothing Then
            xlabel = fieldX
        End If
        If ylabel Is Nothing Then
            ylabel = fieldY
        End If
        If legendTitle.StringEmpty Then
            legendTitle = valueField
        End If
        If size.IsEmpty Then
            size = New Size(1600, 1200)
        End If

        Return g.GraphicsPlots(
            size, padding,
            bg,
            Sub(ByRef g, rect)
                Call g.__plotInternal(rect, data.ToArray, colors,
                                      fieldX, fieldY, valueField,
                                      labelX:=xlabel, labelY:=ylabel)
            End Sub)
    End Function

    <Extension>
    Private Sub __plotInternal(g As Graphics, rect As GraphicsRegion, data As DataSet(), colors As Color(),
                               fieldX$, fieldY$, fieldValue$, 
                               labelX$,labelY$)

        Dim points As (pt As PointF, value#)() = data.ToArray(
            Function(o) (New PointF(o(fieldX), o(fieldY)), o(fieldValue)))
        Dim levels%() = points.Select(Function(pt) pt.value) _
            .GenerateMapping(Level:=colors.Length)
        Dim valueGroups = points _
            .SeqIterator _
            .Select(Function(p) (p.value.pt, p.value.value,seq:=levels (p))) _
            .GroupBy(Function (o)o.seq  )
        Dim serials As SerialData() = valueGroups _
            .Select(Function(o) New SerialData() With {
                .color = colors(o.Key),
                .pts = o.Select(Function(x) New PointData(x.Item1)).ToArray,
                .title = o.Key
            }) _
            .ToArray
        Dim scatterPlotSize As New Size(width:=rect.Size.Width * 0.8, height:=rect.Size.Height)
        Dim left As Image = Scatter.Plot(
            serials, scatterPlotSize,
            Xlabel:=labelX, Ylabel:=labelY, drawLine:=False, showLegend:=False)

        With g
            .DrawImageUnscaled(left, New Point)
        End With
    End Sub
End Module