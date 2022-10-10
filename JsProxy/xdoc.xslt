<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"

                xmlns:util="http://santedb.org/xsl/util"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl util"
>
  <xsl:output method="html" indent="yes" />

  <msxsl:script implements-prefix="util" language="C#">
    <![CDATA[
      public string fixCref(string cref) {
        var lastPeriod = cref.LastIndexOf(".");
        return cref.Substring(lastPeriod + 1);
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
    <p>
      <xsl:apply-templates select="@* | node()" />
    </p>
  </xsl:template>

  <xsl:template match="list[@type='bullet']">
    <ul>
      <xsl:apply-templates select="item" mode="list" />
    </ul>
  </xsl:template>

  <xsl:template match="list[@type='number']">
    <ol>
      <xsl:apply-templates select="item" mode="list" />
    </ol>
  </xsl:template>

  <xsl:template match="item" mode="list">
    <li>
      <xsl:apply-templates select="term|description|text()" mode="list" />
    </li>
  </xsl:template>

  <xsl:template match="term" mode="list">
    <strong>
      <xsl:apply-templates select="@* | node()" />
    </strong>
  </xsl:template>

  <xsl:template match="description" mode="list">
    <span>
      - <xsl:apply-templates select="@* | node()" />
    </span>
  </xsl:template>

  <xsl:template match="list[@type='table']">
    <table>
      <thead>
        <xsl:apply-templates select="listheader" mode="table" />
      </thead>
      <tbody>
        <xsl:apply-templates select="item" mode="table" />
      </tbody>
    </table>
  </xsl:template>

  <xsl:template match="listheader" mode="table">
    <tr>
      <xsl:for-each select="term">
        <th>
          <xsl:apply-templates select="@* | node()" />
        </th>
      </xsl:for-each>
    </tr>
  </xsl:template>

  <xsl:template match="item" mode="table">
    <tr>
      <xsl:for-each select="term|description|resource">
        <td>
          <xsl:apply-templates select="@* | node()" />
        </td>
      </xsl:for-each>
    </tr>
  </xsl:template>

  <xsl:template match="see">
    {@link <xsl:value-of select="util:fixCref(@cref)" />}
  </xsl:template>
</xsl:stylesheet>