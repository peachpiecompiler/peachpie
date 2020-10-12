<?php
// This test was copied from PHP tests. See https://github.com/php/php-src/search?q=SimpleXMLIterator&unscoped_q=SimpleXMLIterator
$xml =<<<EOF
<?xml version='1.0'?>
<!DOCTYPE sxe SYSTEM "notfound.dtd">
<sxe id="elem1">
 <elem1 attr1='first'>
  <!-- comment -->
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


function sxiToArray($sxi){
    $a = array();
    for( $sxi->rewind(); $sxi->valid(); $sxi->next() ) {
      if(!array_key_exists($sxi->key(), $a)){
        $a[$sxi->key()] = array();
      }
      if($sxi->hasChildren()){
        $a[$sxi->key()][] = sxiToArray($sxi->current());
      }
      else{
        $a[$sxi->key()][] = strval($sxi->current());
      }
    }
    return $a;
  }


function normalize($text){
  return trim(str_replace("\r\n", "\n", $text));
}

;
normalize(var_export(sxiToArray(simplexml_load_string($xml, 'SimpleXMLIterator')),true));
?>