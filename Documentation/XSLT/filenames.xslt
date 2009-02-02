<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="http://alphora.com/docs"
  >
<msxsl:script language="C#" implements-prefix="user">
<![CDATA[
    public string DeclaringClassName(String name)
    {
        return name.Substring(0,name.LastIndexOf("."));
    }
    public string shortName(String name)
    {
        return name.Substring(name.LastIndexOf(".") + 1);
    }
    public string getDate()
    {
        DateTime dt = DateTime.Now;
        return dt.ToString("MM/dd/yyyy hh:mm:ss");
    }
]]>
</msxsl:script>
    <!-- -->
    <xsl:template name="getTime">
        <xsl:value-of select="user:getDate()"/>
    </xsl:template>
    <!-- -->
	<xsl:template name="get-filename-for-current-namespace-hierarchy">
		<xsl:value-of select="concat(translate($namespace, '.', ''), 'Hierarchy.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-namespace">
		<xsl:param name="name" />
		<xsl:value-of select="concat(translate($name, '.', ''), '.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-type">
		<xsl:param name="id" />
		<xsl:value-of select="concat(translate(substring-after($id, 'T:'), '.', ''), '.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-constructor-overloads">
		<xsl:variable name="type-part" select="translate(substring-after(../@id, 'T:'), '.', '')" />
		<xsl:value-of select="concat($type-part, 'ConstructorOverloads.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-constructor">
		<!-- .#ctor or .#cctor -->
		<xsl:value-of select="concat(translate(substring-after(substring-before(@id, '.#c'), 'M:'), '.', ''), 'Constructor', @overload, '.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-type-members">
		<xsl:param name="id" />
		<xsl:value-of select="concat(translate(substring-after($id, 'T:'), '.', ''), 'Members.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-field">
		<xsl:value-of select="concat(translate(substring-after(@id, 'F:'), '.', ''), 'Field.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-event">
		<xsl:value-of select="concat(translate(substring-after(@id, 'E:'), '.', ''), 'Event.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-property-overloads">
		<xsl:variable name="type-part" select="translate(substring-after(../@id, 'T:'), '.', '')" />
		<xsl:value-of select="concat($type-part, @name, 'Overloads.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-property">
		<xsl:choose>
			<xsl:when test="contains(@id, '(')">
				<xsl:value-of select="concat(translate(substring-after(substring-before(@id, '('), 'P:'), '.', ''), 'Property', @overload, '.html')" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat(translate(substring-after(@id, 'P:'), '.', ''), 'Property', @overload, '.html')" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-property">
		<xsl:param name="property" select="." />
		<xsl:choose>
			<xsl:when test="contains($property/@id, '(')">
				<xsl:value-of select="concat(translate(substring-after(substring-before($property/@id, '('), 'P:'), '.', ''), 'Property', $property/@overload, '.html')" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat(translate(substring-after($property/@id, 'P:'), '.', ''), 'Property', $property/@overload, '.html')" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-current-method-overloads">
		<xsl:variable name="type-part" select="translate(substring-after(../@id, 'T:'), '.', '')" />
		<xsl:value-of select="concat($type-part, @name, 'Overloads.html')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-method">
		<xsl:param name="method" select="." />
		<xsl:choose>
			<xsl:when test="contains($method/@id, '(')">
				<xsl:value-of select="concat(translate(substring-after(substring-before($method/@id, '('), 'M:'), '.', ''), 'Method', $method/@overload, '.html')" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat(translate(substring-after($method/@id, 'M:'), '.', ''), 'Method', $method/@overload, '.html')" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-system-field">
		<!-- TODO: verify this works -->
		<!-- Beta 2 Example:  ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrfSystemExceptionClassInnerExceptionTopic.htm -->
	    <!-- assumes fully qualified field name -->
		<xsl:param name='field-name'/>
		<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate($field-name, '.', ''), 'ClassTopic.htm')" />

		<!-- <xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(@declaringType, '.', ''), 'Class', @name, 'Topic.htm')" /> -->
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-system-property">
		<!-- Beta 2 Example:  ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrfSystemExceptionClassInnerExceptionTopic.htm -->
	    <!-- assumes fully qualified property name -->
		<xsl:param name='property-name' />
		<xsl:choose>
			<xsl:when test="$property-name=''">
				<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(@declaringType, '.', ''), 'Class', @name, 'Topic.htm')" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(user:DeclaringClassName($property-name), '.', ''), 'Class', user:shortName($property-name), 'Topic.htm')" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-system-method">
		<!-- EXAMPLE:  ms-help://MSDNVS/cpref/html_hh2/frlrfSystemObjectClassEqualsTopic.htm -->
		<!-- Beta 2 Example:  ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrfSystemObjectClassEqualsTopic.htm -->
		<xsl:param name='method-name' />
		<xsl:choose>
			<xsl:when test="$method-name=''">
				<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(@declaringType, '.', ''), 'Class', @name, 'Topic.htm')" />
			</xsl:when>
			<xsl:otherwise>
			    <xsl:choose>
					<xsl:when test="contains($method-name,'(')">
						<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(user:DeclaringClassName($method-name), '.', ''), 'Class', user:shortName(substring-before($method-name,'(')), 'Topic.htm')" />
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate(user:DeclaringClassName($method-name), '.', ''), 'Class', user:shortName($method-name), 'Topic.htm')" />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-system-class">
		<xsl:param name="class-name" />
		<!-- Beta 2 Example:  ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrfSystemObjectClassTopic.htm -->
		<xsl:value-of select="concat('ms-help://MS.VSCC/MS.MSDNVS/cpref/html/frlrf', translate($class-name, '.', ''), 'ClassTopic.htm')" />
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-individual-member">
		<xsl:param name="member" />
		<xsl:choose>
			<xsl:when test="$member = 'field'">
				<xsl:call-template name="get-filename-for-current-field" />
			</xsl:when>
			<xsl:when test="$member = 'property'">
				<xsl:call-template name="get-filename-for-current-property" />
			</xsl:when>
			<xsl:when test="$member = 'event'">
				<xsl:call-template name="get-filename-for-current-event" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="get-filename-for-method" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-individual-member-overloads">
		<xsl:param name="member" />
		<xsl:choose>
			<xsl:when test="$member = 'property'">
				<xsl:call-template name="get-filename-for-current-property-overloads" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="get-filename-for-current-method-overloads" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- -->
	<xsl:template name="get-filename-for-cref">
		<xsl:param name="cref" />
		<xsl:choose>
			<xsl:when test="starts-with($cref, 'T:')">
				<xsl:value-of select="concat(translate(substring-after($cref, 'T:'), '.', ''), '.html')" />
			</xsl:when>
			<xsl:when test="starts-with($cref, 'M:')">
				<xsl:choose>
					<xsl:when test="contains($cref, '(')">
						<xsl:value-of select="concat(translate(substring-after(substring-before($cref, '('), 'M:'), '.', ''), 'Method.html')" />
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="concat(translate(substring-after($cref, 'M:'), '.', ''), 'Method.html')" />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="starts-with($cref, 'P:')">
				<xsl:value-of select="concat(translate(substring-after($cref, 'P:'), '.', ''), 'Property.html')" />
			</xsl:when>
			<xsl:when test="starts-with($cref, 'E:')">
				<xsl:choose>
					<xsl:when test="contains($cref, '(')">
						<xsl:value-of select="concat(translate(substring-after(substring-before($cref, '('), 'E:'), '.', ''), 'Event.html')" />
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="concat(translate(substring-after($cref, 'E:'), '.', ''), 'Event.html')" />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$cref" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
