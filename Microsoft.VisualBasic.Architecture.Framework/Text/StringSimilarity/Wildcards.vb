﻿#Region "Microsoft.VisualBasic::849b650cd0766ed5ef3b622a99bffb50, ..\sciBASIC#\Microsoft.VisualBasic.Architecture.Framework\Extensions\StringHelpers\Wildcards.vb"

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

Imports System.Runtime.CompilerServices

Namespace Text.Similarity

    ''' <summary>
    ''' Extensions to <see cref="String"/> by using wildcards to match string
    ''' 
    ''' ###### A very simple wildcard match
    ''' > https://github.com/picrap/WildcardMatch
    ''' </summary>
    Public Module WildcardsExtension

        ''' <summary>
        ''' Tells if the given string matches the given wildcard.
        ''' Two wildcards are allowed: ``*`` and ``%``.
        ''' 
        ''' + '*' matches any ZERO or more characters
        ''' + '%' matches any single character
        ''' </summary>
        ''' <param name="wildcard">The wildcard.</param>
        ''' <param name="s">The s.</param>
        ''' <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        ''' <returns></returns>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension>
        Public Function WildcardMatch(wildcard$, s$, Optional ignoreCase As Boolean = False) As Boolean
            Return WildcardMatch(wildcard, s, 0, 0, ignoreCase)
        End Function

        Const NeverRun$ = "This code is never run, so this exception is useless."

        ''' <summary>
        ''' Internal matching algorithm.
        ''' </summary>
        ''' <param name="wildcard">The wildcard.</param>
        ''' <param name="s">The s.</param>
        ''' <param name="wildcardIndex">Index of the wildcard.</param>
        ''' <param name="sIndex">Index of the s.</param>
        ''' <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        ''' <returns></returns>
        <Extension>
        Private Function WildcardMatch(wildcard$, s$, wildcardIndex%, sIndex%, ignoreCase As Boolean) As Boolean
            Do While True
                ' in the wildcard end, if we are at tested string end, then strings match
                If wildcardIndex = wildcard.Length Then
                    Return sIndex = s.Length
                End If

                Dim c As Char = wildcard(wildcardIndex)

                Select Case c

                ' always a match
                    Case "%"c
                    Case "*"c

                        ' if this is the last wildcard char, then we have a match, whatever the tested string is
                        If wildcardIndex = wildcard.Length - 1 Then
                            Return True
                        End If

                        ' test if a match follows
                        Return Enumerable _
                            .Range(sIndex, s.Length - 1) _
                            .Any(Function(i)
                                     Return WildcardMatch(wildcard, s, wildcardIndex + 1, i, ignoreCase)
                                 End Function)
                    Case Else
                        If sIndex < s.Length Then
                            Dim cc = If(ignoreCase, Char.ToLower(c), c)
                            Dim sc = If(ignoreCase, Char.ToLower(s(sIndex)), s(sIndex))

                            If cc <> sc Then
                                Return False
                            End If
                        End If
                End Select

                wildcardIndex += 1
                sIndex += 1
            Loop

            ' disable the warning for function no returns value.
            Throw New Exception(NeverRun)
        End Function
    End Module
End Namespace