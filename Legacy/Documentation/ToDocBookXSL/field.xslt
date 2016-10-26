<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<!--<xsl:include href="common.xslt" />-->
	<!-- -->
	<!--<xsl:param name='field-id' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*/field[@id=$field-id]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="field" mode="singleton">
        <xsl:variable name="filename">
            <xsl:call-template name="get-filename-for-current-field"/>
        </xsl:variable>
        <sect4>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <xsl:comment>Generated from field.xsl</xsl:comment>
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
            </indexterm><xsl:value-of select="../@name" />.<xsl:value-of select="@name" /> Field</title>
            <xsl:call-template name="summary-section" />
            <bridgehead renderas="sect4">Declaration</bridgehead>
            <xsl:call-template name="vb-field-or-event-syntax" />
            <xsl:call-template name="cs-field-or-event-syntax" />
            <para/>
            <xsl:call-template name="remarks-section" />
            <xsl:call-template name="example-section" />
            <xsl:call-template name="requirements-section" />
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page">field</xsl:with-param>
            </xsl:call-template>
        </sect4>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>