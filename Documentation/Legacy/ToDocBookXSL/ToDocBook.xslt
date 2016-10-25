<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<!--                exclude-result-prefixes="urn doc exsl set">-->
	<!-- -->
    <xsl:include href="type.xslt" />
	<xsl:include href="memberscommon.xslt" />
    <xsl:include href="allmembers.xslt" />
    <xsl:include href="individualmembers.xslt" />
    <xsl:include href="namespace.xslt" />
    <xsl:include href="namespacefull.xslt" />
    
	<xsl:output method="xml" indent="yes" omit-xml-declaration="yes" encoding="UTF-8"/>
    <!-- -->
    <xsl:param name='namespacename' />
    <xsl:param name='classname' />
    <xsl:param name="dofull" />
    <xsl:param name="includeHierarchy"></xsl:param>
    <xsl:param name="ndoc-document-attributes" />
    <!-- if whant vb syntax uncomment next line, following line otherwise -->
    <!--<xsl:variable name="ndoc-vb-syntax" >yes</xsl:variable>-->
    <xsl:variable name="ndoc-vb-syntax" >yes</xsl:variable>
		<xsl:variable name="htmloutput"></xsl:variable>
		
    <!-- -->
    <xsl:template match="/">
    <!--
        <xsl:choose>
            <xsl:when test="$classname">
                <xsl:call-template name="process-class">
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="process-namespace">
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    -->
        <xsl:comment>namespacename = <xsl:value-of select="$namespacename"></xsl:value-of></xsl:comment>
            <xsl:call-template name="process-namespace">
            </xsl:call-template>
    </xsl:template>
    <!-- -->
    <xsl:template name="process-class">
        <xsl:comment>
            This file was generated from code doc sources.
            Do not edit the text of this file, go to the code comments to change any text.
        </xsl:comment>
        <xsl:text>
</xsl:text>
        <xsl:comment> contains class information from doc class</xsl:comment>
        <xsl:text>
</xsl:text>
        <sect2>
            <!-- utilizing type.xslt produces class/interface/delegate/structure/enumeration topic page -->
            <!-- this works -->
            <xsl:apply-templates select="ndoc/assembly/module/namespace//class[@id=$classname]" />
            <xsl:apply-templates select="ndoc/assembly/module/namespace//class[@id=$classname]" mode="process-members" />
            <xsl:apply-templates select="ndoc/assembly/module/namespace//class[@id=$classname]" mode="process-individuals" />
            <!-- in development/testing -->
        </sect2>
    </xsl:template>
    <!-- -->
    <xsl:template name="process-namespace">
        <!--<xsl:if test="ndoc/assembly/module/namespace[@name=$namespacename]//doc"> future -->
        <xsl:if test="ndoc/assembly/module/namespace[@name=$namespacename]">
            <?xml-stylesheet type="text/css" href="DocBookx.css" ?>
            <xsl:comment>
                This file was generated from code doc sources.
                Do not edit the text of this file, go to the code comments to change any text.
            </xsl:comment>
            <xsl:comment> if this is to be embedded, comment out the doctype declaration </xsl:comment>
            <xsl:text>
</xsl:text>
            <xsl:comment>&lt;!DOCTYPE sect1 PUBLIC "-//OASIS//DTD DocBook XML V4.2//EN" "c:/src/alphora/docs/docbookmanuals/DocBookx.dtd"&gt;</xsl:comment>
            <xsl:text>
</xsl:text>
            <xsl:comment> contains namespace information from doc namespace</xsl:comment>
            <xsl:text>
</xsl:text>
            <xsl:choose>
                <xsl:when test="$dofull='yes'">
                    <xsl:apply-templates select="ndoc/assembly/module/namespace[@name=$namespacename]" mode="namespacefull"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:apply-templates select="ndoc/assembly/module/namespace[@name=$namespacename]"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:if>
    </xsl:template>
</xsl:stylesheet>