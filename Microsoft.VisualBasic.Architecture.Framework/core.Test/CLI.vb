﻿Imports System.ComponentModel
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.Reflection

Module CLI

    Function Main() As Integer
        Return GetType(CLI).RunCLI(App.CommandLine)
    End Function

    <ExportAPI("/CLI.docs.test")>
    <Description("Test description text")>
    <Usage("/CLI.docs.test [/echo ""hello world!"" /out <out.txt>]")>
    <Argument("/echo", True, CLITypes.String, PipelineTypes.std_in, AcceptTypes:={GetType(String)},
              Description:="Echo input text.")>
    <Argument("/out", True, CLITypes.File, PipelineTypes.std_out, AcceptTypes:={GetType(String)},
              Description:="The output file location. looooooooooooooooooooooooooooooooooooooooooooong and looooooooooooooooooooooooooooooooooooooooooooooooooong")>
    <Argument("/b", True, CLITypes.Boolean, Description:="Unknown")>
    Public Function CLIDocumentTest(args As CommandLine) As Integer

    End Function
End Module