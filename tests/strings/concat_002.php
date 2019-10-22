<?php
namespace strings\concat_002;

function test() {
  $arr = ['foo' => 'bar'];
  echo "{$arr['foo']}''";
}

test();
