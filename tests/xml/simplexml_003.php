<?php
namespace xml\simplexml_003;

function __var_dump($x)
{
  __var_dump_rec("", $x);
}

function __var_dump_rec($indent, &$x)
{
  if (is_array($x) || is_object($x))
  {
    echo $indent, is_array($x) ? "array" : "object", "\n";
    echo "$indent{\n";

    $a = array();
    foreach ($x as $k => $_)
      $a[] = $k;

    if (is_object($x))
      sort($a, SORT_STRING);

    foreach ($a as $k)
    {
      $v = @(is_array($x) ? $x[$k] : $x->$k);

      echo "$indent  ";
      if (is_string($k)) echo "'$k'"; else echo "$k";
      echo " => ";

      if (is_array($v) || is_object($v))
      {
        echo "\n";
        __var_dump_rec("$indent  ", $v);
      }
      else
      {
        __var_dump_rec("", $v);
      }
    }

    echo "$indent}\n";
  }
  else if (is_string($x))
    echo "{$indent}'$x'\n";
  else if (is_null($x))
    echo "{$indent}NULL\n";
  else if (is_bool($x))
    echo $indent, ($x ? "TRUE" : "FALSE"), "\n";
  else if (is_resource($x))
    echo $indent, "resource '".get_resource_type($x)."'\n";
  else if (is_double($x))
    printf("{$indent}double(%.10f)\n",$x);
  else if (gettype($x)=="object")
    echo $indent, "object [invalid]\n"; else
    echo $indent, gettype($x), "($x)\n";
}

function test() {
  $xml =<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 <elem1 attr1='first'>
  <elem2>
   <elem3>
    <elem4>
     <?test processing instruction ?>
    </elem4>
   </elem3>
  </elem2>
 </elem1>
</sxe>
EOF;

  $sxe = simplexml_load_string($xml);

  echo "===Property===\n";
  __var_dump($sxe->elem1);
  echo "===Array===\n";
  __var_dump($sxe['id']);
  __var_dump($sxe->elem1['attr1']);
  echo "===Set===\n";
  $sxe['id'] = "Changed1";
  __var_dump($sxe['id']);
  $sxe->elem1['attr1'] = 12;
  __var_dump($sxe->elem1['attr1']);
  echo "===Unset===\n";
  unset($sxe['id']);
  __var_dump($sxe['id']);
  unset($sxe->elem1['attr1']);
  __var_dump($sxe->elem1['attr1']);
  echo "===Misc.===\n";
  $a = 4;
  __var_dump($a);
  $dummy = $sxe->elem1[$a];
  __var_dump($a);
}

test();
