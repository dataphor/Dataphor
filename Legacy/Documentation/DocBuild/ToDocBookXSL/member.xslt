<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<!--<xsl:include href="common.xslt" />-->
	<!-- -->
	<!--<xsl:param name='member-id' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*/*[@id=$member-id]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="method | constructor | operator" mode="singleton">
    <xsl:if test="not(./documentation/nodoc)">
        <xsl:variable name="type">
            <xsl:choose>
                <xsl:when test="local-name(..)='interface'">Interface</xsl:when>
                <xsl:otherwise>Class</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="childType">
            <xsl:choose>
                <xsl:when test="local-name()='method'">Method</xsl:when>
                <xsl:when test="local-name()='operator'">
                <xsl:call-template name="operator-name">
                    <xsl:with-param name="name">
                      <xsl:value-of select="@name" />
                    </xsl:with-param>
                    <xsl:with-param name="from">
                      <xsl:value-of select="parameter/@type" />
                    </xsl:with-param>
                    <xsl:with-param name="to">
                      <xsl:value-of select="@returnType" />
                    </xsl:with-param>
                </xsl:call-template>
                </xsl:when>
                <xsl:otherwise>Constructor</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="filename">
            <xsl:choose>
                <xsl:when test="local-name()='method'">
                    <xsl:call-template name="get-filename-for-method">
                    </xsl:call-template>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:call-template name="get-filename-for-current-constructor"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="memberName" select="@name" />
        <sect4>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <xsl:comment>Generated from member.xsl</xsl:comment>
            <title>
                <xsl:value-of select="../@name" />
                <xsl:if test="local-name()='method'">
                    <xsl:text>.</xsl:text>
                    <xsl:value-of select="@name" />
                </xsl:if>
                <xsl:text>&#32;</xsl:text>
                <xsl:value-of select="$childType" />
                <xsl:if test="local-name() != 'operator'">
                    <xsl:if test="count(parent::node()/*[@name=$memberName]) &gt; 1">
                        <xsl:text>&#32;</xsl:text>
                        <xsl:call-template name="get-param-list" />
                    </xsl:if>
                </xsl:if>
            </title>
            <xsl:call-template name="summary-section" />
             <bridgehead renderas="sect4">Declaration</bridgehead>
            <xsl:call-template name="vb-member-syntax" />
            <xsl:call-template name="cs-member-syntax" />
            <xsl:call-template name="parameter-section" />
            <xsl:call-template name="returnvalue-section" />
            <xsl:call-template name="implements-section" />
            <xsl:call-template name="remarks-section" />
            <xsl:call-template name="events-section" />
            <xsl:call-template name="exceptions-section" />
            <xsl:call-template name="example-section" />
            <xsl:call-template name="requirements-section" />
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page">member</xsl:with-param>
            </xsl:call-template>
        </sect4>
      </xsl:if>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>