﻿#Region "Microsoft.VisualBasic::d6efb5d0b795b760c96c7061629a661e, ..\sciBASIC#\Data_science\Mathematica\Plot\Plots\Testing\CSSTest.vb"

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

Imports Microsoft.VisualBasic.Data.ChartPlots.Statistics.Heatmap
Imports Microsoft.VisualBasic.Imaging.Driver.CSS

Module CSSTest

    Sub Main()

        Call GetType(DensityPlot).LoadDriver(DensityPlot.DriverName).CSSTemplate.SaveTo("./density.plot.css")
    End Sub
End Module
