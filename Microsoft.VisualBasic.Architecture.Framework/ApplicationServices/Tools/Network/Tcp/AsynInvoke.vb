﻿#Region "Microsoft.VisualBasic::769446369ecfa54198379a0e2a6dede1, ..\sciBASIC#\Microsoft.VisualBasic.Architecture.Framework\ApplicationServices\Tools\Network\Tcp\AsynInvoke.vb"

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

Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports Microsoft.VisualBasic.Net.Abstract
Imports Microsoft.VisualBasic.Net.Http
Imports Microsoft.VisualBasic.Net.Protocols

Namespace Net

    ''' <summary>
    ''' The server socket should returns some data string to this client or this client will stuck at the <see cref="SendMessage"></see> function.
    ''' (服务器端<see cref="TcpSynchronizationServicesSocket"></see>必须要返回数据，否则本客户端会在<see cref="SendMessage
    ''' "></see>函数位置一直处于等待的状态)
    ''' </summary>
    ''' <remarks></remarks>
    Public Class AsynInvoke : Implements System.IDisposable

#Region "Internal Fields"

        ''' <summary>
        ''' The port number for the remote device.
        ''' </summary>
        ''' <remarks></remarks>
        Dim port As Integer

        ''' <summary>
        ''' The response from the remote device.
        ''' </summary>
        ''' <remarks></remarks>
        Dim response As Byte()

        ''' <summary>
        ''' ' ManualResetEvent instances signal completion.
        ''' </summary>
        ''' <remarks></remarks>
        Dim connectDone As ManualResetEvent
        Dim sendDone As ManualResetEvent
        Dim receiveDone As ManualResetEvent
        Dim __exceptionHandler As Abstract.ExceptionHandler
        Dim remoteHost As String

        ''' <summary>
        ''' Remote End Point
        ''' </summary>
        ''' <remarks></remarks>
        Protected ReadOnly remoteEP As System.Net.IPEndPoint
