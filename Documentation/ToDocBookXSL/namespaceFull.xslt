<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
    <!--
	<xsl:include href="common.xslt" />
    <xsl:include href="namespacehierarchy.xslt" />
    -->
	<!-- -->
	<!--<xsl:param name='namespace' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="namespace" mode="namespacefull">
        <xsl:comment>In namespace mode namespacefull</xsl:comment>
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-namespace">
                <xsl:with-param name="name" select="@name"/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="namespace" select="@name"/>
        <sect1>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <title>
            <indexterm>
                <primary>
                    <xsl:text>Namespaces</xsl:text>
                </primary>
                <secondary>
                    <xsl:value-of select="@name"/>
                </secondary>
            </indexterm>
            <indexterm>
                <primary>
                    <xsl:value-of select="@name"/>
                </primary>
            </indexterm><xsl:value-of select="@name"/></title>
            <xsl:if test="$includeHierarchy">
              <para>
                  <ulink>
                      <xsl:attribute name="url">
                          <xsl:call-template name="get-filename-for-current-namespace-hierarchy" />
                      </xsl:attribute>
                      <xsl:text>Namespace hierarchy</xsl:text>
                  </ulink>
              </para>
            </xsl:if>
            <para/>
            <!-- the namespace template just gets the summary. -->
            <!-- todo: figure out what the line below is selecting! if it doesn't match the replacement fix it-->
            <!-- <xsl:apply-templates select="assembly/module/namespace[@name=$namespace][1]" /> -->
            <xsl:comment> Applying templates mode namespace</xsl:comment>
            <xsl:apply-templates select="." mode="namespace"/>
            
            <xsl:comment>starting class processing</xsl:comment>
            <xsl:if test="./class">
                <bridgehead renderas="sect3">Classes</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./class" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./interface">
                <bridgehead renderas="sect3">Interfaces</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./interface" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./structure">
                <bridgehead renderas="sect3">Structures</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./structure" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./delegate">
                <bridgehead renderas="sect3">Delegates</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./delegate" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./enumeration">
                <bridgehead renderas="sect3">Enumerations</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./enumeration" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            
            <!-- todo: process all the elements of the namespace, classes, etc -->
            <xsl:if test="./class">
                <xsl:for-each select="./class">
                    <sect2>
                        <xsl:apply-templates select="."/>
                        <xsl:apply-templates select="." mode="process-members"/>
                        <xsl:apply-templates select="." mode="process-individuals"/>
                        <xsl:comment>Processed class</xsl:comment>
                    </sect2>
                </xsl:for-each>
            </xsl:if>
            <xsl:if test="./interface">
                <xsl:for-each select="./interface">
                    <sect2>
                        <xsl:apply-templates select="."/>
                        <xsl:apply-templates select="." mode="process-members"/>
                        <xsl:apply-templates select="." mode="process-individuals"/>
                        <xsl:comment>Processed interface</xsl:comment>
                    </sect2>
                </xsl:for-each>
            </xsl:if>
            <xsl:if test="./structure">
                <xsl:for-each select="./structure">
                    <sect2>
                        <xsl:apply-templates select="."/>
                        <xsl:apply-templates select="." mode="process-members"/>
                        <xsl:apply-templates select="." mode="process-individuals"/>
                        <xsl:comment>Processed structure</xsl:comment>
                    </sect2>
                </xsl:for-each>
            </xsl:if>
            <xsl:if test="./delegate">
                <xsl:for-each select="./delegate">
                    <sect2>
                        <xsl:apply-templates select="."/>
                        <xsl:apply-templates select="." mode="process-members"/>
                        <xsl:apply-templates select="." mode="process-individuals"/>
                        <xsl:comment>Processed delegate</xsl:comment>
                    </sect2>
                </xsl:for-each>
            </xsl:if>
            <xsl:if test="./enumeration">
                <xsl:for-each select="./enumeration">
                    <sect2>
                        <xsl:apply-templates select="."/>
                        <xsl:comment>Processed enumeration</xsl:comment>
                    </sect2>
                </xsl:for-each>
            </xsl:if>
        </sect1>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>