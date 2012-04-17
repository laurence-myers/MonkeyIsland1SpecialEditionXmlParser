<?xml version="1.0" encoding="us-ascii"?>
<xsl:stylesheet
	version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	exclude-result-prefixes="msxsl"
	>

	<xsl:output method="xml" indent="yes"/>

	<!-- template for stuff we simply don't want -->
	<xsl:template match="AnimationHeaderList | SpriteGroupHeaderList | NameAddress | AfterNameAddress | AnimationHeaderAddress | SpriteGroupHeaderAddress | FrameAddress | TextureHeader | PathPointAddress">
		<!-- do nothing -->
	</xsl:template>
	
	<!-- template for all the other stuff -->
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>
