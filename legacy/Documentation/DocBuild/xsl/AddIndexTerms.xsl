<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <!-- -->
    <xsl:output method="xml" omit-xml-declaration='yes' />
    <!-- -->
    <xsl:template match="/">
        <xsl:apply-templates />
    </xsl:template>
    <!-- -->
    <xsl:template match="*|@*|comment()|processing-instruction()|text()">
        <xsl:copy>
            <xsl:apply-templates select="*|@*|comment()|processing-instruction()|text()"/>
        </xsl:copy>
    </xsl:template>
    <!-- -->
    <xsl:template match="title[not(./indexterm)]">
        <title>
            <indexterm><primary><xsl:value-of select="."/></primary></indexterm>
            <xsl:if test="../../title">
                <indexterm>
                    <primary><xsl:value-of select="../../title"/></primary>
                    <secondary><xsl:value-of select="."/></secondary>
                </indexterm>
            </xsl:if>
            <xsl:value-of select="."/>
        </title>
    </xsl:template>
</xsl:stylesheet>
