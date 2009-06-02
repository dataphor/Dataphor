<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<!--<xsl:include href="common.xslt" />-->
	<!-- -->
	<!--<xsl:param name='property-id' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*/property[@id=$property-id]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="property" mode="singleton">
		<xsl:variable name="type">
			<xsl:choose>
				<xsl:when test="local-name(..)='interface'">Interface</xsl:when>
				<xsl:otherwise>Class</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="propertyName" select="@name" />
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-current-property"/>
        </xsl:variable>
        <sect4>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <xsl:comment>Generated from property.xsl</xsl:comment>
            <title>
            <indexterm>
                <primary>
                    <xsl:value-of select="concat(../@name,'.',@name)"/>
                </primary>
            </indexterm>
            <indexterm>
                <primary>
                    <xsl:value-of select="../@name"/>
                </primary>
                <secondary>
                    <xsl:value-of select="@name"/>
                </secondary>
            </indexterm>
            <indexterm>
                <primary>
                    <xsl:value-of select="@name"/>
                </primary>
            </indexterm><xsl:value-of select="../@name" />.<xsl:value-of select="@name" /> Property</title>
            <xsl:call-template name="summary-section" />
            <bridgehead renderas="sect4">Declaration</bridgehead>
            <xsl:if test="$ndoc-vb-syntax">
                    <para role="lang">[Visual&#160;Basic]</para>
                    <xsl:text>
</xsl:text>
                <programlisting role="syntax">
                    <xsl:call-template name="vb-property-syntax" />
                </programlisting>
            </xsl:if>
                <xsl:if test="$ndoc-vb-syntax">
                    <para role="lang">[C#]</para>
                    <xsl:text>
</xsl:text>
                </xsl:if>
            <programlisting role="syntax">
                <xsl:call-template name="cs-property-syntax" />
            </programlisting>
            <para/>
            <xsl:call-template name="parameter-section" />
            <xsl:call-template name="value-section" />
            <xsl:call-template name="implements-section" />
            <xsl:call-template name="remarks-section" />
            <xsl:call-template name="events-section" />
            <xsl:call-template name="exceptions-section" />
            <xsl:call-template name="example-section" />
            <xsl:call-template name="requirements-section" />
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page">property</xsl:with-param>
            </xsl:call-template>
        </sect4>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>