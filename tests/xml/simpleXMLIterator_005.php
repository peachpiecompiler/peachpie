<?php
// This test was copied from PHP tests. See https://github.com/php/php-src/search?q=SimpleXMLIterator&unscoped_q=SimpleXMLIterator
$xml =<<<EOF
<?xml version='1.0'?>
<sxe>
 <elem1/>
 <elem2/>
 <elem2/>
</sxe>
EOF;

class SXETest extends SimpleXMLIterator
{
    function count()
    {
        echo __METHOD__ . "\n";
        return parent::count();
    }
}

$sxe = new SXETest($xml);

print_r(count($sxe));
print_r(count($sxe->elem1));
print_r(count($sxe->elem2));
