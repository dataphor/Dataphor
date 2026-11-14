<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<!--<xsl:include href="common.xslt" />-->
	<!-- -->
	<!--<xsl:param name='namespace' />-->
	<!-- -->
	<xsl:template match="namespace" mode="hierarchy">
        <xsl:variable name="ns" select="ndoc/assembly/module/namespace[@name=$namespacename]" />
        <xsl:variable name="filename">
            <xsl:call-template name="get-filename-for-current-namespace-hierarchy">
                <xsl:with-param name="namespace" select="@name"/>
            </xsl:call-template>
        </xsl:variable>
        <sect2>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <title><xsl:value-of select="concat($ns/@name, ' Hierarchy')"/></title>
            <literallayout>
            <ulink>
                    <xsl:attribute name="url">
                        <xsl:call-template name="get-filename-for-system-type">
                            <xsl:with-param name="type-name" select="'System.Object'" />
                        </xsl:call-template>
                    </xsl:attribute>
                    <xsl:text>System.Object</xsl:text>
            </ulink>
                <!--<para/>-->
            <xsl:variable name="roots" select="$ns//*[(local-name()='class' and not(base)) or (local-name()='base' and not(base))]" />
            <xsl:call-template name="call-draw">
                <xsl:with-param name="nodes" select="$roots" />
                <xsl:with-param name="level" select="1" />
            </xsl:call-template>
                <!--<para/>-->
            <xsl:if test="$ns/interface">
            <bridgehead renderas="sect3">Interfaces</bridgehead>
            <para>
                <xsl:apply-templates select="$ns/interface" mode="hierarchy">
                    <xsl:sort select="@name" />
                </xsl:apply-templates>
            </para>
            </xsl:if>
            </literallayout>
        </sect2>
	</xsl:template>
	<!-- -->
	<xsl:template name="call-draw">
		<xsl:param name="nodes" />
		<xsl:param name="level" />
		<xsl:for-each select="$nodes">
			<xsl:sort select="@name" />
			<xsl:if test="position() = 1">
				<xsl:variable name="head" select="." />
				<xsl:call-template name="draw">
					<xsl:with-param name="head" select="$head" />
					<xsl:with-param name="tail" select="$nodes[@name != $head/@name]" />
					<xsl:with-param name="level" select="$level" />
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!-- -->
	<xsl:template name="draw">
		<xsl:param name="head" />
		<xsl:param name="tail" />
		<xsl:param name="level" />
		<xsl:call-template name="indentHierarchy">
			<xsl:with-param name="count" select="$level" />
		</xsl:call-template>
		<xsl:text>-</xsl:text>
		<ulink>
			<xsl:attribute name="url">
				<xsl:choose>
					<xsl:when test="starts-with($head/@id, 'T:System.')">
						<xsl:call-template name="get-filename-for-system-type">
							<xsl:with-param name="type-name" select="substring-after($head/@id, 'T:')" />
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="get-filename-for-type">
							<xsl:with-param name="id" select="$head/@id" />
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			<xsl:call-template name="get-datatype">
				<xsl:with-param name="datatype" select="substring-after($head/@id, 'T:')" />
			</xsl:call-template>
		</ulink>
		<para/>
		<xsl:variable name="derivatives" select="/ndoc/assembly/module/namespace/class[base/@id = $head/@id] | /ndoc/assembly/module/namespace/class/descendant::base[base[@id = $head/@id]]" />
		<xsl:if test="$derivatives">
			<xsl:call-template name="call-draw">
				<xsl:with-param name="nodes" select="$derivatives" />
				<xsl:with-param name="level" select="$level + 1" />
			</xsl:call-template>
		</xsl:if>
		<xsl:if test="$tail">
			<xsl:call-template name="call-draw">
				<xsl:with-param name="nodes" select="$tail" />
				<xsl:with-param name="level" select="$level" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="indentHierarchy">
		<xsl:param name="count" />
		<xsl:if test="$count &gt; 0">
			<xsl:text>&#32;&#32;</xsl:text>
			<xsl:call-template name="indentHierarchy">
				<xsl:with-param name="count" select="$count - 1" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="interface" mode="hierarchy">
		<ulink>
			<xsl:attribute name="url">
				<xsl:call-template name="get-filename-for-type">
					<xsl:with-param name="id" select="@id" />
				</xsl:call-template>
			</xsl:attribute>
			<xsl:value-of select="@name" />
		</ulink>
		<para/>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>