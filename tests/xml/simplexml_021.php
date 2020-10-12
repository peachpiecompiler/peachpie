<?php
// This test was copied from PHP Manual. See https://www.php.net/manual/en/simplexmlelement.construct.php
$xml = '<ws:example xmlns:ws="http://example.com"><child>Not in namespace</child><ws:child>In example namespace</ws:child></ws:example>';

$sx0 = new SimpleXMLElement($xml, 0, false);
$sx1 = new SimpleXMLElement($xml, 0, false, 'http://example.com');
$sx2 = new SimpleXMLElement($xml, 0, false, 'ws', true);

echo "
    Without: {$sx0->child}
    By namespace: {$sx1->child}
    By prefix: {$sx2->child}
";