<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet [
	<!ENTITY br  "<xsl:text>
</xsl:text>" >
	<!ENTITY tab  "<xsl:text>&#32;&#32;</xsl:text>">
]>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
	<xsl:output method="text" indent="no"/>

	<xsl:template match="Costume">
		&tab;<xsl:text>sprites:</xsl:text>&br;
		<xsl:variable name="costume" select="."/>
		<xsl:for-each select="AnimationList/Animation">
			<xsl:variable name="animationName" select="Name"/>
			<xsl:for-each select="AnimationFrameList/AnimationFrame[SpriteGroupIdentifier != -1]">
				<xsl:variable name="spriteGroupIdentifier" select="SpriteGroupIdentifier"/>
				<xsl:variable name="spriteGroup" select="$costume/SpriteGroupList/SpriteGroup[Identifier = $spriteGroupIdentifier]"/>
				<xsl:variable name="index" select="Index"/>
				<xsl:if test="count($spriteGroup) != 0">
					<xsl:for-each select="FrameList/Frame">
						<xsl:variable name="spriteIdentifier" select="SpriteIdentifier"/>
						<xsl:variable name="sprite" select="$spriteGroup/SpriteList/Sprite[position() - 1 = $spriteIdentifier]"/>
						<xsl:variable name="frameIndex" select="position() - 1"/>
						<xsl:variable name="flip">
							<xsl:choose>
								<xsl:when test="substring($animationName, (string-length($animationName) - 5) + 1) = 'Right'">0</xsl:when>
								<xsl:otherwise>1</xsl:otherwise>
							</xsl:choose>
						</xsl:variable>
						<xsl:if test="$sprite">
							&tab;<xsl:text>- name: </xsl:text><xsl:value-of select="$animationName"/>_<xsl:value-of select="$index"/>-<xsl:value-of select="$frameIndex"/>&br;
							&tab;&tab;<xsl:text>textureIndex: </xsl:text><xsl:value-of select="$sprite/TextureNumber"/>&br;
							&tab;&tab;<xsl:text>bounds:</xsl:text>&br;
							&tab;&tab;&tab;<xsl:text>serializedVersion: 2</xsl:text>&br;
							&tab;&tab;&tab;<xsl:text>x: </xsl:text><xsl:value-of select="$sprite/TextureX"/>&br;
							&tab;&tab;&tab;<xsl:text>y: </xsl:text><xsl:value-of select="$sprite/TextureY"/>&br;
							&tab;&tab;&tab;<xsl:text>width: </xsl:text><xsl:value-of select="$sprite/TextureWidth"/>&br;
							&tab;&tab;&tab;<xsl:text>height: </xsl:text><xsl:value-of select="$sprite/TextureHeight"/>&br;
							&tab;&tab;<xsl:text>flip: </xsl:text><xsl:value-of select="$flip"/>&br;
							&tab;&tab;<xsl:text>offset: {x: </xsl:text><xsl:value-of select="format-number( $sprite/ScreenX * 0.001, '0.0000' )"/><xsl:text>, y: </xsl:text><xsl:value-of select="format-number( $sprite/ScreenY * 0.001, '0.0000' )"/><xsl:text>}</xsl:text>&br;
						</xsl:if>
					</xsl:for-each>
				</xsl:if>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>

</xsl:stylesheet>
