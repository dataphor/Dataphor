<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<xsl:output method="html" indent="yes" />
	<!-- -->
	<xsl:include href="common.xslt" />
	<!-- -->
    <xsl:param name="topic-id" />
    <xsl:param name="select" />
    <xsl:param name="topic" /> <!-- this is name -->
    <xsl:param name="codedoc" />
    
	<!-- -->
    <xsl:template match="/">
        <xsl:choose>
            <xsl:when test="$select">
                <xsl:apply-templates select="$select" />
            </xsl:when>
            <xsl:when test="$topic-id">
                <xsl:apply-templates select="DOC.NET/assembly/module/namespace/*[@id=$topic-id]" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:apply-templates select="DOC.NET/assembly/module/namespace/*[@name=$topic]" />
                <!--<xsl:apply-templates select="*"/>-->
            </xsl:otherwise>
        </xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="indent">
		<xsl:param name="count" />
		<xsl:if test="$count &gt; 0">
			<xsl:text>&#160;&#160;&#160;</xsl:text>
			<xsl:call-template name="indent">
				<xsl:with-param name="count" select="$count - 1" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!-- -->
    <xsl:template match="interface" mode="inheritedAttr">
        <xsl:if test="./dilref">
            <br />
            <xsl:text>&#160;&#160;&#160;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>=&quot;</xsl:text><xsl:value-of select="@value"/><xsl:text>&quot;</xsl:text>
        </xsl:if>
    </xsl:template>
    <!-- -->
    <xsl:template match="property[not(dilref)]" mode="syntax">
    </xsl:template>
    <!-- -->
    <xsl:template match="property[dilref]" mode="syntax">
        <br />
        <xsl:text>&#160;&#160;&#160;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>=&quot;</xsl:text><xsl:value-of select="@value"/><xsl:text>&quot;</xsl:text>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-inherited">
        <xsl:param name="interfaceName" />
        <xsl:for-each select="//interface[@name=$interfaceName]/property">
            <xsl:apply-templates select="." mode="syntax" />
        </xsl:for-each>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-syntax">
        <!-- from type-syntax in syntax.xslt -->
        <h3>Syntax</h3>
		<pre class="syntax">
            <xsl:text>&lt;</xsl:text><xsl:value-of select="translate(substring(@name,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
            <xsl:for-each select="./implements">
                <xsl:call-template name="xml-inherited">
                    <xsl:with-param name="interfaceName" select="./text()"/>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:for-each select="./property">
                <xsl:apply-templates select="." mode="syntax" />
            </xsl:for-each>
            <br />
            <xsl:text>&gt;</xsl:text>
            <br />
            <xsl:text>&#160;&#160;&#160;&lt;!-- See remarks for valid children, if any. --&gt;</xsl:text>
            <br />
            <xsl:text>&lt;/</xsl:text><xsl:value-of select="translate(substring(@name,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>&gt;</xsl:text>
        </pre>
    </xsl:template>
    <!-- -->
    <xsl:template match="property[not(dilref)]" mode="description">
    </xsl:template>
    <!-- -->
    <xsl:template match="property[dilref]" mode="description">
        <!-- from parameter-topic in common.xslt -->
        <xsl:if test="./dilref">
            <p class="i1">
                <i>
                    <!-- todo: make this a link back to the nodes using the ID attribute of the node -->
                    <xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
                </i>
            </p>
            <p class="i2">
                <xsl:apply-templates select="node()" mode="slashdoc" />
            </p>
        </xsl:if>
    </xsl:template>
    <!-- -->
    <xsl:template name="attrib-description">
        <xsl:param name="interfaceName" />
        <xsl:for-each select="//interface[@name=$interfaceName]/property">
            <xsl:apply-templates select="." mode="description" />
        </xsl:for-each>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-attributes">
        <!-- from parameter-topic in common.xslt -->
        <!-- assume that there are always attributes -->
        <h3>Attributes</h3>
        <xsl:for-each select="./implements">
            <xsl:call-template name="attrib-description">
                <xsl:with-param name="interfaceName" select="./text()"/>
            </xsl:call-template>
        </xsl:for-each>
        <xsl:for-each select="./property">
            <xsl:apply-templates select="." mode="description" />
        </xsl:for-each>
    </xsl:template>
	<!-- -->
	<xsl:template match="interface">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Element</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template name="type">
		<xsl:param name="type" />
		<html dir="LTR">
			<xsl:call-template name="html-head">
				<xsl:with-param name="title" select="concat(substring(@name,2), ' ', $type)" />
			</xsl:call-template>
			<body>
				<xsl:call-template name="title-row">
					<xsl:with-param name="type-name" select="concat(translate(substring(@name,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'), ' ', $type)" />
				</xsl:call-template>
				<div id="content">
					<xsl:call-template name="summary-section" />
					<br />
					<xsl:call-template name="xml-syntax" />
                    <xsl:call-template name="xml-attributes"/>
					<xsl:call-template name="remarks-section" />
					<xsl:call-template name="example-section" />
					<xsl:variable name="page">
						<!--<xsl:choose>
							<xsl:when test="local-name() = 'enumeration'">enumeration</xsl:when>
							<xsl:when test="local-name() = 'delegate'">delegate</xsl:when>
							<xsl:otherwise>members</xsl:otherwise>
						</xsl:choose>-->
                        members
					</xsl:variable>
                    <!-- todo: see also should include only the node represented!! or better understanding of the seealso-section template-->
					<xsl:call-template name="seealso-section">
						<xsl:with-param name="page" select="$page" />
					</xsl:call-template>
                   <xsl:call-template name="footer"/>
                    <object type="application/x-oleobject" classid="clsid:1e2a7bd0-dab9-11d0-b93a-00c04fc99f9e" viewastext="viewastext">
                        <xsl:element name="param">
                            <xsl:attribute name="name">Keyword</xsl:attribute>
                            <xsl:attribute name="value"><xsl:value-of select='substring(@name,2)' /> DIL element</xsl:attribute>
                        </xsl:element>
                        <xsl:element name="param">
                            <xsl:attribute name="name">Keyword</xsl:attribute>
                            <xsl:attribute name="value">DIL Element, <xsl:value-of select='substring(@name,2)' /></xsl:attribute>
                        </xsl:element>
                        <xsl:call-template name="keyword-section"/>
                    </object>
				</div>
			</body>
		</html>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>
