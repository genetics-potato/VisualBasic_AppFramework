﻿Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models.Isometric.Shapes
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.MIME.Markup.HTML.CSS
Imports Microsoft.VisualBasic.Scripting
Imports Microsoft.VisualBasic.Webservices.Github.WebAPI
Imports IsometricView = Microsoft.VisualBasic.Imaging.Drawing3D.IsometricEngine

Public Module IsometricContributions

    <Extension>
    Public Function Plot(userName$,
                         Optional schema$ = "Jet",
                         Optional size$ = "3000,2200",
                         Optional padding$ = g.DefaultPadding,
                         Optional bg$ = "white",
                         Optional rectWidth! = 0.5,
                         Optional noColor$ = NameOf(Color.Gray)) As GraphicsData
        Dim contributions = userName.GetUserContributions
        Return contributions.Plot(schema, size, padding, bg, rectWidth, noColor)
    End Function

    <Extension>
    Public Function Plot(contributions As Dictionary(Of Date, Integer),
                         Optional schema$ = "Jet",
                         Optional size$ = "3000,2200",
                         Optional padding$ = g.DefaultPadding,
                         Optional bg$ = "white",
                         Optional rectWidth! = 0.5,
                         Optional noColor$ = NameOf(Color.Gray),
                         Optional statNumberColor$ = Nothing,
                         Optional labelItemCSS$ = CSSFont.Win7VeryLarge, 
                         Optional avatar As Image = Nothing) As GraphicsData

        Dim max% = contributions.Values.Max
        Dim colors As List(Of Color) = Designer.GetColors(schema, max).AsList
        Dim weeks = contributions.Split(7)
        Dim view As New IsometricView
        Dim maxZ = rectWidth * 5.5
        Dim x!, y!

        Call colors.Insert(Scan0, noColor.TranslateColor)

        For Each week In weeks
            For Each day As KeyValuePair(Of Date, Integer) In week
                Dim height! = day.Value / max * maxZ
                Dim o As New Point3D(x, y, 0)
                Dim model3D As New Prism(o, rectWidth, rectWidth, height)

                x += rectWidth

                Call view.Add(model3D, colors(day.Value))
            Next

            x = 0
            y += rectWidth
        Next

        With size.SizeParser
            Call view.Measure(.Width, .Height, False)
        End With

        Dim streak = contributions.Split(Function(day) day.Value = 0, )
        Dim LongestStreak = streak.OrderByDescending(Function(days) days.Length).First
        Dim currentStreak = streak.Last
        Dim total$ = contributions.Sum(Function(day) day.Value).ToString("N").Replace(".00", "")
        Dim busiestDay = contributions.OrderByDescending(Function(day) day.Value).FirstOrDefault
        Dim oneYear$

        With contributions.Keys.OrderBy(Function(day) day).ToArray
            oneYear = $"{ .First.ToString("MMM dd, yyyy")} - { .Last.ToString("MMM dd, yyyy")}"
        End With

        Dim model As Surface() = view.ToArray
        Dim labelItemFont As Font = CSSFont.TryParse(labelItemCSS).GDIObject
        Dim statNumberFont As New Font(labelItemFont.Name, labelItemFont.Size * 3, FontStyle.Bold)
        Dim statNumberPen As Brush = statNumberColor.GetBrush
        Dim plotInternal =
            Sub(ByRef g As IGraphics, region As GraphicsRegion)
                Dim camera As New Camera With {
                    .screen = region.Size,
                    .fov = 10000,
                    .ViewDistance = -85,
                    .angleX = 30,
                    .angleY = 30,
                    .angleZ = 120
                }
                model = model _
                    .Centra _
                    .Offsets(model) _
                    .ToArray
                model = camera.Rotate(model).ToArray

                Call DirectCast(g, Graphics2D) _
                    .Graphics _
                    .SurfacePainter(camera, model)

                Dim fsize As SizeF = g.MeasureString(oneYear, labelItemFont)

                With region
                    x = .Size.Width - .Padding.Right - fsize.Width
                    y = .Padding.Top
                End With

                Dim dev = g
                Dim plotLabelContent =
                    Sub(title$, item$, date$, value$)
                        Call dev.DrawString(item, labelItemFont, Brushes.Black, New PointF(x, y))
                        Call dev.DrawString([date], labelItemFont, Brushes.Gray, New PointF(x, y + labelItemFont.Height + 5))

                        With dev.MeasureString(title, labelItemFont)
                            Call dev.DrawString(title, labelItemFont, Brushes.Black, New PointF(x - .Width, y - .Height - 5))
                        End With

                        fsize = dev.MeasureString(value, statNumberFont)
                        Call dev.DrawString(value, statNumberFont, statNumberPen, New Point(x - fsize.Width, y))
                    End Sub

                ' 右上角的整年的贡献值
                Call plotLabelContent("1 year total", "contributions", oneYear, total)

                y += statNumberFont.Height * 1.5

                With busiestDay
                    Call plotLabelContent("Busiest day", "contributions", .Key.ToString("MMM dd"), .Value)
                End With

                With region
                    y = .Size.Height * 2 / 3
                    x = .Padding.Left + g.MeasureString("Longest streak", labelItemFont).Width
                End With

                With LongestStreak
                    Dim period$

                    With .Select(Function(day) day.Key).OrderBy(Function(day) day).ToArray
                        period = $"{ .First.ToString("MMM dd")} - { .Last.ToString("MMM dd")}"
                    End With

                    Call plotLabelContent("Longest streak", "days", period, .Length)
                End With

                y += statNumberFont.Height * 1.5

                With currentStreak
                    Dim period$

                    With .Select(Function(day) day.Key).OrderBy(Function(day) day).ToArray
                        period = $"{ .First.ToString("MMM dd")} - { .Last.ToString("MMM dd")}"
                    End With

                    Call plotLabelContent("Current streak", "days", period, .Length)
                End With
            End Sub

        Return g.GraphicsPlots(size.SizeParser, padding, bg, plotInternal)
    End Function
End Module
