﻿#Region "Microsoft.VisualBasic::ab725b3d41c5ffacbb9827e44c039ef8, ..\sciBASIC#\Microsoft.VisualBasic.Architecture.Framework\ApplicationServices\Tools\Network\Tcp\Persistent\Protocols\Protocol.vb"

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

Imports System.Net.Sockets
Imports System.Threading
Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.Net.Protocols.Reflection
Imports Microsoft.VisualBasic.Net.Protocols

Namespace Net.Persistent.Application.Protocols

    Public Module ServicesProtocol

        Public Enum Protocols As Long
            Logon = 59867
            ServerHash
            SendMessage
            ConfigConnection
            IsUserOnline
            GetMyIPAddress
            Broadcast
        End Enum

        Public ReadOnly Property ProtocolEntry As Long =
            New Protocol(GetType(Protocols)).EntryPoint

        Public Function SendServerHash(hash As Integer) As RequestStream
            Return New RequestStream(ProtocolEntry, Protocols.ServerHash, CStr(hash))
        End Function

        Public Const WorkSocketPortal As Integer = 66982

        Public Function LogOnRequest(USER_ID As Long, Socket As String) As RequestStream
            Dim post As String = New LogonPOST With {
                .Socket = Socket,
                .USER_ID = USER_ID
            }.GetXml
            Return New RequestStream(ProtocolEntry, Protocols.Logon, post)
        End Function

        Public Function GetLogOnUSER(request As String, ByRef USER_ID As Long, ByRef Socket As String) As Boolean
            Dim post As LogonPOST = request.LoadFromXml(Of LogonPOST)(False)

            If post Is Nothing Then
                Return False
            Else
                USER_ID = post.USER_ID
                Socket = post.Socket

                Return USER_ID <> 0 AndAlso Not String.IsNullOrEmpty(Socket)
            End If
        End Function

        Public Function SendMessageRequest([FROM] As Long, USER_ID As Long, Message As RequestStream) As RequestStream
            Dim post As New SendMessagePost With {
                .FROM = FROM,
                .Message = Message,
                .USER_ID = USER_ID
            }
            Return New RequestStream(ProtocolEntry, Protocols.SendMessage, post.Serialize)
        End Function

        Public Function GetSendMessage(request As RequestStream, ByRef [FROM] As Long, ByRef USER_ID As Long) As Boolean
            Dim post As SendMessagePost = New SendMessagePost(request.ChunkBuffer)

            [FROM] = post.FROM
            USER_ID = post.USER_ID

            Return True
        End Function

        Public Function GetServicesConnection() As RequestStream
            Return New RequestStream(ProtocolEntry, Protocols.ConfigConnection, "GET")
        End Function

        Public Function IsUserOnlineRequest(USER_ID As Long) As RequestStream
            Return New RequestStream(ProtocolEntry, Protocols.IsUserOnline, CStr(USER_ID))
        End Function

        Public Function BroadcastMessage(USER_ID As Long, Message As RequestStream) As RequestStream
            Dim post As New BroadcastPOST With {
                .Message = Message,
                .USER_ID = USER_ID
            }
            Return New RequestStream(ProtocolEntry, Protocols.Broadcast, post.Serialize) With {
                .uid = USER_ID
            }
        End Function
    End Module
End Namespace
