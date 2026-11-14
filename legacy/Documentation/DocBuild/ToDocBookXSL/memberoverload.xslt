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
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*/*[@id=$member-id][1]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="method | constructor | property | operator" mode="overload">
		<xsl:variable name="type">
			<xsl:choose>
				<xsl:when test="local-name(..)='interface'">Interface</xsl:when>
				<xsl:otherwise>Class</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="childType">
			<xsl:choose>
				<xsl:when test="local-name()='method'">Method</xsl:when>
				<xsl:when test="local-name()='constructor'">Constructor</xsl:when>
				<xsl:when test="local-name()='operator'">
                    <xsl:call-template name="operator-name">
                        <xsl:with-param name="name">
                          <xsl:value-of select="@name" />
                        </xsl:with-param>
                    </xsl:call-template>
				</xsl:when>
				<xsl:otherwise>Property</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="member">
			<xsl:choose>
				<xsl:when test="local-name()='method'">method</xsl:when>
				<xsl:when test="local-name()='constructor'">constructor</xsl:when>
				<xsl:when test="local-name()='operator'">
                    <xsl:call-template name="operator-name">
                        <xsl:with-param name="name">
                          <xsl:value-of select="@name" />
                        </xsl:with-param>
                    </xsl:call-template>
				</xsl:when>
				<xsl:otherwise>property</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
        <xsl:variable name="filename">
            <xsl:choose>
                <xsl:when test="local-name()='operator'">
                    <xsl:call-template name="get-filename-for-operator">
                    </xsl:call-template>
                </xsl:when>
                <xsl:when test="local-name()='constructor'">
                    <xsl:call-template name="get-filename-for-current-constructor-overloads"/>
                </xsl:when>
                <!-- handles method and property -->
                <xsl:otherwise>
                    <xsl:call-template name="get-filename-for-individual-member-overloads"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
		<xsl:variable name="memberName" select="@name" />
        <sect3>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <xsl:comment>Generated from memberoverload.xsl</xsl:comment>
            <title>
                <xsl:value-of select="../@name" />
                <xsl:if test="local-name()!='constructor'">
                    <xsl:text>.</xsl:text>
                    <xsl:value-of select="@name" />
                </xsl:if>
                <xsl:text>&#32;</xsl:text>
                <xsl:value-of select="$childType" />
            </title>
            <xsl:call-template name="overloads-summary-section" />
                <variablelist>
                    <title>Overload List</title>
                    <xsl:for-each select="parent::node()/*[@name=$memberName]">
                        <xsl:sort select="@name" />
                        <varlistentry>        
                            <xsl:choose>
                                <xsl:when test="@declaringType and starts-with(@declaringType, 'System.')">
                                    <term>
                                        <xsl:text>Inherited from </xsl:text>
                                        <ulink>
                                            <xsl:attribute name="url">
                                                <xsl:call-template name="get-filename-for-type-name">
                                                    <xsl:with-param name="type-name" select="@declaringType" />
                                                </xsl:call-template>
                                            </xsl:attribute>
                                            <xsl:call-template name="strip-namespace">
                                                <xsl:with-param name="name" select="@declaringType" />
                                            </xsl:call-template>
                                        </ulink>
                                        <xsl:text>.</xsl:text>
                                    </term>
                                    <listitem>
                                        <para>
                                            <ulink>
                                                <xsl:attribute name="url">
                                                    <xsl:call-template name="get-filename-for-system-method" />
                                                </xsl:attribute>
                                                <xsl:apply-templates select="self::node()" mode="syntax" />
                                            </ulink>
                                       </para>
                                    </listitem>
                                </xsl:when>
                                <xsl:when test="@declaringType">
                                    <term>
                                        <xsl:text>Inherited from </xsl:text>
                                        <ulink>
                                            <xsl:attribute name="url">
                                                <xsl:call-template name="get-filename-for-type-name">
                                                    <xsl:with-param name="type-name" select="@declaringType" />
                                                </xsl:call-template>
                                            </xsl:attribute>
                                            <xsl:call-template name="strip-namespace">
                                                <xsl:with-param name="name" select="@declaringType" />
                                            </xsl:call-template>
                                        </ulink>
                                        <xsl:text>.</xsl:text>
                                    </term>
                                    <listitem>
                                        <para>
                                            <ulink>
                                                <xsl:attribute name="url">
                                                    <xsl:call-template name="get-filename-for-inherited-method-overloads">
                                                        <xsl:with-param name="declaring-type" select="@declaringType" />
                                                        <xsl:with-param name="method-name" select="@name" />
                                                    </xsl:call-template>
                                                </xsl:attribute>
                                                <xsl:apply-templates select="self::node()" mode="syntax" />
                                            </ulink>
                                        </para>
                                    </listitem>
                                </xsl:when>
                                <xsl:otherwise>
                                    <term>
                                        <xsl:call-template name="summary-with-no-paragraph">
                                            <xsl:with-param name="member" select="." />
                                        </xsl:call-template>
                                    </term>
                                    <listitem>
                                        <para>
                                            <ulink>
                                                <xsl:attribute name="url">
                                                    <xsl:call-template name="get-filename-for-cref-overload">
                                                        <xsl:with-param name="cref" select="@id" />
                                                        <xsl:with-param name="overload" select="@overload" />
                                                    </xsl:call-template>
                                                </xsl:attribute>
                                                <xsl:apply-templates select="self::node()" mode="syntax" />
                                            </ulink>
                                        </para>
                                    </listitem>
                                </xsl:otherwise>
                            </xsl:choose>
                        </varlistentry>
                    </xsl:for-each>
                </variablelist>
            <xsl:call-template name="overloads-remarks-section" />
            <xsl:call-template name="overloads-example-section" />
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page">memberoverload</xsl:with-param>
            </xsl:call-template>
               
            <xsl:choose>
                <xsl:when test="$member='constructor'">
                    <!-- todo: see if this overduplicates the work -->
                   <xsl:apply-templates select="../constructor" mode="singleton">
                       <xsl:sort select="@name" />
                   </xsl:apply-templates>
                </xsl:when>
                <xsl:when test="$member='method'">
                    <xsl:comment>Generating method overload children for <xsl:value-of select="$memberName"/></xsl:comment>
                   <xsl:apply-templates select="../method[$memberName=@name]" mode="singleton">
                       <xsl:sort select="@name" />
                   </xsl:apply-templates>
                </xsl:when>
                <xsl:when test="$member='property'">
                   <xsl:apply-templates select="../property[$memberName=@name]" mode="singleton">
                       <xsl:sort select="@name" />
                   </xsl:apply-templates>
                </xsl:when>
                <xsl:when test="$member='operator'">
                   <xsl:apply-templates select="../operator[$memberName=@name]" mode="singleton">
                       <xsl:sort select="@name" />
                   </xsl:apply-templates>
                </xsl:when>
            </xsl:choose>
        </sect3>
	</xsl:template>
	<!-- -->
	<xsl:template match="constructor | method | operator" mode="syntax">
		<xsl:call-template name="member-syntax2" />
	</xsl:template>
	<!-- -->
	<xsl:template match="property" mode="syntax">
		<xsl:call-template name="cs-property-syntax">
			<xsl:with-param name="indent" select="false()" />
			<xsl:with-param name="display-names" select="false()" />
			<xsl:with-param name="link-types" select="false()" />
		</xsl:call-template>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>
