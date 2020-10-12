<?php
namespace functions\is_file_001;

// Regression test
function testDir($dir) {
  echo (int)is_file($dir);
  echo (int)is_file($dir ."\\");
}

testDir(__DIR__);
