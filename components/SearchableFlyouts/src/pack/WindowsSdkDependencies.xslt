<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" />
  <xsl:strip-space elements="*" />
  <xsl:param name="projections" />

  <xsl:template match="@*|node()">
    <xsl:copy><xsl:apply-templates select="@*|node()" /></xsl:copy>
  </xsl:template>

  <xsl:template match="*[local-name()='dependencies']/*[*[@id='Microsoft.Windows.SDK.NET.Ref']]">
    <xsl:call-template name="write-groups">
      <xsl:with-param name="remaining" select="$projections" />
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="write-groups">
    <xsl:param name="remaining" />
    <xsl:if test="$remaining != ''">
      <xsl:variable name="mapping" select="substring-before(concat($remaining, ' '), ' ')" />
      <xsl:copy>
        <xsl:attribute name="targetFramework"><xsl:value-of select="substring-before($mapping, '=')" /></xsl:attribute>
        <xsl:copy-of select="*[not(@id='Microsoft.Windows.SDK.NET.Ref')]" />
        <xsl:for-each select="*[@id='Microsoft.Windows.SDK.NET.Ref']">
          <xsl:copy>
            <xsl:copy-of select="@*[name()!='version']" />
            <xsl:attribute name="version">[<xsl:value-of select="substring-after($mapping, '=')" />]</xsl:attribute>
          </xsl:copy>
        </xsl:for-each>
      </xsl:copy>
      <xsl:call-template name="write-groups">
        <xsl:with-param name="remaining" select="substring-after($remaining, ' ')" />
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>
