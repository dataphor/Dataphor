<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" >
	<!-- -->
	<!-- <xsl:output method="html" indent="yes" /> -->
	<!-- -->
	<xsl:include href="common.xslt" />
	<!-- -->
    <!--
    <xsl:param name="topic-id" />
    <xsl:param name="select" />
    <xsl:param name="topic" /> <!-- this is name -->
    -->
	<!-- -->
    <xsl:template match="/">
        <!-- select tested with textxpath.exe -->
        <xsl:apply-templates select="//interface[.//dilref]">
            <xsl:sort select="@name" />
        </xsl:apply-templates>
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
            <xsl:text>
</xsl:text>
            <xsl:text>&#160;&#160;&#160;</xsl:text><xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>=&quot;</xsl:text><xsl:value-of select="@value"/><xsl:text>&quot;</xsl:text>
        </xsl:if>
    </xsl:template>
    <!-- -->
    <xsl:template match="property[not(dilref)]" mode="syntax">
    </xsl:template>
    <!-- -->
    <xsl:template match="property[dilref]" mode="syntax">
        <xsl:text>
</xsl:text>
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
        <bridgehead renderas="sect3">Syntax</bridgehead>
		<programlisting role="syntax">
            <xsl:text>&lt;</xsl:text><xsl:value-of select="translate(substring(@name,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
            <xsl:for-each select="./implements">
                <xsl:call-template name="xml-inherited">
                    <xsl:with-param name="interfaceName" select="./text()"/>
                </xsl:call-template>
            </xsl:for-each>
            <xsl:for-each select="./property">
                <xsl:apply-templates select="." mode="syntax" />
            </xsl:for-each>
            <xsl:text>
</xsl:text>
            <xsl:text>&gt;</xsl:text>
            <xsl:text>
</xsl:text>
            <xsl:text>&#160;&#160;&#160;&lt;!-- See remarks for valid children, if any. --&gt;</xsl:text>
            <xsl:text>
</xsl:text>
            <xsl:text>&lt;/</xsl:text><xsl:value-of select="translate(substring(@name,2),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')"/><xsl:text>&gt;</xsl:text>
        </programlisting>
    </xsl:template>
    <!-- -->
    <xsl:template match="property[not(.//dilref)]" mode="description">
    </xsl:template>
    <!-- -->
    <xsl:template match="property[.//dilref]" mode="description">
        <!-- from parameter-topic in common.xslt -->
        <xsl:if test=".//dilref">
            <varlistentry>
                <term>
                <emphasis>
                    <!-- todo: make this a link back to the nodes using the ID attribute of the node -->
                    <xsl:value-of select="translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')" />
                </emphasis>
                </term>
                <listentry>
                    <para>
                        <xsl:apply-templates select="node()" mode="slashdoc" />
                    </para>
                </listentry>
            </varlistentry>
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
        <xsl:if test="./property[.//dilref]">
            <bridgehead renderas"sect3">Attributes</bridgehead>
            <xsl:for-each select="./implements">
                <xsl:call-template name="attrib-description">
                    <xsl:with-param name="interfaceName" select="./text()"/>
                </xsl:call-template>
            </xsl:for-each>
            <variablelist>
                <xsl:for-each select="./property">
                    <xsl:apply-templates select="." mode="description" />
                </xsl:for-each>
            </variablelist>
        </xsl:if>
    </xsl:template>
	<!-- -->
	<xsl:template match="interface">
		<xsl:call-template name="dilref">
			<xsl:with-param name="type">Element</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template name="dilref">
		<xsl:param name="type" />
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id"/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:comment>Generated from dilref.xsl no mode</xsl:comment>
        <sect3>
            <xsl:attribute name="id">
                <xsl:value-of select="concat('dilref',substring-before($filename,'.html')"/>
            </xsl:attribute>
            <title>
                <indexterm>
                    <primary>
                        <xsl:value-of select="concat(../@name,'.',@name)"/>
                    </primary>
                </indexterm>
                <indexterm>
                    <primary>
                        <xsl:value-of select="@name"/>
                    </primary>
                </indexterm>
                <indexterm>
                    <primary>
                        <xsl:text>dilref element</xsl:text>
                    </primary>
                    <secondary>
                        <xsl:value-of select="@name"/>
                    </secondary>
                </indexterm>
                <xsl:value-of select="concat(@name,' ',$type)"/>
            </title>
            <xsl:call-template name="summary-section" />
            <xsl:call-template name="xml-syntax" />
            <xsl:call-template name="xml-attributes"/>
            <xsl:call-template name="remarks-section" />
            <xsl:call-template name="example-section" />
            <xsl:variable name="page">
                members
            </xsl:variable>
            <!-- todo: see also should include only the node represented!! or better understanding of the seealso-section template-->
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page" select="$page" />
            </xsl:call-template>
        </sect3>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>
