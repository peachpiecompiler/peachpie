<?php

namespace gd\gd_imagettftext_001;

function test() {
  $img = imagecreate(100, 100);
  echo imagettftext($img, 12, 0, 10, 10, 0, "empty.txt", "lorem");   // Invalid font file
}

test();
