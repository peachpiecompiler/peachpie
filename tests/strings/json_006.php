<?php
namespace variables\json_006;

function test_json($val, $flags = 0, $depth = 512) {
  try {
    $json = \json_encode($val, $flags, $depth);
    if ($json === false) {
      echo "FALSE\n";
    } else {
      echo "OK\n";
    }
  }
  catch (\JsonException $e) {
    echo \get_class($e) .": ". $e->getCode() ."\n";
  }
}

test_json(NAN);
test_json(NAN, JSON_PARTIAL_OUTPUT_ON_ERROR);
test_json(NAN, JSON_THROW_ON_ERROR);

test_json([[]], 0, 1);
test_json([[]], JSON_PARTIAL_OUTPUT_ON_ERROR, 1);
test_json([[]], JSON_THROW_ON_ERROR, 1);

$ra = [42];
$ra[0] =& $ra;
test_json($ra);
test_json($ra, JSON_PARTIAL_OUTPUT_ON_ERROR);
test_json($ra, JSON_THROW_ON_ERROR);

$obj = (object)['foo' => new \stdClass];
test_json($obj, 0, 1);
test_json($obj, JSON_PARTIAL_OUTPUT_ON_ERROR, 1);
test_json($obj, JSON_THROW_ON_ERROR, 1);

$ro = new \stdClass;
$ro->foo =& $ro;
test_json($ro);
test_json($ro, JSON_PARTIAL_OUTPUT_ON_ERROR);
test_json($ro, JSON_THROW_ON_ERROR);

$res = \imagecreate(1, 1);
test_json($res);
test_json($res, JSON_PARTIAL_OUTPUT_ON_ERROR);
test_json($res, JSON_THROW_ON_ERROR);

// Incorrect binary UTF-8 string (inspired by https://github.com/cargomedia/cm/issues/2482#issuecomment-270626888)
$txt = hex2bin("B131");
test_json($txt);
test_json($txt, JSON_PARTIAL_OUTPUT_ON_ERROR);
test_json($txt, JSON_THROW_ON_ERROR);
