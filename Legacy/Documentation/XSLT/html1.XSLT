<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" id="html1" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <!-- -->
    <xsl:template match="/">
        <xsl:apply-templates select="/body"/>
    </xsl:template>
    <!-- -->
    <xsl:template match="starts-with(@name,'h'">
        <entry>
            <xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
            <title><xsl:value-of select="."/></title>
            <xsl:apply-templates select=".//*"/>
        </entry>
    </xsl:template>
</xsl:stylesheet>
