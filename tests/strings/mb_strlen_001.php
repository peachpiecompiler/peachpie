<?php
namespace strings\mb_strlen_001;

function test() {
  for ($i = 0; $i < 10; $i++) {
    if (\mb_strlen(\random_bytes(16), "8bit") != 16) {
      echo "KO";
      return;
    }
  }

  echo "OK";
}

test();
