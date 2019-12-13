<?php
namespace xml\bug_33853;

function my_autoloader($className) {
  echo $className;
  exit();
}

spl_autoload_register(__NAMESPACE__ .'\my_autoloader');

function test() {

  $xsl = new \DOMDocument();
  $xsl->loadXML('<?xml version="1.0" encoding="iso-8859-1" ?>
  <xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:php="http://php.net/xsl">
  <xsl:template match="/">
  <xsl:value-of select="php:function(\'TeSt::dateLang\')" />
  </xsl:template>
  </xsl:stylesheet>');
  $inputdom = new \DOMDocument();
  $inputdom->loadXML('<?xml version="1.0" encoding="iso-8859-1" ?>
  <today></today>');

  $proc = new \XSLTProcessor();
  $proc->registerPhpFunctions();
  $xsl = $proc->importStylesheet($xsl);
  $newdom = $proc->transformToDoc($inputdom);
}

test();
