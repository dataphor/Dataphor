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
    <xsl:param name="insert.xref.page.number">yes</xsl:param>
    <xsl:param name="fop.extensions" select="1"/>
		<xsl:param name="draft.mode" select="'no'"/>
		<xsl:param name="confidential.mode" select="'no'"/>
		<xsl:param name="footer.rule">0</xsl:param> <!-- footer rule not desired -->
		
		<xsl:param name="body.font.master">10.5</xsl:param>

		<xsl:param name="formal.title.placement">
			figure after
			example before
			equation before
			table before
			procedure before
			task before
		</xsl:param>

    <xsl:param name="generate.toc">
      /appendix toc,title
      article/appendix  nop
      /article  toc,title
      book      toc,title,figure,example,equation
      /chapter  toc,title
      part      toc,title
      /preface  toc,title
      qandadiv  toc
      qandaset  toc
      reference toc,title
      /sect1    toc
      /sect2    toc
      /sect3    toc
      /sect4    toc
      /sect5    toc
      /section  toc
      set       toc,title
    </xsl:param>		
    
		<xsl:attribute-set name="list.block.spacing">
			<xsl:attribute name="space-before.optimum">4pt</xsl:attribute>
			<xsl:attribute name="space-before.minimum">4pt</xsl:attribute>
			<xsl:attribute name="space-before.maximum">4pt</xsl:attribute>
			<xsl:attribute name="space-after.optimum">4pt</xsl:attribute>
			<xsl:attribute name="space-after.minimum">4pt</xsl:attribute>
			<xsl:attribute name="space-after.maximum">4pt</xsl:attribute>
		</xsl:attribute-set>
        
		<xsl:attribute-set name="list.item.spacing">
			<xsl:attribute name="space-before.optimum">4pt</xsl:attribute>
			<xsl:attribute name="space-before.minimum">4pt</xsl:attribute>
			<xsl:attribute name="space-before.maximum">4pt</xsl:attribute>
		</xsl:attribute-set>
		
		<xsl:attribute-set name="admonition.title.properties">
			<xsl:attribute name="font-size">10pt</xsl:attribute>
			<xsl:attribute name="font-weight">bold</xsl:attribute>
			<xsl:attribute name="font-family">sans-serif</xsl:attribute>
			<xsl:attribute name="hyphenate">false</xsl:attribute>
  		<xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
  		<xsl:attribute name="keep-with-next">always</xsl:attribute>
		</xsl:attribute-set>

        
    <xsl:attribute-set name="normal.para.spacing">
      <xsl:attribute name="space-before.optimum">7pt</xsl:attribute>
      <xsl:attribute name="space-before.minimum">7pt</xsl:attribute>
      <xsl:attribute name="space-before.maximum">7pt</xsl:attribute>
    </xsl:attribute-set>
    
		<xsl:attribute-set name="admonition.properties">
			<xsl:attribute name="font-size">9pt</xsl:attribute>
			<xsl:attribute name="font-weight">normal</xsl:attribute>
			<xsl:attribute name="hyphenate">false</xsl:attribute>
			<xsl:attribute name="font-family">sans-serif</xsl:attribute>
          <xsl:attribute name="space-before.optimum">8pt</xsl:attribute>
          <xsl:attribute name="space-before.minimum">8pt</xsl:attribute>
          <xsl:attribute name="space-before.maximum">8pt</xsl:attribute>
          <xsl:attribute name="space-after.optimum">8pt</xsl:attribute>
          <xsl:attribute name="space-after.minimum">8pt</xsl:attribute>
          <xsl:attribute name="space-after.maximum">8pt</xsl:attribute>
		</xsl:attribute-set>

		<xsl:template name="nongraphical.admonition">
			<xsl:variable name="id">
				<xsl:call-template name="object.id"/>
			</xsl:variable>
		
			<fo:block space-before.minimum="5pt"
								space-before.optimum="5pt"
								space-before.maximum="5pt"
								id="{$id}">
				
				<fo:block xsl:use-attribute-sets="admonition.properties">
					<xsl:if test="$admon.textlabel != 0 or title">
						<fo:inline xsl:use-attribute-sets="admonition.title.properties">
							 <xsl:apply-templates select="." mode="object.title.markup"/>
							 <xsl:text>:  </xsl:text>
						</fo:inline>
					</xsl:if>
					<xsl:apply-templates/>
				</fo:block>
		
			</fo:block>
		</xsl:template>
	
		
		<!--
		<xsl:param name="body.margin.bottom" select="'0.5in'"/>
		<xsl:param name="body.margin.top" select="'0.3in'"/>
		-->
		<xsl:param name="orphan.count" select="3"/>
		<xsl:param name="widow.count" select="3"/>
    
    <!-- params for page printing -->
		<!-- production
    <xsl:param name="page.height" select="'8.5in'"/>
    <xsl:param name="page.width" select="'7in'"/>
		-->
		<!-- draft -->
    <xsl:param name="page.height" select="'8.5in'"/>
    <xsl:param name="page.width" select="'7in'"/>
    <xsl:param name="double.sided" select="1"/> <!-- apparently 1 not supported correctly in FOP for Cashwise -->
		<!-- FOP 0.20.5 handles double sided, however, right side pages have titles and numbers at page
		     edge, fix of page templates required
		-->
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
									xsl:use-attribute-sets="monospace.verbatim.properties shade.verbatim.style">
	
					<xsl:copy-of select="$content"/>
				</fo:block>
			</xsl:when>
			<xsl:otherwise>
				<fo:block wrap-option='wrap'
									white-space-collapse='false'
									linefeed-treatment="preserve"
									xsl:use-attribute-sets="monospace.verbatim.properties">
					<xsl:copy-of select="$content"/>
				</fo:block>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- from footnote.xsl -->
	<xsl:template name="format.footnote.mark">
		<xsl:param name="mark" select="'?'"/>
		<fo:inline vertical-align="super" font-size="80%">
			<xsl:copy-of select="$mark"/>
		</fo:inline>
	</xsl:template>	

	<!-- from Index.xsl, change so the header letter isn't so far over to overwrite left column -->
	<xsl:template name="indexdiv.title">
		<xsl:param name="title"/>
		<xsl:param name="titlecontent"/>

		<!-- -1pc doesn't overlap the left column from the right column -->
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

	
	<!-- header and footer content generation, remove header completely -->
	<xsl:template name="header.table">
	  <xsl:param name="pageclass" select="''"/>
  	  <xsl:param name="sequence" select="''"/>
  	  <xsl:param name="gentext-key" select="''"/>
		<!-- do nothing, headers not desired -->
	</xsl:template>
	
	<!-- change footer "table" format to get title (center column) close to number, outer column -->
	<xsl:template name="footer.table">
		<xsl:param name="pageclass" select="''"/>
		<xsl:param name="sequence" select="''"/>
		<xsl:param name="gentext-key" select="''"/>
	
		<xsl:choose>
				<xsl:when test="$pageclass = 'index'">
					<xsl:attribute name="margin-left">0</xsl:attribute>
				</xsl:when>
		</xsl:choose>
	
		<!-- default is a single table style for all footers -->
		<!-- Customize it for different page classes or sequence location -->
		
		<xsl:variable name="candidate">
			<fo:table table-layout="fixed" width="100%">
				<xsl:call-template name="foot.sep.rule">
					<xsl:with-param name="pageclass" select="$pageclass"/>
					<xsl:with-param name="sequence" select="$sequence"/>
					<xsl:with-param name="gentext-key" select="$gentext-key"/>
				</xsl:call-template>
				<fo:table-column column-number="1" column-width="3em"/>
				<!--<fo:table-column column-number="2" column-width="column-width(1)"/>-->
				<fo:table-column column-number="2" column-width="column-width(90%)"/>
				<fo:table-column column-number="3" column-width="3em"/>
				<fo:table-body>
					<fo:table-row height="14pt">
						<!-- left cell -->
						<fo:table-cell text-align="left"
													 display-align="after">
							<xsl:if test="$fop.extensions = 0">
								<xsl:attribute name="relative-align">baseline</xsl:attribute>
							</xsl:if>
							<fo:block>
								<xsl:call-template name="footer.content">
									<xsl:with-param name="pageclass" select="$pageclass"/>
									<xsl:with-param name="sequence" select="$sequence"/>
									<xsl:with-param name="position" select="'left'"/>
									<xsl:with-param name="gentext-key" select="$gentext-key"/>
								</xsl:call-template>
							</fo:block>
						</fo:table-cell>
						<!-- center cell -->
						<fo:table-cell display-align="after">
							<xsl:if test="$fop.extensions = 0">
								<xsl:attribute name="relative-align">baseline</xsl:attribute>
							</xsl:if>
							<xsl:choose>
								<xsl:when test="$double.sided != 0 and $sequence = 'even'"> <!-- doublesided + left -->
									<xsl:attribute name="text-align">left</xsl:attribute>
								</xsl:when>
								<xsl:when test="$double.sided != 0 and $sequence = 'odd'"> <!-- doublesided + right -->
									<xsl:attribute name="text-align">right</xsl:attribute>
								</xsl:when>
								<xsl:when test="$double.sided != 0 and $sequence = 'first'"> <!-- doublesided + right -->
									<xsl:attribute name="text-align">right</xsl:attribute>
								</xsl:when>
								<xsl:otherwise>
									<xsl:attribute name="text-align">center</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
							<fo:block>
								<xsl:call-template name="footer.content">
									<xsl:with-param name="pageclass" select="$pageclass"/>
									<xsl:with-param name="sequence" select="$sequence"/>
									<xsl:with-param name="position" select="'center'"/>
									<xsl:with-param name="gentext-key" select="$gentext-key"/>
								</xsl:call-template>
							</fo:block>
						</fo:table-cell>
						<!-- right cell -->
						<fo:table-cell text-align="right"
													 display-align="after">
							<xsl:if test="$fop.extensions = 0">
								<xsl:attribute name="relative-align">baseline</xsl:attribute>
							</xsl:if>
							<fo:block>
								<xsl:call-template name="footer.content">
									<xsl:with-param name="pageclass" select="$pageclass"/>
									<xsl:with-param name="sequence" select="$sequence"/>
									<xsl:with-param name="position" select="'right'"/>
									<xsl:with-param name="gentext-key" select="$gentext-key"/>
								</xsl:call-template>
							</fo:block>
						</fo:table-cell>
					</fo:table-row>
				</fo:table-body>
			</fo:table>
		</xsl:variable>
	
		<!-- Really output a footer? -->
		<xsl:choose>
			<xsl:when test="$pageclass='titlepage' and $gentext-key='book'
											and $sequence='first'">
				<!-- no, book titlepages have no footers at all -->
			</xsl:when>
			<xsl:when test="$sequence = 'blank' and $footers.on.blank.pages = 0">
				<!-- no output -->
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy-of select="$candidate"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>	
	
	<!-- change footer content, left page right page -->
	<xsl:template name="footer.content">
		<xsl:param name="pageclass" select="''"/>
		<xsl:param name="sequence" select="''"/>
		<xsl:param name="position" select="''"/>
		<xsl:param name="gentext-key" select="''"/>
	
	<!--
		<fo:block>
			<xsl:value-of select="$pageclass"/>
			<xsl:text>, </xsl:text>
			<xsl:value-of select="$sequence"/>
			<xsl:text>, </xsl:text>
			<xsl:value-of select="$position"/>
			<xsl:text>, </xsl:text>
			<xsl:value-of select="$gentext-key"/>
		</fo:block>
	-->
	
		<fo:block>
			<!-- pageclass can be front, body, back -->
			<!-- sequence can be odd, even, first, blank -->
			<!-- position can be left, center, right -->
			
			<xsl:choose>
				<xsl:when test="$pageclass = 'titlepage'">
					<!-- nop; no footer on title pages -->
				</xsl:when>


				<xsl:when test="$double.sided != 0 and $sequence = 'even'
												and $position='left'">
					<fo:page-number/>
				</xsl:when>

				<xsl:when test="$double.sided != 0 and ($sequence = 'odd' or $sequence = 'first')
												and $position='right'">
					<fo:page-number/>
				</xsl:when>

        
				<xsl:when test="$double.sided != 0 and $sequence = 'even'
												and $position='right'">
          <xsl:if test="$confidential.mode = 'yes'">
            <xsl:text>Confidential</xsl:text>
          </xsl:if>
				</xsl:when>

				<xsl:when test="$double.sided != 0 and ($sequence = 'odd' or $sequence = 'first')
												and $position='left'">
          <xsl:if test="$confidential.mode = 'yes'">
            <xsl:text>Confidential</xsl:text>
          </xsl:if>
				</xsl:when>

				
				<xsl:when test="$double.sided != 0 and $sequence = 'even'
												and $position='center'">
						<xsl:apply-templates select="//book" mode="titleabbrev.markup"/>
				</xsl:when>
				
				
				<xsl:when test="$double.sided != 0 and ($sequence = 'odd' or $sequence = 'first')
												and $position='center'">
                    <!-- todo: How to select for chapter, preface, index, appendix, toc??? -->
                    <xsl:choose>
                        <xsl:when test="ancestor-or-self::chapter">
                            <xsl:apply-templates select="ancestor-or-self::chapter/title" mode="title"/>
                        </xsl:when>
                        <xsl:when test="ancestor-or-self::preface">
                            <xsl:apply-templates select="ancestor-or-self::preface/title" mode="title"/>
                        </xsl:when>
                        <xsl:when test="ancestor-or-self::index">
                            <xsl:apply-templates select="ancestor-or-self::index/title" mode="title"/>
                        </xsl:when>
                        <xsl:when test="ancestor-or-self::appendix">
                            <xsl:apply-templates select="ancestor-or-self::appendix/title" mode="title"/>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:text>Table of Contents</xsl:text>
                        </xsl:otherwise>
                    </xsl:choose>
				</xsl:when>
					
				<xsl:when test="$double.sided = 0 and $position='center'">
					<fo:page-number/>
				</xsl:when>
	
				<xsl:when test="$sequence='blank'">
					<xsl:choose>
						<xsl:when test="$double.sided != 0 and $position = 'left'">
							<fo:page-number/>
						</xsl:when>
						<xsl:when test="$double.sided = 0 and $position = 'center'">
							<fo:page-number/>
						</xsl:when>
						<xsl:otherwise>
							<!-- nop -->
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
	
	
				<xsl:otherwise>
					<!-- nop -->
				</xsl:otherwise>
			</xsl:choose>
		</fo:block>
	</xsl:template>	

	<!-- remove the header rule -->
	<xsl:template name="head.sep.rule">
		<xsl:param name="pageclass"/>
		<xsl:param name="sequence"/>
		<xsl:param name="gentext-key"/>
		<!-- noop -->
	</xsl:template>
	
	<!-- from inline.xsl, break page on begin page, internal usage -->
	<xsl:template match="beginpage">
  	<fo:block break-after="page" keep-with-previous="auto" />
	</xsl:template>
	
	<!-- close up spacing between admonition paragraphs -->
	<xsl:attribute-set name="admonition.para.spacing">
		<xsl:attribute name="space-before.optimum">4pt</xsl:attribute>
		<xsl:attribute name="space-before.minimum">4pt</xsl:attribute>
		<xsl:attribute name="space-before.maximum">4pt</xsl:attribute>
	</xsl:attribute-set>

	
	<!-- include: corpname and address" -->
	<xsl:template match="note/para|important/para|tip/para|warning/para|caution/para">
		<fo:block xsl:use-attribute-sets="admonition.para.spacing">
			<xsl:call-template name="anchor"/>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="bookinfo/corpname" mode="book.titlepage.verso.auto.mode" priority="2">
		<fo:block>
			<xsl:apply-templates />
		</fo:block>
	</xsl:template>
	
	<xsl:template match="bookinfo/address" mode="book.titlepage.verso.auto.mode" priority="2">
		<fo:block>
			<xsl:apply-templates />
		</fo:block>
	</xsl:template>

	<xsl:template name="book.titlepage.verso">
		<xsl:choose>
			<xsl:when test="bookinfo/title">
				<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/title"/>
			</xsl:when>
			<xsl:when test="title">
				<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="title"/>
			</xsl:when>
		</xsl:choose>
	
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/corpauthor"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/authorgroup"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/author"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/othercredit"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/pubdate"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/copyright"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/abstract"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/legalnotice"/>
		
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/corpname"/>
		<xsl:apply-templates mode="book.titlepage.verso.auto.mode" select="bookinfo/address"/>
	</xsl:template>
	
	<!-- bullet closer to text -->
	<xsl:template match="itemizedlist/listitem">
		<xsl:variable name="id"><xsl:call-template name="object.id"/></xsl:variable>
	
		<xsl:variable name="itemsymbol">
			<xsl:call-template name="list.itemsymbol">
				<xsl:with-param name="node" select="parent::itemizedlist"/>
			</xsl:call-template>
		</xsl:variable>
	
		<xsl:variable name="item.contents">
			<fo:list-item-label start-indent="body-start() - 10pt" end-indent="label-end()">
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
						<xsl:otherwise>&#x2022;</xsl:otherwise>
					</xsl:choose>
				</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<xsl:apply-templates/>
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
	
	<!-- font type sans, numbers indented (closer to body) -->
	<xsl:template match="step">
		<xsl:variable name="id">
			<xsl:call-template name="object.id"/>
		</xsl:variable>
	
		<fo:list-item xsl:use-attribute-sets="list.item.spacing">
			<fo:list-item-label 
						font-family="sans-serif" 
						font-weight="bold"
						end-indent="label-end()">
				<fo:block id="{$id}">
					<!-- dwc: fix for one step procedures. Use a bullet if there's no step 2 -->
					<xsl:choose>
						<xsl:when test="position() > 9">
						<xsl:attribute name="start-indent">body-start() - 20pt</xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="start-indent">body-start() - 14pt</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<xsl:when test="count(../step) = 1">
							<xsl:text>&#x2022;</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:apply-templates select="." mode="number">
								<xsl:with-param name="recursive" select="0"/>
							</xsl:apply-templates>.
						</xsl:otherwise>
					</xsl:choose>
				</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<xsl:apply-templates/>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="orderedlist/listitem">
		<xsl:variable name="id"><xsl:call-template name="object.id"/></xsl:variable>
	
		<xsl:variable name="item.contents">
			<fo:list-item-label end-indent="label-end()">
				<xsl:choose>
					<xsl:when test="position() > 9">
					<xsl:attribute name="start-indent">body-start() - 19pt</xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="start-indent">body-start() - 14pt</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
				<fo:block>
					<xsl:apply-templates select="." mode="item-number"/>
				</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<xsl:apply-templates/>
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
	
	<xsl:template name="toc.line">
		<xsl:variable name="id">
			<xsl:call-template name="object.id"/>
		</xsl:variable>
	
		<xsl:variable name="label">
			<xsl:apply-templates select="." mode="label.markup"/>
		</xsl:variable>
	
		<fo:block text-align-last="justify"
							end-indent="{$toc.indent.width}pt"
							last-line-end-indent="-{$toc.indent.width}pt">
			<xsl:choose>
				<xsl:when test="name()='chapter'">
					<xsl:attribute name="font-size">12pt</xsl:attribute>
					<xsl:attribute name="font-weight">bold</xsl:attribute>
					<xsl:attribute name="space-before.optimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.minimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.maximum">1.2em</xsl:attribute>
				</xsl:when>
				<xsl:when test="name()='preface'">
					<xsl:attribute name="font-size">12pt</xsl:attribute>
					<xsl:attribute name="font-weight">bold</xsl:attribute>
					<xsl:attribute name="space-before.optimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.minimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.maximum">1.2em</xsl:attribute>
				</xsl:when>
				<xsl:when test="name()='appendix'">
					<xsl:attribute name="font-size">12pt</xsl:attribute>
					<xsl:attribute name="font-weight">bold</xsl:attribute>
					<xsl:attribute name="space-before.optimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.minimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.maximum">1.2em</xsl:attribute>
				</xsl:when>
				<xsl:when test="name()='index'">
					<xsl:attribute name="font-size">12pt</xsl:attribute>
					<xsl:attribute name="font-weight">bold</xsl:attribute>
					<xsl:attribute name="space-before.optimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.minimum">0.8em</xsl:attribute>
					<xsl:attribute name="space-before.maximum">1.2em</xsl:attribute>
				</xsl:when>
			</xsl:choose>
			<fo:inline keep-with-next.within-line="always">
				<fo:basic-link internal-destination="{$id}">
					<xsl:if test="$label != ''">
						<xsl:copy-of select="$label"/>
						<xsl:value-of select="$autotoc.label.separator"/>
					</xsl:if>
					<xsl:apply-templates select="." mode="title.markup"/>
				</fo:basic-link>
			</fo:inline>
			<fo:inline keep-together.within-line="always">
				<xsl:text> </xsl:text>
				<xsl:choose>
					<xsl:when test="name()='chapter'">
						<fo:leader leader-pattern="space"
									 leader-pattern-width="3pt"
									 leader-alignment="reference-area"
									 keep-with-next.within-line="always"/>
					</xsl:when>
					<xsl:when test="name()='preface'">
						<fo:leader leader-pattern="space"
									 leader-pattern-width="3pt"
									 leader-alignment="reference-area"
									 keep-with-next.within-line="always"/>
					</xsl:when>
					<xsl:when test="name()='appendix'">
						<fo:leader leader-pattern="space"
									 leader-pattern-width="3pt"
									 leader-alignment="reference-area"
									 keep-with-next.within-line="always"/>
					</xsl:when>
					<xsl:when test="name()='index'">
						<fo:leader leader-pattern="space"
									 leader-pattern-width="3pt"
									 leader-alignment="reference-area"
									 keep-with-next.within-line="always"/>
					</xsl:when>
					<xsl:otherwise>
						<fo:leader leader-pattern="dots"
									 leader-pattern-width="3pt"
									 leader-alignment="reference-area"
									 keep-with-next.within-line="always"/>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:text> </xsl:text> 
				<fo:basic-link internal-destination="{$id}">
					<fo:page-number-citation ref-id="{$id}"/>
				</fo:basic-link>
			</fo:inline>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="screenshot">
		<fo:block>
			<xsl:if test="@role='wide'">
				<xsl:attribute name="margin-left">
					<xsl:value-of select="$title.margin.left"></xsl:value-of>
				</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>

    <!-- from xref.xsl -->
	<xsl:template match="step" mode="xref-to">
      <xsl:param name="referrer"/>
      <xsl:param name="xrefstyle"/>
    
      <xsl:call-template name="gentext">
        <xsl:with-param name="key" select="'step'"/>
      </xsl:call-template>
      <xsl:text> </xsl:text>
      <xsl:apply-templates select="." mode="number"/>
    </xsl:template>
    
    <!-- from xref.xsl -->
    <xsl:template match="chapter|appendix" mode="insert.title.markup">
      <xsl:param name="purpose"/>
      <xsl:param name="xrefstyle"/>
      <xsl:param name="title"/>
    
      <xsl:choose>
        <xsl:when test="$purpose = 'xref'">
            <xsl:text>&#8220;</xsl:text>
            <xsl:copy-of select="$title"/>
            <xsl:text>,&#8221;</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:copy-of select="$title"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:template>
        
        
  <!-- from Component.xsl -->        
  <xsl:template name="component.title">
    <xsl:param name="node" select="."/>
    <xsl:param name="pagewide" select="0"/>
    <xsl:variable name="id">
      <xsl:call-template name="object.id">
        <xsl:with-param name="object" select="$node"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="title">
      <xsl:apply-templates select="$node" mode="object.title.markup">
        <xsl:with-param name="allow-anchors" select="1"/>
      </xsl:apply-templates>
    </xsl:variable>
    <xsl:variable name="titleabbrev">
      <xsl:apply-templates select="$node" mode="titleabbrev.markup"/>
    </xsl:variable>
  
    <xsl:if test="$passivetex.extensions != 0">
      <fotex:bookmark xmlns:fotex="http://www.tug.org/fotex"
                      fotex-bookmark-level="2"
                      fotex-bookmark-label="{$id}">
        <xsl:value-of select="$titleabbrev"/>
      </fotex:bookmark>
    </xsl:if>
  
    <fo:block keep-with-next.within-column="always"
              space-before.optimum="72pt"
              space-before.minimum="80pt"
              space-before.maximum="100pt"
              space-after.optimum="18pt"
              hyphenate="false">
       <!-- the old way
       <fo:block keep-with-next.within-column="always"
              space-before.optimum="{$body.font.master}pt"
              space-before.minimum="{$body.font.master * 0.8}pt"
              space-before.maximum="{$body.font.master * 1.2}pt"
              hyphenate="false">
      -->  
      <xsl:if test="$pagewide != 0">
        <!-- Doesn't work to use 'all' here since not a child of fo:flow -->
        <xsl:attribute name="span">inherit</xsl:attribute>
      </xsl:if>
      <xsl:attribute name="hyphenation-character">
        <xsl:call-template name="gentext">
          <xsl:with-param name="key" select="'hyphenation-character'"/>
        </xsl:call-template>
      </xsl:attribute>
      <xsl:attribute name="hyphenation-push-character-count">
        <xsl:call-template name="gentext">
          <xsl:with-param name="key" select="'hyphenation-push-character-count'"/>
        </xsl:call-template>
      </xsl:attribute>
      <xsl:attribute name="hyphenation-remain-character-count">
        <xsl:call-template name="gentext">
          <xsl:with-param name="key" select="'hyphenation-remain-character-count'"/>
        </xsl:call-template>
      </xsl:attribute>
      <xsl:if test="$axf.extensions != 0">
        <xsl:attribute name="axf:outline-level">
          <xsl:value-of select="count($node/ancestor::*)"/>
        </xsl:attribute>
        <xsl:attribute name="axf:outline-expand">false</xsl:attribute>
        <xsl:attribute name="axf:outline-title">
          <xsl:value-of select="$title"/>
        </xsl:attribute>
      </xsl:if>
      <xsl:copy-of select="$title"/>
    </fo:block>
  </xsl:template>
       
</xsl:stylesheet>