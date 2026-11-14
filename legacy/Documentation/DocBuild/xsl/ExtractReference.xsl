<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    
    <xsl:include href="MapUlink.xsl" />
    <xsl:param name="target.database.document" />
    <!-- -->
    <xsl:template match="/">
        <xsl:apply-templates  select="//book[@id='DataphorReference']"/>
    </xsl:template>
    <!-- -->
    <xsl:template match="*|@*|comment()|processing-instruction()|text()" >
        <xsl:copy>
            <xsl:apply-templates select="*|@*|comment()|processing-instruction()|text()"/>
        </xsl:copy>
    </xsl:template>
    <!-- -->
    <xsl:template match="book">
        <book>
            <xsl:attribute name="id">
                <xsl:value-of select="@id"/>
            </xsl:attribute>
            <xsl:apply-templates />
            <index><title>Index</title></index>
        </book>
    </xsl:template>
    <!-- -->
    <xsl:template match="revhistory">
        <!-- do nothing -->
    </xsl:template>
    <!-- -->
    <xsl:template match="revision">
        <!-- do nothing -->
    </xsl:template>
    <!--
    <xsl:template match="chapterinfo">
        <xsl:comment>chapterinfo</xsl:comment>
    </xsl:template>
    -->
</xsl:stylesheet>