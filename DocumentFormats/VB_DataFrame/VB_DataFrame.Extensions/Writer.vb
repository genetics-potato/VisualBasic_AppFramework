﻿#Region "Microsoft.VisualBasic::04a697a0b703b1f2f8f7e1ce64d57b2b, ..\visualbasic_App\DocumentFormats\VB_DataFrame\VB_DataFrame.Extensions\Writer.vb"

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

Imports System.IO
Imports Microsoft.VisualBasic.DocumentFormat.Csv.DocumentStream

''' <summary>
''' <see cref="Writer.Dispose"/>的时候会自动保存Csv文件的数据
''' </summary>
Public Class Writer : Implements IDisposable

    ''' <summary>
    ''' File handle for the csv data file. 
    ''' </summary>
    ReadOnly __file As StreamWriter
    ReadOnly __class As [Class]

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="cls">Schema maps</param>
    ''' <param name="DIR">
    ''' Dump data to this directory. The index file will using ``#.Csv`` as its default name.
    ''' </param>
    ''' <param name="encoding">Text document encoding of the csv file.</param>
    Sub New(cls As [Class], DIR As String, encoding As Encodings)
        Dim path As String = DIR & $"/{cls.Stack.Replace("::", "/")}.Csv"
        Call path.ParentPath.MkDIR
        Dim fs As New FileStream(path, FileMode.OpenOrCreate)

        __class = cls
        __file = New StreamWriter(fs, encoding.GetEncodings)

        row += "#"

        For Each field As Field In cls.Fields
            If Not field.InnerClass Is Nothing Then
                field.InnerClass.__writer =
                    New Writer(field.InnerClass, DIR, encoding)
            End If

            Call row.Add(field.Name)
        Next

        Call __file.WriteLine(New RowObject(row).AsLine)
    End Sub

    ReadOnly row As New List(Of String)

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="obj">.NET object for maps to csv data row.</param>
    ''' <param name="i">Uid reference for the external table.</param>
    Public Sub WriteRow(obj As Object, i As String)
        Call row.Clear()
        Call row.Add(i)

        For Each field As Field In __class.Fields
            Dim x As Object = field.GetValue(obj)

            If field.InnerClass Is Nothing Then  ' 对于简单属性，直接生成字符串
                Call row.Add(field.Binding.ToString(x))
            Else
                Call field.InnerClass.__writer.WriteRow(x, i)
            End If
        Next

        Call __file.WriteLine(New RowObject(row).AsLine)
    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                Call __file.Flush()
                Call __file.Close()
                Call __file.Dispose()

                For Each field In __class.Fields
                    If Not field.InnerClass Is Nothing Then
                        Call field.InnerClass.__writer.Dispose()
                    End If
                Next
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class
