<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  >
	<!-- -->
	<xsl:output method="html" indent="yes" omit-xml-declaration="yes" />
	<!-- -->
	<xsl:include href="common.xslt" />
	<!-- -->
    <xsl:param name="topic-id" />
    <xsl:param name="select" />
    <xsl:param name="topic" /> <!-- this is name -->
    <xsl:param name="Entry-prefix" />
    <!-- -->
    <xsl:template match="/">
        <xsl:choose>
            <xsl:when test="$Entry-prefix">
                <xsl:call-template name="nav-dilref"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="nav-normal"/>
            </xsl:otherwise>
        </xsl:choose>
	</xsl:template>
    <!-- -->
    <xsl:template name="nav-dilref">
        <dt>
            <a>
                <xsl:attribute name="href">
                    <xsl:value-of select="$Entry-prefix"/>
                    <xsl:call-template name="get-filename-for-cref">
                        <xsl:with-param name="cref" select="$topic" />
                    </xsl:call-template><xsl:text>.html</xsl:text>
                </xsl:attribute>
                <xsl:value-of select="translate(substring($topic,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/>
            </a>
        </dt>
        <dd>
            <xsl:choose>
                <xsl:when test="count(//*[@id=$topic-id]/summary/para) &gt; 0">
                    <xsl:value-of select="//*[@id=$topic-id]/summary/para[1]"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="//*[@id=$topic-id]/summary"/>
                </xsl:otherwise>
            </xsl:choose>
        </dd>
        <br /><br />
    </xsl:template>
    <!-- -->
    <xsl:template name="nav-normal">
        <dt>
            <a>
                <xsl:attribute name="href">
                    <xsl:call-template name="get-filename-for-cref">
                        <xsl:with-param name="cref" select="concat('T:',$topic)" />
                    </xsl:call-template>
                </xsl:attribute>
                <xsl:value-of select="$topic"/>
            </a>
        </dt>
        <dd>
            <xsl:choose>
                <xsl:when test="count(//*[@id=$topic]/summary/para) &gt; 0">
                    <xsl:value-of select="//*[@id=$topic]/summary/para[1]"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="//*[@id=$topic]/summary"/>
                </xsl:otherwise>
            </xsl:choose>
        </dd>
        <br /><br />
    </xsl:template>
</xsl:stylesheet>
