<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- -->
	<!--<xsl:output method="html" indent="no" />-->
	<!-- -->
	<xsl:include href="common.xslt" />
    <xsl:include href="namespacehierarchy.xslt" />
	<!-- -->
	<!--<xsl:param name='namespace' />-->
	<!-- -->
    <!--
	<xsl:template match="/">
		<xsl:apply-templates select="ndoc" />
	</xsl:template>
    -->
	<!-- -->
	<xsl:template match="namespace">
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
            <!-- <xsl:apply-templates select=".[1]" /> -->
            <xsl:apply-templates select="." mode="namespace"/>
            <xsl:if test="./class[not(./documentation/nodoc)]">
                <bridgehead renderas="sect3">Classes</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./class[not(./documentation/nodoc)]" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./interface[not(./documentation/nodoc)]">
                <bridgehead renderas="sect3">Interfaces</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./interface[not(./documentation/nodoc)]" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./structure[not(./documentation/nodoc)]">
                <bridgehead renderas="sect3">Structures</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./structure[not(./documentation/nodoc)]" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./delegate[not(./documentation/nodoc)]">
                <bridgehead renderas="sect3">Delegates</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./delegate[not(./documentation/nodoc)]" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
            <xsl:if test="./enumeration[not(./documentation/nodoc)]">
                <bridgehead renderas="sect3">Enumerations</bridgehead>
                 <informaltable>
                      <tgroup cols="2"><colspec colnum="1" colname="col1"
                            colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                                 <row><entry colname="col1">Class</entry><entry
                                      colname="col2">Description</entry>
                                 </row></thead>
                                 <tbody>
                                    <xsl:apply-templates select="./enumeration[not(./documentation/nodoc)]" mode="namespace">
                                        <xsl:sort select="@name" />
                                    </xsl:apply-templates>
                                 </tbody>
                      </tgroup>
                 </informaltable>
            </xsl:if>
        </sect1>
	</xsl:template>
	<!-- -->
	<xsl:template match="namespace" mode="namespace">
		<xsl:call-template name="summary-section" />
	</xsl:template>
	<!-- -->
	<xsl:template match="class" mode="namespace">
    <xsl:if test="not(./documentation/nodoc)">
      <row>
        <entry colname="col1">
          <ulink>
            <xsl:attribute name="url">
              <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id" />
              </xsl:call-template>
            </xsl:attribute>
            <xsl:value-of select="@name" />
          </ulink>
        </entry>
        <entry colname="col2">
          <xsl:apply-templates select="(documentation/summary)[1]/node()" mode="slashdoc" />
        </entry>
      </row>
    </xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="interface" mode="namespace">
    <xsl:if test="not(./documentation/nodoc)">
      <row>
        <entry colname="col1">
          <ulink>
            <xsl:attribute name="url">
              <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id" />
              </xsl:call-template>
            </xsl:attribute>
            <xsl:value-of select="@name" />
          </ulink>
        </entry>
        <entry colname="col2">
          <xsl:apply-templates select="(documentation/summary)[1]/node()" mode="slashdoc" />
        </entry>
      </row>
    </xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="structure" mode="namespace">
    <xsl:if test="not(./documentation/nodoc)">
      <row>
        <entry colname="col1">
          <ulink>
            <xsl:attribute name="url">
              <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id" />
              </xsl:call-template>
            </xsl:attribute>
            <xsl:value-of select="@name" />
          </ulink>
        </entry>
        <entry colname="col2">
          <xsl:apply-templates select="(documentation/summary)[1]/node()" mode="slashdoc" />
        </entry>
      </row>
    </xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="delegate" mode="namespace">
    <xsl:if test="not(./documentation/nodoc)">
      <row>
        <entry colname="col1">
          <ulink>
            <xsl:attribute name="url">
              <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id" />
              </xsl:call-template>
            </xsl:attribute>
            <xsl:value-of select="@name" />
          </ulink>
        </entry>
        <entry colname="col2">
          <xsl:apply-templates select="(documentation/summary)[1]/node()" mode="slashdoc" />
        </entry>
      </row>
    </xsl:if>
	</xsl:template>
	<!-- -->
	<xsl:template match="enumeration" mode="namespace">
    <xsl:if test="not(./documentation/nodoc)">
      <row>
        <entry colname="col1">
          <ulink>
            <xsl:attribute name="url">
              <xsl:call-template name="get-filename-for-type">
                <xsl:with-param name="id" select="@id" />
              </xsl:call-template>
            </xsl:attribute>
            <xsl:value-of select="@name" />
          </ulink>
        </entry>
        <entry colname="col2">
          <xsl:apply-templates select="(documentation/summary)[1]/node()" mode="slashdoc" />
        </entry>
      </row>
    </xsl:if>
	</xsl:template>
	<!-- -->
</xsl:stylesheet>