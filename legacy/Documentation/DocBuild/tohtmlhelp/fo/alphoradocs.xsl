<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:fo="http://www.w3.org/1999/XSL/Format"
                xmlns:doc="http://nwalsh.com/xsl/documentation/1.0"
                exclude-result-prefixes="doc"
                version='1.0'>

    <xsl:import href="docbook.xsl" />
	<xsl:include href="AlphoraPagesetup.xsl" />
		
    <xsl:param name="alignment">left</xsl:param>
    <xsl:param name="hyphenate">false</xsl:param>
    <xsl:param name="show.comments">0</xsl:param>
    <xsl:param name="ulink.show" select="0"/>
    <xsl:param name="make.index.markup" select="0"/>
    <xsl:param name="fop.extensions" select="1"/>
		<xsl:param name="draft.mode" select="'no'"/>

			
		<xsl:param name="orphan.count" select="3"/>
		<xsl:param name="widow.count" select="3"/>
		<xsl:param name="confidential.mode" select="'no'"/>
    
    <!-- params for page printing -->
    <xsl:param name="page.height" select="'8.5in'"/>
    <xsl:param name="page.width" select="'7in'"/>
    <xsl:param name="double.sided" select="'1'"/> <!-- apparently 1 not supported correctly in FOP -->
		<!-- FOP 0.20.5 handles double sided, however, right side pages have titles and numbers at page
		     edge, fix of page templates required
		-->
		<xsl:param name="pdfoutput" select="1" />
		

    <xsl:attribute-set name="table.table.properties">
      <xsl:attribute name="border-before-width.conditionality">retain</xsl:attribute>
      <xsl:attribute name="border-collapse">collapse</xsl:attribute>
      <xsl:attribute name="font-size">9pt</xsl:attribute>
      <xsl:attribute name="hyphenate">true</xsl:attribute>
    </xsl:attribute-set>    
		
    <!-- todo: determine if there is a mode involved with revhistory -->
    <xsl:template match="revhistory">
    <!-- do nothing -->
    </xsl:template>

	<!-- from verbatim.xsl, so that long programlisting lines will wrap rather than be cutoff
			 it is unknown whether this will have the desired effect on long class names.
			 slf 5/30/03
	-->
	<xsl:template match="programlisting|screen|synopsis">
		<xsl:param name="suppress-numbers" select="'0'"/>
		<xsl:variable name="id"><xsl:call-template name="object.id"/></xsl:variable>
	
		<xsl:variable name="content">
			<xsl:choose>
				<xsl:when test="$suppress-numbers = '0'
												and @linenumbering = 'numbered'
												and $use.extensions != '0'
												and $linenumbering.extension != '0'">
					<xsl:call-template name="number.rtf.lines">
						<xsl:with-param name="rtf">
							<xsl:apply-templates/>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
	
		<xsl:choose>
			<xsl:when test="$shade.verbatim != 0">
				<fo:block wrap-option='wrap'
									white-space-collapse='false'
									linefeed-treatment="preserve"
									xsl:use-attribute-sets="monospace.verbatim.properties shade.verbatim.style"
									font-size="8pt">
	
					<xsl:copy-of select="$content"/>
				</fo:block>
			</xsl:when>
			<xsl:otherwise>
				<fo:block wrap-option='wrap'
									white-space-collapse='false'
									linefeed-treatment="preserve"
									xsl:use-attribute-sets="monospace.verbatim.properties"
									font-size="8pt">
					<xsl:copy-of select="$content"/>
				</fo:block>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- from Index.xsl -->
	<xsl:template name="indexdiv.title">
		<xsl:param name="title"/>
		<xsl:param name="titlecontent"/>

		<fo:block margin-left="-1pc"
				font-size="14.4pt"
							font-family="{$title.fontset}"
							font-weight="bold"
							keep-with-next.within-column="always"
							space-before.optimum="{$body.font.master}pt"
							space-before.minimum="{$body.font.master * 0.8}pt"
							space-before.maximum="{$body.font.master * 1.2}pt">
			<xsl:choose>
				<xsl:when test="$title">
					<xsl:apply-templates select="." mode="object.title.markup">
						<xsl:with-param name="allow-anchors" select="1"/>
					</xsl:apply-templates>
				</xsl:when>
				<xsl:otherwise>
					<xsl:copy-of select="$titlecontent"/>
				</xsl:otherwise>
			</xsl:choose>
		</fo:block>
	</xsl:template>
			
	<!-- from xref.xsl, trying to convert internal ulink to xref result -->
	<xsl:template match="ulink" name="ulink">
		<xsl:choose>
			<xsl:when test="$pdfoutput='1'">
				<fo:basic-link xsl:use-attribute-sets="xref.properties">
					<xsl:choose>
						<xsl:when test="starts-with(@url, 'ms-help:')">
							<xsl:attribute name="external-destination">
								<xsl:call-template name="fo-external-image">
									<xsl:with-param name="filename" select="@url"/>
								</xsl:call-template>
							</xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:choose>
								<xsl:when test="contains(@url,'.html')">
									<!-- if with .html find children then internal-destination -->
									<xsl:variable name="LocalID" select="substring-before(@url,'.htm')" />
									<xsl:choose>
										<xsl:when test="/descendant::*[@id = $LocalID]">
											<xsl:attribute name="internal-destination">
												<xsl:value-of select="$LocalID" />
											</xsl:attribute>
										</xsl:when>
										<xsl:otherwise>
											<!-- todo: what does a pdf to pdf link look like? get mapping together... -->
											<xsl:attribute name="external-destination">
												<xsl:call-template name="fo-external-image">
													<xsl:with-param name="filename" select="@url"/>
												</xsl:call-template>
											</xsl:attribute>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:when>
								<xsl:otherwise>
									<xsl:attribute name="internal-destination">
										<xsl:value-of select="@url"></xsl:value-of>
									</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					
					<xsl:choose>
						<xsl:when test="count(child::node())=0">
							<xsl:call-template name="hyphenate-url">
								<xsl:with-param name="url" select="@url"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:apply-templates/>
						</xsl:otherwise>
					</xsl:choose>
				</fo:basic-link>
			
				<!--
				<xsl:if test="count(child::node()) != 0
											and string(.) != @url
											and $ulink.show != 0">
					<!- yes, show the URI ->
					
					<xsl:choose>
						<xsl:when test="$ulink.footnotes != 0 and not(ancestor::footnote)">
							<xsl:text>&#xA0;</xsl:text>
							<fo:footnote>
								<xsl:call-template name="ulink.footnote.number"/>
								<fo:footnote-body font-family="{$body.fontset}"
																	font-size="{$footnote.font.size}">
									<fo:block>
										<xsl:call-template name="ulink.footnote.number"/>
										<xsl:text> </xsl:text>
										<fo:inline>
											<xsl:value-of select="@url"/>
										</fo:inline>
									</fo:block>
								</fo:footnote-body>
							</fo:footnote>
						</xsl:when>
						<xsl:otherwise>
							<fo:inline hyphenate="false">
								<xsl:text> [</xsl:text>
								<xsl:call-template name="hyphenate-url">
									<xsl:with-param name="url" select="@url"/>
								</xsl:call-template>
								<xsl:text>]</xsl:text>
							</fo:inline>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:if>
				-->
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-imports />
			</xsl:otherwise>
			</xsl:choose>
	</xsl:template>			
  
  
	<!-- from List.xsl, added empty as a marker -->
    <xsl:template match="itemizedlist/listitem">
      <xsl:variable name="id"><xsl:call-template name="object.id"/></xsl:variable>
    
      <xsl:variable name="itemsymbol">
        <xsl:call-template name="list.itemsymbol">
          <xsl:with-param name="node" select="parent::itemizedlist"/>
        </xsl:call-template>
      </xsl:variable>
    
      <xsl:variable name="item.contents">
        <fo:list-item-label end-indent="label-end()">
          <fo:block>
            <xsl:choose>
              <xsl:when test="$itemsymbol='disc'">&#x2022;</xsl:when>
              <xsl:when test="$itemsymbol='bullet'">&#x2022;</xsl:when>
              <!-- why do these symbols not work? -->
              <!--
              <xsl:when test="$itemsymbol='circle'">&#x2218;</xsl:when>
              <xsl:when test="$itemsymbol='round'">&#x2218;</xsl:when>
              <xsl:when test="$itemsymbol='square'">&#x2610;</xsl:when>
              <xsl:when test="$itemsymbol='box'">&#x2610;</xsl:when>
              -->
              <xsl:when test="$itemsymbol='empty'"></xsl:when>
              <xsl:otherwise>&#x2022;</xsl:otherwise>
            </xsl:choose>
          </fo:block>
        </fo:list-item-label>
        <fo:list-item-body start-indent="body-start()">
          <fo:block>
        <xsl:apply-templates/>
          </fo:block>
        </fo:list-item-body>
      </xsl:variable>
    
      <xsl:choose>
        <xsl:when test="parent::*/@spacing = 'compact'">
          <fo:list-item id="{$id}" xsl:use-attribute-sets="compact.list.item.spacing">
            <xsl:copy-of select="$item.contents"/>
          </fo:list-item>
        </xsl:when>
        <xsl:otherwise>
          <fo:list-item id="{$id}" xsl:use-attribute-sets="list.item.spacing">
            <xsl:copy-of select="$item.contents"/>
          </fo:list-item>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:template>
		
    <xsl:template match="symbol">
      <xsl:call-template name="inline.monoseq"/>
    </xsl:template>
    
    <xsl:template match="phrase">
      <xsl:choose>
        <xsl:when test="@role = 'code'">
          <xsl:call-template name="inline.monoseq"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="inline.charseq"/>
        </xsl:otherwise>
      </xsl:choose>
      
    </xsl:template>
  	<!-- from inline.xsl, break page on begin page, internal usage -->
  	<xsl:template match="beginpage">
      <fo:block break-after="page" keep-with-previous="auto" />
	  </xsl:template>

        
</xsl:stylesheet>