<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<xsl:include href="filenames.xslt" />
	<xsl:include href="syntax.xslt" />
	<xsl:include href="vb-syntax.xslt" />
	<xsl:include href="tags.xslt" />
	<!-- -->
	<xsl:param name="ndoc-title" />
    <xsl:param name="ndoc-omit-object-tags" select="false" />
	<!-- -->
	<xsl:template name="csharp-type">
		<xsl:param name="runtime-type" />
		<xsl:variable name="old-type">
			<xsl:choose>
				<xsl:when test="contains($runtime-type, '[')">
					<xsl:value-of select="substring-before($runtime-type, '[')" />
				</xsl:when>
				<xsl:when test="contains($runtime-type, '&amp;')">
					<xsl:value-of select="substring-before($runtime-type, '&amp;')" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$runtime-type" />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="new-type">
			<xsl:choose>
				<xsl:when test="$old-type='System.Byte'">byte</xsl:when>
				<xsl:when test="$old-type='Byte'">byte</xsl:when>
				<xsl:when test="$old-type='System.SByte'">sbyte</xsl:when>
				<xsl:when test="$old-type='SByte'">sbyte</xsl:when>
				<xsl:when test="$old-type='System.Int16'">short</xsl:when>
				<xsl:when test="$old-type='Int16'">short</xsl:when>
				<xsl:when test="$old-type='System.UInt16'">ushort</xsl:when>
				<xsl:when test="$old-type='UInt16'">ushort</xsl:when>
				<xsl:when test="$old-type='System.Int32'">int</xsl:when>
				<xsl:when test="$old-type='Int32'">int</xsl:when>
				<xsl:when test="$old-type='System.UInt32'">uint</xsl:when>
				<xsl:when test="$old-type='UInt32'">uint</xsl:when>
				<xsl:when test="$old-type='System.Int64'">long</xsl:when>
				<xsl:when test="$old-type='Int64'">long</xsl:when>
				<xsl:when test="$old-type='System.UInt64'">ulong</xsl:when>
				<xsl:when test="$old-type='UInt64'">ulong</xsl:when>
				<xsl:when test="$old-type='System.Single'">float</xsl:when>
				<xsl:when test="$old-type='Single'">float</xsl:when>
				<xsl:when test="$old-type='System.Double'">double</xsl:when>
				<xsl:when test="$old-type='Double'">double</xsl:when>
				<xsl:when test="$old-type='System.Decimal'">decimal</xsl:when>
				<xsl:when test="$old-type='Decimal'">decimal</xsl:when>
				<xsl:when test="$old-type='System.String'">string</xsl:when>
				<xsl:when test="$old-type='String'">string</xsl:when>
				<xsl:when test="$old-type='System.Char'">char</xsl:when>
				<xsl:when test="$old-type='Char'">char</xsl:when>
				<xsl:when test="$old-type='System.Boolean'">bool</xsl:when>
				<xsl:when test="$old-type='Boolean'">bool</xsl:when>
				<xsl:when test="$old-type='System.Void'">void</xsl:when>
				<xsl:when test="$old-type='Void'">void</xsl:when>
				<xsl:when test="$old-type='System.Object'">object</xsl:when>
				<xsl:when test="$old-type='Object'">object</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$old-type" />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="contains($runtime-type, '[')">
				<xsl:value-of select="concat($new-type, '[', substring-after($runtime-type, '['))" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$new-type" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="type-access">
		<xsl:param name="access" />
        <xsl:param name="type" />
		<xsl:choose>
			<xsl:when test="$access='Public'">public</xsl:when>
			<xsl:when test="$access='NotPublic' and $type='interface'">internal</xsl:when>
			<xsl:when test="$access='NotPublic' and $type!='interface'">private</xsl:when>
			<xsl:when test="$access='NestedPublic'">public</xsl:when>
			<xsl:when test="$access='NestedFamily'">protected</xsl:when>
			<xsl:when test="$access='NestedFamilyOrAssembly'">protected internal</xsl:when>
			<xsl:when test="$access='NestedAssembly'">internal</xsl:when>
			<xsl:when test="$access='NestedPrivate'">private</xsl:when>
			<xsl:otherwise>/* unknown */</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="method-access">
		<xsl:param name="access" />
		<xsl:choose>
			<xsl:when test="$access='Public'">public</xsl:when>
			<xsl:when test="$access='Family'">protected</xsl:when>
			<xsl:when test="$access='FamilyOrAssembly'">protected internal</xsl:when>
			<xsl:when test="$access='Assembly'">internal</xsl:when>
			<xsl:when test="$access='Private'">private</xsl:when>
			<xsl:otherwise>/* unknown */</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="contract">
		<xsl:param name="contract" />
		<xsl:choose>
			<xsl:when test="$contract='Static'">static</xsl:when>
			<xsl:when test="$contract='Abstract'">abstract</xsl:when>
            <!-- todo: determine if final is to be displayed or treated as normal -->
			<xsl:when test="$contract='Final'">final</xsl:when>
			<xsl:when test="$contract='Virtual'">virtual</xsl:when>
			<xsl:when test="$contract='Override'">override</xsl:when>
			<xsl:when test="$contract='Normal'"></xsl:when>
			<xsl:otherwise>/* unknown */</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="parameter-topic">
        <variablelist>
            <xsl:for-each select="parameter">
			     <xsl:variable name="name" select="@name" />
                <varlistentry>
                    <term><xsl:value-of select="@name" /></term>
                    <listitem>
                        <para>
                            <xsl:apply-templates select="parent::node()/documentation/param[@name=$name]/node()" mode="slashdoc" />
                        </para>
                    </listitem>
                </varlistentry>
            </xsl:for-each>
        </variablelist>
	</xsl:template>
	<!-- -->
	<xsl:template name="type-mixed">
		<xsl:choose>
			<xsl:when test="local-name()='constructor' or local-name()='property' or local-name()='method' or local-name()='event' or local-name()='operator'">
				<xsl:choose>
					<xsl:when test="local-name(..)='interface'">Interface</xsl:when>
					<xsl:otherwise>Class</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="local-name()='interface'">Interface</xsl:when>
					<xsl:otherwise>Class</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="type-element">
		<xsl:choose>
			<xsl:when test="local-name()='constructor' or local-name()='field' or local-name()='property' or local-name()='method' or local-name()='event' or local-name()='operator'">
				<xsl:value-of select="local-name(..)" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="local-name()" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="type-name">
		<xsl:choose>
			<xsl:when test="local-name()='constructor' or local-name()='field' or local-name()='property' or local-name()='method' or local-name()='event' or local-name()='operator'">
				<xsl:value-of select="../@name" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="@name" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="type-id">
		<xsl:choose>
			<xsl:when test="local-name()='constructor' or local-name()='field' or local-name()='property' or local-name()='method' or local-name()='event' or local-name()='operator'">
				<xsl:value-of select="../@id" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="@id" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="namespace-name">
		<xsl:choose>
			<xsl:when test="local-name()='constructor' or local-name()='field' or local-name()='property' or local-name()='method' or local-name()='event' or local-name()='operator'">
				<xsl:value-of select="../../@name" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="../@name" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
  <!-- -->
  <xsl:template name="get-system-href">
    <xsl:param name="cref" />
    <xsl:choose>
		<xsl:when test="starts-with($cref,'T:')">
			<xsl:call-template name="get-filename-for-system-type">
				<xsl:with-param name="class-name" select="substring-after($cref, 'T:')" />
			</xsl:call-template>
		</xsl:when>
		<xsl:when test="starts-with($cref,'P:')">
			<!-- TODO: verify this works with the change in filename.xslt -->
			<xsl:call-template name="get-filename-for-system-property">
				<xsl:with-param name="property-name" select="substring-after($cref, 'P:')" />
			</xsl:call-template>
		</xsl:when>              
		<!-- commented out until required
		<xsl:when test="starts-with($cref,'F:')">
			<xsl:call-template name="get-filename-for-system-field">
				<xsl:with-param name="field-name" select="substring-after($cref, 'F:')" />
			</xsl:call-template>
		</xsl:when>
		-->
		<xsl:when test="starts-with($cref,'M:')">
			<xsl:call-template name="get-filename-for-system-method">
				<xsl:with-param name="method-name" select="substring-after($cref, 'M:')" />
			</xsl:call-template>
		</xsl:when>
		<xsl:when test="starts-with($cref,'E:')">
			<xsl:call-template name="get-filename-for-system-method">
				<xsl:with-param name="method-name" select="substring-after($cref, 'E:')" />
			</xsl:call-template>
		</xsl:when>
		<!-- TODO: handle the F: and E: prefixes -->
        <!-- D: is an "external" model started with the DILRef -->
		<xsl:when test="starts-with($cref,'D:')">
            <xsl:value-of select="substring($cref,3)"/>
		</xsl:when>
		<xsl:otherwise>
			<xsl:call-template name="get-filename-for-system-type">
				<xsl:with-param name="class-name" select="$cref" />
			</xsl:call-template>
		</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!-- -->
  <xsl:template name="parts-section">
    <xsl:for-each select="part">
        <xsl:if test=".//para//text() | .//list//text() | ..//p//text()">
			<xsl:apply-templates select="*" mode="slashdoc"/>
        </xsl:if>
    </xsl:for-each>
  </xsl:template>
	<!-- -->
	<xsl:template name="seealso-section">
		<xsl:param name="page" />
		<xsl:variable name="typeMixed">
			<xsl:call-template name="type-mixed" />
		</xsl:variable>
		<xsl:variable name="typeElement">
			<xsl:call-template name="type-element" />
		</xsl:variable>
		<xsl:variable name="typeName">
			<xsl:call-template name="type-name" />
		</xsl:variable>
		<xsl:variable name="typeID">
			<xsl:call-template name="type-id" />
		</xsl:variable>
		<xsl:variable name="namespaceName">
			<xsl:call-template name="namespace-name" />
		</xsl:variable>
        <bridgehead renderas="sect3">See Also</bridgehead>
            <para>
                <xsl:if test="$page!='type' and $page!='enumeration' and $page!='delegate'">
                    <xsl:variable name="type-filename">
                        <xsl:call-template name="get-filename-for-type">
                            <xsl:with-param name="id" select="$typeID" />
                        </xsl:call-template>
                    </xsl:variable>
                    <!-- todo: convert to link -->
                    <ulink url="{$type-filename}">
                        <xsl:value-of select="concat($typeName, ' ', $typeMixed)" />
                    </ulink>
                    <xsl:text> | </xsl:text>
                </xsl:if>
                <xsl:if test="$page!='members' and $page!='enumeration' and $page!='delegate' and $page!='methods' and $page!='properties' and $page!='fields' and $page!='events'">
                    <ulink>
                        <xsl:attribute name="url">
                            <xsl:call-template name="get-filename-for-type-members">
                                <xsl:with-param name="id" select="$typeID" />
                            </xsl:call-template>
                        </xsl:attribute>
                        <xsl:value-of select="$typeName" />
                        <xsl:text> Members</xsl:text>
                    </ulink>
                    <xsl:text> | </xsl:text>
                </xsl:if>
                <ulink>
                    <xsl:attribute name="url">
                        <xsl:call-template name="get-filename-for-namespace">
                            <xsl:with-param name="name" select="$namespaceName" />
                        </xsl:call-template>
                    </xsl:attribute>
                    <xsl:value-of select="$namespaceName" />
                    <xsl:text> Namespace</xsl:text>
                </ulink>
                <xsl:if test="$page='member' or $page='property'">
                    <xsl:variable name="memberName" select="@name" />
                    <xsl:if test="count(parent::node()/*[@name=$memberName]) &gt; 1">
                        <xsl:choose>
                            <xsl:when test="local-name()='operator'">
                                <xsl:text> | </xsl:text>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-current-method-overloads" />
                                    </xsl:attribute>
                                    <xsl:value-of select="concat($typeName, '.', @name)" />
                                    <xsl:text> Overload List</xsl:text>
                                </ulink>
                            </xsl:when>
                            <xsl:when test="local-name()='constructor'">
                                <xsl:text> | </xsl:text>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-current-constructor-overloads" />
                                    </xsl:attribute>
                                    <xsl:value-of select="$typeName" />
                                    <xsl:text> Constructor Overload List</xsl:text>
                                </ulink>
                            </xsl:when>
                            <xsl:when test="local-name()='property'">
                                <xsl:text> | </xsl:text>
                                <ulink>
                                <xsl:attribute name="url">
                                    <xsl:call-template name="get-filename-for-current-property-overloads" />
                                </xsl:attribute>
                                <xsl:value-of select="concat($typeName, '.', @name)" />
                                <xsl:text> Overload List</xsl:text>
                            </ulink>
                        </xsl:when>
                            <xsl:when test="local-name()='method'">
                                <xsl:text> | </xsl:text>
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-current-method-overloads" />
                                    </xsl:attribute>
                                    <xsl:value-of select="concat($typeName, '.', @name)" />
                                    <xsl:text> Overload List</xsl:text>
                                </ulink>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:if>
                </xsl:if>
                <xsl:if test="documentation/seealso">
                    <xsl:for-each select="documentation//seealso">
                        <xsl:text> | </xsl:text>
                        <xsl:choose>
                            <xsl:when test="@cref">
                                <xsl:call-template name="get-a-href">
                                    <xsl:with-param name="cref" select="@cref"/>
                                </xsl:call-template>
                            </xsl:when>
                            <xsl:when test="@href">
                                <ulink url="{@href}">
                                    <xsl:value-of select="." />
                                </ulink>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:for-each>
                </xsl:if>
            </para>
	</xsl:template>
	<!-- -->
	<xsl:template name="output-paragraph">
		<xsl:param name="nodes" />
		<xsl:choose>
			<xsl:when test="not($nodes/self::para | $nodes/self::p)">
				<para>
					<xsl:apply-templates select="$nodes" mode="slashdoc" />
				</para>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="$nodes" mode="slashdoc" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template match="para | p" mode="no-para">
		<xsl:apply-templates mode="slashdoc" />
	</xsl:template>
	<!-- -->
	<xsl:template match="note" mode="no-para">
        <note>
            <title>Note</title>
            <!-- todo: determine if the para is required -->
            <para>
			<xsl:apply-templates mode="slashdoc" />
            </para>
        </note>
	</xsl:template>
	<!-- -->
	<xsl:template match="node()" mode="no-para">
		<xsl:apply-templates select="." mode="slashdoc" />
	</xsl:template>
	<!-- -->
	<xsl:template name="summary-section">
    <xsl:if test="(documentation/summary)[1]/node()">
      <xsl:call-template name="output-paragraph">
        <xsl:with-param name="nodes" select="(documentation/summary)[1]/node()" />
      </xsl:call-template>
    </xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="summary-with-no-paragraph">
		<xsl:param name="member" select="." />
	    <xsl:apply-templates select="($member/documentation/summary)[1]/node()" mode="no-para" />
	</xsl:template>
	<!-- -->
	<xsl:template name="overloads-summary-section">
        <xsl:variable name="memberName" select="@name" />
		<xsl:choose>
			<xsl:when test="parent::node()/*[@name=$memberName]/documentation/overloads/summary">
				<xsl:call-template name="output-paragraph">
					<xsl:with-param name="nodes" select="(parent::node()/*[@name=$memberName]/documentation/overloads/summary)[1]/node()" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="parent::node()/*[@name=$memberName]/documentation/overloads">
				<xsl:call-template name="output-paragraph">
					<xsl:with-param name="nodes" select="(parent::node()/*[@name=$memberName]/documentation/overloads)[1]/node()" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="summary-section" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="overloads-summary-with-no-paragraph">
		<xsl:param name="overloads" select="." />
		<xsl:variable name="memberName" select="@name" />
		<xsl:choose>
			<xsl:when test="$overloads/../*[@name=$memberName]/documentation/overloads/summary">
				<xsl:apply-templates select="($overloads/../*[@name=$memberName]/documentation/overloads/summary)[1]/node()" mode="no-para" />
			</xsl:when>
			<xsl:when test="$overloads/../*[@name=$memberName]/documentation/overloads">
				<xsl:apply-templates select="($overloads/../*[@name=$memberName]/documentation/overloads)[1]/node()" mode="no-para" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="summary-with-no-paragraph">
					<xsl:with-param name="member" select="$overloads" />
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="overloads-remarks-section">
		<xsl:if test="documentation/overloads/remarks">
			<bridgehead renderas="sect3">Remarks</bridgehead>
			<para>
				<xsl:apply-templates select="(documentation/overloads/remarks)[1]/node()" mode="slashdoc" />
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="overloads-example-section">
		<xsl:if test="documentation/overloads/example">
            <!-- todo: validate that the para is needed -->
            <bridgehead renderas="sect3">Example</bridgehead>
			<para>
				<xsl:apply-templates select="(documentation/overloads/example)[1]/node()" mode="slashdoc" />
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="parameter-section">
		<xsl:if test="documentation/param">
			<bridgehead renderas="sect3">Parameters</bridgehead>
			<xsl:call-template name="parameter-topic" />
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="returnvalue-section">
		<xsl:if test="documentation/returns">
			<bridgehead renderas="sect3">Return Value</bridgehead>
			<para>
				<xsl:apply-templates select="documentation/returns/node()" mode="slashdoc" />
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="implements-section">
		<xsl:if test="implements">
			<xsl:variable name="member" select="local-name()" />
			<bridgehead renderas="sect3">Implements</bridgehead>
			<xsl:for-each select="implements">
				<xsl:variable name="declaring-type-id" select="@interfaceId" />
				<xsl:variable name="name" select="@name" />
				<xsl:variable name="declaring-interface" select="//interface[@id=$declaring-type-id]" />
				<para>
					<ulink>
						<xsl:attribute name="url">
							<xsl:choose>
								<xsl:when test="$member='property'">
									<xsl:choose>
										<xsl:when test="starts-with(@declaringType, 'System.')" >
											<xsl:call-template name="get-filename-for-system-property" />
										</xsl:when>
										<xsl:otherwise>
											<xsl:call-template name="get-filename-for-property">
												<xsl:with-param name="property" select="$declaring-interface/property[@name=$name]" />
											</xsl:call-template>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:when>
								<xsl:when test="$member='event'">
									<xsl:choose>
										<xsl:when test="starts-with(@declaringType, 'System.')" >
											<xsl:call-template name="get-filename-for-system-event" />
										</xsl:when>
										<xsl:otherwise>
											<xsl:call-template name="get-filename-for-event">
												<xsl:with-param name="event" select="$declaring-interface/event[@name=$name]" />
											</xsl:call-template>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:when>
								<xsl:otherwise>
									<xsl:choose>
										<xsl:when test="starts-with(@declaringType, 'System.')" >
											<xsl:call-template name="get-filename-for-system-method" />
										</xsl:when>
										<xsl:otherwise>
											<xsl:call-template name="get-filename-for-method">
												<xsl:with-param name="method" select="$declaring-interface/method[@name=$name]" />
											</xsl:call-template>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:attribute>
						<xsl:value-of select="@interface" /><xsl:text>.</xsl:text><xsl:value-of select="@name" />
					</ulink>
				</para>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="remarks-section">
		<xsl:if test="documentation/remarks">
			<bridgehead renderas="sect3">Remarks</bridgehead>
			<xsl:variable name="first-element" select="local-name(documentation/remarks/*[1])" />
			<xsl:choose>
				<xsl:when test="$first-element!='para' and $first-element!='p'">
					<para>
						<xsl:apply-templates select="documentation/remarks/node()" mode="slashdoc" />
					</para>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates select="documentation/remarks/node()" mode="slashdoc" />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="value-section">
		<xsl:if test="documentation/value">
			<bridgehead renderas="sect3">Property Value</bridgehead>
			<para>
				<xsl:apply-templates select="documentation/value/node()" mode="slashdoc" />
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="events-section">
		<xsl:if test="documentation/event">
			<bridgehead renderas="sect3">Events</bridgehead>
             <informaltable>
                  <tgroup cols="2"><colspec colnum="1" colname="col1"
                        colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                             <row><entry colname="col1">Event Type</entry><entry
                                  colname="col2">Reason</entry>
                             </row></thead><tbody>
                             <xsl:for-each select="documentation/event">
                                <xsl:sort select="@name" />
                                <xsl:variable name="cref" select="@cref" />
                                 <row>
                                    <entry colname="col1">
                                        <para>
                                            <xsl:variable name="type-filename">
                                                <xsl:call-template name="get-filename-for-cref">
                                                    <xsl:with-param name="cref" select="@cref" />
                                                </xsl:call-template>
                                            </xsl:variable>
                                            <ulink url="{$type-filename}">
                                                <xsl:call-template name="strip-namespace">
                                                    <xsl:with-param name="name" select="substring-after(@cref, 'F:')" />
                                                </xsl:call-template>
                                            </ulink>
                                        </para>
                                    </entry>
                                    <entry colname="col2">
                                        <xsl:apply-templates select="./node()" mode="slashdoc" />
                                    </entry>
                                 </row>
                             </xsl:for-each>
                         </tbody>
                  </tgroup>
             </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="exceptions-section">
		<xsl:if test="documentation/exception">
			<bridgehead renderas="sect3">Exceptions</bridgehead>
             <informaltable>
                  <tgroup cols="2"><colspec colnum="1" colname="col1"
                        colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                             <row><entry colname="col1">Exception Type</entry><entry
                                  colname="col2">Condition</entry>
                             </row></thead><tbody>
                             <xsl:for-each select="documentation/exception">
                                <xsl:sort select="@name" />
                                <xsl:variable name="cref" select="@cref" />
                                 <row>
                                    <entry colname="col1">
                                        <xsl:variable name="type-filename">
                                            <xsl:call-template name="get-filename-for-cref">
                                                <xsl:with-param name="cref" select="@cref" />
                                            </xsl:call-template>
                                        </xsl:variable>
                                        <ulink url="{$type-filename}">
                                            <xsl:call-template name="strip-namespace">
                                                <xsl:with-param name="name" select="substring-after(@cref, 'T:')"/>
                                            </xsl:call-template>
                                        </ulink>
                                    </entry>
                                    <entry colname="col2">
                                        <xsl:apply-templates select="./node()" mode="slashdoc" />
                                    </entry>
                                 </row>
                             </xsl:for-each>
                         </tbody>
                  </tgroup>
             </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="example-section">
		<xsl:if test="documentation/example">
			<bridgehead renderas="sect3">Example</bridgehead>
			<para>
				<xsl:apply-templates select="documentation/example/node()" mode="slashdoc" />
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="members-section">
		<xsl:if test="field">
			<bridgehead renderas="sect3">Members</bridgehead>
             <informaltable>
                  <tgroup cols="2"><colspec colnum="1" colname="col1"
                        colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                             <row><entry colname="col1">Member Name</entry><entry
                                  colname="col2">Description</entry>
                             </row></thead><tbody>
                             <xsl:for-each select="field">
                                <xsl:sort select="@name" />
                                <xsl:variable name="cref" select="@cref" />
                                 <row>
                                    <entry colname="col1">
                                        <para>
                                            <emphasis role="bold">
                                                <xsl:value-of select="@name" />
                                            </emphasis>
                                        </para>
                                    </entry>
                                    <entry colname="col2">
                                        <xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" />
                                    </entry>
                                 </row>
                             </xsl:for-each>
                         </tbody>
                  </tgroup>
             </informaltable>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-lang">
		<xsl:param name="lang" />
		<xsl:choose>
			<xsl:when test="$lang = 'VB' or $lang='Visual Basic'">
				<xsl:value-of select="'Visual&#160;Basic'" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$lang" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- get-a-href -->
	<xsl:template name="get-a-href">
		<xsl:param name="cref" />
		<xsl:variable name="href">
			<xsl:call-template name="get-href">
				<xsl:with-param name="cref" select="$cref" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$href=''">
				<emphasis role="bold">
					<xsl:call-template name="get-a-name">
						<xsl:with-param name="cref" select="$cref" />
					</xsl:call-template>
				</emphasis>
			</xsl:when>
			<xsl:otherwise>
				<ulink>
					<xsl:attribute name="url">
						<xsl:value-of select="$href" />
					</xsl:attribute>

					<xsl:choose>
						<xsl:when test="node()">
							<xsl:value-of select="." />
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="get-a-name">
								<xsl:with-param name="cref" select="$cref" />
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</ulink>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-href">
		<xsl:param name="cref" />
		<xsl:choose>
			<xsl:when test="starts-with(substring-after($cref, ':'), 'System.')">
                <!-- todo: determine if this works as a link, the new ndoc is listing file name only -->
				<ulink>
					<xsl:attribute name="url">
						<xsl:call-template name="get-filename-for-system-cref">
							<xsl:with-param name="cref" select="$cref" />
						</xsl:call-template>
					</xsl:attribute>
					<xsl:choose>
						<xsl:when test="contains($cref, '(')">
							<xsl:value-of select="substring-after(substring-before($cref, '('), ':')" />
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="substring-after($cref, ':')" />
						</xsl:otherwise>
					</xsl:choose>
				</ulink>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="seethis" select="//*[@id=$cref][name()!='base']" />
				<xsl:choose>
					<xsl:when test="$seethis">
						<!--<xsl:for-each select="$seethis">-->
							<xsl:call-template name="get-filename-for-cref-overload">
								<xsl:with-param name="cref" select="$seethis/@id" />
								<xsl:with-param name="overload" select="$seethis/@overload" />
							</xsl:call-template>
						<!--</xsl:for-each>-->
					</xsl:when>
					<xsl:otherwise>
						<!-- this is an incredibly lame hack. -->
						<!-- it can go away once microsoft stops prefix event crefs with 'F:'. -->
						<xsl:if test="starts-with($cref, 'F:')">
							<xsl:variable name="event-cref" select="concat('E:', substring-after($cref, 'F:'))" />
							<xsl:variable name="event-seethis" select="//*[@id=$event-cref]" />
							<xsl:if test="$event-seethis">
								<xsl:call-template name="get-filename-for-cref">
									<xsl:with-param name="cref" select="$event-cref" />
								</xsl:call-template>
							</xsl:if>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- get-a-name -->
	<xsl:template name="get-a-name">
		<xsl:param name="cref" />
		<xsl:choose>
			<xsl:when test="starts-with(substring-after($cref, ':'), 'System.')">
				<xsl:choose>
					<xsl:when test="contains($cref, '.#c')">
						<xsl:call-template name="strip-namespace">
						<xsl:with-param name="name" select="substring-after(substring-before($cref, '.#c'), 'M:')" />
						</xsl:call-template>
					</xsl:when>
					<xsl:when test="contains($cref, '(')">
						<xsl:call-template name="strip-namespace">
							<xsl:with-param name="name" select="substring-after(substring-before($cref, '('), ':')" />
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="strip-namespace">
							<xsl:with-param name="name" select="substring-after($cref, ':')" />
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="seethis" select="//*[@id=$cref][name()!='base']" />
				<xsl:choose>
					<xsl:when test="$seethis">
						<!--<xsl:for-each select="$seethis">-->
							<xsl:choose>
								<xsl:when test="local-name()='constructor'">
									<xsl:value-of select="$seethis/../@name" />
								</xsl:when>
								<xsl:when test="local-name()='operator'">
									<xsl:call-template name="operator-name">
										<xsl:with-param name="name" select="$seethis/@name" />
									</xsl:call-template>
								</xsl:when>
								<xsl:otherwise>
									<xsl:value-of select="$seethis/@name" />
								</xsl:otherwise>
							</xsl:choose>
						<!--</xsl:for-each>-->
					</xsl:when>
					<xsl:otherwise>
						<xsl:choose>
							<!-- this is an incredibly lame hack. -->
							<!-- it can go away once microsoft stops prefix event crefs with 'F:'. -->
							<xsl:when test="starts-with($cref, 'F:')">
								<xsl:variable name="event-cref" select="concat('E:', substring-after($cref, 'F:'))" />
								<xsl:variable name="event-seethis" select="//*[@id=$event-cref]" />
								<xsl:choose>
									<xsl:when test="$event-seethis">
										<xsl:value-of select="$event-seethis/@name" />
									</xsl:when>
									<xsl:when test="string-length(substring-before($cref, ':')) = 1">
										<xsl:value-of select="substring($cref, 3)" />
									</xsl:when>
									<xsl:when test="string-length(substring-before($cref, ':')) = 1">
										<b><xsl:value-of select="substring($cref, 3)" /></b>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="$cref" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:when>
							<xsl:when test="string-length(substring-before($cref, ':')) = 1">
								<xsl:value-of select="substring($cref, 3)" />
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="$cref" />
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="value">
		<xsl:param name="type" />
		<xsl:variable name="namespace">
			<xsl:value-of select="concat(../../@name, '.')" />
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="contains($type, $namespace)">
				<xsl:value-of select="substring-after($type, $namespace)" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="csharp-type">
					<xsl:with-param name="runtime-type" select="$type" />
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="copyright-notice">
		<xsl:variable name="copyright-text">
			<xsl:value-of select="/ndoc/copyright/@text" />
		</xsl:variable>
		<xsl:variable name="copyright-href">
			<xsl:value-of select="/ndoc/copyright/@href" />
		</xsl:variable>
		<xsl:if test="$copyright-text != ''">
			<ulink>
				<xsl:if test="$copyright-href != ''">
					<xsl:attribute name="url">
						<xsl:value-of select="$copyright-href" />
					</xsl:attribute>
				</xsl:if>
				<xsl:value-of select="/ndoc/copyright/@text" />
			</ulink>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="generated-from-assembly-version">
		<xsl:variable name="assembly-name">
			<xsl:value-of select="ancestor-or-self::assembly/./@name" />
		</xsl:variable>
		<xsl:variable name="assembly-version">
			<xsl:value-of select="ancestor-or-self::assembly/./@version" />
		</xsl:variable>
		<xsl:if test="$assembly-version != ''">
			<xsl:text>Generated from assembly </xsl:text>
			<xsl:value-of select="$assembly-name" />
			<xsl:text> [</xsl:text>
			<xsl:value-of select="$assembly-version" />
			<xsl:text>]</xsl:text>
		</xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template name="operator-name">
		<xsl:param name="name" />
		<xsl:param name="from" />
		<xsl:param name="to" />
		<xsl:choose>
			<xsl:when test="$name='op_UnaryPlus'">Unary Plus Operator</xsl:when>
			<xsl:when test="$name='op_UnaryNegation'">Unary Negation Operator</xsl:when>
			<xsl:when test="$name='op_LogicalNot'">Logical Not Operator</xsl:when>
			<xsl:when test="$name='op_OnesComplement'">Ones Complement Operator</xsl:when>
			<xsl:when test="$name='op_Increment'">Increment Operator</xsl:when>
			<xsl:when test="$name='op_Decrement'">Decrement Operator</xsl:when>
			<xsl:when test="$name='op_True'">True Operator</xsl:when>
			<xsl:when test="$name='op_False'">False Operator</xsl:when>
			<xsl:when test="$name='op_Addition'">Addition Operator</xsl:when>
			<xsl:when test="$name='op_Subtraction'">Subtraction Operator</xsl:when>
			<xsl:when test="$name='op_Multiply'">Multiplication Operator</xsl:when>
			<xsl:when test="$name='op_Division'">Division Operator</xsl:when>
			<xsl:when test="$name='op_Modulus'">Modulus Operator</xsl:when>
			<xsl:when test="$name='op_BitwiseAnd'">Bitwise And Operator</xsl:when>
			<xsl:when test="$name='op_BitwiseOr'">Bitwise Or Operator</xsl:when>
			<xsl:when test="$name='op_ExclusiveOr'">Exclusive Or Operator</xsl:when>
			<xsl:when test="$name='op_LeftShift'">Left Shift Operator</xsl:when>
			<xsl:when test="$name='op_RightShift'">Right Shift Operator</xsl:when>
			<xsl:when test="$name='op_Equality'">Equality Operator</xsl:when>
			<xsl:when test="$name='op_Inequality'">Inequality Operator</xsl:when>
			<xsl:when test="$name='op_LessThan'">Less Than Operator</xsl:when>
			<xsl:when test="$name='op_GreaterThan'">Greater Than Operator</xsl:when>
			<xsl:when test="$name='op_LessThanOrEqual'">Less Than Or Equal Operator</xsl:when>
			<xsl:when test="$name='op_GreaterThanOrEqual'">Greater Than Or Equal Operator</xsl:when>
			<xsl:when test="$name='op_Implicit'">
				<xsl:text>Implicit </xsl:text>
				<xsl:value-of select="$from" />
				<xsl:text> to </xsl:text>
				<xsl:value-of select="$to" />
				<xsl:text> Conversion</xsl:text>
			</xsl:when>
			<xsl:when test="$name='op_Explicit'">
				<xsl:text>Explicit </xsl:text>
				<xsl:value-of select="$from" />
				<xsl:text> to </xsl:text>
				<xsl:value-of select="$to" />
				<xsl:text> Conversion</xsl:text>
			</xsl:when>
			<xsl:otherwise>ERROR</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="csharp-operator-name">
		<xsl:param name="name" />
		<xsl:choose>
			<xsl:when test="$name='op_UnaryPlus'">operator +</xsl:when>
			<xsl:when test="$name='op_UnaryNegation'">operator -</xsl:when>
			<xsl:when test="$name='op_LogicalNot'">operator !</xsl:when>
			<xsl:when test="$name='op_OnesComplement'">operator ~</xsl:when>
			<xsl:when test="$name='op_Increment'">operator ++</xsl:when>
			<xsl:when test="$name='op_Decrement'">operator --</xsl:when>
			<xsl:when test="$name='op_True'">operator true</xsl:when>
			<xsl:when test="$name='op_False'">operator false</xsl:when>
			<xsl:when test="$name='op_Addition'">operator +</xsl:when>
			<xsl:when test="$name='op_Subtraction'">operator -</xsl:when>
			<xsl:when test="$name='op_Multiply'">operator *</xsl:when>
			<xsl:when test="$name='op_Division'">operator /</xsl:when>
			<xsl:when test="$name='op_Modulus'">operator %</xsl:when>
			<xsl:when test="$name='op_BitwiseAnd'">operator &amp;</xsl:when>
			<xsl:when test="$name='op_BitwiseOr'">operator |</xsl:when>
			<xsl:when test="$name='op_ExclusiveOr'">operator ^</xsl:when>
			<xsl:when test="$name='op_LeftShift'">operator &lt;&lt;</xsl:when>
			<xsl:when test="$name='op_RightShift'">operator >></xsl:when>
			<xsl:when test="$name='op_Equality'">operator ==</xsl:when>
			<xsl:when test="$name='op_Inequality'">operator !=</xsl:when>
			<xsl:when test="$name='op_LessThan'">operator &lt;</xsl:when>
			<xsl:when test="$name='op_GreaterThan'">operator ></xsl:when>
			<xsl:when test="$name='op_LessThanOrEqual'">operator &lt;=</xsl:when>
			<xsl:when test="$name='op_GreaterThanOrEqual'">operator >=</xsl:when>
			<xsl:otherwise>ERROR</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-namespace">
		<xsl:param name="name" />
		<xsl:param name="namespace" />
		<xsl:choose>
			<xsl:when test="contains($name, '.')">
				<xsl:call-template name="get-namespace">
					<xsl:with-param name="name" select="substring-after($name, '.')" />
					<xsl:with-param name="namespace" select="concat($namespace, substring-before($name, '.'), '.')" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="substring($namespace, 1, string-length($namespace) - 1)" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="strip-namespace">
		<xsl:param name="name" />
		<xsl:choose>
			<xsl:when test="contains($name, '.')">
				<xsl:call-template name="strip-namespace">
					<xsl:with-param name="name" select="substring-after($name, '.')" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$name" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="requirements-section">
		<xsl:if test="documentation/permission">
			<bridgehead renderas="sect3">Requirements</bridgehead>
			<para>
				<emphasis role="bold">.NET Framework Security: </emphasis>
				<itemizedlist>
					<xsl:for-each select="documentation/permission">
						<listitem>
								<para>
										<ulink>
												<xsl:attribute name="url">
														<xsl:call-template name="get-filename-for-type-name">
																<xsl:with-param name="type-name" select="substring-after(@cref, 'T:')" />
														</xsl:call-template>
												</xsl:attribute>
												<xsl:value-of select="substring-after(@cref, 'T:')" />
										</ulink>
										<xsl:text>&#160;</xsl:text>
										<xsl:apply-templates mode="slashdoc" />
								</para>
						</listitem>
					</xsl:for-each>
				</itemizedlist>
			</para>
		</xsl:if>
	</xsl:template>
	<!-- -->
    <xsl:template match="br">
        <xsl:text>
</xsl:text>
    </xsl:template>
	<!-- -->
    <xsl:template match="br" mode="slashdoc">
        <xsl:text>
</xsl:text>
    </xsl:template>
    <!-- -->
</xsl:stylesheet>