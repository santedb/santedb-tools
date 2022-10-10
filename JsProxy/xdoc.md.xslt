<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"

                xmlns:util="http://santedb.org/xsl/util"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl util"
>
  <xsl:output method="text" indent="yes" />

  <xsl:variable name='nl'>
    <xsl:text>&#xa;</xsl:text>
  </xsl:variable>
  
  <msxsl:script implements-prefix="util" language="C#">
    <![CDATA[
      public string fixCref(string cref) {
        var lastPeriod = cref.LastIndexOf(".");
        return cref.Substring(lastPeriod + 1);
      }
      
      public string getLink(string cref) {
        if(cref.StartsWith("T:SanteDB")) {
          return "http://santesuite.org/assets/doc/net/html/" + cref.Replace("T:","T_").Replace(".","_").Replace("`","_") + ".htm";
        }
        else if(cref.StartsWith("T:System")) {
          return "https://docs.microsoft.com/en-us/dotnet/api/" + cref.Replace("T:", "").ToLowerInvariant();
        }
        else {
          return "#" + fixCref(cref);
        }
      }
    ]]>
  </msxsl:script>
  <xsl:template match="remarks|summary">
    <xsl:apply-templates select="@* | node()" />
  </xsl:template>

  <xsl:template match="text()">
    <xsl:value-of select="." />
  </xsl:template>

  <xsl:template match="para">
    <xsl:value-of select="$nl"/>
    <xsl:apply-templates select="@* | node()" />
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="list[@type='bullet']">
    <xsl:value-of select="$nl"/>
    <xsl:apply-templates select="item" mode="list-unordered" />
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="list[@type='number']">
    <xsl:value-of select="$nl"/>
    <xsl:apply-templates select="item" mode="list-ordered" />
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="item" mode="list-unordered">* <xsl:apply-templates select="term/*|description/*|node()" />
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="item" mode="list-ordered">1. <xsl:apply-templates select="term/*|description/*|node()" />
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="term"> **<xsl:apply-templates select="@* | node()" />** </xsl:template>

  <xsl:template match="description">- <xsl:apply-templates select="@* | node()" /></xsl:template>

  <xsl:template match="list[@type='table']">
    <xsl:apply-templates select="listheader" mode="table" />
    <xsl:value-of select="$nl"/>
    <xsl:apply-templates select="item" mode="table" />
  </xsl:template>

  <xsl:template match="listheader" mode="table"><xsl:for-each select="term">|<xsl:apply-templates select="@* | node()" /></xsl:for-each>|
<xsl:for-each select="term">|-</xsl:for-each>|
    <xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="item" mode="table">
      <xsl:for-each select="term|description|resource">|<xsl:apply-templates select="@* | node()" /></xsl:for-each>|<xsl:value-of select="$nl"/>
  </xsl:template>

  <xsl:template match="c">```<xsl:value-of select="."/>```</xsl:template>

  <xsl:template match="code">
```
<xsl:apply-templates select="@* | node()" />
```
  </xsl:template>

  <xsl:template match="see[@href]">[<xsl:value-of select="."/>](<xsl:value-of select="@href"/>)</xsl:template>
  <xsl:template match="see[@cref]">[<xsl:value-of select="util:fixCref(@cref)"/>](<xsl:value-of select="util:getLink(@cref)"/>)</xsl:template>
</xsl:stylesheet>