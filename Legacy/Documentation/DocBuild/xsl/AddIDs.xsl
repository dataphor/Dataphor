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
        <xsl:copy>
            <xsl:apply-templates select="*|@*|comment()|processing-instruction()|text()"/>
        </xsl:copy>
    </xsl:template>
    <!-- -->
    <xsl:template name="natural-attr">
        <xsl:param name="name"/>
        <xsl:param name="value"/>
        <xsl:attribute name="{$name}">
            <xsl:value-of select="$value"/>
        </xsl:attribute>
    </xsl:template>
    <!-- -->
    <xsl:template match="book[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <book>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </book>
    </xsl:template>
    <!-- -->
    <xsl:template match="part[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <part>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </part>
    </xsl:template>
    <!-- -->
    <xsl:template match="chapter[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <chapter>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </chapter>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect1[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <xsl:variable name="lParentTitle" select="translate(normalize-space(../title/text()),' ','')"/>
        <sect1>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="$lParentTitle"/><xsl:text>-</xsl:text><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </sect1>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect2[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <xsl:variable name="lParentTitle" select="translate(normalize-space(../title/text()),' ','')"/>
        <xsl:variable name="lGParentTitle" select="translate(normalize-space(../../title/text()),' ','')"/>
        <sect2>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="$lGParentTitle"/><xsl:text>-</xsl:text><xsl:value-of select="$lParentTitle"/><xsl:text>-</xsl:text><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </sect2>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect3[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <sect3>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </sect3>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect4[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <sect4>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </sect4>
    </xsl:template>
    <!-- -->
    <xsl:template match="sect5[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <sect5>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </sect5>
    </xsl:template>
    <!-- -->
    <xsl:template match="simplesect[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <simplesect>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </simplesect>
    </xsl:template>
    <!-- -->
    <xsl:template match="section[not(@id)]">
        <xsl:variable name="ltitle" select="./title/text()"/>
        <xsl:variable name="lParentTitle" select="translate(normalize-space(../title/text()),' ','')"/>
        <section>
            <xsl:attribute name="id">
                <xsl:value-of select="$id-prefix"/><xsl:value-of select="$lParentTitle"/><xsl:text>-</xsl:text><xsl:value-of select="translate(normalize-space($ltitle),' ','')"/>
            </xsl:attribute>
            <xsl:for-each select="./@*">
                <xsl:call-template name="natural-attr">
                    <xsl:with-param name="name" select="name(.)"/>
                    <xsl:with-param name="value"><xsl:value-of select="."/></xsl:with-param>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </section>
    </xsl:template>
    <!-- -->
    <xsl:template match="title">
        <xsl:variable name="ltitle" select="./text()"/>
        <xsl:variable name="lParentTitle" select="../../title/text()"/>
        <title>
            <xsl:choose>
                <xsl:when test="parent::formalpara">
                </xsl:when>
                <xsl:when test="not(./indexterm)">
                    <indexterm><primary><xsl:value-of select="$ltitle"/></primary></indexterm>
                    <xsl:if test="$lParentTitle">
                        <indexterm>
                            <primary><xsl:value-of select="$lParentTitle"/></primary>
                            <secondary><xsl:value-of select="$ltitle"/></secondary>
                        </indexterm>
                    </xsl:if>
                </xsl:when>
            </xsl:choose>
            <xsl:apply-templates select="*|comment()|processing-instruction()|text()"/>
        </title>
    </xsl:template>
</xsl:stylesheet>