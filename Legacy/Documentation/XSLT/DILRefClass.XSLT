<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<xsl:output method="html" indent="yes" />
	<!-- -->
	<xsl:include href="common.xslt" />
	<!-- -->
	<xsl:param name='type-id' />
	<!-- -->
	<xsl:template match="/">
		<xsl:apply-templates select="DOC.NET/assembly/module/namespace/*[@id=$type-id]" />
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
	<xsl:template name="draw-hierarchy">
		<xsl:param name="list" />
		<xsl:param name="level" />
		<!-- this is commented out because XslTransform is throwing an InvalidCastException in it. -->
		<xsl:if test="count($list) &gt; 0">
			<!-- last() is causing an InvalidCastException in Beta 2. -->
			<xsl:variable name="last" select="count($list)" />
			<xsl:call-template name="indent">
				<xsl:with-param name="count" select="$level" />
			</xsl:call-template>
			<a>
				<xsl:attribute name="href">
					<xsl:choose>
						<xsl:when test="starts-with($list[$last]/@type, 'System.')">
							<xsl:call-template name="get-filename-for-system-class">
								<xsl:with-param name="class-name" select="$list[$last]/@type" />
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="get-filename-for-type">
								<xsl:with-param name="id" select="$list[$last]/@id" />
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<xsl:call-template name="get-datatype">
					<xsl:with-param name="datatype" select="$list[$last]/@type" />
					<xsl:with-param name="namespace-name" select="../@name" />
				</xsl:call-template>
			</a>
			<br />
			<xsl:call-template name="draw-hierarchy">
				<xsl:with-param name="list" select="$list[position()!=$last]" />
				<xsl:with-param name="level" select="$level + 1" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
    <!-- -->
    <xsl:template match="property" mode="inheritedAttr">
        <xsl:if test="./set">
            <br />
            <xsl:text>&#160;&#160;&#160;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>=&quot;</xsl:text><xsl:value-of select="@value"/><xsl:text>&quot;</xsl:text>
        </xsl:if>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-inherited">
        <xsl:param name="propertyName" />
        <xsl:apply-templates select="/descendant::property[@id=$propertyName]" mode="inheritedAttr"/>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-syntax">
        <!-- from type-syntax in syntax.xslt -->
        <h3>Syntax</h3>
		<pre class="syntax">
            <xsl:text>&lt;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
            <xsl:for-each select="./property[@declaringType]">
                <xsl:call-template name="xml-inherited">
                    <xsl:with-param name="propertyName" select="concat('P:',@declaringType,'.',@name)"/>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:for-each select="./property[dilref]">
                <br />
                <xsl:text>&#160;&#160;&#160;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>=&quot;</xsl:text><xsl:value-of select="@value"/><xsl:text>&quot;</xsl:text>
            </xsl:for-each>
            <br />
            <xsl:text>&gt;</xsl:text>
            <br />
            <xsl:text>&#160;&#160;&#160;&lt;!-- See remarks for valid children. --&gt;</xsl:text>
            <br />
            <xsl:text>&lt;/</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>&gt;</xsl:text>
        </pre>
    </xsl:template>
    <!-- -->
    <xsl:template match="property" mode="description">
        <!-- from parameter-topic in common.xslt -->
        <xsl:if test="./set">
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
        <xsl:param name="propertyName" />
        <xsl:apply-templates select="/descendant::property[@id=$propertyName]" mode="description"/>
    </xsl:template>
    <!-- -->
    <xsl:template name="xml-attributes">
        <!-- from parameter-topic in common.xslt -->
        <xsl:if test="./property">
            <h3>Attributes</h3>
            <xsl:for-each select="./property[@declaringType]">
                <xsl:call-template name="attrib-description">
                    <xsl:with-param name="propertyName" select="concat('P:',@declaringType,'.',@name)"/>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:for-each select="./property[dilref]">
                <p class="i1">
                    <i>
                        <!-- todo: make this a link back to the nodes using the ID attribute of the node -->
                        <xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
                    </i>
                </p>
                <p class="i2">
                    <xsl:apply-templates select="node()" mode="slashdoc" />
                </p>
            </xsl:for-each>
        </xsl:if>
    </xsl:template>
	<!-- -->
	<xsl:template match="class">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Node</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template name="type">
		<xsl:param name="type" />
		<html dir="LTR">
			<xsl:call-template name="html-head">
				<xsl:with-param name="title" select="concat(@name, ' ', $type)" />
			</xsl:call-template>
			<body>
				<xsl:call-template name="title-row">
					<xsl:with-param name="type-name" select="concat(translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'), ' ', $type)" />
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
                   <!--
                    <object type="application/x-oleobject" classid="clsid:1e2a7bd0-dab9-11d0-b93a-00c04fc99f9e" viewastext="viewastext">
                        <xsl:element name="param">
                            <xsl:attribute name="name">Keyword</xsl:attribute>
                            <xsl:attribute name="value"><xsl:value-of select='@name' /> enumeration</xsl:attribute>
                        </xsl:element>
                        <xsl:for-each select="field">
                            <xsl:element name="param">
                                <xsl:attribute name="name">Keyword</xsl:attribute>
                                <xsl:attribute name="value"><xsl:value-of select='@name' /> enumeration member</xsl:attribute>
                            </xsl:element>
                        </xsl:for-each>
                        <xsl:call-template name="keyword-section"/>
                    </object>
                    -->
				</div>
			</body>
		</html>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>
