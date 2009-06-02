<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <!-- -->
  <!--<xsl:output method="html" indent="no" />-->
  <!-- -->
  <!--<xsl:include href="common.xslt" />-->
  <!-- -->
  <!--<xsl:param name='event-id' />-->
  <!-- -->
  <!--
  <xsl:template match="/">
    <xsl:apply-templates select="ndoc/assembly/module/namespace/*/event[@id=$event-id]" />
  </xsl:template>
  -->
  <!-- -->
  <xsl:template match="event" mode="singleton">
        <xsl:variable name="filename" >
            <xsl:call-template name="get-filename-for-current-event"/>
        </xsl:variable>
        <sect4>
            <xsl:attribute name="id">
                <xsl:value-of select="substring-before($filename,'.html')"/>
            </xsl:attribute>
            <xsl:comment>Generated from event.xsl</xsl:comment>
            <title>
            <indexterm>
                <primary>
                    <xsl:value-of select="concat(../@name,'.',@name)"/>
                </primary>
            </indexterm>
            <indexterm>
                <primary>
                    <xsl:value-of select="../@name"/>
                </primary>
                <secondary>
                    <xsl:value-of select="@name"/>
                </secondary>
            </indexterm>
            <indexterm>
                <primary>
                    <xsl:value-of select="@name"/>
                </primary>
            </indexterm><xsl:value-of select="../@name" />.<xsl:value-of select="@name" /> Event</title>
          <xsl:call-template name="summary-section" />
          <bridgehead renderas="sect4">Declaration</bridgehead>
          <xsl:call-template name="vb-field-or-event-syntax" />
          <xsl:call-template name="cs-field-or-event-syntax" />
          <para/>
          <xsl:variable name="type" select="@type" />
          <xsl:variable name="eventargs-id" select="concat('T:', //delegate[@id=concat('T:', $type)]/parameter[contains(@type, 'EventArgs')][1]/@type)" />
          <xsl:variable name="thisevent" select="//class[@id=$eventargs-id]" />
          <xsl:variable name="properties" select="$thisevent/property[@access='Public' and not(@static)]" />
          <xsl:variable name="properties-count" select="count($properties)" />
          <xsl:if test="$properties-count > 0">
                <bridgehead renderas="sect3">Event Data</bridgehead>
                <para>
                    <xsl:text>The event handler receives an argument of type </xsl:text>
                    <ulink>
                        <xsl:attribute name="url">
                            <xsl:call-template name="get-filename-for-type">
                                <xsl:with-param name="id" select="$eventargs-id" />
                            </xsl:call-template>
                        </xsl:attribute>
                        <xsl:value-of select="$thisevent/@name" />
                    </ulink>
                    <xsl:text> containing data related to this event. The following </xsl:text>
                    <emphasis role="bold">
                        <xsl:value-of select="//class[@id=$eventargs-id]/@name" />
                    </emphasis>
                    <xsl:choose>
                        <xsl:when test="$properties-count > 1">
                            <xsl:text> properties provide </xsl:text>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:text> property provides </xsl:text>
                        </xsl:otherwise>
                    </xsl:choose>
                    <xsl:text>information specific to this event.</xsl:text>
                </para>
              <informaltable>
                    <tgroup cols="2"><colspec colnum="1" colname="col1"
                         colwidth="*"/><colspec colnum="2" colname="col2" colwidth="*"/><thead>
                              <row><entry colname="col1">Property</entry>
                              <entry colname="col2">Description</entry>
                              </row></thead>
                              <tbody>
                              
                                <xsl:apply-templates select="$properties" mode="eventprop">
                                    <xsl:sort select="@name" />
                                </xsl:apply-templates>
                              </tbody>
                    </tgroup>
              </informaltable>
          </xsl:if>
          <xsl:call-template name="implements-section" />
          <xsl:call-template name="remarks-section" />
          <xsl:call-template name="exceptions-section" />
          <xsl:call-template name="example-section" />
          <xsl:call-template name="requirements-section" />
          <xsl:call-template name="seealso-section">
            <xsl:with-param name="page">event</xsl:with-param>
          </xsl:call-template>
      </sect4>
  </xsl:template>
  <!-- -->
  <xsl:template match="property" mode="eventprop">
    <xsl:variable name="name" select="@name" />
    <xsl:if test="not(preceding-sibling::property[@name=$name])">
      <row>
        <xsl:choose>
          <xsl:when test="following-sibling::property[@name=$name]">
            <entry colname="col1">
              <ulink>
                <xsl:attribute name="url">
                  <xsl:call-template name="get-filename-for-current-property-overloads" />
                </xsl:attribute>
                <xsl:value-of select="@name" />
              </ulink>
            </entry>
            <entry colname="col2">
                <para>
                    <xsl:text>Overloaded. </xsl:text>
                    <xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" />
                </para>
            </entry>
          </xsl:when>
          <xsl:otherwise>
            <entry colname="col1">
                <xsl:choose>
                    <xsl:when test="@declaringType">
                        <xsl:variable name="declaring-type-id" select="concat('T:', @declaringType)" />
                        <xsl:variable name="declaring-class" select="//class[@id=$declaring-type-id]" />
                        <xsl:choose>
                            <xsl:when test="$declaring-class">
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-property" >
                                            <xsl:with-param name="property" select="$declaring-class/property[@name=$name]" />
                                        </xsl:call-template>
                                    </xsl:attribute>
                                    <xsl:value-of select="@name" />
                                </ulink>
                            </xsl:when>
                            <xsl:when test="starts-with(@declaringType, 'System.')">
                                <ulink>
                                    <xsl:attribute name="url">
                                        <xsl:call-template name="get-filename-for-system-property" />
                                    </xsl:attribute>
                                    <xsl:value-of select="@name" />
                                </ulink>
                            </xsl:when>
                            <xsl:otherwise>
                                <xsl:value-of select="@name" />
                            </xsl:otherwise>
                        </xsl:choose>
                    </xsl:when>
                    <xsl:otherwise>
                        <ulink>
                            <xsl:attribute name="url">
                                <xsl:call-template name="get-filename-for-current-property" />
                            </xsl:attribute>
                            <xsl:value-of select="@name" />
                        </ulink>
                    </xsl:otherwise>
                </xsl:choose>
            </entry>
            <entry colname="col2">
                <para>
                    <xsl:apply-templates select="documentation/summary/node()" mode="nopara" />
                </para>
            </entry>
          </xsl:otherwise>
        </xsl:choose>
      </row>
    </xsl:if>
  </xsl:template>
  <!-- -->
</xsl:stylesheet>