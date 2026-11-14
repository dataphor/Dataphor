<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:saxon="http://icl.com/saxon"
                xmlns:lxslt="http://xml.apache.org/xslt"
                xmlns:xalanredirect="org.apache.xalan.xslt.extensions.Redirect"
                xmlns:exsl="http://exslt.org/common"
                xmlns:doc="http://nwalsh.com/xsl/documentation/1.0"
		version="1.1"
                exclude-result-prefixes="doc"
                extension-element-prefixes="saxon xalanredirect lxslt exsl">
    <!-- -->
    <xsl:template match="/">
        <xsl:apply-templates />
    </xsl:template>
    <!-- -->
    <xsl:template match="*|@*|comment()|processing-instruction()|text()">
        <xsl:copy>
            <xsl:apply-templates select="*|@*|comment()|processing-instruction()|text()"/>
        </xsl:copy>
    </xsl:template>
    <!-- -->
    <xsl:template match="assembly">
        <!-- write a new file -->
        <xsl:param name="filename" select="./@name"/>
        <saxon:output saxon:character-representation="hex"
                    href="{$filename}.xml"
                    method="xml"
                    encoding="UTF-8"
                    indent="yes"
                    omit-xml-declaration="no"
                    doctype="ndoc"
                    standalone="yes">
            <ndoc>
                <xsl:copy-of select="."/>
            </ndoc>
        </saxon:output>
    </xsl:template>
    
</xsl:stylesheet>