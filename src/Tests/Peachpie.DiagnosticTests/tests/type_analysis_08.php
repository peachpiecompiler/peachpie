<?php

function test_plus(int $length) {
  for (/*|integer|*/$i = 0; /*|integer|*/$i < /*|integer|*/$length; /*|integer|*/$i++)
  {
  	echo /*|integer|*/$i;
    /*|integer|*/$i = $i + 1;
  }
}

function test_minus(int $length) {
  for (/*|integer|*/$i = $length; /*|integer|*/$i >= 0; /*|integer|*/$i--)
  {
  	echo /*|integer|*/$i;
    /*|integer|*/$i = $i - 1;
  }
}
