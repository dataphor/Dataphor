<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <!-- -->
    <xsl:output method="xml" omit-xml-declaration='yes' />
    <xsl:param name="id-prefix" />
    <!-- -->
    <xsl:template match="/">
      <xsl:apply-templates />
    </xsl:template>
    <!-- -->
    <xsl:template match="*|@*|comment()|processing-instruction()|text()">
      <xsl:if test="not(sect1)">
        <xsl:copy>
            <xsl:apply-templates select="*|@*|comment()|processing-instruction()|text()"/>
        </xsl:copy>
      </xsl:if>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect1">
      <sect1>
        <xsl:apply-templates select="@*"/>
        <xsl:apply-templates select="./*[not (name() = 'sect2')]" />
        <xsl:apply-templates select="./sect2">
          <xsl:sort select="@id" />
        </xsl:apply-templates>
     </sect1>
    </xsl:template>
</xsl:stylesheet>