<?php
namespace xml\dom_008;

function test()
{
  $html = <<<END
<!doctype html>
<html lang="en">
	<head>
		<script>
		  var test = "something";
		</script>
	</head>
</html>	
END;

//

$dom = new \DOMDocument;
  $dom->loadHTML($html);

  // https://github.com/peachpiecompiler/peachpie/issues/622
  echo "getElementsByTagName:";
  $els = $dom->getElementsByTagName('script');
  foreach ($els as $node) {
    $text = trim($node->nodeValue);
    print_r($text);
  }
}
test();

echo "Done.";
