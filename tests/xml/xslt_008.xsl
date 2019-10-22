<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:php="http://php.net/xsl"
  version='1.0'>
<xsl:output  method="text"/>
<xsl:template match="/">
<xsl:value-of select="php:functionString('xml\xslt_008\foobar', /doc/@id, 'secondArg')"/>
<xsl:text>
</xsl:text>
<xsl:value-of select="php:function('xml\xslt_008\foobar', /doc/@id)"/>
<xsl:text>
</xsl:text>
<xsl:value-of select="php:function('xml\xslt_008\nodeSet')"/>
<xsl:text>
</xsl:text>
<xsl:value-of select="php:function('xml\xslt_008\nodeSet',/doc)/i"/>
<xsl:text>
</xsl:text>
<xsl:value-of select="php:function('xml\xslt_008\aClass::aStaticFunction','static')"/>
<xsl:text>
</xsl:text>
<!-- TODO: Find out the exact logic of PHP objects conversion -->
<!--<xsl:value-of select="php:function('xml\xslt_008\nonDomNode')"/>--> 
</xsl:template>
</xsl:stylesheet>
