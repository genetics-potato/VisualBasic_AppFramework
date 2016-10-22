﻿Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Mathematical
Imports Microsoft.VisualBasic.Mathematical.BasicR
Imports Microsoft.VisualBasic.Mathematical.Distributions
Imports Microsoft.VisualBasic.Mathematical.Plots

Module PDFTest

    Public Sub betaTest()
        Dim x As New Vector(VBMathExtensions.seq(0.02, 0.98, 0.001))
        Dim s1 = Scatter.FromVector(
            Beta.beta(x, 0.5, 0.5), "red",
            ptSize:=5,
            width:=10,
            xrange:=x,
            title:="α = β = 0.5")
        Dim s2 = Scatter.FromVector(
            Beta.beta(x, 5, 1), "blue",
            ptSize:=5,
            width:=10,
            xrange:=x,
            title:="α = 5, β = 1")
        Dim s3 = Scatter.FromVector(
            Beta.beta(x, 1, 3), "green",
            ptSize:=5,
            width:=10,
            xrange:=x,
            title:="α = 1, β = 3")
        Dim s4 = Scatter.FromVector(
            Beta.beta(x, 2, 2), "purple",
            ptSize:=5,
            width:=10,
            xrange:=x,
            title:="α = 2, β = 2")
        Dim s5 = Scatter.FromVector(
            Beta.beta(x, 2, 5), "orange",
            ptSize:=5,
            width:=10,
            xrange:=x,
            title:="α = 2, β = 5")

        Dim canvasSize As New Size(3000, 3500)
        Dim png As Bitmap = Scatter.Plot({s1, s2, s3, s4, s5}, canvasSize)

        Call png.SaveAs("./beta_PDF.png")

        Pause()
    End Sub
End Module
