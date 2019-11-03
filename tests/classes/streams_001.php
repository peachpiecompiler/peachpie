<?php
namespace classes\streams_001;

function test() {
  echo (int)stream_is_local("subdir/unknown_file.txt");
  echo (int)stream_is_local(__FILE__);
  echo (int)stream_is_local("file://". __DIR__ ."/somefile.txt");
  echo (int)stream_is_local("http://www.peachpie.io");
  echo (int)stream_is_local("https://www.peachpie.io");
  echo (int)stream_is_local(fopen(__FILE__, "r"));
}

test();
