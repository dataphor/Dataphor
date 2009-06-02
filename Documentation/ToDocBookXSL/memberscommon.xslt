<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--
    <xsl:template match="/">
		<xsl:apply-templates select="ndoc/assembly/module/namespace/*[@id=$id]" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="class" mode="process-members" >
		<xsl:call-template name="type-members">
			<xsl:with-param name="type">Class</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="interface" mode="process-members">
		<xsl:call-template name="type-members" >
			<xsl:with-param name="type">Interface</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template match="structure" mode="process-members" >
		<xsl:call-template name="type-members">
			<xsl:with-param name="type">Structure</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-big-member-plural">
		<xsl:param name="member" />
		<xsl:choose>
			<xsl:when test="$member='field'">Fields</xsl:when>
			<xsl:when test="$member='property'">Properties</xsl:when>
			<xsl:when test="$member='event'">Events</xsl:when>
			<xsl:when test="$member='operator'">Operators</xsl:when>
            <!-- todo: determine usage of the constructor, in the new it is missing -->
			<xsl:when test="$member='constructor'">Constructors</xsl:when>
			<xsl:otherwise>Methods</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-small-member-plural">
		<xsl:param name="member" />
		<xsl:choose>
			<xsl:when test="$member='field'">fields</xsl:when>
			<xsl:when test="$member='property'">properties</xsl:when>
			<xsl:when test="$member='event'">events</xsl:when>
			<xsl:when test="$member='operator'">operators</xsl:when>
            <!-- todo: determine usage of the constructor, in the new it is missing -->
			<xsl:when test="$member='constructor'">constructors</xsl:when>
			<xsl:otherwise>methods</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="public-static-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Public' and @contract='Static']">
			<bridgehead renderas="sect3">
				<xsl:text>Public Static </xsl:text>
				<xsl:if test="$ndoc-vb-syntax">
  				<xsl:text>(Shared) </xsl:text>
  			</xsl:if>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                           <xsl:apply-templates select="*[local-name()=$member and @access='Public' and @contract='Static']" mode="table">
                               <xsl:sort select="@name" />
                           </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="protected-static-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Family' and @contract='Static']">
			<bridgehead renderas="sect3">
				<xsl:text>Protected Static </xsl:text>
                <xsl:if test="$ndoc-vb-syntax">
                    <xsl:text>(Shared) </xsl:text>
                </xsl:if>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                            <xsl:apply-templates select="*[local-name()=$member and @access='Family' and @contract='Static']" mode="table">
                                <xsl:sort select="@name" />
                            </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="protected-internal-static-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='FamilyOrAssembly' and @contract='Static']">
			<bridgehead renderas="sect3">
				<xsl:text>Protected Internal Static </xsl:text>
                <xsl:if test="$ndoc-vb-syntax">
                    <xsl:text>(Shared) </xsl:text>
                </xsl:if>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                            <xsl:apply-templates select="*[local-name()=$member and @access='FamilyOrAssembly' and @contract='Static']" mode="table">
                                <xsl:sort select="@name" />
                            </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="internal-static-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Assembly' and @contract='Static']">
			<bridgehead renderas="sect3">
				<xsl:text>Internal Static </xsl:text>
                <xsl:if test="$ndoc-vb-syntax">
                    <xsl:text>(Shared) </xsl:text>
                </xsl:if>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                            <xsl:apply-templates select="*[local-name()=$member and @access='Assembly' and @contract='Static']" mode="table">
                                <xsl:sort select="@name" />
                            </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="private-static-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Private' and @contract='Static']">
			<bridgehead renderas="sect3">
				<xsl:text>Private Static </xsl:text>
                <xsl:if test="$ndoc-vb-syntax">
                    <xsl:text>(Shared) </xsl:text>
                </xsl:if>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='Private' and @contract='Static']" mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="public-instance-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Public' and not(@contract='Static')]">
			<bridgehead renderas="sect3">
				<xsl:text>Public Instance </xsl:text>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                            <xsl:apply-templates select="*[local-name()=$member and @access='Public' and not(@contract='Static')]" mode="table">
                                <xsl:sort select="@name" />
                            </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="protected-instance-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Family' and not(@contract='Static')]">
			<bridgehead renderas="sect3">
				<xsl:text>Protected Instance </xsl:text>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='Family' and not(@contract='Static')]" mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="protected-internal-instance-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='FamilyOrAssembly' and not(@contract='Static')]">
			<bridgehead renderas="sect3">
				<xsl:text>Protected Internal Instance </xsl:text>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='FamilyOrAssembly' and not(@contract='Static')]" mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="internal-instance-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Assembly' and not(@contract='Static')]">
			<bridgehead renderas="sect3">
				<xsl:text>Internal Instance </xsl:text>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='Assembly' and not(@contract='Static')]"  mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="private-instance-section">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Private' and not(@contract='Static') and not(@interface)]">
			<bridgehead renderas="sect3">
				<xsl:text>Private Instance </xsl:text>
				<xsl:call-template name="get-big-member-plural">
					<xsl:with-param name="member" select="$member" />
				</xsl:call-template>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='Private' and not(@contract='Static') and not(@interface)]" mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="explicit-interface-implementations">
		<xsl:param name="member" />
		<xsl:if test="*[local-name()=$member and @access='Private' and not(@contract='Static') and @interface]">
			<bridgehead renderas="sect3">
				<xsl:text>Explicit Interface Implementations</xsl:text>
			</bridgehead>
            <informaltable>
                 <tgroup cols="2"><colspec colnum="1" colname="col1"
                      colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/>
                      <tbody>
                        <xsl:apply-templates select="*[local-name()=$member and @access='Private' and not(@contract='Static') and @interface]" mode="table">
                            <xsl:sort select="@name" />
                        </xsl:apply-templates>
                    </tbody>
                 </tgroup>
            </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="property[@declaringType]" mode="table" >
		<xsl:variable name="name" select="@name" />
		<xsl:variable name="declaring-type-id" select="concat('T:', @declaringType)" />
		<xsl:text>&#10;</xsl:text>
		<row>
			<xsl:variable name="declaring-class" select="//class[@id=$declaring-type-id]" />
			<xsl:choose>
				<xsl:when test="$declaring-class">
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubproperty.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protproperty.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <ulink>
                                <xsl:attribute name="url">
                                    <xsl:call-template name="get-filename-for-property">
                                        <xsl:with-param name="property" select="$declaring-class/property[@name=$name]" />
                                    </xsl:call-template>
                                </xsl:attribute>
                                <xsl:value-of select="@name" />
                            </ulink>
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:call-template name="get-datatype">
                                    <xsl:with-param name="datatype" select="@declaringType" />
                                </xsl:call-template>
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
                            <xsl:call-template name="summary-with-no-paragraph">
                                <xsl:with-param name="member" select="//class[@id=$declaring-type-id]/property[@name=$name]" />
                            </xsl:call-template>
                        </para>
					</entry>
				</xsl:when>
				<xsl:otherwise>
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubproperty.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protproperty.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <xsl:value-of select="@name" />
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:value-of select="@declaringType" />
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
						    <xsl:call-template name="summary-with-no-paragraph" />
                        </para>
					</entry>
				</xsl:otherwise>
			</xsl:choose>
		</row>
	</xsl:template>
    <!-- todo: update to docbook from here -->
	<!-- -->
	<xsl:template match="field[@declaringType]" mode="table">
		<xsl:variable name="name" select="@name" />
		<xsl:variable name="declaring-type-id" select="concat('T:', @declaringType)" />
		<xsl:text>&#10;</xsl:text>
		<row>
			<xsl:variable name="declaring-class" select="//class[@id=$declaring-type-id]" />
			<xsl:choose>
				<xsl:when test="$declaring-class">
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubfield.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protfield.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <ulink>
                                <xsl:attribute name="url">
                                    <xsl:call-template name="get-filename-for-field">
                                        <xsl:with-param name="field" select="$declaring-class/field[@name=$name]" />
                                    </xsl:call-template>
                                </xsl:attribute>
                                <xsl:value-of select="@name" />
                            </ulink>
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:call-template name="get-datatype">
                                    <xsl:with-param name="datatype" select="@declaringType" />
                                </xsl:call-template>
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
                            <xsl:call-template name="summary-with-no-paragraph">
                                <xsl:with-param name="member" select="//class[@id=$declaring-type-id]/field[@name=$name]" />
                            </xsl:call-template>
                        </para>
					</entry>
				</xsl:when>
				<xsl:otherwise>
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubfield.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protfield.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <xsl:value-of select="@name" />
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:value-of select="@declaringType" />
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
						    <xsl:call-template name="summary-with-no-paragraph" />
                        </para>
					</entry>
				</xsl:otherwise>
			</xsl:choose>
		</row>
	</xsl:template>
	<!-- -->
	<xsl:template match="property[@declaringType and starts-with(@declaringType, 'System.')]" mode="table">
		<xsl:text>&#10;</xsl:text>
		<row>
			<entry colname="col1">
                <para>
                    <xsl:choose>
                        <xsl:when test="@access='Public'">
                            <inlinegraphic fileref="pubproperty.gif" />
                        </xsl:when>
                        <xsl:otherwise>
                            <inlinegraphic fileref="protproperty.gif" />
                        </xsl:otherwise>
                    </xsl:choose>
                    <xsl:if test="@contract='Static'">
                        <inlinegraphic fileref="static.gif" />
                    </xsl:if>
                    <ulink>
                        <xsl:attribute name="url">
                            <xsl:call-template name="get-filename-for-system-property" />
                        </xsl:attribute>
                        <xsl:value-of select="@name" />
                    </ulink>
                    <xsl:text> (inherited from </xsl:text>
                    <emphasis role="bold">
                        <xsl:call-template name="strip-namespace">
                            <xsl:with-param name="name" select="@declaringType" />
                        </xsl:call-template>
                    </emphasis>
                    <xsl:text>)</xsl:text>
                </para>
			</entry>
			<entry colname="col2">
                <para>
				    <xsl:call-template name="summary-with-no-paragraph" />
                </para>
			</entry>
		</row>
	</xsl:template>
	<!-- -->
	<xsl:template match="method[@declaringType]" mode="table">
		<xsl:variable name="name" select="@name" />
		<xsl:variable name="declaring-type-id" select="concat('T:', @declaringType)" />
		<xsl:if test="not(preceding-sibling::method[@name=$name])">
			<xsl:text>&#10;</xsl:text>
			<row>
				<xsl:variable name="declaring-class" select="//class[@id=$declaring-type-id]" />
				<xsl:choose>
					<xsl:when test="$declaring-class">
						<xsl:choose>
							<xsl:when test="following-sibling::method[@name=$name]">
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
                                      <xsl:if test="@contract='Static'">
                                        <inlinegraphic fileref="static.gif" />
                                      </xsl:if>
                                        <ulink>
                                            <xsl:attribute name="url">
                                                <xsl:call-template name="get-filename-for-inherited-method-overloads">
                                                    <xsl:with-param name="declaring-type" select="@declaringType" />
                                                    <xsl:with-param name="method-name" select="@name" />
                                                </xsl:call-template>
                                            </xsl:attribute>
                                            <xsl:value-of select="@name" />
                                        </ulink>
                                        <xsl:text> (inherited from </xsl:text>
                                        <emphasis role="bold">
                                            <xsl:call-template name="get-datatype">
                                                <xsl:with-param name="datatype" select="@declaringType" />
                                            </xsl:call-template>
                                        </emphasis>
                                        <xsl:text>)</xsl:text>
                                    </para>
								</entry>
								<entry colname="col2">
                                    <para>
                                        <xsl:text>Overloaded. </xsl:text>
                                        <xsl:call-template name="overloads-summary-with-no-paragraph">
                                            <xsl:with-param name="overloads" select="//class[@id=$declaring-type-id]/method[@name=$name]" />
                                        </xsl:call-template>
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
                                      <xsl:if test="@contract='Static'">
                                        <inlinegraphic fileref="static.gif" />
                                      </xsl:if>
                                        <ulink>
                                            <xsl:attribute name="url">
                                                <xsl:call-template name="get-filename-for-method">
                                                    <xsl:with-param name="method" select="$declaring-class/method[@name=$name]" />
                                                </xsl:call-template>
                                            </xsl:attribute>
                                            <xsl:value-of select="@name" />
                                        </ulink>
                                        <xsl:text> (inherited from </xsl:text>
                                        <emphasis role="bold">
                                            <xsl:call-template name="get-datatype">
                                                <xsl:with-param name="datatype" select="@declaringType" />
                                            </xsl:call-template>
                                        </emphasis>
                                        <xsl:text>)</xsl:text>
                                    </para>
								</entry>
								<entry colname="col2">
                                    <para>
                                        <xsl:call-template name="summary-with-no-paragraph">
                                            <xsl:with-param name="member" select="//class[@id=$declaring-type-id]/method[@name=$name]" />
                                        </xsl:call-template>
                                    </para>
								</entry>
							</xsl:otherwise>
						</xsl:choose>
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
                                <xsl:if test="@contract='Static'">
                                    <inlinegraphic fileref="static.gif" />
                                </xsl:if>
                                <xsl:value-of select="@name" />
                                <xsl:text> (inherited from </xsl:text>
                                <emphasis role="bold">
                                    <xsl:value-of select="@declaringType" />
                                </emphasis>
                                <xsl:text>)</xsl:text>
                            </para>
						</entry>
						<entry colname="col2">
                            <para>
							    <xsl:call-template name="summary-with-no-paragraph" />
                            </para>
						</entry>
					</xsl:otherwise>
				</xsl:choose>
			</row>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="method[@declaringType and starts-with(@declaringType, 'System.')]" mode="table">
		<xsl:text>&#10;</xsl:text>
		<row>
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
                    <xsl:if test="@contract='Static'">
                        <inlinegraphic fileref="static.gif" />
                    </xsl:if>
                    <ulink>
                        <xsl:attribute name="url">
                            <xsl:call-template name="get-filename-for-system-method" />
                        </xsl:attribute>
                        <xsl:value-of select="@name" />
                    </ulink>
                    <xsl:text> (inherited from </xsl:text>
                    <emphasis role="bold">
                        <xsl:call-template name="strip-namespace">
                            <xsl:with-param name="name" select="@declaringType" />
                        </xsl:call-template>
                    </emphasis>
                    <xsl:text>)</xsl:text>
                </para>
			</entry>
			<entry colname="col2">
                <para>
				    <xsl:call-template name="summary-with-no-paragraph" />
                </para>
			</entry>
		</row>
	</xsl:template>
	<!-- -->
	<xsl:template match="event[@declaringType]" mode="table">
		<xsl:variable name="name" select="@name" />
		<xsl:variable name="declaring-type-id" select="concat('T:', @declaringType)" />
		<xsl:text>&#10;</xsl:text>
		<row>
			<xsl:variable name="declaring-class" select="//class[@id=$declaring-type-id]" />
			<xsl:choose>
				<xsl:when test="$declaring-class">
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubevent.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protevent.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <ulink>
                                <xsl:attribute name="url">
                                    <xsl:call-template name="get-filename-for-event">
                                        <xsl:with-param name="event" select="$declaring-class/event[@name=$name]" />
                                    </xsl:call-template>
                                </xsl:attribute>
                                <xsl:value-of select="@name" />
                            </ulink>
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:call-template name="get-datatype">
                                    <xsl:with-param name="datatype" select="@declaringType" />
                                </xsl:call-template>
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
                            <xsl:call-template name="summary-with-no-paragraph">
                                <xsl:with-param name="member" select="//class[@id=$declaring-type-id]/event[@name=$name]" />
                            </xsl:call-template>
                        </para>
					</entry>
				</xsl:when>
				<xsl:otherwise>
					<entry colname="col1">
                        <para>
                            <xsl:choose>
                                <xsl:when test="@access='Public'">
                                    <inlinegraphic fileref="pubevent.gif" />
                                </xsl:when>
                                <xsl:otherwise>
                                    <inlinegraphic fileref="protevent.gif" />
                                </xsl:otherwise>
                            </xsl:choose>
                            <xsl:if test="@contract='Static'">
                                <inlinegraphic fileref="static.gif" />
                            </xsl:if>
                            <xsl:value-of select="@name" />
                            <xsl:text> (inherited from </xsl:text>
                            <emphasis role="bold">
                                <xsl:value-of select="@declaringType" />
                            </emphasis>
                            <xsl:text>)</xsl:text>
                        </para>
					</entry>
					<entry colname="col2">
                        <para>
						    <xsl:call-template name="summary-with-no-paragraph" />
                        </para>
					</entry>
				</xsl:otherwise>
			</xsl:choose>
		</row>
	</xsl:template>
	<!-- -->
	<xsl:template match="event[@declaringType and starts-with(@declaringType, 'System.')]" mode="table">
		<xsl:text>&#10;</xsl:text>
		<row>
			<entry colname="col1">
                <para>
                    <xsl:choose>
                        <xsl:when test="@access='Public'">
                            <inlinegraphic fileref="pubevent.gif" />
                        </xsl:when>
                        <xsl:otherwise>
                            <inlinegraphic fileref="protevent.gif" />
                        </xsl:otherwise>
                    </xsl:choose>
                    <xsl:if test="@contract='Static'">
                        <inlinegraphic fileref="static.gif" />
                    </xsl:if>
                    <ulink>
                        <xsl:attribute name="url">
                            <xsl:call-template name="get-filename-for-system-event" />
                        </xsl:attribute>
                        <xsl:value-of select="@name" />
                    </ulink>
                    <xsl:text> (inherited from </xsl:text>
                    <emphasis role="bold">
                        <xsl:call-template name="strip-namespace">
                            <xsl:with-param name="name" select="@declaringType" />
                        </xsl:call-template>
                    </emphasis>
                    <xsl:text>)</xsl:text>
                </para>
			</entry>
			<entry colname="col2">
                <para>
				    <xsl:call-template name="summary-with-no-paragraph" />
                </para>
			</entry>
		</row>
	</xsl:template>
	<!-- -->
	<xsl:template match="field[not(@declaringType)]|property[not(@declaringType)]|event[not(@declaringType)]|method[not(@declaringType)]|operator" mode="table">
		<xsl:variable name="member" select="local-name()" />
		<xsl:variable name="name" select="@name" />
		<xsl:variable name="contract" select="@contract" />
		<xsl:if test="@name='op_Implicit' or @name='op_Explicit' or not(preceding-sibling::*[local-name()=$member and @name=$name and (($contract='Static' and @contract='Static') or ($contract!='Static' and @contract!='Static'))])">
			<xsl:text>&#10;</xsl:text>
			<row>
				<xsl:choose>
					<xsl:when test="@name!='op_Implicit' and @name!='op_Explicit' and following-sibling::*[local-name()=$member and @name=$name and (($contract='Static' and @contract='Static') or ($contract!='Static' and @contract!='Static'))]">
						<entry colname="col1">
                            <para>
                                <xsl:choose>
                                    <xsl:when test="@access='Public'">
                                      <inlinegraphic>
                                        <xsl:attribute name="fileref">
                                          <xsl:text>pub</xsl:text>
                                          <xsl:value-of select="local-name()"/>
                                          <xsl:text>.gif</xsl:text>
                                        </xsl:attribute>
                                      </inlinegraphic>
                                    </xsl:when>
                                    <xsl:otherwise>
                                      <inlinegraphic>
                                        <xsl:attribute name="fileref">
                                          <xsl:text>prot</xsl:text>
                                          <xsl:value-of select="local-name()"/>
                                          <xsl:text>.gif</xsl:text>
                                        </xsl:attribute>
                                      </inlinegraphic>
                                    </xsl:otherwise>
                                </xsl:choose>
                                <xsl:if test="@contract='Static'">
                                    <inlinegraphic fileref="static.gif" />
                                </xsl:if>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-individual-member-overloads">
                                            <xsl:with-param name="member">
                                                <xsl:value-of select="$member" />
                                            </xsl:with-param>
                                        </xsl:call-template>
                                    </xsl:attribute>
                                    <xsl:choose>
                                        <xsl:when test="local-name()='operator'">
                                            <xsl:call-template name="operator-name">
                                                <xsl:with-param name="name" select="@name" />
                                                <xsl:with-param name="from" select="parameter/@type"/>
                                                <xsl:with-param name="to" select="@returnType" />
                                            </xsl:call-template>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <xsl:value-of select="@name" />
                                        </xsl:otherwise>
                                    </xsl:choose>
                                </ulink>
                            </para>
						</entry>
						<entry colname="col2">
                            <para>
                                <xsl:text>Overloaded. </xsl:text>
                                <xsl:call-template name="overloads-summary-with-no-paragraph" />
                            </para>
						</entry>
					</xsl:when>
					<xsl:otherwise>
						<entry colname="col1">
                            <para>
                                <xsl:choose>
                                    <xsl:when test="@access='Public'">
                                      <inlinegraphic>
                                        <xsl:attribute name="fileref">
                                          <xsl:text>pub</xsl:text>
                                          <xsl:value-of select="local-name()"/>
                                          <xsl:text>.gif</xsl:text>
                                        </xsl:attribute>
                                      </inlinegraphic>
                                    </xsl:when>
                                    <xsl:otherwise>
                                      <inlinegraphic>
                                        <xsl:attribute name="fileref">
                                          <xsl:text>prot</xsl:text>
                                          <xsl:value-of select="local-name()"/>
                                          <xsl:text>.gif</xsl:text>
                                        </xsl:attribute>
                                      </inlinegraphic>
                                    </xsl:otherwise>
                                </xsl:choose>
                                <xsl:if test="@contract='Static'">
                                    <inlinegraphic fileref="static.gif" />
                                </xsl:if>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-individual-member">
                                            <xsl:with-param name="member">
                                                <xsl:value-of select="$member" />
                                            </xsl:with-param>
                                        </xsl:call-template>
                                    </xsl:attribute>
                                    <xsl:choose>
                                        <xsl:when test="local-name()='operator'">
                                            <xsl:call-template name="operator-name">
                                                <xsl:with-param name="name" select="@name" />
                                                <xsl:with-param name="from" select="parameter/@type"/>
                                                <xsl:with-param name="to" select="@returnType" />
                                            </xsl:call-template>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <xsl:value-of select="@name" />
                                        </xsl:otherwise>
                                    </xsl:choose>
                                </ulink>
                            </para>
						</entry>
						<entry colname="col2">
                            <para>
							    <xsl:call-template name="summary-with-no-paragraph" />
                            </para>
						</entry>
					</xsl:otherwise>
				</xsl:choose>
			</row>
		</xsl:if>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>
