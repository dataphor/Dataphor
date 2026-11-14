<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<!--<xsl:include href="common.xslt" />-->
	<!-- -->
	<!--<xsl:param name='type-id' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*[@id=$type-id]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template name="indent">
		<xsl:param name="count" />
		<xsl:if test="$count &gt; 0">
			<!-- <xsl:text>&#160;&#160;&#160;</xsl:text> -->
            <xsl:text>&#32;&#32;&#32;</xsl:text>
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
			
			<xsl:choose>
				<xsl:when test="starts-with($list[$last]/@type, 'System.')">
					<ulink>
						<xsl:attribute name="url">
							<xsl:call-template name="get-filename-for-system-type">
								<xsl:with-param name="type-name" select="$list[$last]/@type" />
							</xsl:call-template>
						</xsl:attribute>
						<xsl:call-template name="get-datatype">
							<xsl:with-param name="datatype" select="$list[$last]/@type" />
						</xsl:call-template>
					</ulink>
				</xsl:when>
				<xsl:otherwise>
					<xsl:variable name="base-class-id" select="string($list[$last]/@id)" />
					<xsl:variable name="base-class" select="//class[@id=$base-class-id]" />
					<xsl:choose>
						<xsl:when test="$base-class">
							<ulink>
								<xsl:attribute name="url">
									<xsl:call-template name="get-filename-for-type">
										<xsl:with-param name="id" select="$list[$last]/@id" />
									</xsl:call-template>
								</xsl:attribute>
								<xsl:call-template name="get-datatype">
									<xsl:with-param name="datatype" select="$list[$last]/@type" />
								</xsl:call-template>
							</ulink>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="get-datatype">
								<xsl:with-param name="datatype" select="$list[$last]/@type" />
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
            <xsl:text>
</xsl:text>
			<xsl:call-template name="draw-hierarchy">
				<xsl:with-param name="list" select="$list[position()!=$last]" />
				<xsl:with-param name="level" select="$level + 1" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="class">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Class</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="interface">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Interface</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="structure">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Structure</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="delegate">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Delegate</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="enumeration">
		<xsl:call-template name="type">
			<xsl:with-param name="type">Enumeration</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template name="type">
		<xsl:param name="type" />
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id"/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:attribute name="id">
            <xsl:value-of select="substring-before($filename,'.html')"/>
        </xsl:attribute>
        <xsl:comment>Generated from type.xsl no mode</xsl:comment>
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
            <xsl:value-of select="concat(@name,' ',$type)"/>
        </title>
        <xsl:call-template name="summary-section" />
        <xsl:if test="local-name()!='delegate' and local-name()!='enumeration'">
            <xsl:variable name="members-href">
                <xsl:call-template name="get-filename-for-type-members">
                    <xsl:with-param name="id" select="@id" />
                </xsl:call-template>
            </xsl:variable>
            <xsl:if test="constructor|field|property|method|operator|event">
                <para>For a list of all members of this type, see <ulink url="{$members-href}"><xsl:value-of select="@name" /> Members</ulink>.</para>
            </xsl:if>
        </xsl:if>
        <xsl:if test="local-name() != 'delegate' and local-name() != 'enumeration' and local-name() != 'interface'">
            <bridgehead renderas="sect4">Hierarchy</bridgehead>
            <literallayout>
                <xsl:choose>
                    <xsl:when test="self::interface">
                        <xsl:if test="base">
                            <xsl:call-template name="draw-hierarchy">
                                <xsl:with-param name="list" select="descendant::base" />
                                <xsl:with-param name="level" select="0" />
                            </xsl:call-template>
                            <xsl:call-template name="indent">
                                <xsl:with-param name="count" select="count(descendant::base)" />
                            </xsl:call-template>
                            <emphasis role="bold">
                                <xsl:value-of select="@name" />
                            </emphasis>
                        </xsl:if>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="href">
                            <xsl:call-template name="get-filename-for-system-type">
                                <xsl:with-param name="type-name" select="'System.Object'" />
                            </xsl:call-template>
                        </xsl:variable>
<ulink url="{$href}">System.Object</ulink>
                        <xsl:text>
</xsl:text>
                        <xsl:call-template name="draw-hierarchy">
                            <xsl:with-param name="list" select="descendant::base" />
                            <xsl:with-param name="level" select="1" />
                        </xsl:call-template>
                        <xsl:call-template name="indent">
                            <xsl:with-param name="count" select="count(descendant::base) + 1" />
                        </xsl:call-template>
                        <emphasis role="bold">
                            <xsl:value-of select="@name" />
                        </emphasis>
                    </xsl:otherwise>
                </xsl:choose>
            </literallayout>
        </xsl:if>
        <bridgehead renderas="sect4">Declaration</bridgehead>
        <xsl:call-template name="vb-type-syntax" />
        <xsl:call-template name="cs-type-syntax" />
        <xsl:if test="local-name() = 'delegate'">
            <xsl:call-template name="parameter-section" />
        </xsl:if>
        <xsl:call-template name="remarks-section" />
        <xsl:call-template name="example-section" />
        <xsl:if test="local-name() = 'enumeration'">
            <xsl:call-template name="members-section" />
        </xsl:if>
        <bridgehead renderas="sect3">Requirements</bridgehead>
        <para>
            <emphasis role="bold">Namespace: </emphasis>
            <ulink>
                <xsl:attribute name="url">
                    <xsl:call-template name="get-filename-for-namespace">
                        <xsl:with-param name="name" select="../@name" />
                    </xsl:call-template>
                </xsl:attribute>
                <xsl:value-of select="../@name" />
                <xsl:text> Namespace</xsl:text>
            </ulink>
        </para>
        <para>
            <emphasis role="bold">Assembly: </emphasis>
            <xsl:value-of select="../../@name" />
        </para>
            <xsl:if test="documentation/permission">
                <para>
                    <!-- todo: complete conversion of the UL -->
                    <variablelist class="permissions">
                        <title>.NET Framework Security: </title>
                        <xsl:for-each select="documentation/permission">
                            <term>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-type-name">
                                            <xsl:with-param name="type-name" select="substring-after(@cref, 'T:')" />
                                        </xsl:call-template>
                                    </xsl:attribute>
                                    <xsl:value-of select="substring-after(@cref, 'T:')" />
                                </ulink>
                           </term>
                           <listitem>
                                <xsl:apply-templates mode="slashdoc" />
                            </listitem>
                        </xsl:for-each>
                    </variablelist>
                </para>
            </xsl:if>
        <xsl:variable name="page">
            <xsl:choose>
                <xsl:when test="local-name() = 'enumeration'">enumeration</xsl:when>
                <xsl:when test="local-name() = 'delegate'">delegate</xsl:when>
                <xsl:otherwise>type</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:call-template name="seealso-section">
            <xsl:with-param name="page" select="$page" />
        </xsl:call-template>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>