<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:doc="http://nwalsh.com/xsl/documentation/1.0"
                xmlns:exsl="http://exslt.org/common"
                xmlns:set="http://exslt.org/sets"
                xmlns:saxon="http://icl.com/saxon"
                xmlns:lxslt="http://xml.apache.org/xslt"
                xmlns:xalanredirect="org.apache.xalan.xslt.extensions.Redirect"
		version="1.1"
                exclude-result-prefixes="doc exsl set saxon xalanredirect lxslt">

    <xsl:import href="htmlhelp.xsl" />
    <xsl:include href="param.xsl" />
    
    <xsl:template match="/">
      
      <xsl:call-template name="hhp"/>
      <xsl:call-template name="hhc"/>
      <xsl:if test="($rootid = '' and //processing-instruction('dbhh')) or
                    ($rootid != '' and key('id',$rootid)//processing-instruction('dbhh'))">
        <xsl:call-template name="hh-map"/>
        <xsl:call-template name="hh-alias"/>
      </xsl:if>
    </xsl:template>
</xsl:stylesheet>
