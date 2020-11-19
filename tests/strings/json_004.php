<?php
namespace variables\json_004;

function testSimpleXmlElement()
{
    // https://github.com/peachpiecompiler/peachpie/issues/863
    $str = '<?xml version="1.0" encoding="UTF-8"?>
<configuration>
	<databaseConfig>
		<database dsn="mysql:host=127.0.0.1;port=3306;charset=utf8">
			<name>peachpie</name>
			<username>foo</username>
			<password>bar</password>
		</database>
	</databaseConfig>
</configuration>';
	$xml = simplexml_load_string($str);
	$json = json_encode($xml);
	print_r($json);
}

testSimpleXmlElement();

echo "Done";
