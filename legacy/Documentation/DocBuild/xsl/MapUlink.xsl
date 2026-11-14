<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <!-- Create keys for quickly looking up olink targets -->
    <xsl:key name="targetdoc-key" match="document" use="@targetdoc" />
    <xsl:key name="targetptr-key"  match="div|obj"
             use="concat(ancestor::document/@targetdoc, '/', @targetptr)" />


    <!-- useful for mapping links during the extract document to print step -->
    <!-- -->
    <xsl:template name="ExtractID">
        <!-- formats expected: Xxx.html Xxx.html#Yyy -->
        <xsl:param name="url"/>
        <xsl:choose>
            <xsl:when test="contains($url,'#')">
                <xsl:value-of select="substring-after($url,'#')"/>
            </xsl:when>
            <xsl:when test="substring($url, string-length($url) - 4) = '.html'">
                <xsl:value-of select="substring-before($url,'.html')"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$url"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <!-- -->
    <xsl:template name="ulink-link">
        <xsl:param name="LinkURL"/>
        <xsl:param name="LinkText"/>
        
        <xsl:variable name="LinkID">
            <xsl:call-template name="ExtractID">
                <xsl:with-param name="url" select="$LinkURL"/>
            </xsl:call-template>
        </xsl:variable>

        <xsl:choose>
            <xsl:when test="$LinkID">
                <link>
                    <xsl:attribute name="linkend">
                        <xsl:value-of select="$LinkID"/>
                    </xsl:attribute>
                    <xsl:value-of select="$LinkText"/>
                </link>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="ulink-ulink">
                    <xsl:with-param name="LinkURL" select="$LinkURL"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                    <xsl:with-param name="LinkType" >link-PassThrough</xsl:with-param>
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <!-- -->
    <xsl:template name="ulink-olinka">
        <xsl:param name="target.database"
            select="document($target.database.document, /)"/>
            
        <xsl:param name="LinkURL"/>
        <xsl:param name="LinkText"/>

        <xsl:variable name="LinkID">
            <xsl:call-template name="ExtractID">
                <xsl:with-param name="url" select="$LinkURL"/>
            </xsl:call-template>
        </xsl:variable>
        
        <xsl:variable name="TargetDoc">
            <xsl:value-of select="$target.database/descendant::div[@targetptr = $LinkID]/ancestor::document/@targetdoc" />
        </xsl:variable>
        
        <xsl:choose>
            <xsl:when test="$LinkID and $TargetDoc">
                <olink>
                    <xsl:attribute name="targetptr">
                        <xsl:value-of select="$LinkID"/>
                    </xsl:attribute>
                    <xsl:attribute name="targetdoc">
                        <xsl:value-of select="$TargetDoc"/>
                    </xsl:attribute>
                    <xsl:value-of select="$LinkText"/>
                </olink>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="ulink-ulink">
                    <xsl:with-param name="LinkURL" select="$LinkURL"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                    <xsl:with-param name="LinkType" >olinka-PassThrough</xsl:with-param>
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <!-- -->
    <xsl:template name="ulink-olinkb">
        <xsl:param name="target.database"
            select="document($target.database.document, /)"/>
        <xsl:param name="LinkURL"/>
        <xsl:param name="LinkText"/>

        <xsl:variable name="LinkID">
            <xsl:call-template name="ExtractID">
                <xsl:with-param name="url" select="$LinkURL"/>
            </xsl:call-template>
        </xsl:variable>
        
        <xsl:variable name="TargetDoc">
            <xsl:value-of select="$target.database/descendant::div[@targetptr = $LinkID]/ancestor::document/@targetdoc" />
        </xsl:variable>
        
        <xsl:choose>
            <xsl:when test="$LinkID and $TargetDoc">
                <olink>
                    <xsl:attribute name="targetptr">
                        <xsl:value-of select="$LinkID"/>
                    </xsl:attribute>
                    <xsl:attribute name="targetdoc">
                        <xsl:value-of select="$TargetDoc"/>
                    </xsl:attribute>
                </olink>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="ulink-ulink">
                    <xsl:with-param name="LinkURL" select="$LinkURL"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                    <xsl:with-param name="LinkType" >olinkb-PassThrough</xsl:with-param>
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <!-- -->
    <xsl:template name="ulink-xref">
        <xsl:param name="LinkURL"/>
        <xsl:param name="LinkText"/>

        <xsl:variable name="LinkID">
            <xsl:call-template name="ExtractID">
                <xsl:with-param name="url" select="$LinkURL"/>
            </xsl:call-template>
        </xsl:variable>
        
        <xsl:choose>
            <xsl:when test="$LinkID">
                <xref>
                    <xsl:attribute name="linkend">
                        <xsl:value-of select="$LinkID"/>
                    </xsl:attribute>
                </xref>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="ulink-ulink">
                    <xsl:with-param name="LinkURL" select="$LinkURL"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                    <xsl:with-param name="LinkType" >xref-PassThrough</xsl:with-param>
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <!-- -->
    <xsl:template name="ulink-ulink">
        <xsl:param name="LinkURL"/>
        <xsl:param name="LinkText"/>
        <xsl:param name="LinkType"/>
        <ulink>
            <xsl:attribute name="url"><xsl:value-of select="$LinkURL"/></xsl:attribute>
            <xsl:attribute name="type"><xsl:value-of select="$LinkType"/></xsl:attribute>
            <xsl:value-of select="$LinkText"/>
        </ulink>
    </xsl:template>
    <!-- -->
    <xsl:template match="ulink">
        <!-- link types: link, olinka(like link), olinkb(like xref), xref, url, mshelp -->
        <!-- url types: <id>.html, <id>.html#<id>, url -->
        <xsl:variable name="LinkUrl" select="@url"/>
        <xsl:variable name="LinkType" select="@type"/>
        <xsl:variable name="LinkText" select="text()"/>
        <xsl:choose>
            <xsl:when test="$LinkType = 'link'">
                <xsl:call-template name="ulink-link">
                    <xsl:with-param name="LinkURL" select="$LinkUrl"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="$LinkType = 'olinka'">
                <xsl:call-template name="ulink-olinka">
                    <xsl:with-param name="LinkURL" select="$LinkUrl"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="$LinkType = 'olinkb'">
                <xsl:call-template name="ulink-olinkb">
                    <xsl:with-param name="LinkURL" select="$LinkUrl"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="$LinkType = 'xref'">
                <xsl:call-template name="ulink-xref">
                    <xsl:with-param name="LinkURL" select="$LinkUrl"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="ulink-ulink">
                    <xsl:with-param name="LinkURL" select="$LinkUrl"/>
                    <xsl:with-param name="LinkText" select="$LinkText"/>
                    <xsl:with-param name="LinkType" >PassThrough</xsl:with-param>
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>