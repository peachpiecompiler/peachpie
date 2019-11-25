<?php

function test(int $length) {
  for (/*|integer|*/$i = 0; /*|integer|*/$i < /*|integer|*/$length; /*|integer|*/$i++)
  {
  	echo /*|integer|*/$i;
    /*|integer|*/$i = $i + 1;
  }
}