#End Region

        ''' <summary>
        ''' Gets the IP address of this local machine.
        ''' (获取本机对象的IP地址，请注意这个属性获取得到的仅仅是本机在局域网内的ip地址，假若需要获取得到公网IP地址，还需要外部服务器的帮助才行)
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property LocalIPAddress As String
            Get
#Disable Warning
                Dim IP As System.Net.IPAddress = Dns.Resolve(Dns.GetHostName).AddressList(0)
                Dim IPAddr As String = IP.ToString
#Enable Warning
                Return IPAddr
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"Remote_connection={remoteHost}:{port},  local_host={LocalIPAddress}"
        End Function

        Sub New(remoteDevice As System.Net.IPEndPoint, Optional ExceptionHandler As Abstract.ExceptionHandler = Nothing)
            Call Me.New(remoteDevice.Address.ToString, remoteDevice.Port, ExceptionHandler)
        End Sub

        Sub New(remoteDevice As IPEndPoint, Optional ExceptionHandler As Abstract.ExceptionHandler = Nothing)
            Call Me.New(remoteDevice.IPAddress, remoteDevice.Port, ExceptionHandler)
        End Sub

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="Client">Copy the TCP client connection profile data from this object.(从本客户端对象之中复制出连接配置参数以进行初始化操作)</param>
        ''' <param name="ExceptionHandler"></param>
        ''' <remarks></remarks>
        Sub New(Client As AsynInvoke, Optional ExceptionHandler As Abstract.ExceptionHandler = Nothing)
            remoteHost = Client.remoteHost
            port = Client.port
            __exceptionHandler = If(ExceptionHandler Is Nothing, Sub(ex As Exception) Call ex.PrintException, ExceptionHandler)
            remoteEP = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(remoteHost), port)
        End Sub

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="remotePort"></param>
        ''' <param name="ExceptionHandler">Public Delegate Sub ExceptionHandler(ex As Exception)</param>
        ''' <remarks></remarks>
        Sub New(hostName As String, remotePort As Integer, Optional ExceptionHandler As Abstract.ExceptionHandler = Nothing)
            remoteHost = hostName

            If String.Equals(remoteHost, "localhost", StringComparison.OrdinalIgnoreCase) Then
                remoteHost = LocalIPAddress
            End If

            port = remotePort
            __exceptionHandler = If(ExceptionHandler Is Nothing, Sub(ex As Exception) Call ex.PrintException, ExceptionHandler)
            remoteEP = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(remoteHost), port)
        End Sub

        ''' <summary>
        ''' 初始化一个在本机进行进程间通信的Socket对象
        ''' </summary>
        ''' <param name="LocalPort"></param>
        ''' <param name="ExceptionHandler"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function LocalConnection(LocalPort As Integer, Optional ExceptionHandler As Abstract.ExceptionHandler = Nothing) As AsynInvoke
            Return New AsynInvoke(LocalIPAddress, LocalPort, ExceptionHandler)
        End Function

        ''' <summary>
        ''' 判断服务器所返回来的数据是否为操作超时
        ''' </summary>
        ''' <param name="str"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function OperationTimeOut(str As String) As Boolean
            Return String.Equals(str, NetResponse.RFC_REQUEST_TIMEOUT.GetUTF8String)
        End Function

        ''' <summary>
        ''' Returns the server reply.(假若操作超时的话，则会返回<see cref="NetResponse.RFC_REQUEST_TIMEOUT"></see>)
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="OperationTimeOut">操作超时的时间长度，默认为30秒</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function SendMessage(Message As String,
                                    Optional OperationTimeOut As Integer = 30 * 1000,
                                    Optional OperationTimeoutHandler As Action = Nothing) As String
            Dim request As New RequestStream(0, 0, Message)
            Dim response = SendMessage(request, OperationTimeOut, OperationTimeoutHandler).GetUTF8String
            Return response
        End Function 'Main

        ''' <summary>
        ''' Returns the server reply.(假若操作超时的话，则会返回<see cref="NetResponse.RFC_REQUEST_TIMEOUT"></see>，
        ''' 请注意，假若目标服务器启用了ssl加密服务的话，假若这个请求是明文数据，则服务器会直接拒绝请求返回<see cref="HTTP_RFC.RFC_NO_CERT"/> 496错误代码，
        ''' 所以调用前请确保参数<paramref name="Message"/>已经使用证书加密)
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="timeOut">操作超时的时间长度，默认为30秒</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function SendMessage(Message As RequestStream,
                                    Optional timeout% = 30 * 1000,
                                    Optional timeout_handler As Action = Nothing) As RequestStream

            Dim response As RequestStream = Nothing
            Dim bResult As Boolean = Parallel.OperationTimeOut(
                AddressOf SendMessage,
                [In]:=Message,
                Out:=response,
                TimeOut:=timeout / 1000)

            If bResult Then
                If Not timeout_handler Is Nothing Then Call timeout_handler() '操作超时了

                If Not connectDone Is Nothing Then Call connectDone.Set()  ' ManualResetEvent instances signal completion.
                If Not sendDone Is Nothing Then Call sendDone.Set()
                If Not receiveDone Is Nothing Then Call receiveDone.Set() '中断服务器的连接

                Dim ex As Exception = New Exception("[OPERATION_TIME_OUT] " & Message.GetUTF8String)
                Dim ret As New RequestStream(0, HTTP_RFC.RFC_REQUEST_TIMEOUT, "HTTP/408  " & Me.ToString)
                Call __exceptionHandler(New Exception(ret.GetUTF8String, ex))
                Return ret
            Else
                Return response
            End If
        End Function

        Public Function SafelySendMessage(Message As RequestStream,
                                          CA As SSL.Certificate,
                                          Optional OperationTimeOut As Integer = 30 * 1000,
                                          Optional OperationTimeoutHandler As Action = Nothing) As RequestStream

            Message = CA.Encrypt(Message)
            Message = SendMessage(Message, OperationTimeOut, OperationTimeoutHandler)

            If Message.IsSSLProtocol OrElse Message.IsSSL_PublicToken Then
                Message = CA.Decrypt(Message)
            Else
                Try
                    Message.ChunkBuffer = CA.Decrypt(Message.ChunkBuffer)
                Catch ex As Exception
                    Return Message
                End Try
            End If

            Return Message
        End Function

        Public Delegate Function SendMessageInvoke(Message As String) As String

        Public Function SendMessage(Message As String, Callback As Action(Of String)) As IAsyncResult
            Dim SendMessageClient As New AsynInvoke(Me, ExceptionHandler:=Me.__exceptionHandler)
            Return (Sub() Call Callback(SendMessageClient.SendMessage(Message))).BeginInvoke(Nothing, Nothing)
        End Function

        ''' <summary>
        ''' This function returns the server reply for this request <paramref name="Message"></paramref>.
        ''' </summary>
        ''' <param name="Message">The client request to the server.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function SendMessage(Message As String) As String
            Dim byteData As Byte() = System.Text.Encoding.UTF8.GetBytes(Message)
            byteData = SendMessage(byteData)
            Dim response As String = New RequestStream(byteData).GetUTF8String
            Return response
        End Function 'Main

        Public Function SendMessage(Message As String, CA As SSL.Certificate) As String
            Dim request = New RequestStream(0, 0, Message)
            Dim byteData = CA.Encrypt(request).Serialize
            byteData = SendMessage(byteData)
            Return __decryptMessageCommon(byteData, CA).GetUTF8String
        End Function

        ''' <summary>
        ''' Send a request message to the remote server.
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <returns></returns>
        Public Function SendMessage(Message As RequestStream) As RequestStream
            Dim byteData As Byte() = Message.Serialize
            byteData = SendMessage(byteData)
            If RequestStream.IsAvaliableStream(byteData) Then
                Return New RequestStream(byteData)
            Else
                Return New RequestStream(0, 0, byteData)
            End If
        End Function

        Private Function __decryptMessageCommon(retData As Byte(), CA As SSL.Certificate) As RequestStream
            If Not RequestStream.IsAvaliableStream(retData) Then Return New RequestStream(0, 0, retData)

            Dim Message = New RequestStream(retData)

            If Message.IsSSLProtocol OrElse Message.IsSSL_PublicToken Then
                Message = CA.Decrypt(Message)
            Else
                Try
                    Message.ChunkBuffer = CA.Decrypt(Message.ChunkBuffer)
                Catch ex As Exception
                    Call App.LogException(ex, MethodBase.GetCurrentMethod.GetFullName)
                End Try
            End If

            Return Message
        End Function

        ''' <summary>
        ''' 发送一段使用证书对象<paramref name="CA"/>进行数据加密操作的消息请求<paramref name="Message"/>
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="CA"></param>
        ''' <param name="isPublicToken"></param>
        ''' <returns></returns>
        Public Function SendMessage(Message As RequestStream, CA As SSL.Certificate, Optional isPublicToken As Boolean = False) As RequestStream
            Dim byteData = If(isPublicToken, CA.PublicEncrypt(Message), CA.Encrypt(Message)).Serialize
            byteData = SendMessage(byteData)
            Return __decryptMessageCommon(byteData, CA)
        End Function

        ''' <summary>
        ''' 最底层的消息发送函数
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <returns></returns>
        Public Function SendMessage(Message As Byte()) As Byte()
            If Not RequestStream.IsAvaliableStream(Message) Then
                Message = New RequestStream(0, 0, Message).Serialize
            End If

            connectDone = New ManualResetEvent(False) ' ManualResetEvent instances signal completion.
            sendDone = New ManualResetEvent(False)
            receiveDone = New ManualResetEvent(False)
            response = Nothing

            ' Establish the remote endpoint for the socket.
            ' For this example use local machine.
            ' Create a TCP/IP socket.
            Dim client As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            Call client.Bind(New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0))
            ' Connect to the remote endpoint.
            Call client.BeginConnect(remoteEP, New AsyncCallback(AddressOf ConnectCallback), client)
            ' Wait for connect.
            Call connectDone.WaitOne()
            ' Send test data to the remote device.
            Call __send(client, Message)
            Call sendDone.WaitOne()

            ' Receive the response from the remote device.
            Call Receive(client)
            Call receiveDone.WaitOne()

            On Error Resume Next

            ' Release the socket.
            Call client.Shutdown(SocketShutdown.Both)
            Call client.Close()

            Return response
        End Function

        Private Sub ConnectCallback(ar As IAsyncResult)

            ' Retrieve the socket from the state object.
            Dim client As Socket = DirectCast(ar.AsyncState, Socket)

            ' Complete the connection.
            Try
                client.EndConnect(ar)
                ' Signal that the connection has been made.
                connectDone.Set()
            Catch ex As Exception
                Call __exceptionHandler(ex)
            End Try
        End Sub 'ConnectCallback

        ''' <summary>
        ''' An exception of type '<see cref="System.Net.Sockets.SocketException"/>' occurred in System.dll but was not handled in user code
        ''' Additional information: A request to send or receive data was disallowed because the socket is not connected and
        ''' (when sending on a datagram socket using a sendto call) no address was supplied
        ''' </summary>
        ''' <param name="client"></param>
        Private Sub Receive(client As Socket)

            ' Create the state object.
            Dim state As New StateObject
            state.workSocket = client
            ' Begin receiving the data from the remote device.
            Try
                Call client.BeginReceive(state.readBuffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
            Catch ex As Exception
                Call Me.__exceptionHandler(ex)
            End Try
        End Sub 'Receive

        Private Sub ReceiveCallback(ar As IAsyncResult)

            ' Retrieve the state object and the client socket
            ' from the asynchronous state object.
            Dim state As StateObject = DirectCast(ar.AsyncState, StateObject)
            Dim client As Socket = state.workSocket
            ' Read data from the remote device.

            Dim bytesRead As Integer

            Try
                bytesRead = client.EndReceive(ar)
            Catch ex As Exception
                Call __exceptionHandler(ex)
                GoTo EX_EXIT
            End Try

            If bytesRead > 0 Then

                ' There might be more data, so store the data received so far.
                state.ChunkBuffer.AddRange(state.readBuffer.Takes(bytesRead))
                ' Get the rest of the data.
                client.BeginReceive(state.readBuffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
            Else
                ' All the data has arrived; put it in response.
                If state.ChunkBuffer.Count > 1 Then

                    response = state.ChunkBuffer.ToArray
                Else
EX_EXIT:            response = Nothing
                End If
                ' Signal that all bytes have been received.
                Call receiveDone.Set()
            End If
        End Sub 'ReceiveCallback

        ''' <summary>
        ''' ????
        ''' An exception of type 'System.Net.Sockets.SocketException' occurred in System.dll but was not handled in user code
        ''' Additional information: A request to send or receive data was disallowed because the socket is not connected and
        ''' (when sending on a datagram socket using a sendto call) no address was supplied
        ''' </summary>
        ''' <param name="client"></param>
        ''' <param name="byteData"></param>
        ''' <remarks></remarks>
        Private Sub __send(client As Socket, byteData As Byte())

            ' Begin sending the data to the remote device.
            Try
                Call client.BeginSend(byteData, 0, byteData.Length, 0, New AsyncCallback(AddressOf SendCallback), client)
            Catch ex As Exception
                Call Me.__exceptionHandler(ex)
            End Try
        End Sub 'Send

        Private Sub SendCallback(ar As IAsyncResult)

            ' Retrieve the socket from the state object.
            Dim client As Socket = DirectCast(ar.AsyncState, Socket)
            ' Complete sending the data to the remote device.
            Dim bytesSent As Integer = client.EndSend(ar)
            'Console.WriteLine("Sent {0} bytes to server.", bytesSent)
            ' Signal that all bytes have been sent.
            sendDone.Set()
        End Sub 'SendCallback

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    Call connectDone.Set()  ' ManualResetEvent instances signal completion.
                    Call sendDone.Set()
                    Call receiveDone.Set() '中断服务器的连接
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(      disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(      disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class 'AsynchronousClient
End Namespace
