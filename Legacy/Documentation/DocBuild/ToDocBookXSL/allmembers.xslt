<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
    <!--
	<xsl:include href="common.xslt" />
	<xsl:include href="memberscommon.xslt" />
    -->
	<!-- -->
	<xsl:param name='id' />
	<!-- -->
	<xsl:template name="type-members">
		<xsl:param name="type" />
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id"/>
            </xsl:call-template>
        </xsl:variable>
        <sect3>
            <xsl:attribute name="id">
                <xsl:value-of select="concat(substring-before($filename,'.html'),'Members')"/>
            </xsl:attribute>
            <xsl:comment>Generated from allmembers.xsl</xsl:comment>
            <title><xsl:value-of select="concat(@name, ' Members')"/></title>
            <!-- public static members -->
            <xsl:call-template name="public-static-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="public-static-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="public-static-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="public-static-section">
                <xsl:with-param name="member" select="'operator'" />
            </xsl:call-template>
            <xsl:call-template name="public-static-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- protected static members -->
            <xsl:call-template name="protected-static-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="protected-static-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="protected-static-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="protected-static-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- protected internal static members -->
            <xsl:call-template name="protected-internal-static-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-static-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-static-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-static-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- internal static members -->
            <xsl:call-template name="internal-static-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="internal-static-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="internal-static-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="internal-static-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- private static members -->
            <xsl:if test="constructor[@access='Private' and @contract='Static']">
                <bridgehead renderas="sect3">Private Static Constructor</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                            <xsl:apply-templates select="constructor[@access='Private' and @contract='Static']"  mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable>
            </xsl:if>
            <xsl:call-template name="private-static-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="private-static-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="private-static-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="private-static-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- public instance members -->
            <xsl:if test="constructor[@access='Public']">
                <bridgehead renderas="sect3">Public Instance Constructors</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                          <xsl:apply-templates select="constructor[@access='Public']" mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable> 
            </xsl:if>
            <xsl:call-template name="public-instance-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="public-instance-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="public-instance-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="public-instance-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- protected instance members -->
            <xsl:if test="constructor[@access='Family']">
                <bridgehead renderas="sect3">Protected Instance Constructors</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                            <xsl:apply-templates select="constructor[@access='Family']"  mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable>
            </xsl:if>
            <xsl:call-template name="protected-instance-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="protected-instance-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="protected-instance-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="protected-instance-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- protected internal instance members -->
            <xsl:if test="constructor[@access='FamilyOrAssembly']">
                <bridgehead renderas="sect3">Protected Internal Instance Constructors</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                            <xsl:apply-templates select="constructor[@access='FamilyOrAssembly']" mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable>
            </xsl:if>
            <xsl:call-template name="protected-internal-instance-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-instance-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-instance-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="protected-internal-instance-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- internal instance members -->
            <xsl:if test="constructor[@access='Assembly']">
                <bridgehead renderas="sect3">Internal Instance Constructors</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                            <xsl:apply-templates select="constructor[@access='Assembly']"  mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable>
            </xsl:if>
            <xsl:call-template name="internal-instance-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="internal-instance-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="internal-instance-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="internal-instance-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <!-- private instance members -->
            <xsl:if test="constructor[@access='Private']">
                <bridgehead renderas="sect3">Private Instance Constructors</bridgehead>
                <informaltable>
                     <tgroup cols="2"><colspec colnum="1" colname="col1"
                          colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                          <tbody>
                            <xsl:apply-templates select="constructor[@access='Private']"  mode="table"/>
                        </tbody>
                     </tgroup>
                </informaltable>
            </xsl:if>
            <xsl:call-template name="private-instance-section">
                <xsl:with-param name="member" select="'field'" />
            </xsl:call-template>
            <xsl:call-template name="private-instance-section">
                <xsl:with-param name="member" select="'property'" />
            </xsl:call-template>
            <xsl:call-template name="private-instance-section">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="private-instance-section">
                <xsl:with-param name="member" select="'event'" />
            </xsl:call-template>
            <xsl:call-template name="explicit-interface-implementations">
                <xsl:with-param name="member" select="'method'" />
            </xsl:call-template>
            <xsl:call-template name="seealso-section">
                <xsl:with-param name="page">members</xsl:with-param>
            </xsl:call-template>
        </sect3>
	</xsl:template>
	<!-- -->
	<xsl:template match="constructor" mode="table">
		<xsl:variable name="access" select="@access" />
        <!--
        <xsl:variable name="filename">
            <xsl:call-template name="get-filename-for-current-constructor"/>
        </xsl:variable>
        -->
        <xsl:comment>generated by constructor template</xsl:comment>
		<xsl:if test="not(preceding-sibling::constructor[@access=$access])">
            <row>
				<xsl:choose>
					<xsl:when test="count(../constructor) &gt; 1">
                        <entry colname="col1">
                            <para>
                                <xsl:choose>
                                    <xsl:when test="@access='Public'">
                                        <inlinegraphic fileref="pubmethod.gif" />
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <inlinegraphic fileref="protmethod.gif" />
                                    </xsl:otherwise>
                                </xsl:choose>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-current-constructor-overloads"/>
                                    </xsl:attribute>
                                    <xsl:value-of select="../@name" />
                                </ulink>
                            </para>
                        </entry>
                        <entry colname="col2">
                            <para>
                                <xsl:text>Overloaded. </xsl:text>
                                <xsl:choose>
                                    <xsl:when test="../constructor/documentation/overloads">
                                        <xsl:call-template name="overloads-summary-with-no-paragraph">
                                            <xsl:with-param name="overloads" select="../constructor" />
                                        </xsl:call-template>
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <xsl:text>Initialize a new instance of the </xsl:text>
                                        <symbol><xsl:value-of select="../@name" /></symbol>
                                        <xsl:text> class.</xsl:text>
                                    </xsl:otherwise>
                                </xsl:choose>
                            </para>
                        </entry>
					</xsl:when>
					<xsl:otherwise>
                        <entry colname="col1">
                            <para>
                                <xsl:choose>
                                    <xsl:when test="@access='Public'">
                                        <inlinegraphic fileref="pubmethod.gif" />
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <inlinegraphic fileref="protmethod.gif" />
                                    </xsl:otherwise>
                                </xsl:choose>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-current-constructor" />
                                    </xsl:attribute>
                                    <xsl:value-of select="../@name" />
                                    <xsl:text> Constructor</xsl:text>
                                </ulink>
                            </para>
                        </entry>
                        <entry colname="col2">
                            <para>
							    <xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" />
                            </para>
                        </entry>
					</xsl:otherwise>
				</xsl:choose>
			</row>
		</xsl:if>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>